// STRATFY.Controllers/DashboardsController.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // Para SelectListItem
using STRATFY.Interfaces.IServices;
using STRATFY.Models;
using STRATFY.DTOs; // Usar os DTOs da API

namespace STRATFY.Controllers
{
    [Authorize]
    public class DashboardsController : Controller
    {
        private readonly IDashboardService _dashboardService;
        // O IUsuarioContexto pode ser injetado diretamente na Service e não ser necessário aqui,
        // dependendo de onde você decide fazer as validações de usuário.
        // Se as services já fazem a validação baseada no usuário logado, ele pode ser removido daqui.
        // private readonly IUsuarioContexto _usuarioContexto;

        public DashboardsController(IDashboardService dashboardService) // Removi IUsuarioContexto daqui
        {
            _dashboardService = dashboardService;
            // _usuarioContexto = usuarioContexto;
        }

        // GET: Dashboards
        public async Task<IActionResult> Index()
        {
            try
            {
                var dashboards = await _dashboardService.ObterTodosDashboardsDoUsuarioAsync();
                return View(dashboards);
            }
            catch (UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] = "Você precisa estar logado para acessar os dashboards.";
                return RedirectToAction("Index", "Login");
            }
            catch (Exception ex)
            {
                // Logar o erro completo para depuração
                // _logger.LogError(ex, "Erro ao carregar dashboards na Index.");
                TempData["ErrorMessage"] = "Ocorreu um erro ao carregar seus dashboards. " + ex.Message;
                return View(new List<STRATFY.Models.Dashboard>()); // Retorna uma lista vazia para evitar null
            }
        }

