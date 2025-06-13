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
}
