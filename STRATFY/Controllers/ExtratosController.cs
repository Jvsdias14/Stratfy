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

        public IActionResult Index()
        {
            var viewModel = _context.Extratos
                .Select(e => new ExtratoIndexViewModel
                {
                    Id = e.Id,
                    Nome = e.Nome,
                    DataCriacao = e.DataCriacao,
                    //IsFavorito = "",
                    DataInicioMovimentacoes = e.Movimentacaos.Min(m => (DateOnly)m.DataMovimentacao),
                    DataFimMovimentacoes = e.Movimentacaos.Max(m => (DateOnly)m.DataMovimentacao),
                    TotalMovimentacoes = e.Movimentacaos.Count()
                })
                .OrderByDescending(e => e.Id)
                .ToList();

            return View(viewModel);
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

                                var categoriaEncontrada = _context.Categoria.FirstOrDefault(c => c.Nome == mov.Categoria.Nome);

                                if (categoriaEncontrada != null)
                                {
                                    mov.Categoria = categoriaEncontrada;
                                }
                                else
                                {
                                    // Lógica para criar uma nova categoria
                                    var novaCategoria = new Categoria
                                    {
                                        Id = 0, // Indica que é uma nova categoria a ser criada
                                        Nome = mov.Categoria.Nome
                                    };
                                    mov.Categoria = novaCategoria;
                                    // _context.Categoria.Add(novaCategoria); // Se o contexto rastrear novas entidades
                                }

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

            // Processe as movimentações
            var idsRecebidos = model.Movimentacoes.Select(m => m.Id).ToList();
            var movimentacoesRemovidas = extrato.Movimentacaos.Where(m => !idsRecebidos.Contains(m.Id)).ToList();
            _movRepository.RemoverVarias(movimentacoesRemovidas);

            // Buscar todas as categorias existentes uma vez só
            var todasCategorias = _context.Categoria.ToList();
            var categoriasMap = new Dictionary<string, Categoria>(StringComparer.OrdinalIgnoreCase);

            // Popula o dicionário de forma segura, evitando duplicações
            foreach (var cat in todasCategorias)
            {
                // Se já existe uma categoria com este nome (case insensitive), não adiciona novamente
                if (!categoriasMap.ContainsKey(cat.Nome.Trim()))
                {
                    categoriasMap[cat.Nome.Trim()] = cat;
                }
            }

            foreach (var mov in model.Movimentacoes)
            {
                if (mov.CategoriaId == 0 && mov.Categoria != null && !string.IsNullOrWhiteSpace(mov.Categoria.Nome))
                {
                    string categoriaNome = mov.Categoria.Nome.Trim();

                    if (categoriasMap.TryGetValue(categoriaNome, out var categoriaExistente))
                    {
                        mov.CategoriaId = categoriaExistente.Id;
                    }
                    else
                    {
                        var novaCategoria = new Categoria { Nome = categoriaNome };
                        _context.Categoria.Add(novaCategoria);
                        await _context.SaveChangesAsync(); // necessário para obter o ID

                        categoriasMap[categoriaNome] = novaCategoria;
                        mov.CategoriaId = novaCategoria.Id;
                    }
                }

                // 🔒 Previne que o EF tente adicionar categoria manualmente
                mov.Categoria = null;

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

            // Salva tudo de uma vez ao final
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
