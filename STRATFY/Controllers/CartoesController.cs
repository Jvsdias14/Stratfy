using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using STRATFY.Models;

namespace STRATFY.Controllers
{
    [Authorize]
    public class CartoesController : Controller
    {
        private readonly AppDbContext _context;

        public CartoesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Cartaos
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.Cartoes.Include(c => c.Dashboard);
            return View(await appDbContext.ToListAsync());
        }

        // GET: Cartaos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cartao = await _context.Cartoes
                .Include(c => c.Dashboard)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (cartao == null)
            {
                return NotFound();
            }

            return View(cartao);
        }

        // GET: Cartaos/Create
        public IActionResult Create()
        {
            ViewData["DashboardId"] = new SelectList(_context.Dashboards, "Id", "Id");
            return View();
        }

        // POST: Cartaos/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,DashboardId,Nome,Campo,TipoAgregacao,Cor")] Cartao cartao)
        {
            if (ModelState.IsValid)
            {
                _context.Add(cartao);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["DashboardId"] = new SelectList(_context.Dashboards, "Id", "Id", cartao.DashboardId);
            return View(cartao);
        }

        // GET: Cartaos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cartao = await _context.Cartoes.FindAsync(id);
            if (cartao == null)
            {
                return NotFound();
            }
            ViewData["DashboardId"] = new SelectList(_context.Dashboards, "Id", "Id", cartao.DashboardId);
            return View(cartao);
        }

        // POST: Cartaos/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DashboardId,Nome,Campo,TipoAgregacao,Cor")] Cartao cartao)
        {
            if (id != cartao.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(cartao);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CartaoExists(cartao.Id))
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
            ViewData["DashboardId"] = new SelectList(_context.Dashboards, "Id", "Id", cartao.DashboardId);
            return View(cartao);
        }

        // GET: Cartaos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cartao = await _context.Cartoes
                .Include(c => c.Dashboard)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (cartao == null)
            {
                return NotFound();
            }

            return View(cartao);
        }

        // POST: Cartaos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cartao = await _context.Cartoes.FindAsync(id);
            if (cartao != null)
            {
                _context.Cartoes.Remove(cartao);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CartaoExists(int id)
        {
            return _context.Cartoes.Any(e => e.Id == id);
        }
    }
}
