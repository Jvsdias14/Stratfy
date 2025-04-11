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
    //[Authorize]
    public class DashboardsController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Dashboards
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.Dashboards.Include(d => d.Extrato);
            return View(await appDbContext.ToListAsync());
        }

        // GET: Dashboards/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dashboard = await _context.Dashboards
                .Include(d => d.Extrato)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (dashboard == null)
            {
                return NotFound();
            }

            return View(dashboard);
        }

        // GET: Dashboards/Create
        [HttpGet]
        public IActionResult Create()
        {
            var model = new DashboardVM
            {
                ExtratosDisponiveis = _context.Extratos
                    .Select(e => new SelectListItem
                    {
                        Value = e.Id.ToString(),
                        Text = e.Nome
                    }).ToList()
            };

            return View(model);
        }


        [HttpPost]
        public IActionResult Create(DashboardVM model, string action)
        {
            if (action == "padrao")
            {
                // Redireciona para a action CriarPadrao (já existente)
                return RedirectToAction("CriarPadrao", new { nome = model.Nome, extratoId = model.ExtratoId });
            }

            // Criação personalizada
            if (!ModelState.IsValid)
                return View(model);

            var dashboard = new Dashboard
            {
                Descricao = model.Nome,
                ExtratoId = model.ExtratoId,
                Graficos = model.Graficos,
                Cartoes = model.Cartoes
            };

            _context.Dashboards.Add(dashboard);
            _context.SaveChanges();

            return RedirectToAction("Details", new { id = dashboard.Id });
        }


        // Botão Criar Dash Padrão
        [HttpGet]
        public IActionResult CriarPadrao(string nome, int extratoId)
        {
            if (string.IsNullOrWhiteSpace(nome) || extratoId == 0)
            {
                var model = new DashboardVM
                {
                    Nome = nome,
                    ExtratoId = extratoId,
                    ExtratosDisponiveis = ObterListaExtratos()
                };
                ModelState.AddModelError("", "Preencha todos os campos obrigatórios.");
                return View("Create", model);
            }

            var graficosPadrao = new List<Grafico>
    {
        new Grafico { Titulo = "Gasto diário", Campo1 = "DataMovimentacao", Campo2 = "Valor", Tipo = "Barra", Cor = "#3366cc", AtivarLegenda = false },
        new Grafico { Titulo = "Gasto por categoria", Campo1 = "Categoria", Campo2 = "Valor", Tipo = "Pizza", Cor = "#3366cc", AtivarLegenda = false }
    };

            var cartoesPadrao = new List<Cartao>
    {
        new Cartao { Nome = "Total Geral", Campo = "Valor", TipoAgregacao = "soma", Cor = "black" },
        new Cartao { Nome = "Contagem de Categorias", Campo = "Categoria", TipoAgregacao = "contagem", Cor = "blue" }
    };

            var dashboard = new Dashboard
            {
                Descricao = nome,
                ExtratoId = extratoId,
                Graficos = graficosPadrao,
                Cartoes = cartoesPadrao
            };

            _context.Dashboards.Add(dashboard);
            _context.SaveChanges();

            return RedirectToAction("Details", new { id = dashboard.Id });
        }


        [HttpGet]
        public IActionResult VisualizarStreamlit(int id)
        {
            var dashboard = _context.Dashboards
                .Include(d => d.Graficos)
                .Include(d => d.Cartoes)
                .FirstOrDefault(d => d.Id == id);

            if (dashboard == null)
            {
                return NotFound();
            }

            return View(dashboard); // a View usará o dashboard.Id para montar a URL
        }



        [HttpGet("api/dashboarddata/{id}")]
        public IActionResult GetDashboardData(int id)
        {
            var dashboard = _context.Dashboards
                .Include(d => d.Graficos)
                .Include(d => d.Cartoes)
                .Include(d => d.Extrato)
                    .ThenInclude(e => e.Movimentacaos)
                     .ThenInclude(m => m.Categoria)
                .FirstOrDefault(d => d.Id == id);

            if (dashboard == null)
                return NotFound();

            var result = new
            {
                Dashboard = new { dashboard.Id, dashboard.Descricao },
                Extrato = dashboard.Extrato.Nome,
                Movimentacoes = dashboard.Extrato.Movimentacaos.Select(m => new
                {
                    m.DataMovimentacao,
                    m.Descricao,
                    m.Valor,
                    m.Tipo,
                    Categoria = m.Categoria != null ? m.Categoria.Nome : null
                }),
                Graficos = dashboard.Graficos.Select(g => new
                {
                    g.Titulo,
                    g.Campo1,
                    g.Campo2,
                    g.Tipo,
                    g.Cor,
                    g.AtivarLegenda
                }),
                Cartoes = dashboard.Cartoes.Select(c => new
                {
                    c.Nome,
                    c.Campo,
                    c.TipoAgregacao,
                    c.Cor
                })
            };

            return Ok(result);
        }


        private List<SelectListItem> ObterListaExtratos()
        {
            return _context.Extratos.Select(e => new SelectListItem
            {
                Value = e.Id.ToString(),
                Text = e.Nome
            }).ToList();
        }


        // GET: Dashboards/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dashboard = await _context.Dashboards.FindAsync(id);
            if (dashboard == null)
            {
                return NotFound();
            }
            ViewData["ExtratoId"] = new SelectList(_context.Extratos, "Id", "Id", dashboard.ExtratoId);
            return View(dashboard);
        }

        // POST: Dashboards/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ExtratoId,Descricao")] Dashboard dashboard)
        {
            if (id != dashboard.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(dashboard);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DashboardExists(dashboard.Id))
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
            ViewData["ExtratoId"] = new SelectList(_context.Extratos, "Id", "Id", dashboard.ExtratoId);
            return View(dashboard);
        }

        // GET: Dashboards/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dashboard = await _context.Dashboards
                .Include(d => d.Extrato)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (dashboard == null)
            {
                return NotFound();
            }

            return View(dashboard);
        }

        // POST: Dashboards/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var dashboard = await _context.Dashboards.FindAsync(id);
            if (dashboard != null)
            {
                _context.Dashboards.Remove(dashboard);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DashboardExists(int id)
        {
            return _context.Dashboards.Any(e => e.Id == id);
        }
    }
}
