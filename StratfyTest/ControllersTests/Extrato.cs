using Xunit;
using NSubstitute;
using FluentAssertions;
using FluentAssertions.Collections;
using STRATFY.Controllers;
using STRATFY.Interfaces.IServices;
using STRATFY.Models; // Para Extrato, ExtratoIndexViewModel, Movimentacao, Categoria, ExtratoEdicaoViewModel
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures; // Para ITempDataDictionary
using Microsoft.AspNetCore.Http; // Para DefaultHttpContext, IFormFile
using Microsoft.AspNetCore.Mvc.Rendering; // Para SelectList
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO; // Para MemoryStream
using System.Linq;
using NSubstitute.ExceptionExtensions; // Necessário para ThrowsAsync
using System.Security.Claims; // Para ClaimsPrincipal (se precisar simular usuário logado)


public class ExtratosControllerTests
{
    private readonly IExtratoService _mockExtratoService;
    private readonly ICategoriaService _mockCategoriaService;
    private readonly ExtratosController _controller;
    private readonly ITempDataDictionary _mockTempData;

    public ExtratosControllerTests()
    {
        _mockExtratoService = Substitute.For<IExtratoService>();
        _mockCategoriaService = Substitute.For<ICategoriaService>();
        _mockTempData = Substitute.For<ITempDataDictionary>();

        _controller = new ExtratosController(_mockExtratoService, _mockCategoriaService);
        _controller.TempData = _mockTempData;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        // Para simular um usuário logado se os services precisarem acessar o HttpContext.User
        // Se a service precisar do userId, você precisaria adicionar isso:
        // _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
        //      new Claim(ClaimTypes.NameIdentifier, "1"), // Exemplo de ID do usuário
        //      new Claim(ClaimTypes.Name, "testuser@example.com")
        // }, "mock"));
    }

    // --- Testes para o método Index (GET) ---

    [Fact]
    public async Task Index_ReturnsViewWithListOfExtratos_WhenSuccessful()
    {
        // Arrange
        var expectedExtratos = new List<ExtratoIndexViewModel>
        {
            new ExtratoIndexViewModel { Id = 1, Nome = "Extrato 1", DataCriacao = DateOnly.FromDateTime(DateTime.Now) },
            new ExtratoIndexViewModel { Id = 2, Nome = "Extrato 2", DataCriacao = DateOnly.FromDateTime(DateTime.Now) }
        };
        _mockExtratoService.ObterExtratosDoUsuarioParaIndexAsync().Returns(expectedExtratos);

        // Act
        var result = await _controller.Index();

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.Model.Should().BeOfType<List<ExtratoIndexViewModel>>();
        viewResult.Model.As<List<ExtratoIndexViewModel>>().Should().HaveCount(2).And.BeEquivalentTo(expectedExtratos);
        await _mockExtratoService.Received(1).ObterExtratosDoUsuarioParaIndexAsync();
        // Não há mensagens de TempData para este cenário no controller
        _mockTempData.DidNotReceiveWithAnyArgs()["ErrorMessage"] = Arg.Any<object>();
        _mockTempData.DidNotReceiveWithAnyArgs()["SuccessMessage"] = Arg.Any<object>();
    }

    [Fact]
    public async Task Index_ReturnsRedirectToLogin_WhenUnauthorizedAccessExceptionOccurs()
    {
        // Arrange
        _mockExtratoService.ObterExtratosDoUsuarioParaIndexAsync().ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.Index();

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Login");
        redirectResult.ControllerName.Should().Be("Account");
        await _mockExtratoService.Received(1).ObterExtratosDoUsuarioParaIndexAsync();
        // CORREÇÃO: O controller não define ErrorMessage aqui.
        _mockTempData.DidNotReceiveWithAnyArgs()["ErrorMessage"] = Arg.Any<object>();
    }

