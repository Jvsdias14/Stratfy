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
    public class GraficosController : Controller
    {
        private readonly AppDbContext _context;

        public GraficosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Graficos
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.Graficos.Include(g => g.Dashboard);
            return View(await appDbContext.ToListAsync());
        }

        // GET: Graficos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var grafico = await _context.Graficos
                .Include(g => g.Dashboard)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (grafico == null)
            {
                return NotFound();
            }

            return View(grafico);
        }

        // GET: Graficos/Create
        public IActionResult Create()
        {
            ViewData["DashboardId"] = new SelectList(_context.Dashboards, "Id", "Id");
            return View();
        }

        // POST: Graficos/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,DashboardId,Titulo,Tipo,Campo1,Campo2,Cor,AtivarLegenda")] Grafico grafico)
        {
            ModelState.Remove("Dashboard");
            if (ModelState.IsValid)
            {
                _context.Add(grafico);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["DashboardId"] = new SelectList(_context.Dashboards, "Id", "Id", grafico.DashboardId);
            return View(grafico);
        }

        // GET: Graficos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var grafico = await _context.Graficos.FindAsync(id);
            if (grafico == null)
            {
                return NotFound();
            }
            ViewData["DashboardId"] = new SelectList(_context.Dashboards, "Id", "Id", grafico.DashboardId);
            return View(grafico);
        }

        // POST: Graficos/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DashboardId,Titulo,Tipo,Campo1,Campo2,Cor,AtivarLegenda")] Grafico grafico)
        {
            if (id != grafico.Id)
            {
                return NotFound();
            }
            ModelState.Remove("Dashboard");
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(grafico);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GraficoExists(grafico.Id))
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
            ViewData["DashboardId"] = new SelectList(_context.Dashboards, "Id", "Id", grafico.DashboardId);
            return View(grafico);
        }

        // GET: Graficos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var grafico = await _context.Graficos
                .Include(g => g.Dashboard)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (grafico == null)
            {
                return NotFound();
            }

            return View(grafico);
        }

        // POST: Graficos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var grafico = await _context.Graficos.FindAsync(id);
            if (grafico != null)
            {
                _context.Graficos.Remove(grafico);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GraficoExists(int id)
        {
            return _context.Graficos.Any(e => e.Id == id);
        }
    }
}
