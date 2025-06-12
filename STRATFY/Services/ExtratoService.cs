using System.Text.Json;
using Microsoft.AspNetCore.Http;
using STRATFY.Models;
using STRATFY.Interfaces.IContexts; // Adicione este using
using STRATFY.Interfaces.IRepositories;
using STRATFY.Interfaces.IServices;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System; // Para ApplicationException, UnauthorizedAccessException
using System.IO; // Para Stream

namespace STRATFY.Services
{
    public class ExtratoService : IExtratoService
    {
        private readonly IRepositoryExtrato _extratoRepository;
        private readonly IMovimentacaoService _movimentacaoService;
        private readonly ICategoriaService _categoriaService; // Mantido para caso haja uso indireto ou futuro na service de Extrato
        private readonly ICsvExportService _csvExportService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IUsuarioContexto _usuarioContexto; // Injetar IUsuarioContexto

        public ExtratoService(
            IRepositoryExtrato extratoRepository,
            IMovimentacaoService movimentacaoService,
            ICategoriaService categoriaService,
            ICsvExportService csvExportService,
            IHttpClientFactory httpClientFactory,
            IUsuarioContexto usuarioContexto)
        {
            _extratoRepository = extratoRepository;
            _movimentacaoService = movimentacaoService;
            _categoriaService = categoriaService;
            _csvExportService = csvExportService;
            _httpClientFactory = httpClientFactory;
            _usuarioContexto = usuarioContexto; // Atribuir
        }

        public async Task<List<ExtratoIndexViewModel>> ObterExtratosDoUsuarioParaIndexAsync()
        {
            var userId = _usuarioContexto.ObterUsuarioId();
            if (userId <= 0)
            {
                throw new UnauthorizedAccessException("Usuário não autenticado ou ID inválido para obter extratos.");
            }

            var extratos = await _extratoRepository.SelecionarTodosDoUsuarioAsync(userId);

            var viewModel = extratos
                .Select(e => new ExtratoIndexViewModel
                {
                    Id = e.Id,
                    Nome = e.Nome,
                    DataCriacao = e.DataCriacao,
                    DataInicioMovimentacoes = e.Movimentacaos != null && e.Movimentacaos.Any() ? e.Movimentacaos.Min(m => (DateOnly?)m.DataMovimentacao) : null,
                    DataFimMovimentacoes = e.Movimentacaos != null && e.Movimentacaos.Any() ? e.Movimentacaos.Max(m => (DateOnly?)m.DataMovimentacao) : null,
                    TotalMovimentacoes = e.Movimentacaos != null ? e.Movimentacaos.Count() : 0
                })
                .OrderByDescending(e => e.Id)
                .ToList();

            return viewModel;
        }

        public async Task<Extrato> ObterExtratoDetalhesAsync(int extratoId)
        {
            var userId = _usuarioContexto.ObterUsuarioId();
            if (userId <= 0)
            {
                throw new UnauthorizedAccessException("Usuário não autenticado ou ID inválido para obter detalhes do extrato.");
            }

            var extrato = _extratoRepository.CarregarExtratoCompleto(extratoId);

            // Validação de segurança: garantir que o extrato pertence ao usuário logado
            if (extrato == null || extrato.UsuarioId != userId)
            {
                // É importante que a Controller lide com essa exceção para retornar 404/403
                throw new ApplicationException($"Extrato com ID {extratoId} não encontrado ou acesso negado.");
            }
            return extrato;
        }

        public async Task<int> CriarExtratoComMovimentacoesAsync(Extrato extrato, IFormFile csvFile)
        {
            var userId = _usuarioContexto.ObterUsuarioId();
            if (userId <= 0)
            {
                throw new UnauthorizedAccessException("Usuário não autenticado ou ID inválido para criar extrato.");
            }

            extrato.UsuarioId = userId; // Definir o ID do usuário aqui na Service
            extrato.DataCriacao = DateOnly.FromDateTime(DateTime.Now);

            // Incluir o extrato e salvar imediatamente para obter o ID
            await _extratoRepository.IncluirAsync(extrato);
            _extratoRepository.Salvar(); // Salva o extrato para que extrato.Id seja populado

            if (csvFile != null && csvFile.Length > 0)
            {
                using var memoryStream = new MemoryStream();
                await csvFile.CopyToAsync(memoryStream);
                var byteArrayContent = new ByteArrayContent(memoryStream.ToArray());

                using var httpClient = _httpClientFactory.CreateClient();
                using var form = new MultipartFormDataContent();
                form.Add(byteArrayContent, "file", csvFile.FileName);

                var response = await httpClient.PostAsync("http://localhost:8000/api/uploadcsv", form);

                if (!response.IsSuccessStatusCode)
                {
                    var erro = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Erro ao enviar CSV para API: " + erro);
                    throw new ApplicationException($"Falha ao processar CSV na API externa: {erro}");
                }

                var content = await response.Content.ReadAsStringAsync();
                var movimentacoes = JsonSerializer.Deserialize<List<Movimentacao>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (movimentacoes != null && movimentacoes.Any())
                {
                    await _movimentacaoService.ImportarMovimentacoesDoCsvAsync(movimentacoes, extrato.Id);
                }
            }

            return extrato.Id;
        }

