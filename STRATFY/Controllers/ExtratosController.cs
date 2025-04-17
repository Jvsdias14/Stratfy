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
using STRATFY.Repositories;

namespace STRATFY.Controllers
{
    [Authorize]
    public class ExtratosController : Controller
    {
        private readonly RepositoryExtrato _extratoRepository;
        private readonly IRepositoryBase<Usuario> _usuarioRepository;
        private readonly IRepositoryBase<Categoria> _categoriaRepository;
        private readonly RepositoryMovimentacao _movRepository;
        private readonly AppDbContext _context;
        

        public ExtratosController(AppDbContext context, RepositoryExtrato extratoRepository, IRepositoryBase<Usuario> usuarioRepo, RepositoryMovimentacao movRepository, IRepositoryBase<Categoria> categoriaRepository)
        {
            _context = context;
            _extratoRepository = extratoRepository;
            _usuarioRepository = usuarioRepo;
            _movRepository = movRepository;
            _categoriaRepository = categoriaRepository;
        }


        // GET: Extratos
        public async Task<IActionResult> Index()
        {
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

            var extrato = _extratoRepository.CarregarExtratoCompleto(id.Value);
            if (extrato == null)
            {
                return NotFound();
            }

            return View(extrato);
        }

        // GET: Extratos/Create
        public IActionResult Create()
        {
            var usuarios = _usuarioRepository.SelecionarTodos();
            ViewData["UsuarioId"] = new SelectList(usuarios, "Id", "Nome");
            return View();
        }

  
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UsuarioId,Nome,DataCriacao")] Extrato extrato)
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

            var extrato = _extratoRepository.CarregarExtratoCompleto(id.Value);

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
                    ExtratoId = m.ExtratoId,
                    DataMovimentacao = m.DataMovimentacao

                }).ToList()
            };

            ViewData["CategoriaId"] = new SelectList(_categoriaRepository.SelecionarTodos(), "Id", "Nome");

            return View(viewModel);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ExtratoEdicaoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewData["CategoriaId"] = new SelectList(_categoriaRepository.SelecionarTodos(), "Id", "Nome");
                return View(model);
            }

            var extrato = _extratoRepository.CarregarExtratoCompleto(model.ExtratoId);

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

            _movRepository.RemoverVarias(movimentacoesRemovidas);

            // Itera sobre cada movimentação recebida no form
            foreach (var mov in model.Movimentacoes)
            {
                if (mov.Id == 0)
                {
                    // Nova movimentação
                    mov.ExtratoId = model.ExtratoId;
                    _movRepository.Incluir(mov);
                }
                else
                {
                    // Atualização
                    var movBanco = _movRepository.SelecionarChave(mov.Id);
                    if (movBanco != null)
                    {
                        movBanco.Descricao = mov.Descricao;
                        movBanco.Valor = mov.Valor;
                        movBanco.Tipo = mov.Tipo;
                        movBanco.CategoriaId = mov.CategoriaId;
                        movBanco.DataMovimentacao = mov.DataMovimentacao;
                    }
                }
            }

            _extratoRepository.Salvar();
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var extrato = _extratoRepository.CarregarExtratoCompleto(id.Value);
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
            var extrato = _extratoRepository.SelecionarChave(id);
            try
            {
                if (extrato != null)
                {
                    _extratoRepository.Excluir(extrato);
                }
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                TempData["DeleteError"] = "Não foi possível excluir o extrato porque ele possui movimentações vinculadas.";
                return RedirectToAction(nameof(Delete), new { id }); // Redireciona de volta pra tela de confirmação de delete
            }
        }
    }
}
