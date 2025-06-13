using Xunit;
using NSubstitute;
using FluentAssertions;
using STRATFY.Controllers;
using STRATFY.Interfaces.IServices;
using STRATFY.Models;
using STRATFY.DTOs; // Usar os DTOs da API, incluindo DashboardDetailsDTO
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures; // Para ITempDataDictionary
using Microsoft.AspNetCore.Http; // Para DefaultHttpContext
using Microsoft.AspNetCore.Mvc.Rendering; // Para SelectListItem
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims; // Para ClaimsPrincipal, ClaimsIdentity, Claim
using NSubstitute.ExceptionExtensions; // Necessário para ThrowsAsync


public class DashboardsControllerTests
{
    private readonly IDashboardService _mockDashboardService;
    private readonly DashboardsController _controller;
    private readonly ITempDataDictionary _mockTempData;

    public DashboardsControllerTests()
    {
        _mockDashboardService = Substitute.For<IDashboardService>();
        _mockTempData = Substitute.For<ITempDataDictionary>();

        _controller = new DashboardsController(_mockDashboardService);
        _controller.TempData = _mockTempData;

        // Configurar um HttpContext com um usuário autenticado para simular o [Authorize]
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "123"), // Exemplo de ID do usuário
            new Claim(ClaimTypes.Name, "testuser@example.com")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    // --- Métodos Auxiliares para Mocks ---
    private List<SelectListItem> GetMockExtratosAsSelectListItems()
    {
        return new List<SelectListItem>
        {
            new SelectListItem { Value = "1", Text = "Extrato A" },
            new SelectListItem { Value = "2", Text = "Extrato B" },
            new SelectListItem { Value = "10", Text = "Extrato Selecionado" }, // Para testes de Edit
            new SelectListItem { Value = "11", Text = "Outro Extrato" }
        };
    }

    // --- Testes para o método Index (GET) ---

    [Fact]
    public async Task Index_ReturnsViewWithListOfDashboards_WhenSuccessful()
    {
        // Arrange
        var expectedDashboards = new List<Dashboard>
        {
            new Dashboard { Id = 1, Descricao = "Dashboard 1" },
            new Dashboard { Id = 2, Descricao = "Dashboard 2" }
        };
        _mockDashboardService.ObterTodosDashboardsDoUsuarioAsync().Returns(expectedDashboards);

        // Act
        var result = await _controller.Index();

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.Model.Should().BeOfType<List<Dashboard>>();
        viewResult.Model.As<List<Dashboard>>().Should().HaveCount(2).And.BeEquivalentTo(expectedDashboards);
        await _mockDashboardService.Received(1).ObterTodosDashboardsDoUsuarioAsync();
        // Nenhuma mensagem de erro ou sucesso esperada em caso de sucesso
        _mockTempData.DidNotReceiveWithAnyArgs()["ErrorMessage"] = Arg.Any<object>();
        _mockTempData.DidNotReceiveWithAnyArgs()["SuccessMessage"] = Arg.Any<object>();
    }

    [Fact]
    public async Task Index_ReturnsRedirectToLogin_WhenUnauthorizedAccessExceptionOccurs()
    {
        // Arrange
        _mockDashboardService.ObterTodosDashboardsDoUsuarioAsync().ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.Index();

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Index");
        redirectResult.ControllerName.Should().Be("Login");
        // CORREÇÃO: Usando Received para verificar a atribuição
        _mockTempData.Received(1)["ErrorMessage"] = "Você precisa estar logado para acessar os dashboards.";
        await _mockDashboardService.Received(1).ObterTodosDashboardsDoUsuarioAsync();
    }

    [Fact]
    public async Task Index_ReturnsViewWithEmptyListAndSetsErrorMessage_WhenGeneralExceptionOccurs()
    {
        // Arrange
        var errorMessage = "Simulated error loading dashboards.";
        _mockDashboardService.ObterTodosDashboardsDoUsuarioAsync().ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.Index();

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.Model.Should().BeOfType<List<Dashboard>>();
        viewResult.Model.As<List<Dashboard>>().Should().BeEmpty();
        // CORREÇÃO: Usando Received para verificar a atribuição
        _mockTempData.Received(1)["ErrorMessage"] = "Ocorreu um erro ao carregar seus dashboards. " + errorMessage;
        await _mockDashboardService.Received(1).ObterTodosDashboardsDoUsuarioAsync();
    }

    // --- Testes para o método Details (GET) ---

