using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using STRATFY.Models;
using STRATFY.Interfaces;
using Microsoft.VisualBasic;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography.Xml;

namespace STRATFY.Controllers
{
    [Authorize]
    public class ExtratosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IRepositoryBase<Extrato> _extratoRepository;

        public ExtratosController(AppDbContext context, IRepositoryBase<Extrato> extratoRepository)
        {
            _context = context;
            _extratoRepository = extratoRepository;
        }


        // GET: Extratos
        public async Task<IActionResult> Index()
        {
            //var appDbContext = _context.Extratos.Include(e => e.Usuario);
            //return View(await appDbContext.ToListAsync());
            var extratos = await _extratoRepository.SelecionarTodosAsync();
            return View(extratos);
        }

        // GET: Extratos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var extrato = await _context.Extratos
                .Include(e => e.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (extrato == null)
            {
                return NotFound();
            }

            return View(extrato);
        }

        // GET: Extratos/Create
        public IActionResult Create()
        {
            ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "Id", "Id");
            return View();
        }

  
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("id,UsuarioId,Nome,DataCriacao")] Extrato extrato)
        {
            ModelState.Remove("Usuario");
            if (ModelState.IsValid)
            {
                extrato.DataCriacao = DateOnly.FromDateTime(DateTime.Now);
                await _extratoRepository.IncluirAsync(extrato);
                return RedirectToAction("Edit", "Extratos", new { id = extrato.Id });
            }
            ViewData["UsuarioId"] = _extratoRepository.SelecionarChaveAsync(extrato.UsuarioId);
            return View(extrato);
        }

        // GET: Extratos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var extrato = await _context.Extratos
                .Include(e => e.Movimentacaos)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (extrato == null)
                return NotFound();

            var viewModel = new ExtratoEdicaoViewModel
            {
                ExtratoId = extrato.Id,
                NomeExtrato = extrato.Nome,
                DataCriacao = extrato.DataCriacao,
                Movimentacoes = extrato.Movimentacaos.Select(m => new Movimentacao
                {
                    Id = m.Id,
                    Descricao = m.Descricao,
                    Valor = m.Valor,
                    Tipo = m.Tipo,
                    CategoriaId = m.CategoriaId,
                    ExtratoId = m.ExtratoId
                }).ToList()
            };

            ViewData["CategoriaId"] = new SelectList(await _context.Categoria.ToListAsync(), "Id", "Nome");

            return View(viewModel);
        }

        //if (id == null)
        //{
        //    return NotFound();
        //}

        //var extrato = await _context.Extratos.FindAsync(id);
        //if (extrato == null)
        //{
        //    return NotFound();
        //}
        //ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "Id", "Id", extrato.UsuarioId);
        //return View(extrato);


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ExtratoEdicaoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewData["CategoriaId"] = new SelectList(await _context.Categoria.ToListAsync(), "Id", "Nome");
                return View(model);
            }

            var extrato = await _context.Extratos
                .Include(e => e.Movimentacaos)
                .FirstOrDefaultAsync(e => e.Id == model.ExtratoId);

            if (extrato == null)
            {
                return NotFound();
            }

            // Atualiza os dados do extrato
            extrato.Nome = model.NomeExtrato;
            extrato.DataCriacao = model.DataCriacao;

            // Lista de IDs recebidos do form
            var idsRecebidos = model.Movimentacoes.Select(m => m.Id).ToList();

            // REMOVE movimentações que existiam no banco mas não vieram no form
            var movimentacoesRemovidas = extrato.Movimentacaos
                .Where(m => !idsRecebidos.Contains(m.Id))
                .ToList();

            _context.Movimentacaos.RemoveRange(movimentacoesRemovidas);

            // Itera sobre cada movimentação recebida no form
            foreach (var mov in model.Movimentacoes)
            {
                if (mov.Id == 0)
                {
                    // Nova movimentação
                    mov.ExtratoId = model.ExtratoId;
                    _context.Movimentacaos.Add(mov);
                }
                else
                {
                    // Atualização
                    var movBanco = extrato.Movimentacaos.FirstOrDefault(m => m.Id == mov.Id);
                    if (movBanco != null)
                    {
                        movBanco.Descricao = mov.Descricao;
                        movBanco.Valor = mov.Valor;
                        movBanco.Tipo = mov.Tipo;
                        movBanco.CategoriaId = mov.CategoriaId;
                    }
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }



        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit( Extrato extrato)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            extrato.DataCriacao = DateOnly.FromDateTime(DateTime.Now);
        //            _context.Update(extrato);
        //            await _context.SaveChangesAsync();
        //        }
        //        catch (DbUpdateConcurrencyException)
        //        {
        //            if (!ExtratoExists(extrato.Id))
        //            {
        //                return NotFound();
        //            }
        //            else
        //            {
        //                throw;
        //            }
        //        }
        //        return RedirectToAction(nameof(Index));
        //    }
        //    ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "Id", "Id", extrato.UsuarioId);
        //    return View(extrato);
        //}

        // GET: Extratos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var extrato = await _context.Extratos
                .Include(e => e.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (extrato == null)
            {
                return NotFound();
            }

            return View(extrato);
        }

        // POST: Extratos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var extrato = await _context.Extratos.FindAsync(id);
            try
            {
                if (extrato != null)
                {
                    _context.Extratos.Remove(extrato);
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                TempData["DeleteError"] = "Não foi possível excluir o extrato porque ele possui movimentações vinculadas.";
                return RedirectToAction(nameof(Delete), new { id }); // Redireciona de volta pra tela de confirmação de delete
            }
        }

        private bool ExtratoExists(int id)
        {
            return _context.Extratos.Any(e => e.Id == id);
        }
    }
}
