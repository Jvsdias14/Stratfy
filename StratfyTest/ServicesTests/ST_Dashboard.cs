using NSubstitute;
using FluentAssertions;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using STRATFY.Models;
using STRATFY.Interfaces.IRepositories;
using STRATFY.Services;
using STRATFY.Interfaces.IContexts;
using STRATFY.DTOs; // Garanta que este using está presente
using Microsoft.AspNetCore.Mvc.Rendering; // Para SelectListItem

namespace StratfyTest.ServicesTests
{
    public class ST_Dashboard
    {
        private readonly DashboardService _dashboardService;
        private readonly IRepositoryDashboard _mockDashboardRepository;
        private readonly IRepositoryExtrato _mockExtratoRepository;
        private readonly IUsuarioContexto _mockUsuarioContexto;

        private const int TestUserId = 1;

        public ST_Dashboard()
        {
            _mockDashboardRepository = Substitute.For<IRepositoryDashboard>();
            _mockExtratoRepository = Substitute.For<IRepositoryExtrato>();
            _mockUsuarioContexto = Substitute.For<IUsuarioContexto>();

            _dashboardService = new DashboardService(
                _mockDashboardRepository,
                _mockExtratoRepository,
                _mockUsuarioContexto);

            // Configuração padrão para o usuário logado
            _mockUsuarioContexto.ObterUsuarioId().Returns(TestUserId);
        }

        // --- Testes para GetUsuarioId() (Método auxiliar, testado implicitamente, mas pode ter um teste direto) ---
        [Fact]
        public void GetUsuarioId_ShouldReturnUserId_WhenUserIsAuthenticated()
        {
            // Arrange (já configurado no construtor)
            // Act
            var userId = CallPrivateGetUsuarioId(); // Chama o método privado via reflexão ou um wrapper público para teste

            // Assert
            userId.Should().Be(TestUserId);
            _mockUsuarioContexto.Received(1).ObterUsuarioId();
        }


