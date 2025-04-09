using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using STRATFY.Interfaces;
using STRATFY.Models;

namespace STRATFY.Controllers
{
    //[Authorize]
    public class MovimentacoesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IRepositoryBase<Movimentacao> _movimentacaoRepository;

        public MovimentacoesController(AppDbContext context, IRepositoryBase<Movimentacao> movimentacaoRepository)
        {
            _context = context;
            _movimentacaoRepository = movimentacaoRepository;
        }

        // GET: Movimentacaos
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.Movimentacaos.Include(m => m.Categoria).Include(m => m.Extrato);
            return View(await appDbContext.ToListAsync());
        }

        // GET: Movimentacaos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movimentacao = await _context.Movimentacaos
                .Include(m => m.Categoria)
                .Include(m => m.Extrato)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (movimentacao == null)
            {
                return NotFound();
            }

            return View(movimentacao);
        }

        // GET: Movimentacaos/Create
        public IActionResult Create(Extrato extrato)
        {
            ViewData["CategoriaId"] = new SelectList(_context.Categoria.ToList(), "Id", "Nome");
            ViewData["NomeExtrato"] = extrato.Nome;

            var movimentacao = new Movimentacao
            {
                ExtratoId = extrato.Id
            };
            
            return View(movimentacao);
        }

        // POST: Movimentacaos/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ExtratoId,CategoriaId,Descricao,Tipo,Valor,DataMovimentacao")] Movimentacao movimentacao)
        {
            if (ModelState.IsValid)
            {
                _context.Add(movimentacao);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoriaId"] = new SelectList(_context.Categoria.ToList(), "Id", "Nome");
            ViewData["ExtratoId"] = new SelectList(_context.Extratos.ToList(), "Id", "Nome");
            return View(movimentacao);
        }

        // GET: Movimentacaos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movimentacao = await _context.Movimentacaos.FindAsync(id);
            if (movimentacao == null)
            {
                return NotFound();
            }
            ViewData["CategoriaId"] = new SelectList(_context.Categoria.ToList(), "Id", "Nome");
            ViewData["ExtratoId"] = new SelectList(_context.Extratos.ToList(), "Id", "Nome");
            return View(movimentacao);
        }

        // POST: Movimentacaos/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ExtratoId,CategoriaId,Descricao,Tipo,Valor,DataMovimentacao")] Movimentacao movimentacao)
        {
            if (id != movimentacao.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(movimentacao);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MovimentacaoExists(movimentacao.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoriaId"] = new SelectList(_context.Categoria.ToList(), "Id", "Nome");
            ViewData["ExtratoId"] = new SelectList(_context.Extratos.ToList(), "Id", "Nome");
            return View(movimentacao);
        }

        // GET: Movimentacaos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movimentacao = await _context.Movimentacaos
                .Include(m => m.Categoria)
                .Include(m => m.Extrato)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (movimentacao == null)
            {
                return NotFound();
            }

            return View(movimentacao);
        }

        // POST: Movimentacaos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var movimentacao = await _context.Movimentacaos.FindAsync(id);
            if (movimentacao != null)
            {
                _context.Movimentacaos.Remove(movimentacao);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MovimentacaoExists(int id)
        {
            return _context.Movimentacaos.Any(e => e.Id == id);
        }
    }
}