        // GET: Dashboards/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var dashboard = await _dashboardService.ObterDashboardPorIdAsync(id.Value);
                if (dashboard == null)
                {
                    // Se a service retorna null, é porque não encontrou ou não pertence ao usuário
                    TempData["ErrorMessage"] = "Dashboard não encontrado ou você não tem permissão para visualizá-lo.";
                    return NotFound();
                }
                return View(dashboard);
            }
            catch (UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] = "Acesso não autorizado para visualizar este dashboard.";
                return RedirectToAction("Index", "Login");
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Erro ao carregar detalhes do dashboard.");
                TempData["ErrorMessage"] = "Ocorreu um erro ao carregar os detalhes do dashboard: " + ex.Message;
                return NotFound();
            }
        }

        // GET: Dashboards/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new DashboardVM
            {
                ExtratosDisponiveis = await _dashboardService.ObterExtratosDisponiveisParaUsuarioAsync()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DashboardVM model, string action)
        {
            if (action == "padrao")
            {
                // Redireciona para o método de criação padrão
                return RedirectToAction("CriarPadrao", new { nome = model.Nome, extratoId = model.ExtratoId });
            }

            // Remove ModelState para propriedades que não vêm da UI ou são apenas para exibição
            ModelState.Remove("ExtratosDisponiveis");
            // Se você está postando Graficos/Cartoes vazios ou incompletos na criação normal, pode precisar limpar o ModelState deles também
            for (int i = 0; i < model.Graficos?.Count; i++) { ModelState.Remove($"Graficos[{i}].Dashboard"); }
            for (int i = 0; i < model.Cartoes?.Count; i++) { ModelState.Remove($"Cartoes[{i}].Dashboard"); }


            if (!ModelState.IsValid)
            {
                model.ExtratosDisponiveis = await _dashboardService.ObterExtratosDisponiveisParaUsuarioAsync();
                return View(model);
            }

            try
            {
                var dashboard = await _dashboardService.CriarDashboardAsync(model);
                TempData["SuccessMessage"] = "Dashboard criado com sucesso!";
                return RedirectToAction("Edit", new { id = dashboard.Id });
            }
            catch (ApplicationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] = "Você não tem permissão para realizar esta ação.";
                return RedirectToAction("Index", "Login");
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Erro inesperado ao criar dashboard.");
                ModelState.AddModelError(string.Empty, "Ocorreu um erro inesperado ao criar o dashboard.");
            }

            // Se houve erro, recarrega a lista de extratos e retorna a view com os erros
            model.ExtratosDisponiveis = await _dashboardService.ObterExtratosDisponiveisParaUsuarioAsync();
            return View(model);
        }

        // Botão Criar Dash Padrão
        [HttpGet]
        public async Task<IActionResult> CriarPadrao(string nome, int extratoId)
        {
            if (string.IsNullOrWhiteSpace(nome) || extratoId == 0)
            {
                var model = new DashboardVM
                {
                    Nome = nome,
                    ExtratoId = extratoId,
                    ExtratosDisponiveis = await _dashboardService.ObterExtratosDisponiveisParaUsuarioAsync()
                };
                ModelState.AddModelError(string.Empty, "Preencha todos os campos obrigatórios.");
                return View("Create", model);
            }

            try
            {
                var dashboard = await _dashboardService.CriarDashboardPadraoAsync(nome, extratoId);
                TempData["SuccessMessage"] = "Dashboard padrão criado com sucesso!";
                return RedirectToAction("Details", new { id = dashboard.Id });
            }
            catch (ApplicationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] = "Você não tem permissão para realizar esta ação.";
                return RedirectToAction("Index", "Login");
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Erro inesperado ao criar dashboard padrão.");
                ModelState.AddModelError(string.Empty, "Ocorreu um erro inesperado ao criar o dashboard padrão.");
            }

            // Se houve erro, recarrega a lista de extratos e retorna a view de Create
            var modelErro = new DashboardVM
            {
                Nome = nome,
                ExtratoId = extratoId,
                ExtratosDisponiveis = await _dashboardService.ObterExtratosDisponiveisParaUsuarioAsync()
            };
            return View("Create", modelErro);
        }

        [HttpGet("api/dashboarddata/{id}")]
        [AllowAnonymous] // Ajuste conforme a necessidade de autenticação para a API
        public async Task<IActionResult> GetDashboardData(int id)
        {
            try
            {
                var dashboardData = await _dashboardService.ObterDadosDashboardParaApiAsync(id);
                if (dashboardData == null)
                {
                    return NotFound();
                }
                return Ok(dashboardData);
            }
            catch (ApplicationException ex)
            {
                // Para APIs, retorne um BadRequest com a mensagem de erro
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, new { message = "Acesso não autorizado." }); // Forbidden
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Erro na API GetDashboardData.");
                return StatusCode(500, new { message = "Ocorreu um erro interno do servidor." });
            }
        }

        // GET: Dashboards/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var dashboard = await _dashboardService.ObterDashboardPorIdAsync(id);
                if (dashboard == null)
                {
                    TempData["ErrorMessage"] = "Dashboard não encontrado ou você não tem permissão para editá-lo.";
                    return NotFound();
                }

                var model = new DashboardVM
                {
                    Id = dashboard.Id,
                    Nome = dashboard.Descricao,
                    ExtratoId = dashboard.ExtratoId,
                    Graficos = dashboard.Graficos.ToList(), // Carrega os gráficos e cartões existentes
                    Cartoes = dashboard.Cartoes.ToList(),
                    ExtratosDisponiveis = await _dashboardService.ObterExtratosDisponiveisParaUsuarioAsync()
                };

                return View(model);
            }
            catch (UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] = "Acesso não autorizado para editar este dashboard.";
                return RedirectToAction("Index", "Login");
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Erro ao carregar dashboard para edição.");
                TempData["ErrorMessage"] = "Ocorreu um erro ao carregar o dashboard para edição: " + ex.Message;
                return NotFound();
            }
        }

        // POST: Dashboards/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DashboardVM model)
        {
            // Remover do ModelState propriedades de navegação ou listas que não são postadas diretamente,
            // ou que seriam validadas de forma circular.
            ModelState.Remove("ExtratosDisponiveis");
            for (int i = 0; i < model.Graficos?.Count; i++)
            {
                ModelState.Remove($"Graficos[{i}].Dashboard");
                ModelState.Remove($"Graficos[{i}].DashboardId"); // Se DashboardId for uma propriedade em Grafico
            }

            for (int i = 0; i < model.Cartoes?.Count; i++)
            {
                ModelState.Remove($"Cartoes[{i}].Dashboard");
                ModelState.Remove($"Cartoes[{i}].DashboardId"); // Se DashboardId for uma propriedade em Cartao
            }

            if (!ModelState.IsValid)
            {
                model.ExtratosDisponiveis = await _dashboardService.ObterExtratosDisponiveisParaUsuarioAsync();
                return View(model);
            }

            try
            {
                await _dashboardService.AtualizarDashboardAsync(model);
                TempData["SuccessMessage"] = "Dashboard atualizado com sucesso!";
                return RedirectToAction("Details", new { id = model.Id });
            }
            catch (ApplicationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] = "Você não tem permissão para realizar esta ação.";
                return RedirectToAction("Index", "Login");
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Erro inesperado ao atualizar dashboard.");
                ModelState.AddModelError(string.Empty, "Ocorreu um erro inesperado ao atualizar o dashboard.");
            }

            model.ExtratosDisponiveis = await _dashboardService.ObterExtratosDisponiveisParaUsuarioAsync();
            return View(model);
        }

        // GET: Dashboards/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var dashboard = await _dashboardService.ObterDashboardPorIdAsync(id.Value);
                if (dashboard == null)
                {
                    TempData["ErrorMessage"] = "Dashboard não encontrado ou você não tem permissão para excluí-lo.";
                    return NotFound();
                }
                return View(dashboard);
            }
            catch (UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] = "Acesso não autorizado para excluir este dashboard.";
                return RedirectToAction("Index", "Login");
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Erro ao carregar dashboard para exclusão.");
                TempData["ErrorMessage"] = "Ocorreu um erro ao carregar o dashboard para exclusão: " + ex.Message;
                return NotFound();
            }
        }

        // POST: Dashboards/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _dashboardService.ExcluirDashboardAsync(id);
                TempData["SuccessMessage"] = "Dashboard excluído com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (ApplicationException ex)
            {
                TempData["DeleteError"] = ex.Message; // Exibir este erro na View de Delete, se existir
                return RedirectToAction(nameof(Delete), new { id });
            }
            catch (UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] = "Você não tem permissão para realizar esta ação.";
                return RedirectToAction("Index", "Login");
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Erro inesperado ao excluir dashboard.");
                TempData["DeleteError"] = "Ocorreu um erro inesperado ao excluir o dashboard: " + ex.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
        }
    }
}