    [Fact]
    public async Task Details_ReturnsNotFound_WhenIdIsNull()
    {
        // Act
        var result = await _controller.Details(null);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        await _mockDashboardService.DidNotReceiveWithAnyArgs().ObterDashboardPorIdAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task Details_ReturnsViewWithDashboard_WhenSuccessful()
    {
        // Arrange
        var dashboardId = 1;
        var expectedDashboard = new Dashboard { Id = dashboardId, Descricao = "Detalhes do Dashboard" };
        _mockDashboardService.ObterDashboardPorIdAsync(dashboardId).Returns(expectedDashboard);

        // Act
        var result = await _controller.Details(dashboardId);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.Model.Should().BeOfType<Dashboard>().And.BeEquivalentTo(expectedDashboard);
        await _mockDashboardService.Received(1).ObterDashboardPorIdAsync(dashboardId);
        // Nenhuma mensagem de erro ou sucesso esperada em caso de sucesso
        _mockTempData.DidNotReceiveWithAnyArgs()["ErrorMessage"] = Arg.Any<object>();
        _mockTempData.DidNotReceiveWithAnyArgs()["SuccessMessage"] = Arg.Any<object>();
    }

    [Fact]
    public async Task Details_ReturnsNotFoundAndSetsErrorMessage_WhenDashboardIsNullFromService()
    {
        // Arrange
        var dashboardId = 1;
        _mockDashboardService.ObterDashboardPorIdAsync(dashboardId).Returns((Dashboard)null);

        // Act
        var result = await _controller.Details(dashboardId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        // CORREÇÃO: Usando Received para verificar a atribuição
        _mockTempData.Received(1)["ErrorMessage"] = "Dashboard não encontrado ou você não tem permissão para visualizá-lo.";
        await _mockDashboardService.Received(1).ObterDashboardPorIdAsync(dashboardId);
    }

    [Fact]
    public async Task Details_ReturnsRedirectToLogin_WhenUnauthorizedAccessExceptionOccurs()
    {
        // Arrange
        var dashboardId = 1;
        _mockDashboardService.ObterDashboardPorIdAsync(dashboardId).ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.Details(dashboardId);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Index");
        redirectResult.ControllerName.Should().Be("Login");
        // CORREÇÃO: Usando Received para verificar a atribuição
        _mockTempData.Received(1)["ErrorMessage"] = "Acesso não autorizado para visualizar este dashboard.";
        await _mockDashboardService.Received(1).ObterDashboardPorIdAsync(dashboardId);
    }

    [Fact]
    public async Task Details_ReturnsNotFoundAndSetsErrorMessage_WhenGeneralExceptionOccurs()
    {
        // Arrange
        var dashboardId = 1;
        var errorMessage = "Simulated error loading details.";
        _mockDashboardService.ObterDashboardPorIdAsync(dashboardId).ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.Details(dashboardId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        // CORREÇÃO: Usando Received para verificar a atribuição
        _mockTempData.Received(1)["ErrorMessage"] = "Ocorreu um erro ao carregar os detalhes do dashboard: " + errorMessage;
        await _mockDashboardService.Received(1).ObterDashboardPorIdAsync(dashboardId);
    }

    // --- Testes para o método Create (GET) ---

    [Fact]
    public async Task CreateGet_ReturnsViewWithDashboardVMAndExtratos()
    {
        // Arrange
        var extratosSelectList = GetMockExtratosAsSelectListItems();
        _mockDashboardService.ObterExtratosDisponiveisParaUsuarioAsync().Returns(extratosSelectList);

        // Act
        var result = await _controller.Create();

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.Model.Should().BeOfType<DashboardVM>();
        var model = viewResult.Model.As<DashboardVM>();
        model.ExtratosDisponiveis.Should().HaveCount(extratosSelectList.Count).And.BeEquivalentTo(extratosSelectList);
        await _mockDashboardService.Received(1).ObterExtratosDisponiveisParaUsuarioAsync();
        // Nenhuma mensagem de erro ou sucesso esperada em caso de sucesso
        _mockTempData.DidNotReceiveWithAnyArgs()["ErrorMessage"] = Arg.Any<object>();
        _mockTempData.DidNotReceiveWithAnyArgs()["SuccessMessage"] = Arg.Any<object>();
    }

    // --- Testes para o método Create (POST) ---

    [Fact]
    public async Task CreatePost_ReturnsRedirectToCriarPadrao_WhenActionIsPadrao()
    {
        // Arrange
        var model = new DashboardVM { Nome = "Dashboard Padrão", ExtratoId = 5 };
        var action = "padrao";

        // Act
        var result = await _controller.Create(model, action);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("CriarPadrao");
        redirectResult.RouteValues["nome"].Should().Be(model.Nome);
        redirectResult.RouteValues["extratoId"].Should().Be(model.ExtratoId);
        await _mockDashboardService.DidNotReceiveWithAnyArgs().CriarDashboardAsync(Arg.Any<DashboardVM>());
        // Nenhuma mensagem de erro ou sucesso esperada neste fluxo
        _mockTempData.DidNotReceiveWithAnyArgs()["ErrorMessage"] = Arg.Any<object>();
        _mockTempData.DidNotReceiveWithAnyArgs()["SuccessMessage"] = Arg.Any<object>();
    }

    [Fact]
    public async Task CreatePost_ReturnsViewWithModelAndRepostsExtratos_WhenModelStateIsInvalid()
    {
        // Arrange
        var model = new DashboardVM { Nome = "" }; // Nome inválido para ModelState
        var extratosSelectList = GetMockExtratosAsSelectListItems();
        _controller.ModelState.AddModelError("Nome", "Nome é obrigatório.");
        _mockDashboardService.ObterExtratosDisponiveisParaUsuarioAsync().Returns(extratosSelectList);

        // Act
        var result = await _controller.Create(model, "normal");

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        // Ao comparar o modelo, excluímos ExtratosDisponiveis pois ele é repopulado
        viewResult.Model.Should().BeOfType<DashboardVM>().And.BeEquivalentTo(model, options => options.Excluding(x => x.ExtratosDisponiveis));
        viewResult.Model.As<DashboardVM>().ExtratosDisponiveis.Should().BeEquivalentTo(extratosSelectList); // Verificar ExtratosDisponiveis separadamente
        viewResult.ViewData.ModelState.IsValid.Should().BeFalse();
        await _mockDashboardService.Received(1).ObterExtratosDisponiveisParaUsuarioAsync();
        await _mockDashboardService.DidNotReceiveWithAnyArgs().CriarDashboardAsync(Arg.Any<DashboardVM>());
        // Nenhuma mensagem de sucesso esperada, mas talvez ErrorMessage possa ser setada pelo ModelState.
        _mockTempData.DidNotReceiveWithAnyArgs()["SuccessMessage"] = Arg.Any<object>();
    }
}