// STRATFY.Controllers/ExtratosController.cs
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using STRATFY.Interfaces.IServices;
using STRATFY.Models;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
// using STRATFY.Interfaces.IRepositories; // Não é necessário aqui se você usa ICategoriaService para a View

namespace STRATFY.Controllers
{
    [Authorize]
    public class ExtratosController : Controller
    {
        private readonly IExtratoService _extratoService;
        private readonly ICategoriaService _categoriaService; // Mantido para popular o SelectList de categorias

        public ExtratosController(IExtratoService extratoService, ICategoriaService categoriaService)
        {
            _extratoService = extratoService;
            _categoriaService = categoriaService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var viewModel = await _extratoService.ObterExtratosDoUsuarioParaIndexAsync();
                return View(viewModel);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ocorreu um erro ao carregar os extratos: " + ex.Message;
                return View(new List<ExtratoIndexViewModel>());
            }
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            try
            {
                var extrato = await _extratoService.ObterExtratoDetalhesAsync(id.Value);

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
                TempData["ErrorMessage"] = "Ocorreu um erro ao carregar os detalhes do extrato: " + ex.Message;
                return NotFound();
            }
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nome")] Extrato extrato, IFormFile csvFile)
        {
            ModelState.Remove("Usuario");
            ModelState.Remove("DataCriacao");
            ModelState.Remove("csvFile");

            if (!ModelState.IsValid)
            {
                return View(extrato);
            }

            try
            {
                var extratoId = await _extratoService.CriarExtratoComMovimentacoesAsync(extrato, csvFile);
                return RedirectToAction("Edit", new { id = extratoId });
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account");
            }
            catch (ApplicationException ex)
            {
                ModelState.AddModelError("", ex.Message);
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
                var viewModel = await _extratoService.ObterExtratoParaEdicaoAsync(id.Value);

                if (viewModel == null)
                    return NotFound();

                // Garanta que ObterTodasCategoriasParaSelectListAsync é um método async na sua ICategoriaService
                // e que retorna um IEnumerable<Categoria>
                ViewData["CategoriaId"] = new SelectList(await _categoriaService.ObterTodasCategoriasParaSelectListAsync(), "Id", "Nome");

                return View(viewModel);
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
                TempData["ErrorMessage"] = "Ocorreu um erro ao carregar o extrato para edição: " + ex.Message;
                return NotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ExtratoEdicaoViewModel model)
        {
            foreach (var mov in model.Movimentacoes)
            {

                ModelState.Remove($"Movimentacoes[{model.Movimentacoes.IndexOf(mov)}].Categoria.Nome");
            }

            if (!ModelState.IsValid)
            {
                // Se o ModelState ainda não for válido, repopula o SelectList e retorna a View com erros
                ViewData["CategoriaId"] = new SelectList(await _categoriaService.ObterTodasCategoriasParaSelectListAsync(), "Id", "Nome");
                return View(model);
            }

            try
            {
                await _extratoService.AtualizarExtratoEMovimentacoesAsync(model);
                TempData["SuccessMessage"] = "Extrato e movimentações atualizados com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (ApplicationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                ViewData["CategoriaId"] = new SelectList(await _categoriaService.ObterTodasCategoriasParaSelectListAsync(), "Id", "Nome");
                return View(model);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Ocorreu um erro inesperado ao atualizar o extrato: " + ex.Message);
                ViewData["CategoriaId"] = new SelectList(await _categoriaService.ObterTodasCategoriasParaSelectListAsync(), "Id", "Nome");
                return View(model);
            }
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            try
            {
                var extrato = await _extratoService.ObterExtratoDetalhesAsync(id.Value);

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
                var success = await _extratoService.ExcluirExtratoAsync(id);
                if (success)
                {
                    TempData["SuccessMessage"] = "Extrato excluído com sucesso!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = "Não foi possível excluir o extrato.";
                    return RedirectToAction(nameof(Delete), new { id });
                }
            }
            catch (ApplicationException ex)
            {
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
                var csvStream = await _extratoService.ExportarMovimentacoesDoExtratoParaCsvAsync(id);

                if (csvStream == null)
                {
                    return NotFound();
                }

                var extrato = await _extratoService.ObterExtratoDetalhesAsync(id);
                var fileName = extrato != null ? $"Extrato_{extrato.Nome?.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.csv" : $"Extrato_Movimentacoes_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

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
                return BadRequest();
            }
        }
    }
}