    [Fact]
    public async Task Index_ReturnsViewWithEmptyListAndSetsErrorMessage_WhenGeneralExceptionOccurs()
    {
        // Arrange
        var errorMessage = "Erro simulado ao carregar extratos.";
        _mockExtratoService.ObterExtratosDoUsuarioParaIndexAsync().ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.Index();

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.Model.Should().BeOfType<List<ExtratoIndexViewModel>>();
        viewResult.Model.As<List<ExtratoIndexViewModel>>().Should().BeEmpty();
        _mockTempData.Received(1)["ErrorMessage"] = "Ocorreu um erro ao carregar os extratos: " + errorMessage;
        await _mockExtratoService.Received(1).ObterExtratosDoUsuarioParaIndexAsync();
    }

    // --- Testes para o método Create (GET) ---

    [Fact]
    public void Create_ReturnsViewResult()
    {
        // Act
        var result = _controller.Create();

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.ViewName.Should().BeNull();
        viewResult.Model.Should().BeNull();
        // Não há mensagens de TempData para este cenário no controller
        _mockTempData.DidNotReceiveWithAnyArgs()["ErrorMessage"] = Arg.Any<object>();
        _mockTempData.DidNotReceiveWithAnyArgs()["SuccessMessage"] = Arg.Any<object>();
    }

    // --- Testes para o método Create (POST) ---

    [Fact]
    public async Task CreatePost_ReturnsViewWithModel_WhenModelStateIsInvalid()
    {
        // Arrange
        var model = new Extrato { Nome = "" }; // Nome vazio para invalidar
        _controller.ModelState.AddModelError("Nome", "Nome é obrigatório.");
        var mockFile = Substitute.For<IFormFile>();

        // Act
        var result = await _controller.Create(model, mockFile);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.ViewName.Should().BeNull();
        viewResult.Model.Should().BeOfType<Extrato>().And.BeEquivalentTo(model);
        viewResult.ViewData.ModelState.IsValid.Should().BeFalse();
        await _mockExtratoService.DidNotReceiveWithAnyArgs().CriarExtratoComMovimentacoesAsync(Arg.Any<Extrato>(), Arg.Any<IFormFile>());
        // Não há mensagens de TempData para este cenário no controller
        _mockTempData.DidNotReceiveWithAnyArgs()["ErrorMessage"] = Arg.Any<object>();
        _mockTempData.DidNotReceiveWithAnyArgs()["SuccessMessage"] = Arg.Any<object>();
    }

    [Fact]
    public async Task CreatePost_ReturnsRedirectToEdit_WhenSuccessful()
    {
        // Arrange
        var model = new Extrato { Nome = "Extrato Teste" };
        var extratoId = 10;
        var mockFile = Substitute.For<IFormFile>();
        _mockExtratoService.CriarExtratoComMovimentacoesAsync(Arg.Any<Extrato>(), Arg.Any<IFormFile>()).Returns(extratoId);

        // Act
        var result = await _controller.Create(model, mockFile);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Edit");
        redirectResult.RouteValues["id"].Should().Be(extratoId);
        // CORREÇÃO: O controller NÃO define TempData["SuccessMessage"] aqui.
        _mockTempData.DidNotReceiveWithAnyArgs()["SuccessMessage"] = Arg.Any<object>();
        await _mockExtratoService.Received(1).CriarExtratoComMovimentacoesAsync(Arg.Any<Extrato>(), mockFile);
    }

    [Fact]
    public async Task CreatePost_ReturnsRedirectToLogin_WhenUnauthorizedAccessExceptionOccurs()
    {
        // Arrange
        var model = new Extrato { Nome = "Extrato Teste" };
        var mockFile = Substitute.For<IFormFile>();
        _mockExtratoService.CriarExtratoComMovimentacoesAsync(Arg.Any<Extrato>(), Arg.Any<IFormFile>()).ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.Create(model, mockFile);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Login");
        redirectResult.ControllerName.Should().Be("Account");
        await _mockExtratoService.Received(1).CriarExtratoComMovimentacoesAsync(Arg.Any<Extrato>(), mockFile);
        // CORREÇÃO: O controller NÃO define TempData["ErrorMessage"] aqui.
        _mockTempData.DidNotReceiveWithAnyArgs()["ErrorMessage"] = Arg.Any<object>();
    }

