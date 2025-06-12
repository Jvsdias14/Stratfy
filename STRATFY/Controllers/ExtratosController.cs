using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using STRATFY.Interfaces.IServices; // Usar as interfaces das Services
using STRATFY.Models; // Para Extrato e Movimentacao (ViewModels)
using System; // Para Exception
using System.Threading.Tasks;
using System.Collections.Generic; // Para List

namespace STRATFY.Controllers
{
    [Authorize]
    public class ExtratosController : Controller
    {
        private readonly IExtratoService _extratoService;
        private readonly ICategoriaService _categoriaService; // Para popular o SelectList de categorias

        // A Controller agora só injeta as Services de alto nível
        public ExtratosController(IExtratoService extratoService, ICategoriaService categoriaService)
        {
            _extratoService = extratoService;
            _categoriaService = categoriaService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // A Service já lida com o userId internamente
                var viewModel = await _extratoService.ObterExtratosDoUsuarioParaIndexAsync();
                return View(viewModel);
            }
            catch (UnauthorizedAccessException)
            {
                // Se o usuário não estiver autenticado ou o ID for inválido (mesmo com [Authorize])
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                // Capturar outras exceções da Service e lidar adequadamente
                TempData["ErrorMessage"] = "Ocorreu um erro ao carregar os extratos: " + ex.Message;
                // Logar o erro (ex: com um logger)
                return View(new List<ExtratoIndexViewModel>()); // Retorna uma lista vazia ou erro
            }
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            try
            {
                // Delega para a Service
                var extrato = await _extratoService.ObterExtratoDetalhesAsync(id.Value);

                if (extrato == null) // A service pode retornar null ou lançar exceção se não encontrar ou acesso negado
                    return NotFound();

                return View(extrato);
            }
            catch (ApplicationException) // Captura a exceção de acesso negado ou não encontrado da Service
            {
                return NotFound(); // Ou Forbid() se você quiser ser mais explícito sobre a permissão
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ocorreu um erro ao carregar os detalhes do extrato: " + ex.Message;
                return NotFound(); // Ou View("Error")
            }
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nome")] Extrato extrato, IFormFile csvFile) // Removido DataCriacao do Bind, a service define
        {
            // Remove as validações de propriedades que serão preenchidas pela Service ou não são enviadas pelo form
            ModelState.Remove("Usuario"); // A service vai definir o UsuarioId
            ModelState.Remove("DataCriacao"); // A service vai definir a DataCriacao
            ModelState.Remove("csvFile"); // Não faz parte do modelo de dados da entidade Extrato

            if (!ModelState.IsValid)
            {
                // Se o ModelState ainda não for válido após remover, retorna a View com erros
                return View(extrato);
            }

            try
            {
                // Delega para a Service: A service lida com userId, DataCriacao e processamento do CSV
                var extratoId = await _extratoService.CriarExtratoComMovimentacoesAsync(extrato, csvFile);
                return RedirectToAction("Edit", new { id = extratoId });
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account");
            }
            catch (ApplicationException ex)
            {
                ModelState.AddModelError("", ex.Message); // Adiciona a mensagem de erro ao ModelState
                return View(extrato);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Ocorreu um erro inesperado ao criar o extrato: " + ex.Message);
                return View(extrato);
            }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            try
            {
                // Delega para a Service
                var viewModel = await _extratoService.ObterExtratoParaEdicaoAsync(id.Value);

                if (viewModel == null) // A service pode retornar null ou lançar exceção se não encontrar ou acesso negado
                    return NotFound();

                // Popula o ViewData para o SelectList usando a CategoriaService
                ViewData["CategoriaId"] = new SelectList(_categoriaService.ObterTodasCategoriasParaSelectList(), "Id", "Nome");
                return View(viewModel);
            }
            catch (ApplicationException) // Captura a exceção de acesso negado ou não encontrado da Service
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ocorreu um erro ao carregar o extrato para edição: " + ex.Message;
                return NotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ExtratoEdicaoViewModel model)
        {
            // Remova a validação para Categoria.Nome em cada Movimentacao (se CategoriaId for o suficiente)
            // Cuidado: Dependendo do seu ViewModel, você pode precisar ajustar a validação aqui.
            // A service de Movimentacao agora valida se Categoria.Id é válido.
            foreach (var mov in model.Movimentacoes)
            {
                ModelState.Remove($"Movimentacoes[{model.Movimentacoes.IndexOf(mov)}].Categoria.Nome");
                // Remova outras validações se a Service for responsável por elas
                // Ex: ModelState.Remove($"Movimentacoes[{model.Movimentacoes.IndexOf(mov)}].CategoriaId"); // Se a service buscar a categoria pelo nome
            }

            if (!ModelState.IsValid)
            {
                // Se o ModelState ainda não for válido, repopula o SelectList e retorna a View com erros
                ViewData["CategoriaId"] = new SelectList(_categoriaService.ObterTodasCategoriasParaSelectList(), "Id", "Nome");
                return View(model);
            }

            try
            {
                // Delega para a Service: A service lida com a atualização do extrato e de suas movimentações
                await _extratoService.AtualizarExtratoEMovimentacoesAsync(model);
                TempData["SuccessMessage"] = "Extrato e movimentações atualizados com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (ApplicationException ex)
            {
                ModelState.AddModelError("", ex.Message); // Adiciona o erro ao ModelState
                ViewData["CategoriaId"] = new SelectList(_categoriaService.ObterTodasCategoriasParaSelectList(), "Id", "Nome");
                return View(model);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Ocorreu um erro inesperado ao atualizar o extrato: " + ex.Message);
                ViewData["CategoriaId"] = new SelectList(_categoriaService.ObterTodasCategoriasParaSelectList(), "Id", "Nome");
                return View(model);
            }
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            try
            {
                // Delega para a Service
                var extrato = await _extratoService.ObterExtratoDetalhesAsync(id.Value); // Use ObterExtratoDetalhesAsync para carregar completo e validar acesso

                if (extrato == null)
                    return NotFound();

                return View(extrato);
            }
            catch (ApplicationException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ocorreu um erro ao carregar o extrato para exclusão: " + ex.Message;
                return NotFound();
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                // Delega para a Service
                var success = await _extratoService.ExcluirExtratoAsync(id);
                if (success)
                {
                    TempData["SuccessMessage"] = "Extrato excluído com sucesso!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    // Se a service retornar false (por não encontrar ou erro interno não lançado como exceção)
                    TempData["ErrorMessage"] = "Não foi possível excluir o extrato.";
                    return RedirectToAction(nameof(Delete), new { id });
                }
            }
            catch (ApplicationException ex)
            {
                // Mensagem de erro específica da Service, como "Não foi possível excluir porque possui movimentações"
                TempData["DeleteError"] = ex.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                TempData["DeleteError"] = "Ocorreu um erro inesperado ao excluir o extrato: " + ex.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadCsv(int id)
        {
            try
            {
                // Delega para a Service
                var csvStream = await _extratoService.ExportarMovimentacoesDoExtratoParaCsvAsync(id);

                if (csvStream == null) // Service retorna null se extrato não for encontrado ou acesso negado
                {
                    return NotFound();
                }

                // Você pode querer buscar o nome do extrato para o nome do arquivo,
                // mas a Service de Exportação pode retornar isso também.
                // Por simplicidade, usaremos um nome genérico ou tentaremos obter do extrato carregado na Service
                var extrato = await _extratoService.ObterExtratoDetalhesAsync(id); // Reobtem o extrato apenas para o nome, se necessário
                var fileName = extrato != null ? $"Extrato_{extrato.Nome}_{DateTime.Now:yyyyMMdd_HHmmss}.csv" : $"Extrato_Movimentacoes_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                return File(csvStream, "text/csv; charset=utf-8", fileName);
            }
            catch (ApplicationException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ocorreu um erro ao exportar o CSV: " + ex.Message;
                return BadRequest(); // Ou outra resposta de erro
            }
        }
    }
}