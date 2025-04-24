using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using STRATFY.Models;
using STRATFY.Interfaces;
using STRATFY.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace STRATFY.Controllers
{
    [Authorize]
    public class ExtratosController : Controller
    {
        private readonly RepositoryExtrato _extratoRepository;
        private readonly RepositoryUsuario _usuarioRepository;
        private readonly IRepositoryBase<Categoria> _categoriaRepository;
        private readonly RepositoryMovimentacao _movRepository;
        private readonly AppDbContext _context;

        public ExtratosController(AppDbContext context, RepositoryExtrato extratoRepository, RepositoryUsuario usuarioRepo, RepositoryMovimentacao movRepository, IRepositoryBase<Categoria> categoriaRepository)
        {
            _context = context;
            _extratoRepository = extratoRepository;
            _usuarioRepository = usuarioRepo;
            _movRepository = movRepository;
            _categoriaRepository = categoriaRepository;
        }

        public async Task<IActionResult> Index()
        {
            var extratos = await _extratoRepository.SelecionarTodosDoUsuarioAsync();
            return View(extratos);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var extrato = _extratoRepository.CarregarExtratoCompleto(id.Value);
            if (extrato == null)
                return NotFound();

            return View(extrato);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nome,DataCriacao")] Extrato extrato, IFormFile csvFile)
        {
            ModelState.Remove("Usuario");
            ModelState.Remove("csvFile");

            if (ModelState.IsValid)
            {
                extrato.Usuario = await _usuarioRepository.ObterUsuarioLogado();
                extrato.DataCriacao = DateOnly.FromDateTime(DateTime.Now);
                await _extratoRepository.IncluirAsync(extrato);

                if (csvFile != null && csvFile.Length > 0)
                {
                    using var memoryStream = new MemoryStream();
                    await csvFile.CopyToAsync(memoryStream);
                    var byteArrayContent = new ByteArrayContent(memoryStream.ToArray());

                    using var httpClient = new HttpClient();
                    using var form = new MultipartFormDataContent();
                    form.Add(byteArrayContent, "file", csvFile.FileName);

                    var response = await httpClient.PostAsync("http://localhost:8000/api/uploadcsv", form);

                    var erro = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Erro ao enviar CSV para API: " + erro);


                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var movimentacoes = JsonSerializer.Deserialize<List<Movimentacao>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (movimentacoes != null && movimentacoes.Any())
                        {
                            foreach (var mov in movimentacoes)
                            {
                                mov.ExtratoId = extrato.Id;
                                mov.Categoria = _context.Categoria.FirstOrDefault(c => c.Nome == "Outros");
                                
                                _movRepository.Incluir(mov);
                            }
                            _movRepository.Salvar();
                        }
                    }
                }

                return RedirectToAction("Edit", new { id = extrato.Id });
            }

            ViewData["UsuarioId"] = _extratoRepository.SelecionarChaveAsync(extrato.UsuarioId);
            return View(extrato);
        }


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
                    Categoria = m.Categoria,
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
            ModelState.Remove("CategoriaId"); // se necessário
            if (!ModelState.IsValid)
            {
                ViewData["CategoriaId"] = new SelectList(_categoriaRepository.SelecionarTodos(), "Id", "Nome");
                return View(model);
            }

            var extrato = _extratoRepository.CarregarExtratoCompleto(model.ExtratoId);
            if (extrato == null)
                return NotFound();

            extrato.Nome = model.NomeExtrato;
            extrato.DataCriacao = model.DataCriacao;

            var idsRecebidos = model.Movimentacoes.Select(m => m.Id).ToList();
            var movimentacoesRemovidas = extrato.Movimentacaos.Where(m => !idsRecebidos.Contains(m.Id)).ToList();
            _movRepository.RemoverVarias(movimentacoesRemovidas);

            foreach (var mov in model.Movimentacoes)
            {
                if (!string.IsNullOrWhiteSpace(mov.Categoria?.Nome))
                {
                    var categoria = _context.Categoria.FirstOrDefault(c => c.Nome.ToLower() == mov.Categoria.Nome.ToLower());

                    if (categoria == null)
                    {
                        categoria = new Categoria { Nome = mov.Categoria.Nome };
                        _context.Categoria.Add(categoria);
                        _context.SaveChanges();
                    }

                    mov.CategoriaId = categoria.Id;
                }

                if (mov.Id == 0)
                {
                    mov.ExtratoId = model.ExtratoId;
                    _movRepository.Incluir(mov);
                }
                else
                {
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
                return NotFound();

            var extrato = _extratoRepository.CarregarExtratoCompleto(id.Value);
            if (extrato == null)
                return NotFound();

            return View(extrato);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var extrato = _extratoRepository.SelecionarChave(id);
            try
            {
                if (extrato != null)
                    _extratoRepository.Excluir(extrato);

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                TempData["DeleteError"] = "Não foi possível excluir o extrato porque ele possui movimentações vinculadas.";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }
    }
}