    [Fact]
    public async Task CreatePost_ReturnsViewWithModelAndApplicationError_WhenApplicationExceptionOccurs()
    {
        // Arrange
        var model = new Extrato { Nome = "Extrato Teste" };
        var mockFile = Substitute.For<IFormFile>();
        var errorMessage = "Nome de extrato já existe.";
        _mockExtratoService.CriarExtratoComMovimentacoesAsync(Arg.Any<Extrato>(), Arg.Any<IFormFile>()).ThrowsAsync(new ApplicationException(errorMessage));

        // Act
        var result = await _controller.Create(model, mockFile);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.ViewName.Should().BeNull();
        viewResult.Model.Should().BeOfType<Extrato>().And.BeEquivalentTo(model);
        viewResult.ViewData.ModelState.IsValid.Should().BeFalse();
        viewResult.ViewData.ModelState.Should().ContainSingle(m => m.Value.Errors.Any(e => e.ErrorMessage == errorMessage));
        await _mockExtratoService.Received(1).CriarExtratoComMovimentacoesAsync(Arg.Any<Extrato>(), mockFile);
        // Não há mensagens de TempData para este cenário no controller (erro vai para ModelState)
        _mockTempData.DidNotReceiveWithAnyArgs()["ErrorMessage"] = Arg.Any<object>();
        _mockTempData.DidNotReceiveWithAnyArgs()["SuccessMessage"] = Arg.Any<object>();
    }

    [Fact]
    public async Task CreatePost_ReturnsViewWithModelAndGenericError_WhenGeneralExceptionOccurs()
    {
        // Arrange
        var model = new Extrato { Nome = "Extrato Teste" };
        var mockFile = Substitute.For<IFormFile>();
        var errorMessage = "Erro inesperado ao processar arquivo.";
        _mockExtratoService.CriarExtratoComMovimentacoesAsync(Arg.Any<Extrato>(), Arg.Any<IFormFile>()).ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.Create(model, mockFile);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.ViewName.Should().BeNull();
        viewResult.Model.Should().BeOfType<Extrato>().And.BeEquivalentTo(model);
        viewResult.ViewData.ModelState.IsValid.Should().BeFalse();
        viewResult.ViewData.ModelState.Should().ContainSingle(m => m.Value.Errors.Any(e => e.ErrorMessage == "Ocorreu um erro inesperado ao criar o extrato: " + errorMessage));
        await _mockExtratoService.Received(1).CriarExtratoComMovimentacoesAsync(Arg.Any<Extrato>(), mockFile);
        // Não há mensagens de TempData para este cenário no controller (erro vai para ModelState)
        _mockTempData.DidNotReceiveWithAnyArgs()["ErrorMessage"] = Arg.Any<object>();
        _mockTempData.DidNotReceiveWithAnyArgs()["SuccessMessage"] = Arg.Any<object>();
    }

    // --- Testes para o método Edit (GET) ---

    [Fact]
    public async Task EditGet_ReturnsNotFound_WhenIdIsNull()
    {
        // Act
        var result = await _controller.Edit((int?)null);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        await _mockExtratoService.DidNotReceiveWithAnyArgs().ObterExtratoParaEdicaoAsync(Arg.Any<int>());
        // Não há mensagens de TempData para este cenário no controller
        _mockTempData.DidNotReceiveWithAnyArgs()["ErrorMessage"] = Arg.Any<object>();
        _mockTempData.DidNotReceiveWithAnyArgs()["SuccessMessage"] = Arg.Any<object>();
    }

