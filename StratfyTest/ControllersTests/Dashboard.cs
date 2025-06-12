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

    [Fact]
    public async Task CreatePost_ReturnsRedirectToEdit_WhenSuccessful()
    {
        // Arrange
        var model = new DashboardVM { Nome = "Novo Dashboard", ExtratoId = 1 };
        var createdDashboard = new Dashboard { Id = 10, Descricao = "Novo Dashboard", ExtratoId = 1 };
        _mockDashboardService.CriarDashboardAsync(model).Returns(createdDashboard);
        // REMOVIDO: _mockTempData["SuccessMessage"] = null;

        // Act
        var result = await _controller.Create(model, "normal");

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Edit");
        redirectResult.RouteValues["id"].Should().Be(createdDashboard.Id);
        // CORREÇÃO: Usando Received para verificar a atribuição
        _mockTempData.Received(1)["SuccessMessage"] = "Dashboard criado com sucesso!";
        await _mockDashboardService.Received(1).CriarDashboardAsync(model);
    }

    [Fact]
    public async Task CreatePost_ReturnsViewWithModelAndApplicationError_WhenApplicationExceptionOccurs()
    {
        // Arrange
        var model = new DashboardVM { Nome = "Dashboard Existente", ExtratoId = 1 };
        var errorMessage = "Dashboard com este nome já existe.";
        var extratosSelectList = GetMockExtratosAsSelectListItems();
        _mockDashboardService.CriarDashboardAsync(model).ThrowsAsync(new ApplicationException(errorMessage));
        _mockDashboardService.ObterExtratosDisponiveisParaUsuarioAsync().Returns(extratosSelectList);

        // Act
        var result = await _controller.Create(model, "normal");

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.Model.Should().BeOfType<DashboardVM>().And.BeEquivalentTo(model, options => options.Excluding(x => x.ExtratosDisponiveis));
        viewResult.Model.As<DashboardVM>().ExtratosDisponiveis.Should().BeEquivalentTo(extratosSelectList);
        viewResult.ViewData.ModelState.IsValid.Should().BeFalse();
        // O ModelState é verificado diretamente, não via TempData para este erro.
        viewResult.ViewData.ModelState.Should().ContainSingle(m => m.Value.Errors.Any(e => e.ErrorMessage == errorMessage));
        await _mockDashboardService.Received(1).CriarDashboardAsync(model);
        await _mockDashboardService.Received(1).ObterExtratosDisponiveisParaUsuarioAsync();
        // Nenhuma mensagem de sucesso ou erro explícita via TempData para este fluxo.
        _mockTempData.DidNotReceiveWithAnyArgs()["ErrorMessage"] = Arg.Any<object>();
        _mockTempData.DidNotReceiveWithAnyArgs()["SuccessMessage"] = Arg.Any<object>();
    }

    [Fact]
    public async Task CreatePost_ReturnsRedirectToLogin_WhenUnauthorizedAccessExceptionOccurs()
    {
        // Arrange
        var model = new DashboardVM { Nome = "Dashboard Teste", ExtratoId = 1 };
        _mockDashboardService.CriarDashboardAsync(model).ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.Create(model, "normal");

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Index");
        redirectResult.ControllerName.Should().Be("Login");
        // CORREÇÃO: Usando Received para verificar a atribuição
        _mockTempData.Received(1)["ErrorMessage"] = "Você não tem permissão para realizar esta ação.";
        await _mockDashboardService.Received(1).CriarDashboardAsync(model);
    }

    [Fact]
    public async Task CreatePost_ReturnsViewWithModelAndGenericError_WhenGeneralExceptionOccurs()
    {
        // Arrange
        var model = new DashboardVM { Nome = "Dashboard Teste", ExtratoId = 1 };
        var errorMessage = "Erro inesperado ao criar dashboard.";
        var extratosSelectList = GetMockExtratosAsSelectListItems();
        _mockDashboardService.CriarDashboardAsync(model).ThrowsAsync(new Exception(errorMessage));
        _mockDashboardService.ObterExtratosDisponiveisParaUsuarioAsync().Returns(extratosSelectList);

        // Act
        var result = await _controller.Create(model, "normal");

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.Model.Should().BeOfType<DashboardVM>().And.BeEquivalentTo(model, options => options.Excluding(x => x.ExtratosDisponiveis));
        viewResult.Model.As<DashboardVM>().ExtratosDisponiveis.Should().BeEquivalentTo(extratosSelectList);
        viewResult.ViewData.ModelState.IsValid.Should().BeFalse();
        // Verifique a mensagem genérica que a controller adiciona ao ModelState.
        viewResult.ViewData.ModelState.Should().ContainSingle(m => m.Value.Errors.Any(e => e.ErrorMessage == "Ocorreu um erro inesperado ao criar o dashboard."));
        await _mockDashboardService.Received(1).CriarDashboardAsync(model);
        await _mockDashboardService.Received(1).ObterExtratosDisponiveisParaUsuarioAsync();
        // Nenhuma mensagem de erro ou sucesso explícita via TempData para este fluxo.
        _mockTempData.DidNotReceiveWithAnyArgs()["ErrorMessage"] = Arg.Any<object>();
        _mockTempData.DidNotReceiveWithAnyArgs()["SuccessMessage"] = Arg.Any<object>();
    }

    // --- Testes para o método CriarPadrao (GET) ---

    [Theory]
    [InlineData("", 1, "Preencha todos os campos obrigatórios.")] // Nome vazio
    [InlineData("Meu Dash", 0, "Preencha todos os campos obrigatórios.")] // ExtratoId inválido
    [InlineData(" ", 1, "Preencha todos os campos obrigatórios.")] // Nome em branco
    public async Task CriarPadrao_ReturnsCreateViewWithModelAndError_WhenInvalidInput(string nome, int extratoId, string expectedErrorMessage)
    {
        // Arrange
        var extratosSelectList = GetMockExtratosAsSelectListItems();
        _mockDashboardService.ObterExtratosDisponiveisParaUsuarioAsync().Returns(extratosSelectList);

        // Act
        var result = await _controller.CriarPadrao(nome, extratoId);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.ViewName.Should().Be("Create");
        viewResult.Model.Should().BeOfType<DashboardVM>();
        var model = viewResult.Model.As<DashboardVM>();
        model.Nome.Should().Be(nome);
        model.ExtratoId.Should().Be(extratoId);
        model.ExtratosDisponiveis.Should().BeEquivalentTo(extratosSelectList);
        viewResult.ViewData.ModelState.IsValid.Should().BeFalse();
        viewResult.ViewData.ModelState.Should().ContainSingle(m => m.Value.Errors.Any(e => e.ErrorMessage == expectedErrorMessage));
        await _mockDashboardService.Received(1).ObterExtratosDisponiveisParaUsuarioAsync();
        await _mockDashboardService.DidNotReceiveWithAnyArgs().CriarDashboardPadraoAsync(Arg.Any<string>(), Arg.Any<int>());
        // Nenhuma mensagem de erro ou sucesso explícita via TempData para este fluxo.
        _mockTempData.DidNotReceiveWithAnyArgs()["ErrorMessage"] = Arg.Any<object>();
        _mockTempData.DidNotReceiveWithAnyArgs()["SuccessMessage"] = Arg.Any<object>();
    }

    [Fact]
    public async Task CriarPadrao_ReturnsRedirectToDetails_WhenSuccessful()
    {
        // Arrange
        var nome = "Dashboard Padrão Teste";
        var extratoId = 1;
        var createdDashboard = new Dashboard { Id = 20, Descricao = nome, ExtratoId = extratoId };
        _mockDashboardService.CriarDashboardPadraoAsync(nome, extratoId).Returns(createdDashboard);
        // REMOVIDO: _mockTempData["SuccessMessage"] = null;

        // Act
        var result = await _controller.CriarPadrao(nome, extratoId);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Details");
        redirectResult.RouteValues["id"].Should().Be(createdDashboard.Id);
        // CORREÇÃO: Usando Received para verificar a atribuição
        _mockTempData.Received(1)["SuccessMessage"] = "Dashboard padrão criado com sucesso!";
        await _mockDashboardService.Received(1).CriarDashboardPadraoAsync(nome, extratoId);
    }

    [Fact]
    public async Task CriarPadrao_ReturnsCreateViewWithModelAndApplicationError_WhenApplicationExceptionOccurs()
    {
        // Arrange
        var nome = "Dashboard Existente";
        var extratoId = 1;
        var errorMessage = "Dashboard padrão com este nome já existe.";
        var extratosSelectList = GetMockExtratosAsSelectListItems();
        _mockDashboardService.CriarDashboardPadraoAsync(nome, extratoId).ThrowsAsync(new ApplicationException(errorMessage));
        _mockDashboardService.ObterExtratosDisponiveisParaUsuarioAsync().Returns(extratosSelectList);

        // Act
        var result = await _controller.CriarPadrao(nome, extratoId);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.ViewName.Should().Be("Create");
        viewResult.Model.Should().BeOfType<DashboardVM>().And.BeEquivalentTo(new DashboardVM { Nome = nome, ExtratoId = extratoId }, options => options.Excluding(x => x.ExtratosDisponiveis));
        viewResult.Model.As<DashboardVM>().ExtratosDisponiveis.Should().BeEquivalentTo(extratosSelectList);
        viewResult.ViewData.ModelState.IsValid.Should().BeFalse();
        viewResult.ViewData.ModelState.Should().ContainSingle(m => m.Value.Errors.Any(e => e.ErrorMessage == errorMessage));
        await _mockDashboardService.Received(1).CriarDashboardPadraoAsync(nome, extratoId);
        await _mockDashboardService.Received(1).ObterExtratosDisponiveisParaUsuarioAsync();
        // Nenhuma mensagem de erro ou sucesso explícita via TempData para este fluxo.
        _mockTempData.DidNotReceiveWithAnyArgs()["ErrorMessage"] = Arg.Any<object>();
        _mockTempData.DidNotReceiveWithAnyArgs()["SuccessMessage"] = Arg.Any<object>();
    }

    [Fact]
    public async Task CriarPadrao_ReturnsRedirectToLogin_WhenUnauthorizedAccessExceptionOccurs()
    {
        // Arrange
        var nome = "Dashboard Teste";
        var extratoId = 1;
        _mockDashboardService.CriarDashboardPadraoAsync(nome, extratoId).ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.CriarPadrao(nome, extratoId);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Index");
        redirectResult.ControllerName.Should().Be("Login");
        // CORREÇÃO: Usando Received para verificar a atribuição
        _mockTempData.Received(1)["ErrorMessage"] = "Você não tem permissão para realizar esta ação.";
        await _mockDashboardService.Received(1).CriarDashboardPadraoAsync(nome, extratoId);
    }

    [Fact]
    public async Task CriarPadrao_ReturnsCreateViewWithModelAndGenericError_WhenGeneralExceptionOccurs()
    {
        // Arrange
        var nome = "Dashboard Teste";
        var extratoId = 1;
        var errorMessage = "Erro inesperado ao criar dashboard padrão.";
        var extratosSelectList = GetMockExtratosAsSelectListItems();
        _mockDashboardService.CriarDashboardPadraoAsync(nome, extratoId).ThrowsAsync(new Exception(errorMessage));
        _mockDashboardService.ObterExtratosDisponiveisParaUsuarioAsync().Returns(extratosSelectList);

        // Act
        var result = await _controller.CriarPadrao(nome, extratoId);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.ViewName.Should().Be("Create");
        viewResult.Model.Should().BeOfType<DashboardVM>().And.BeEquivalentTo(new DashboardVM { Nome = nome, ExtratoId = extratoId }, options => options.Excluding(x => x.ExtratosDisponiveis));
        viewResult.Model.As<DashboardVM>().ExtratosDisponiveis.Should().BeEquivalentTo(extratosSelectList);
        viewResult.ViewData.ModelState.IsValid.Should().BeFalse();
        viewResult.ViewData.ModelState.Should().ContainSingle(m => m.Value.Errors.Any(e => e.ErrorMessage == "Ocorreu um erro inesperado ao criar o dashboard padrão."));
        await _mockDashboardService.Received(1).CriarDashboardPadraoAsync(nome, extratoId);
        await _mockDashboardService.Received(1).ObterExtratosDisponiveisParaUsuarioAsync();
        // Nenhuma mensagem de erro ou sucesso explícita via TempData para este fluxo.
        _mockTempData.DidNotReceiveWithAnyArgs()["ErrorMessage"] = Arg.Any<object>();
        _mockTempData.DidNotReceiveWithAnyArgs()["SuccessMessage"] = Arg.Any<object>();
    }

    // --- Testes para o método GetDashboardData (GET API) ---

    [Fact]
    public async Task GetDashboardData_ReturnsOkWithDashboardDetailsDTO_WhenSuccessful()
    {
        // Arrange
        var dashboardId = 1;
        // Mock de MovimentacaoDTO, GraficoDTO e CartaoDTO conforme a estrutura do DashboardDetailsDTO
        var expectedData = new DashboardDetailsDTO
        {
            Id = dashboardId,
            Descricao = "Dashboard de Teste",
            ExtratoNome = "Extrato Mensal",
            Movimentacoes = new List<MovimentacaoDTO> { /* Adicionar mocks se necessário para testes específicos */ },
            Graficos = new List<GraficoDTO> { /* Adicionar mocks se necessário para testes específicos */ },
            Cartoes = new List<CartaoDTO> { new CartaoDTO { Nome = "Total Receitas" } } // CartãoDTO tem apenas Nome
        };
        _mockDashboardService.ObterDadosDashboardParaApiAsync(dashboardId).Returns(expectedData);

        // Act
        var result = await _controller.GetDashboardData(dashboardId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result.As<OkObjectResult>();
        okResult.Value.Should().BeOfType<DashboardDetailsDTO>().And.BeEquivalentTo(expectedData);
        await _mockDashboardService.Received(1).ObterDadosDashboardParaApiAsync(dashboardId);
    }

    [Fact]
    public async Task GetDashboardData_ReturnsNotFound_WhenDashboardDataIsNullFromService()
    {
        // Arrange
        var dashboardId = 1;
        _mockDashboardService.ObterDadosDashboardParaApiAsync(dashboardId).Returns((DashboardDetailsDTO)null);

        // Act
        var result = await _controller.GetDashboardData(dashboardId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        await _mockDashboardService.Received(1).ObterDadosDashboardParaApiAsync(dashboardId);
    }

    [Fact]
    public async Task GetDashboardData_ReturnsBadRequest_WhenApplicationExceptionOccurs()
    {
        // Arrange
        var dashboardId = 1;
        var errorMessage = "Dashboard não acessível para API.";
        _mockDashboardService.ObterDadosDashboardParaApiAsync(dashboardId).ThrowsAsync(new ApplicationException(errorMessage));

        // Act
        var result = await _controller.GetDashboardData(dashboardId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result.As<BadRequestObjectResult>();
        badRequestResult.Value.Should().BeEquivalentTo(new { message = errorMessage });
        await _mockDashboardService.Received(1).ObterDadosDashboardParaApiAsync(dashboardId);
    }

    [Fact]
    public async Task GetDashboardData_ReturnsForbidden_WhenUnauthorizedAccessExceptionOccurs()
    {
        // Arrange
        var dashboardId = 1;
        _mockDashboardService.ObterDadosDashboardParaApiAsync(dashboardId).ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.GetDashboardData(dashboardId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result.As<ObjectResult>();
        objectResult.StatusCode.Should().Be(403);
        objectResult.Value.Should().BeEquivalentTo(new { message = "Acesso não autorizado." });
        await _mockDashboardService.Received(1).ObterDadosDashboardParaApiAsync(dashboardId);
    }

    [Fact]
    public async Task GetDashboardData_ReturnsInternalServerError_WhenGeneralExceptionOccurs()
    {
        // Arrange
        var dashboardId = 1;
        _mockDashboardService.ObterDadosDashboardParaApiAsync(dashboardId).ThrowsAsync(new Exception("Erro interno."));

        // Act
        var result = await _controller.GetDashboardData(dashboardId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result.As<ObjectResult>();
        objectResult.StatusCode.Should().Be(500);
        objectResult.Value.Should().BeEquivalentTo(new { message = "Ocorreu um erro interno do servidor." });
        await _mockDashboardService.Received(1).ObterDadosDashboardParaApiAsync(dashboardId);
    }

    // --- Testes para o método Edit (GET) ---

    [Fact]
    public async Task EditGet_ReturnsViewWithDashboardVM_WhenSuccessful()
    {
        // Arrange
        var dashboardId = 1;
        var dashboardFromService = new Dashboard
        {
            Id = dashboardId,
            Descricao = "Dashboard Existente",
            ExtratoId = 10,
            Graficos = new List<Grafico> { new Grafico { Id = 1, Tipo = "Linha" } },
            Cartoes = new List<Cartao> { new Cartao { Id = 1, Nome = "Total" } } // Cartão tem Nome
        };
        var extratosSelectList = GetMockExtratosAsSelectListItems();

        _mockDashboardService.ObterDashboardPorIdAsync(dashboardId).Returns(dashboardFromService);
        _mockDashboardService.ObterExtratosDisponiveisParaUsuarioAsync().Returns(extratosSelectList);

        // Act
        var result = await _controller.Edit(dashboardId);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.Model.Should().BeOfType<DashboardVM>();
        var model = viewResult.Model.As<DashboardVM>();

        model.Id.Should().Be(dashboardId);
        model.Nome.Should().Be(dashboardFromService.Descricao); // Controller usa Descricao para Nome
        model.ExtratoId.Should().Be(dashboardFromService.ExtratoId);
        model.Graficos.Should().BeEquivalentTo(dashboardFromService.Graficos);
        model.Cartoes.Should().BeEquivalentTo(dashboardFromService.Cartoes);
        model.ExtratosDisponiveis.Should().BeEquivalentTo(extratosSelectList);

        await _mockDashboardService.Received(1).ObterDashboardPorIdAsync(dashboardId);
        await _mockDashboardService.Received(1).ObterExtratosDisponiveisParaUsuarioAsync();
        // Nenhuma mensagem de erro ou sucesso esperada neste fluxo
        _mockTempData.DidNotReceiveWithAnyArgs()["ErrorMessage"] = Arg.Any<object>();
        _mockTempData.DidNotReceiveWithAnyArgs()["SuccessMessage"] = Arg.Any<object>();
    }

    [Fact]
    public async Task EditGet_ReturnsNotFoundAndSetsErrorMessage_WhenDashboardIsNullFromService()
    {
        // Arrange
        var dashboardId = 1;
        _mockDashboardService.ObterDashboardPorIdAsync(dashboardId).Returns((Dashboard)null);

        // Act
        var result = await _controller.Edit(dashboardId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        // CORREÇÃO: Usando Received para verificar a atribuição
        _mockTempData.Received(1)["ErrorMessage"] = "Dashboard não encontrado ou você não tem permissão para editá-lo.";
        await _mockDashboardService.Received(1).ObterDashboardPorIdAsync(dashboardId);
        await _mockDashboardService.DidNotReceiveWithAnyArgs().ObterExtratosDisponiveisParaUsuarioAsync(); // Não deve carregar extratos se dashboard não for encontrado
    }

    [Fact]
    public async Task EditGet_ReturnsRedirectToLogin_WhenUnauthorizedAccessExceptionOccurs()
    {
        // Arrange
        var dashboardId = 1;
        _mockDashboardService.ObterDashboardPorIdAsync(dashboardId).ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.Edit(dashboardId);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Index");
        redirectResult.ControllerName.Should().Be("Login");
        // CORREÇÃO: Usando Received para verificar a atribuição
        _mockTempData.Received(1)["ErrorMessage"] = "Acesso não autorizado para editar este dashboard.";
        await _mockDashboardService.Received(1).ObterDashboardPorIdAsync(dashboardId);
    }

    [Fact]
    public async Task EditGet_ReturnsNotFoundAndSetsErrorMessage_WhenGeneralExceptionOccurs()
    {
        // Arrange
        var dashboardId = 1;
        var errorMessage = "Erro inesperado ao carregar para edição.";
        _mockDashboardService.ObterDashboardPorIdAsync(dashboardId).ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.Edit(dashboardId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        // CORREÇÃO: Usando Received para verificar a atribuição
        _mockTempData.Received(1)["ErrorMessage"] = "Ocorreu um erro ao carregar o dashboard para edição: " + errorMessage;
        await _mockDashboardService.Received(1).ObterDashboardPorIdAsync(dashboardId);
        await _mockDashboardService.DidNotReceiveWithAnyArgs().ObterExtratosDisponiveisParaUsuarioAsync();
    }

    // --- Testes para o método Edit (POST) ---

    [Fact]
    public async Task EditPost_ReturnsViewWithModelAndRepostsExtratos_WhenModelStateIsInvalid()
    {
        // Arrange
        var model = new DashboardVM { Id = 1, Nome = "" }; // Nome inválido
        var extratosSelectList = GetMockExtratosAsSelectListItems();
        _controller.ModelState.AddModelError("Nome", "Nome é obrigatório.");
        _mockDashboardService.ObterExtratosDisponiveisParaUsuarioAsync().Returns(extratosSelectList);

        // Act
        var result = await _controller.Edit(model);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.Model.Should().BeOfType<DashboardVM>().And.BeEquivalentTo(model, options => options.Excluding(x => x.ExtratosDisponiveis));
        viewResult.Model.As<DashboardVM>().ExtratosDisponiveis.Should().BeEquivalentTo(extratosSelectList);
        viewResult.ViewData.ModelState.IsValid.Should().BeFalse();
        await _mockDashboardService.Received(1).ObterExtratosDisponiveisParaUsuarioAsync();
        await _mockDashboardService.DidNotReceiveWithAnyArgs().AtualizarDashboardAsync(Arg.Any<DashboardVM>());
        // Nenhuma mensagem de sucesso esperada, mas talvez ErrorMessage possa ser setada pelo ModelState.
        _mockTempData.DidNotReceiveWithAnyArgs()["SuccessMessage"] = Arg.Any<object>();
    }

    [Fact]
    public async Task EditPost_ReturnsRedirectToDetails_WhenSuccessful()
    {
        // Arrange
        var model = new DashboardVM { Id = 1, Nome = "Dashboard Atualizado", ExtratoId = 5 };
        _mockDashboardService.AtualizarDashboardAsync(model).Returns(Task.CompletedTask);
        // REMOVIDO: _mockTempData["SuccessMessage"] = null;

        // Act
        var result = await _controller.Edit(model);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Details");
        redirectResult.RouteValues["id"].Should().Be(model.Id);
        // CORREÇÃO: Usando Received para verificar a atribuição
        _mockTempData.Received(1)["SuccessMessage"] = "Dashboard atualizado com sucesso!";
        await _mockDashboardService.Received(1).AtualizarDashboardAsync(model);
    }

    [Fact]
    public async Task EditPost_ReturnsViewWithModelAndApplicationError_WhenApplicationExceptionOccurs()
    {
        // Arrange
        var model = new DashboardVM { Id = 1, Nome = "Dashboard Duplicado", ExtratoId = 5 };
        var errorMessage = "Nome do dashboard já existe.";
        var extratosSelectList = GetMockExtratosAsSelectListItems();
        _mockDashboardService.AtualizarDashboardAsync(model).ThrowsAsync(new ApplicationException(errorMessage));
        _mockDashboardService.ObterExtratosDisponiveisParaUsuarioAsync().Returns(extratosSelectList);

        // Act

        var result = await _controller.Edit(model);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.Model.Should().BeOfType<DashboardVM>().And.BeEquivalentTo(model, options => options.Excluding(x => x.ExtratosDisponiveis));
        viewResult.Model.As<DashboardVM>().ExtratosDisponiveis.Should().BeEquivalentTo(extratosSelectList);
        viewResult.ViewData.ModelState.IsValid.Should().BeFalse();
        viewResult.ViewData.ModelState.Should().ContainSingle(m => m.Value.Errors.Any(e => e.ErrorMessage == errorMessage));
        await _mockDashboardService.Received(1).AtualizarDashboardAsync(model);
        await _mockDashboardService.Received(1).ObterExtratosDisponiveisParaUsuarioAsync();
        // Nenhuma mensagem de erro ou sucesso explícita via TempData para este fluxo.
        _mockTempData.DidNotReceiveWithAnyArgs()["ErrorMessage"] = Arg.Any<object>();
        _mockTempData.DidNotReceiveWithAnyArgs()["SuccessMessage"] = Arg.Any<object>();
    }

    [Fact]
    public async Task EditPost_ReturnsRedirectToLogin_WhenUnauthorizedAccessExceptionOccurs()
    {
        // Arrange
        var model = new DashboardVM { Id = 1, Nome = "Dashboard Teste", ExtratoId = 5 };
        _mockDashboardService.AtualizarDashboardAsync(model).ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.Edit(model);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Index");
        redirectResult.ControllerName.Should().Be("Login");
        // CORREÇÃO: Usando Received para verificar a atribuição
        _mockTempData.Received(1)["ErrorMessage"] = "Você não tem permissão para realizar esta ação.";
        await _mockDashboardService.Received(1).AtualizarDashboardAsync(model);
    }

    [Fact]
    public async Task EditPost_ReturnsViewWithModelAndGenericError_WhenGeneralExceptionOccurs()
    {
        // Arrange
        var model = new DashboardVM { Id = 1, Nome = "Dashboard Teste", ExtratoId = 5 };
        var errorMessage = "Erro inesperado ao atualizar dashboard.";
        var extratosSelectList = GetMockExtratosAsSelectListItems();
        _mockDashboardService.AtualizarDashboardAsync(model).ThrowsAsync(new Exception(errorMessage));
        _mockDashboardService.ObterExtratosDisponiveisParaUsuarioAsync().Returns(extratosSelectList);

        // Act
        var result = await _controller.Edit(model);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.Model.Should().BeOfType<DashboardVM>().And.BeEquivalentTo(model, options => options.Excluding(x => x.ExtratosDisponiveis));
        viewResult.Model.As<DashboardVM>().ExtratosDisponiveis.Should().BeEquivalentTo(extratosSelectList);
        viewResult.ViewData.ModelState.IsValid.Should().BeFalse();
        viewResult.ViewData.ModelState.Should().ContainSingle(m => m.Value.Errors.Any(e => e.ErrorMessage == "Ocorreu um erro inesperado ao atualizar o dashboard."));
        await _mockDashboardService.Received(1).AtualizarDashboardAsync(model);
        await _mockDashboardService.Received(1).ObterExtratosDisponiveisParaUsuarioAsync();
        // Nenhuma mensagem de erro ou sucesso explícita via TempData para este fluxo.
        _mockTempData.DidNotReceiveWithAnyArgs()["ErrorMessage"] = Arg.Any<object>();
        _mockTempData.DidNotReceiveWithAnyArgs()["SuccessMessage"] = Arg.Any<object>();
    }

    // --- Testes para o método Delete (GET) ---

    [Fact]
    public async Task DeleteGet_ReturnsNotFound_WhenIdIsNull()
    {
        // Act
        var result = await _controller.Delete(null);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        await _mockDashboardService.DidNotReceiveWithAnyArgs().ObterDashboardPorIdAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task DeleteGet_ReturnsViewWithDashboard_WhenSuccessful()
    {
        // Arrange
        var dashboardId = 1;
        var expectedDashboard = new Dashboard { Id = dashboardId, Descricao = "Dashboard para Exclusão" };
        _mockDashboardService.ObterDashboardPorIdAsync(dashboardId).Returns(expectedDashboard);

        // Act
        var result = await _controller.Delete(dashboardId);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.Model.Should().BeOfType<Dashboard>().And.BeEquivalentTo(expectedDashboard);
        await _mockDashboardService.Received(1).ObterDashboardPorIdAsync(dashboardId);
        // Nenhuma mensagem de erro ou sucesso esperada neste fluxo
        _mockTempData.DidNotReceiveWithAnyArgs()["ErrorMessage"] = Arg.Any<object>();
        _mockTempData.DidNotReceiveWithAnyArgs()["SuccessMessage"] = Arg.Any<object>();
    }

    [Fact]
    public async Task DeleteGet_ReturnsNotFoundAndSetsErrorMessage_WhenDashboardIsNullFromService()
    {
        // Arrange
        var dashboardId = 1;
        _mockDashboardService.ObterDashboardPorIdAsync(dashboardId).Returns((Dashboard)null);

        // Act
        var result = await _controller.Delete(dashboardId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        // CORREÇÃO: Usando Received para verificar a atribuição
        _mockTempData.Received(1)["ErrorMessage"] = "Dashboard não encontrado ou você não tem permissão para excluí-lo.";
        await _mockDashboardService.Received(1).ObterDashboardPorIdAsync(dashboardId);
    }

    [Fact]
    public async Task DeleteGet_ReturnsRedirectToLogin_WhenUnauthorizedAccessExceptionOccurs()
    {
        // Arrange
        var dashboardId = 1;
        _mockDashboardService.ObterDashboardPorIdAsync(dashboardId).ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.Delete(dashboardId);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Index");
        redirectResult.ControllerName.Should().Be("Login");
        // CORREÇÃO: Usando Received para verificar a atribuição
        _mockTempData.Received(1)["ErrorMessage"] = "Acesso não autorizado para excluir este dashboard.";
        await _mockDashboardService.Received(1).ObterDashboardPorIdAsync(dashboardId);
    }

    [Fact]
    public async Task DeleteGet_ReturnsNotFoundAndSetsErrorMessage_WhenGeneralExceptionOccurs()
    {
        // Arrange
        var dashboardId = 1;
        var errorMessage = "Erro ao carregar para exclusão.";
        _mockDashboardService.ObterDashboardPorIdAsync(dashboardId).ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.Delete(dashboardId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        // CORREÇÃO: Usando Received para verificar a atribuição
        _mockTempData.Received(1)["ErrorMessage"] = "Ocorreu um erro ao carregar o dashboard para exclusão: " + errorMessage;
        await _mockDashboardService.Received(1).ObterDashboardPorIdAsync(dashboardId);
    }

    // --- Testes para o método DeleteConfirmed (POST) ---

    [Fact]
    public async Task DeleteConfirmed_ReturnsRedirectToIndexAndSetsSuccessMessage_WhenSuccessful()
    {
        // Arrange
        var dashboardId = 1;
        _mockDashboardService.ExcluirDashboardAsync(dashboardId).Returns(Task.CompletedTask);
        // REMOVIDO: _mockTempData["SuccessMessage"] = null;

        // Act
        var result = await _controller.DeleteConfirmed(dashboardId);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Index");
        // CORREÇÃO: Usando Received para verificar a atribuição
        _mockTempData.Received(1)["SuccessMessage"] = "Dashboard excluído com sucesso!";
        await _mockDashboardService.Received(1).ExcluirDashboardAsync(dashboardId);
    }

    [Fact]
    public async Task DeleteConfirmed_ReturnsRedirectToDeleteAndSetsApplicationError_WhenApplicationExceptionOccurs()
    {
        // Arrange
        var dashboardId = 1;
        var errorMessage = "Não foi possível excluir o dashboard, pois ele contém dados.";
        _mockDashboardService.ExcluirDashboardAsync(dashboardId).ThrowsAsync(new ApplicationException(errorMessage));
        // REMOVIDO: _mockTempData["DeleteError"] = null;

        // Act
        var result = await _controller.DeleteConfirmed(dashboardId);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Delete");
        redirectResult.RouteValues["id"].Should().Be(dashboardId);
        // CORREÇÃO: Usando Received para verificar a atribuição
        _mockTempData.Received(1)["DeleteError"] = errorMessage;
        await _mockDashboardService.Received(1).ExcluirDashboardAsync(dashboardId);
    }

    [Fact]
    public async Task DeleteConfirmed_ReturnsRedirectToLogin_WhenUnauthorizedAccessExceptionOccurs()
    {
        // Arrange
        var dashboardId = 1;
        _mockDashboardService.ExcluirDashboardAsync(dashboardId).ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.DeleteConfirmed(dashboardId);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Index");
        redirectResult.ControllerName.Should().Be("Login");
        // CORREÇÃO: Usando Received para verificar a atribuição
        _mockTempData.Received(1)["ErrorMessage"] = "Você não tem permissão para realizar esta ação.";
        await _mockDashboardService.Received(1).ExcluirDashboardAsync(dashboardId);
    }

    [Fact]
    public async Task DeleteConfirmed_ReturnsRedirectToDeleteAndSetsGenericError_WhenGeneralExceptionOccurs()
    {
        // Arrange
        var dashboardId = 1;
        var errorMessage = "Erro inesperado ao excluir dashboard.";
        _mockDashboardService.ExcluirDashboardAsync(dashboardId).ThrowsAsync(new Exception(errorMessage));
        // REMOVIDO: _mockTempData["DeleteError"] = null;

        // Act
        var result = await _controller.DeleteConfirmed(dashboardId);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Delete");
        redirectResult.RouteValues["id"].Should().Be(dashboardId);
        // CORREÇÃO: Usando Received para verificar a atribuição
        _mockTempData.Received(1)["DeleteError"] = "Ocorreu um erro inesperado ao excluir o dashboard: " + errorMessage;
        await _mockDashboardService.Received(1).ExcluirDashboardAsync(dashboardId);
    }
}