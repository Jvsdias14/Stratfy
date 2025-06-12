using STRATFY.Interfaces.IRepositories;
using STRATFY.Interfaces.IServices;
using STRATFY.Interfaces.IContexts;
using STRATFY.Models;
using STRATFY.DTOs; // Importantíssimo para os DTOs!
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace STRATFY.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IRepositoryDashboard _dashboardRepository;
        private readonly IRepositoryExtrato _extratoRepository;
        private readonly IUsuarioContexto _usuarioContexto;

        public DashboardService(
            IRepositoryDashboard dashboardRepository,
            IRepositoryExtrato extratoRepository,
            IUsuarioContexto usuarioContexto)
        {
            _dashboardRepository = dashboardRepository;
            _extratoRepository = extratoRepository;
            _usuarioContexto = usuarioContexto;
        }

        private int GetUsuarioId() // Método corrigido
        {
            var userId = _usuarioContexto.ObterUsuarioId();
            if (userId <= 0)
            {
                throw new UnauthorizedAccessException("Usuário não autenticado ou ID inválido.");
            }
            return userId;
        }

        public async Task<List<Dashboard>> ObterTodosDashboardsDoUsuarioAsync()
        {
            var userId = GetUsuarioId();
            return await _dashboardRepository.SelecionarTodosDoUsuarioAsync(userId);
        }

        public async Task<Dashboard> ObterDashboardPorIdAsync(int dashboardId)
        {
            var userId = GetUsuarioId(); // Chamada corrigida
            var dashboard = await _dashboardRepository.SelecionarDashboardCompletoPorIdAsync(dashboardId);

            if (dashboard == null || dashboard.Extrato?.UsuarioId != userId)
            {
                return null; // Ou lançar uma exceção de acesso negado/não encontrado
            }
            return dashboard;
        }

        public async Task<Dashboard> CriarDashboardAsync(DashboardVM model)
        {
            var userId = GetUsuarioId();

            var extrato = await _extratoRepository.SelecionarChaveAsync(new object[] { model.ExtratoId });
            if (extrato == null || extrato.UsuarioId != userId)
            {
                throw new ApplicationException("O extrato selecionado não é válido ou não pertence ao usuário.");
            }

            var dashboard = new Dashboard
            {
                Descricao = model.Nome,
                ExtratoId = model.ExtratoId,
                Graficos = new List<Grafico>(),
                Cartoes = new List<Cartao>()
            };

            await _dashboardRepository.IncluirAsync(dashboard);
            _dashboardRepository.Salvar();

            return dashboard;
        }

        public async Task<Dashboard> CriarDashboardPadraoAsync(string nome, int extratoId)
        {
            var userId = GetUsuarioId();

            var extrato = await _extratoRepository.SelecionarChaveAsync(new object[] { extratoId });
            if (extrato == null || extrato.UsuarioId != userId)
            {
                throw new ApplicationException("O extrato selecionado para o dashboard padrão não é válido ou não pertence ao usuário.");
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

            await _dashboardRepository.IncluirAsync(dashboard);
            _dashboardRepository.Salvar();

            return dashboard;
        }

        public async Task AtualizarDashboardAsync(DashboardVM model)
        {
            var userId = GetUsuarioId();

            var dashboard = await _dashboardRepository.SelecionarDashboardCompletoPorIdAsync(model.Id);

            if (dashboard == null || dashboard.Extrato?.UsuarioId != userId)
            {
                throw new ApplicationException("Dashboard não encontrado ou você não tem permissão para editá-lo.");
            }

            if (dashboard.ExtratoId != model.ExtratoId)
            {
                var novoExtrato = await _extratoRepository.SelecionarChaveAsync(new object[] { model.ExtratoId });
                if (novoExtrato == null || novoExtrato.UsuarioId != userId)
                {
                    throw new ApplicationException("O novo extrato selecionado não é válido ou não pertence ao usuário.");
                }
            }

            dashboard.Descricao = model.Nome;
            dashboard.ExtratoId = model.ExtratoId;

            dashboard.Graficos.Clear();
            if (model.Graficos != null)
            {
                foreach (var grafico in model.Graficos)
                {
                    grafico.DashboardId = dashboard.Id;
                    dashboard.Graficos.Add(grafico);
                }
            }

            dashboard.Cartoes.Clear();
            if (model.Cartoes != null)
            {
                foreach (var cartao in model.Cartoes)
                {
                    cartao.DashboardId = dashboard.Id;
                    dashboard.Cartoes.Add(cartao);
                }
            }

            await _dashboardRepository.AlterarAsync(dashboard);
            _dashboardRepository.Salvar();
        }

        public async Task ExcluirDashboardAsync(int dashboardId)
        {
            var userId = GetUsuarioId();
            var dashboard = await _dashboardRepository.SelecionarDashboardCompletoPorIdAsync(dashboardId);

            if (dashboard == null || dashboard.Extrato?.UsuarioId != userId)
            {
                throw new ApplicationException("Dashboard não encontrado ou você não tem permissão para excluí-lo.");
            }

            await _dashboardRepository.ExcluirAsync(dashboard);
            _dashboardRepository.Salvar();
        }

        public async Task<DashboardDetailsDTO> ObterDadosDashboardParaApiAsync(int dashboardId)
        {
            // 1. REMOVA ESTA LINHA:
            // var userId = GetUsuarioId(); // Esta linha lançava a UnauthorizedAccessException

            var dashboard = await _dashboardRepository.SelecionarDashboardCompletoPorIdAsync(dashboardId);

            // 2. SIMPLIFIQUE ESTA VALIDAÇÃO:
            // Antes: if (dashboard == null || dashboard.Extrato?.UsuarioId != userId)
            if (dashboard == null)
            {
                // Agora, simplesmente não foi encontrado um dashboard com esse ID.
                throw new ApplicationException("Dashboard não encontrado.");
            }

            // O restante da lógica de mapeamento para DTO permanece o mesmo
            return new DashboardDetailsDTO
            {
                Id = dashboard.Id,
                Descricao = dashboard.Descricao,
                ExtratoNome = dashboard.Extrato?.Nome,
                Movimentacoes = dashboard.Extrato?.Movimentacaos?.Select(m => new MovimentacaoDTO
                {
                    DataMovimentacao = m.DataMovimentacao,
                    Descricao = m.Descricao,
                    Valor = m.Valor,
                    Tipo = m.Tipo,
                    Categoria = m.Categoria?.Nome
                }) ?? Enumerable.Empty<MovimentacaoDTO>(),
                Graficos = dashboard.Graficos?.Select(g => new GraficoDTO
                {
                    Titulo = g.Titulo,
                    Campo1 = g.Campo1,
                    Campo2 = g.Campo2,
                    Tipo = g.Tipo,
                    Cor = g.Cor,
                    AtivarLegenda = g.AtivarLegenda
                }) ?? Enumerable.Empty<GraficoDTO>(),
                Cartoes = dashboard.Cartoes?.Select(c => new CartaoDTO
                {
                    Nome = c.Nome,
                    Campo = c.Campo,
                    TipoAgregacao = c.TipoAgregacao,
                    Cor = c.Cor
                }) ?? Enumerable.Empty<CartaoDTO>()
            };
        }


        public async Task<List<SelectListItem>> ObterExtratosDisponiveisParaUsuarioAsync()
        {
            var userId = GetUsuarioId();
            var extratos = await _extratoRepository.SelecionarTodosDoUsuarioAsync(userId);
            return extratos.Select(e => new SelectListItem
            {
                Value = e.Id.ToString(),
                Text = e.Nome
            }).ToList();
        }
    }
}