        // Helper para chamar o método privado GetUsuarioId()
        private int CallPrivateGetUsuarioId()
        {
            var method = typeof(DashboardService).GetMethod("GetUsuarioId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (int)method.Invoke(_dashboardService, null);
        }


        // --- Testes para ObterTodosDashboardsDoUsuarioAsync ---
        [Fact]
        public async Task ObterTodosDashboardsDoUsuarioAsync_ShouldReturnDashboards_ForAuthenticatedUser()
        {
            // Arrange
            var dashboards = new List<Dashboard> { new Dashboard { Id = 1, Descricao = "Meu Dashboard" } };
            _mockDashboardRepository.SelecionarTodosDoUsuarioAsync(TestUserId).Returns(dashboards);

            // Act
            var result = await _dashboardService.ObterTodosDashboardsDoUsuarioAsync();

            // Assert
            result.Should().BeEquivalentTo(dashboards);
            await _mockDashboardRepository.Received(1).SelecionarTodosDoUsuarioAsync(TestUserId);
            _mockUsuarioContexto.Received(1).ObterUsuarioId();
        }

        [Fact]
        public async Task ObterTodosDashboardsDoUsuarioAsync_ShouldThrowUnauthorizedAccessException_WhenUserIsNotAuthenticated()
        {
            // Arrange
            _mockUsuarioContexto.ObterUsuarioId().Returns(0);

            // Act
            Func<Task> act = async () => await _dashboardService.ObterTodosDashboardsDoUsuarioAsync();

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>();
            await _mockDashboardRepository.DidNotReceive().SelecionarTodosDoUsuarioAsync(Arg.Any<int>());
        }

        // --- Testes para ObterDashboardPorIdAsync ---
        [Fact]
        public async Task ObterDashboardPorIdAsync_ShouldReturnDashboard_WhenFoundAndBelongsToUser()
        {
            // Arrange
            var dashboardId = 10;
            var extrato = new Extrato { Id = 1, UsuarioId = TestUserId };
            var dashboard = new Dashboard { Id = dashboardId, Descricao = "Dashboard Teste", Extrato = extrato };
            _mockDashboardRepository.SelecionarDashboardCompletoPorIdAsync(dashboardId).Returns(dashboard);

            // Act
            var result = await _dashboardService.ObterDashboardPorIdAsync(dashboardId);

            // Assert
            result.Should().BeEquivalentTo(dashboard);
            await _mockDashboardRepository.Received(1).SelecionarDashboardCompletoPorIdAsync(dashboardId);
            _mockUsuarioContexto.Received(1).ObterUsuarioId();
        }

        [Fact]
        public async Task ObterDashboardPorIdAsync_ShouldReturnNull_WhenDashboardNotFound()
        {
            // Arrange
            var dashboardId = 10;
            _mockDashboardRepository.SelecionarDashboardCompletoPorIdAsync(dashboardId).Returns((Dashboard)null);

            // Act
            var result = await _dashboardService.ObterDashboardPorIdAsync(dashboardId);

            // Assert
            result.Should().BeNull();
            await _mockDashboardRepository.Received(1).SelecionarDashboardCompletoPorIdAsync(dashboardId);
            _mockUsuarioContexto.Received(1).ObterUsuarioId();
        }

        [Fact]
        public async Task ObterDashboardPorIdAsync_ShouldReturnNull_WhenDashboardDoesNotBelongToUser()
        {
            // Arrange
            var dashboardId = 10;
            var extrato = new Extrato { Id = 1, UsuarioId = 999 }; // Outro usuário
            var dashboard = new Dashboard { Id = dashboardId, Descricao = "Dashboard Alheio", Extrato = extrato };
            _mockDashboardRepository.SelecionarDashboardCompletoPorIdAsync(dashboardId).Returns(dashboard);

            // Act
            var result = await _dashboardService.ObterDashboardPorIdAsync(dashboardId);

            // Assert
            result.Should().BeNull();
            await _mockDashboardRepository.Received(1).SelecionarDashboardCompletoPorIdAsync(dashboardId);
            _mockUsuarioContexto.Received(1).ObterUsuarioId();
        }

        [Fact]
        public async Task ObterDashboardPorIdAsync_ShouldThrowUnauthorizedAccessException_WhenUserIsNotAuthenticated()
        {
            // Arrange
            _mockUsuarioContexto.ObterUsuarioId().Returns(0);
            var dashboardId = 10;

            // Act
            Func<Task> act = async () => await _dashboardService.ObterDashboardPorIdAsync(dashboardId);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>();
            await _mockDashboardRepository.DidNotReceive().SelecionarDashboardCompletoPorIdAsync(Arg.Any<int>());
        }

        // --- Testes para CriarDashboardAsync ---


        [Fact]
        public async Task CriarDashboardAsync_ShouldThrowApplicationException_WhenExtratoNotFound()
        {
            // Arrange
            var extratoId = 100;
            var dashboardVm = new DashboardVM { Nome = "Novo Dashboard", ExtratoId = extratoId };
            _mockExtratoRepository.SelecionarChaveAsync(Arg.Any<object[]>()).Returns((Extrato)null);

            // Act
            Func<Task> act = async () => await _dashboardService.CriarDashboardAsync(dashboardVm);

            // Assert
            await act.Should().ThrowAsync<ApplicationException>()
                .WithMessage("O extrato selecionado não é válido ou não pertence ao usuário.");
            _mockUsuarioContexto.Received(1).ObterUsuarioId();
            await _mockExtratoRepository.Received(1).SelecionarChaveAsync(Arg.Is<object[]>(args => (int)args[0] == extratoId));
            await _mockDashboardRepository.DidNotReceive().IncluirAsync(Arg.Any<Dashboard>());
            _mockDashboardRepository.DidNotReceive().Salvar();
        }
    }
}

