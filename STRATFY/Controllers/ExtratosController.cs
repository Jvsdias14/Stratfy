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

namespace STRATFY.Controllers
{
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

        // POST: Extratos/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("id,UsuarioId,Nome,DataCriacao")] Extrato extrato)
        {
            ModelState.Remove("Usuario");
            
            if (ModelState.IsValid)
            {
                //Usuario usuario = await _context.Usuarios.FindAsync(extrato.UsuarioId);
                //extrato.Usuario = usuario;
                extrato.DataCriacao = DateOnly.FromDateTime(DateTime.Now);
                //_context.Add(extrato);
                //await _context.SaveChangesAsync();
                //return RedirectToAction(nameof(Index));
                return RedirectToAction("Create", "Movimentacoes", new { extratoId = extrato.Id });
                _extratoRepository.IncluirAsync(extrato);
            }
            //ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "Id", "Id", extrato.UsuarioId);
            ViewData["UsuarioId"] = _extratoRepository.SelecionarChaveAsync(extrato.UsuarioId);
            return View(extrato);
        }

        // GET: Extratos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var extrato = await _context.Extratos.FindAsync(id);
            if (extrato == null)
            {
                return NotFound();
            }
            ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "Id", "Id", extrato.UsuarioId);
            return View(extrato);
        }

        // POST: Extratos/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit( Extrato extrato)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(extrato);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ExtratoExists(extrato.Id))
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
            ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "Id", "Id", extrato.UsuarioId);
            return View(extrato);
        }

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
            if (extrato != null)
            {
                _context.Extratos.Remove(extrato);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ExtratoExists(int id)
        {
            return _context.Extratos.Any(e => e.Id == id);
        }
    }
}