    [Fact]
    public async Task EditGet_ReturnsViewWithExtratoForEdit_WhenSuccessful()
    {
        // Arrange
        var extratoId = 1;
        var expectedViewModel = new ExtratoEdicaoViewModel
        {
            ExtratoId = extratoId, // Usar ExtratoId
            NomeExtrato = "Extrato Editado", // Usar NomeExtrato
            DataCriacao = DateOnly.FromDateTime(DateTime.Now), // Usar DateOnly
            Movimentacoes = new List<Movimentacao>()
        };
        // Mockar para retornar Categoria, pois a service retorna IEnumerable<Categoria>
        var categorias = new List<Categoria>
        {
            new Categoria { Id = 1, Nome = "Categoria A" }
        };

        _mockExtratoService.ObterExtratoParaEdicaoAsync(extratoId).Returns(expectedViewModel);
        _mockCategoriaService.ObterTodasCategoriasParaSelectList().Returns(categorias); // Mockar Categoria

        // Act
        var result = await _controller.Edit(extratoId);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.Model.Should().BeOfType<ExtratoEdicaoViewModel>().And.BeEquivalentTo(expectedViewModel);
        viewResult.ViewData.Should().ContainKey("CategoriaId");
        // Precisamos ter certeza de que o SelectList foi criado a partir das Categorias
        viewResult.ViewData["CategoriaId"].Should().BeOfType<SelectList>();
        ((SelectList)viewResult.ViewData["CategoriaId"]).Items.Cast<object>().ToList().Count().Should().Be(1);
        ((SelectList)viewResult.ViewData["CategoriaId"]).First().Value.Should().Be("1");
        ((SelectList)viewResult.ViewData["CategoriaId"]).First().Text.Should().Be("Categoria A");

        await _mockExtratoService.Received(1).ObterExtratoParaEdicaoAsync(extratoId);
        _mockCategoriaService.Received(1).ObterTodasCategoriasParaSelectList();
        // Não há mensagens de TempData para este cenário no controller
        _mockTempData.DidNotReceiveWithAnyArgs()["ErrorMessage"] = Arg.Any<object>();
        _mockTempData.DidNotReceiveWithAnyArgs()["SuccessMessage"] = Arg.Any<object>();
    }

