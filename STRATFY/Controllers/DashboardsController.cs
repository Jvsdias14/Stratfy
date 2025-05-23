using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using STRATFY.Models;
using STRATFY.Repositories;

namespace STRATFY.Controllers
{
    [Authorize]
    public class DashboardsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly RepositoryDashboard _dashboardRepository;
        private readonly RepositoryExtrato _extratoRepository;

        public DashboardsController(AppDbContext context, RepositoryDashboard repositoryDashboard, RepositoryExtrato extratoRepository)
        {
            _context = context;
            _dashboardRepository = repositoryDashboard;
            _extratoRepository = extratoRepository;
        }

        // GET: Dashboards
        public async Task<IActionResult> Index()
        {
            var dashboards = await _dashboardRepository.SelecionarTodosDoUsuarioAsync();
            return View(dashboards);
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
        public async Task<IActionResult> Create()
        {
            var extratos = await _extratoRepository.SelecionarTodosDoUsuarioAsync();

            var model = new DashboardVM
            {
                ExtratosDisponiveis = extratos.Select(e => new SelectListItem
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
                return RedirectToAction("CriarPadrao", new { nome = model.Nome, extratoId = model.ExtratoId });
            }
            ModelState.Remove("ExtratosDisponiveis");
            if (!ModelState.IsValid)
            {
                model.ExtratosDisponiveis = ObterListaExtratos();
                return View(model);
            }

            var dashboard = new Dashboard
            {
                Descricao = model.Nome,
                ExtratoId = model.ExtratoId
            };

            _context.Dashboards.Add(dashboard);
            _context.SaveChanges();

            return RedirectToAction("Edit", new { id = dashboard.Id });
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
        new Grafico { Titulo = "Gasto diário", Campo1 = "Datamovimentacao", Campo2 = "Valor", Tipo = "Barra", Cor = "#3366cc", AtivarLegenda = false },
        new Grafico { Titulo = "Gasto por categoria", Campo1 = "Categoria", Campo2 = "Valor", Tipo = "Pizza", Cor = "#3366cc", AtivarLegenda = false }
    };

            var cartoesPadrao = new List<Cartao>
    {
        new Cartao { Nome = "Total de Gastos", Campo = "Valor", TipoAgregacao = "soma", Cor = "#3366cc" },
        new Cartao { Nome = "Média de Gastos", Campo = "Valor", TipoAgregacao = "media", Cor = "#3366cc" },
        new Cartao { Nome = "Movimentações", Campo = "Valor", TipoAgregacao = "contagem", Cor = "#3366cc" }
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

        [HttpGet("api/dashboarddata/{id}")]
        [AllowAnonymous]
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
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var dashboard = _context.Dashboards
                .Include(d => d.Graficos)
                .Include(d => d.Cartoes)
                .FirstOrDefault(d => d.Id == id);

            if (dashboard == null)
                return NotFound();

            var model = new DashboardVM
            {
                Id = dashboard.Id,
                Nome = dashboard.Descricao,
                ExtratoId = dashboard.ExtratoId,
                Graficos = dashboard.Graficos.ToList(),
                Cartoes = dashboard.Cartoes.ToList(),
            };
            var extratos = await _extratoRepository.SelecionarTodosDoUsuarioAsync();
            {
                model.ExtratosDisponiveis = extratos.Select(e => new SelectListItem
                {
                    Value = e.Id.ToString(),
                    Text = e.Nome
                }).ToList();
            };

            return View(model);
        }

        // POST: Dashboards/Edit/5
        [HttpPost]
        public async Task<IActionResult> Edit(DashboardVM model)
        {

            ModelState.Remove("ExtratosDisponiveis");

            for (int i = 0; i < model.Graficos?.Count; i++)
            {
                ModelState.Remove($"Graficos[{i}].Dashboard");
            }

            for (int i = 0; i < model.Cartoes?.Count; i++)
            {
                ModelState.Remove($"Cartoes[{i}].Dashboard");
            }

            if (!ModelState.IsValid)
            {
                model.ExtratosDisponiveis = ObterListaExtratos();
                return View(model);
            }

            var dashboard = _context.Dashboards
                .Include(d => d.Graficos)
                .Include(d => d.Cartoes)
                .FirstOrDefault(d => d.Id == model.Id);

            if (dashboard == null)
                return NotFound();

            dashboard.Descricao = model.Nome;
            dashboard.ExtratoId = model.ExtratoId;

            dashboard.Graficos = model.Graficos;
            dashboard.Cartoes = model.Cartoes;

            _context.Update(dashboard);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = dashboard.Id });
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