        public async Task<ExtratoEdicaoViewModel> ObterExtratoParaEdicaoAsync(int extratoId)
        {
            var userId = _usuarioContexto.ObterUsuarioId();
            if (userId <= 0)
            {
                throw new UnauthorizedAccessException("Usuário não autenticado ou ID inválido para edição do extrato.");
            }

            var extrato = _extratoRepository.CarregarExtratoCompleto(extratoId);

            // Validação de segurança
            if (extrato == null || extrato.UsuarioId != userId)
            {
                throw new ApplicationException($"Extrato com ID {extratoId} não encontrado ou acesso negado.");
            }

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
                    Categoria = m.Categoria, // Categoria é carregada pelo CarregarExtratoCompleto
                    ExtratoId = m.ExtratoId,
                    DataMovimentacao = m.DataMovimentacao
                }).ToList()
            };

            return viewModel;
        }

        public async Task AtualizarExtratoEMovimentacoesAsync(ExtratoEdicaoViewModel model)
        {
            var userId = _usuarioContexto.ObterUsuarioId();
            if (userId <= 0)
            {
                throw new UnauthorizedAccessException("Usuário não autenticado ou ID inválido para atualizar extrato.");
            }

            var extrato = _extratoRepository.CarregarExtratoCompleto(model.ExtratoId);
            if (extrato == null || extrato.UsuarioId != userId) // Validação de segurança
            {
                throw new ApplicationException($"Extrato com ID {model.ExtratoId} não encontrado ou acesso negado.");
            }

            extrato.Nome = model.NomeExtrato;

            // DELEGANDO para a MovimentacaoService:
            // Obter as movimentações existentes diretamente do extrato carregado para garantir que estejam tracked
            await _movimentacaoService.AtualizarMovimentacoesDoExtratoAsync(model.Movimentacoes.ToList(), extrato.Movimentacaos.ToList(), model.ExtratoId);

            // Salva apenas as alterações do extrato (nome), as movimentações serão salvas pela MovimentacaoService
            _extratoRepository.Salvar();
        }

        public async Task<bool> ExcluirExtratoAsync(int extratoId)
        {
            var userId = _usuarioContexto.ObterUsuarioId();
            if (userId <= 0)
            {
                throw new UnauthorizedAccessException("Usuário não autenticado ou ID inválido para excluir extrato.");
            }

            var extrato = _extratoRepository.SelecionarChave(extratoId);
            if (extrato == null || extrato.UsuarioId != userId) // Validação de segurança
            {
                // Se não encontrar ou não pertencer ao usuário, a exclusão falha silenciosamente ou lança erro.
                // Para consistência com outros métodos, lançar uma exceção é mais robusto.
                throw new ApplicationException($"Extrato com ID {extratoId} não encontrado ou acesso negado para exclusão.");
            }

            try
            {
                _extratoRepository.Excluir(extrato);
                _extratoRepository.Salvar(); // Salva a exclusão
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao excluir extrato {extratoId}: {ex.Message}");
                return false;
            }
        }

        public async Task<Stream> ExportarMovimentacoesDoExtratoParaCsvAsync(int extratoId)
        {
            var userId = _usuarioContexto.ObterUsuarioId();
            if (userId <= 0)
            {
                throw new UnauthorizedAccessException("Usuário não autenticado ou ID inválido para exportar extrato.");
            }

            var extrato = _extratoRepository.CarregarExtratoCompleto(extratoId);

            if (extrato == null || extrato.UsuarioId != userId) // Validação de segurança
            {
                throw new ApplicationException($"Extrato com ID {extratoId} não encontrado ou acesso negado para exportação.");
            }

            if (extrato.Movimentacaos == null || !extrato.Movimentacaos.Any())
            {
                return await _csvExportService.ExportMovimentacoesToCsvAsync(new List<Movimentacao>());
            }

            var csvStream = await _csvExportService.ExportMovimentacoesToCsvAsync(extrato.Movimentacaos);
            return csvStream;
        }
    }
}