    [Fact]
    public async Task EditGet_ReturnsNotFound_WhenExtratoIsNullFromService()
    {
        // Arrange
        var extratoId = 1;
        _mockExtratoService.ObterExtratoParaEdicaoAsync(extratoId).Returns((ExtratoEdicaoViewModel)null);

        // Act
        var result = await _controller.Edit(extratoId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        await _mockExtratoService.Received(1).ObterExtratoParaEdicaoAsync(extratoId);
        _mockCategoriaService.DidNotReceiveWithAnyArgs().ObterTodasCategoriasParaSelectList();
        // CORREÇÃO: O controller NÃO define TempData["ErrorMessage"] aqui.
        _mockTempData.DidNotReceiveWithAnyArgs()["ErrorMessage"] = Arg.Any<object>();
    }

    [Fact]
    public async Task EditGet_ReturnsNotFound_WhenApplicationExceptionOccurs()
    {
        // Arrange
        var extratoId = 1;
        var errorMessage = "Extrato não encontrado ou acesso negado.";
        _mockExtratoService.ObterExtratoParaEdicaoAsync(extratoId).ThrowsAsync(new ApplicationException(errorMessage));

        // Act
        var result = await _controller.Edit(extratoId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        await _mockExtratoService.Received(1).ObterExtratoParaEdicaoAsync(extratoId);
        // CORREÇÃO: O controller NÃO define TempData["ErrorMessage"] aqui.
        _mockTempData.DidNotReceiveWithAnyArgs()["ErrorMessage"] = Arg.Any<object>();
    }

    [Fact]
    public async Task EditGet_ReturnsRedirectToLogin_WhenUnauthorizedAccessExceptionOccurs()
    {
        // Arrange
        var extratoId = 1;
        _mockExtratoService.ObterExtratoParaEdicaoAsync(extratoId).ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.Edit(extratoId);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Login");
        redirectResult.ControllerName.Should().Be("Account");
        await _mockExtratoService.Received(1).ObterExtratoParaEdicaoAsync(extratoId);
        // CORREÇÃO: O controller NÃO define TempData["ErrorMessage"] aqui.
        _mockTempData.DidNotReceiveWithAnyArgs()["ErrorMessage"] = Arg.Any<object>();
    }

    [Fact]
    public async Task EditGet_ReturnsNotFoundAndSetsErrorMessage_WhenGeneralExceptionOccurs()
    {
        // Arrange
        var extratoId = 1;
        var errorMessage = "Erro inesperado ao carregar para edição.";
        _mockExtratoService.ObterExtratoParaEdicaoAsync(extratoId).ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.Edit(extratoId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        _mockTempData.Received(1)["ErrorMessage"] = "Ocorreu um erro ao carregar o extrato para edição: " + errorMessage;
        await _mockExtratoService.Received(1).ObterExtratoParaEdicaoAsync(extratoId);
    }

    // --- Testes para o método Edit (POST) ---

    [Fact]
    public async Task EditPost_ReturnsViewWithModelAndRepostsCategories_WhenModelStateIsInvalid()
    {
        // Arrange
        var model = new ExtratoEdicaoViewModel { ExtratoId = 1, NomeExtrato = "" }; // Usar NomeExtrato
        var categorias = new List<Categoria> { new Categoria { Id = 1, Nome = "Categoria A" } }; // Mockar Categoria
        _controller.ModelState.AddModelError("NomeExtrato", "Nome do extrato é obrigatório."); // Usar NomeExtrato no erro
        _mockCategoriaService.ObterTodasCategoriasParaSelectList().Returns(categorias);

        // Act
        var result = await _controller.Edit(model);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.Model.Should().BeOfType<ExtratoEdicaoViewModel>().And.BeEquivalentTo(model);
        viewResult.ViewData.ModelState.IsValid.Should().BeFalse();
        viewResult.ViewData.Should().ContainKey("CategoriaId");
        ((SelectList)viewResult.ViewData["CategoriaId"]).Items.Cast<object>().ToList().Count().Should().Be(1);
        await _mockExtratoService.DidNotReceiveWithAnyArgs().AtualizarExtratoEMovimentacoesAsync(Arg.Any<ExtratoEdicaoViewModel>());
        _mockCategoriaService.Received(1).ObterTodasCategoriasParaSelectList();
        // Não há mensagens de TempData para este cenário no controller (erro vai para ModelState)
        _mockTempData.DidNotReceiveWithAnyArgs()["ErrorMessage"] = Arg.Any<object>();
        _mockTempData.DidNotReceiveWithAnyArgs()["SuccessMessage"] = Arg.Any<object>();
    }

    [Fact]
    public async Task EditPost_ReturnsRedirectToIndexAndSetsSuccessMessage_WhenSuccessful()
    {
        // Arrange
        var model = new ExtratoEdicaoViewModel { ExtratoId = 1, NomeExtrato = "Extrato Editado", DataCriacao = DateOnly.FromDateTime(DateTime.Now) }; // Usar NomeExtrato e DateOnly
        _mockExtratoService.AtualizarExtratoEMovimentacoesAsync(Arg.Any<ExtratoEdicaoViewModel>()).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Edit(model);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Index");
        _mockTempData.Received(1)["SuccessMessage"] = "Extrato e movimentações atualizados com sucesso!";
        await _mockExtratoService.Received(1).AtualizarExtratoEMovimentacoesAsync(model);
    }

    [Fact]
    public async Task EditPost_ReturnsViewWithModelAndApplicationError_WhenApplicationExceptionOccurs()
    {
        // Arrange
        var model = new ExtratoEdicaoViewModel { ExtratoId = 1, NomeExtrato = "Extrato Editado", DataCriacao = DateOnly.FromDateTime(DateTime.Now) }; // Usar NomeExtrato e DateOnly
        var errorMessage = "Não foi possível atualizar extrato.";
        var categorias = new List<Categoria> { new Categoria { Id = 1, Nome = "Categoria A" } }; // Mockar Categoria
        _mockExtratoService.AtualizarExtratoEMovimentacoesAsync(Arg.Any<ExtratoEdicaoViewModel>()).ThrowsAsync(new ApplicationException(errorMessage));
        _mockCategoriaService.ObterTodasCategoriasParaSelectList().Returns(categorias);

        // Act
        var result = await _controller.Edit(model);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.ViewName.Should().BeNull();
        viewResult.Model.Should().BeOfType<ExtratoEdicaoViewModel>().And.BeEquivalentTo(model);
        viewResult.ViewData.ModelState.IsValid.Should().BeFalse();
        viewResult.ViewData.ModelState.Should().ContainSingle(m => m.Value.Errors.Any(e => e.ErrorMessage == errorMessage));
        viewResult.ViewData.Should().ContainKey("CategoriaId");
        _mockCategoriaService.Received(1).ObterTodasCategoriasParaSelectList();
        await _mockExtratoService.Received(1).AtualizarExtratoEMovimentacoesAsync(model);
        // Não há mensagens de TempData para este cenário no controller (erro vai para ModelState)
        _mockTempData.DidNotReceiveWithAnyArgs()["ErrorMessage"] = Arg.Any<object>();
        _mockTempData.DidNotReceiveWithAnyArgs()["SuccessMessage"] = Arg.Any<object>();
    }

    [Fact]
    public async Task EditPost_ReturnsRedirectToLogin_WhenUnauthorizedAccessExceptionOccurs()
    {
        // Arrange
        var model = new ExtratoEdicaoViewModel { ExtratoId = 1, NomeExtrato = "Extrato Editado", DataCriacao = DateOnly.FromDateTime(DateTime.Now) }; // Usar NomeExtrato e DateOnly
        _mockExtratoService.AtualizarExtratoEMovimentacoesAsync(Arg.Any<ExtratoEdicaoViewModel>()).ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.Edit(model);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Login");
        redirectResult.ControllerName.Should().Be("Account");
        await _mockExtratoService.Received(1).AtualizarExtratoEMovimentacoesAsync(model);
        // CORREÇÃO: O controller NÃO define TempData["ErrorMessage"] aqui.
        _mockTempData.DidNotReceiveWithAnyArgs()["ErrorMessage"] = Arg.Any<object>();
    }

    [Fact]
    public async Task EditPost_ReturnsViewWithModelAndGenericError_WhenGeneralExceptionOccurs()
    {
        // Arrange
        var model = new ExtratoEdicaoViewModel { ExtratoId = 1, NomeExtrato = "Extrato Editado", DataCriacao = DateOnly.FromDateTime(DateTime.Now) }; // Usar NomeExtrato e DateOnly
        var errorMessage = "Erro inesperado na atualização.";
        var categorias = new List<Categoria> { new Categoria { Id = 1, Nome = "Categoria A" } }; // Mockar Categoria
        _mockExtratoService.AtualizarExtratoEMovimentacoesAsync(Arg.Any<ExtratoEdicaoViewModel>()).ThrowsAsync(new Exception(errorMessage));
        _mockCategoriaService.ObterTodasCategoriasParaSelectList().Returns(categorias);

        // Act
        var result = await _controller.Edit(model);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.Model.Should().BeOfType<ExtratoEdicaoViewModel>().And.BeEquivalentTo(model);
        viewResult.ViewData.ModelState.IsValid.Should().BeFalse();
        viewResult.ViewData.ModelState.Should().ContainSingle(m => m.Value.Errors.Any(e => e.ErrorMessage == "Ocorreu um erro inesperado ao atualizar o extrato: " + errorMessage));
        viewResult.ViewData.Should().ContainKey("CategoriaId");
        _mockCategoriaService.Received(1).ObterTodasCategoriasParaSelectList();
        await _mockExtratoService.Received(1).AtualizarExtratoEMovimentacoesAsync(model);
        // Não há mensagens de TempData para este cenário no controller (erro vai para ModelState)
        _mockTempData.DidNotReceiveWithAnyArgs()["ErrorMessage"] = Arg.Any<object>();
        _mockTempData.DidNotReceiveWithAnyArgs()["SuccessMessage"] = Arg.Any<object>();
    }

    // --- Testes para o método Delete (GET) ---
    // (Mantendo, pois o botão de editar pode levar para o delete, mas não para os detalhes)

    [Fact]
    public async Task DeleteGet_ReturnsNotFound_WhenIdIsNull()
    {
        // Act
        var result = await _controller.Delete(null);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        await _mockExtratoService.DidNotReceiveWithAnyArgs().ObterExtratoDetalhesAsync(Arg.Any<int>());
        // Não há mensagens de TempData para este cenário no controller
        _mockTempData.DidNotReceiveWithAnyArgs()["ErrorMessage"] = Arg.Any<object>();
        _mockTempData.DidNotReceiveWithAnyArgs()["SuccessMessage"] = Arg.Any<object>();
    }

    [Fact]
    public async Task DeleteGet_ReturnsViewWithExtratoDetails_WhenSuccessful()
    {
        // Arrange
        var extratoId = 1;
        var expectedExtrato = new Extrato { Id = extratoId, Nome = "Extrato para Exclusão", DataCriacao = DateOnly.FromDateTime(DateTime.Now) };
        _mockExtratoService.ObterExtratoDetalhesAsync(extratoId).Returns(expectedExtrato);

        // Act
        var result = await _controller.Delete(extratoId);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.Model.Should().BeOfType<Extrato>().And.BeEquivalentTo(expectedExtrato);
        await _mockExtratoService.Received(1).ObterExtratoDetalhesAsync(extratoId);
        // Não há mensagens de TempData para este cenário no controller
        _mockTempData.DidNotReceiveWithAnyArgs()["ErrorMessage"] = Arg.Any<object>();
        _mockTempData.DidNotReceiveWithAnyArgs()["SuccessMessage"] = Arg.Any<object>();
    }

    [Fact]
    public async Task DeleteGet_ReturnsNotFound_WhenExtratoIsNullFromService()
    {
        // Arrange
        var extratoId = 1;
        _mockExtratoService.ObterExtratoDetalhesAsync(extratoId).Returns((Extrato)null); // Assumindo retorno de Extrato ou null

        // Act
        var result = await _controller.Delete(extratoId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        await _mockExtratoService.Received(1).ObterExtratoDetalhesAsync(extratoId);
        // CORREÇÃO: O controller NÃO define TempData["ErrorMessage"] aqui.
        _mockTempData.DidNotReceiveWithAnyArgs()["ErrorMessage"] = Arg.Any<object>();
    }

    [Fact]
    public async Task DeleteGet_ReturnsNotFound_WhenApplicationExceptionOccurs()
    {
        // Arrange
        var extratoId = 1;
        var errorMessage = "Extrato não acessível.";
        _mockExtratoService.ObterExtratoDetalhesAsync(extratoId).ThrowsAsync(new ApplicationException(errorMessage));

        // Act
        var result = await _controller.Delete(extratoId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        await _mockExtratoService.Received(1).ObterExtratoDetalhesAsync(extratoId);
        // CORREÇÃO: O controller NÃO define TempData["ErrorMessage"] aqui.
        _mockTempData.DidNotReceiveWithAnyArgs()["ErrorMessage"] = Arg.Any<object>();
    }

    [Fact]
    public async Task DeleteGet_ReturnsRedirectToLogin_WhenUnauthorizedAccessExceptionOccurs()
    {
        // Arrange
        var extratoId = 1;
        _mockExtratoService.ObterExtratoDetalhesAsync(extratoId).ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.Delete(extratoId);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Login");
        redirectResult.ControllerName.Should().Be("Account");
        await _mockExtratoService.Received(1).ObterExtratoDetalhesAsync(extratoId);
        // CORREÇÃO: O controller NÃO define TempData["ErrorMessage"] aqui.
        _mockTempData.DidNotReceiveWithAnyArgs()["ErrorMessage"] = Arg.Any<object>();
    }

    [Fact]
    public async Task DeleteGet_ReturnsNotFoundAndSetsErrorMessage_WhenGeneralExceptionOccurs()
    {
        // Arrange
        var extratoId = 1;
        var errorMessage = "Erro ao carregar para exclusão.";
        _mockExtratoService.ObterExtratoDetalhesAsync(extratoId).ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.Delete(extratoId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        _mockTempData.Received(1)["ErrorMessage"] = "Ocorreu um erro ao carregar o extrato para exclusão: " + errorMessage;
        await _mockExtratoService.Received(1).ObterExtratoDetalhesAsync(extratoId);
    }

    // --- Testes para o método DeleteConfirmed (POST) ---

    [Fact]
    public async Task DeleteConfirmed_ReturnsRedirectToIndexAndSetsSuccessMessage_WhenSuccessful()
    {
        // Arrange
        var extratoId = 1;
        _mockExtratoService.ExcluirExtratoAsync(extratoId).Returns(true);

        // Act
        var result = await _controller.DeleteConfirmed(extratoId);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Index");
        _mockTempData.Received(1)["SuccessMessage"] = "Extrato excluído com sucesso!";
        await _mockExtratoService.Received(1).ExcluirExtratoAsync(extratoId);
    }

    [Fact]
    public async Task DeleteConfirmed_ReturnsRedirectToDeleteAndSetsErrorMessage_WhenExclusionFails()
    {
        // Arrange
        var extratoId = 1;
        _mockExtratoService.ExcluirExtratoAsync(extratoId).Returns(false);

        // Act
        var result = await _controller.DeleteConfirmed(extratoId);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Delete");
        redirectResult.RouteValues["id"].Should().Be(extratoId);
        _mockTempData.Received(1)["ErrorMessage"] = "Não foi possível excluir o extrato.";
        await _mockExtratoService.Received(1).ExcluirExtratoAsync(extratoId);
    }

    [Fact]
    public async Task DeleteConfirmed_ReturnsRedirectToDeleteAndSetsApplicationError_WhenApplicationExceptionOccurs()
    {
        // Arrange
        var extratoId = 1;
        var errorMessage = "Não foi possível excluir porque possui movimentações.";
        _mockExtratoService.ExcluirExtratoAsync(extratoId).ThrowsAsync(new ApplicationException(errorMessage));

        // Act
        var result = await _controller.DeleteConfirmed(extratoId);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Delete");
        redirectResult.RouteValues["id"].Should().Be(extratoId);
        _mockTempData.Received(1)["DeleteError"] = errorMessage;
        await _mockExtratoService.Received(1).ExcluirExtratoAsync(extratoId);
    }

    [Fact]
    public async Task DeleteConfirmed_ReturnsRedirectToLogin_WhenUnauthorizedAccessExceptionOccurs()
    {
        // Arrange
        var extratoId = 1;
        _mockExtratoService.ExcluirExtratoAsync(extratoId).ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.DeleteConfirmed(extratoId);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Login");
        redirectResult.ControllerName.Should().Be("Account");
        await _mockExtratoService.Received(1).ExcluirExtratoAsync(extratoId);
        // CORREÇÃO: O controller NÃO define TempData["ErrorMessage"] aqui.
        _mockTempData.DidNotReceiveWithAnyArgs()["ErrorMessage"] = Arg.Any<object>();
    }

    [Fact]
    public async Task DeleteConfirmed_ReturnsRedirectToDeleteAndSetsGenericError_WhenGeneralExceptionOccurs()
    {
        // Arrange
        var extratoId = 1;
        var errorMessage = "Ocorreu um erro inesperado durante a exclusão.";
        _mockExtratoService.ExcluirExtratoAsync(extratoId).ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.DeleteConfirmed(extratoId);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Delete");
        redirectResult.RouteValues["id"].Should().Be(extratoId);
        _mockTempData.Received(1)["DeleteError"] = "Ocorreu um erro inesperado ao excluir o extrato: " + errorMessage;
        await _mockExtratoService.Received(1).ExcluirExtratoAsync(extratoId);
    }
}