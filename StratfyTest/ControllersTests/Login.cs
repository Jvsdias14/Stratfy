using Xunit;
using NSubstitute;
using FluentAssertions;
using NSubstitute.ExceptionExtensions;
using STRATFY.Controllers;
using STRATFY.Interfaces.IServices;
using STRATFY.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

public class LoginControllerTests
{
    private readonly IAccountService _mockAccountService;
    private readonly LoginController _controller;
    private readonly ITempDataDictionary _mockTempData;

    public LoginControllerTests()
    {
        _mockAccountService = Substitute.For<IAccountService>();
        _mockTempData = Substitute.For<ITempDataDictionary>();

        _controller = new LoginController(_mockAccountService);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        var mockUrlHelper = Substitute.For<IUrlHelper>();
        mockUrlHelper.IsLocalUrl(Arg.Any<string>()).Returns(x =>
        {
            string url = x.Arg<string>();
            return !string.IsNullOrEmpty(url) && (url.StartsWith("/") || url.StartsWith("~"));
        });
        _controller.Url = mockUrlHelper;
        _controller.TempData = _mockTempData;
    }

    // --- Testes para o método Index (GET) ---

    [Fact]
    public void Index_ReturnsRedirectToExtratosIndex_WhenUserIsAuthenticated()
    {
        // Arrange
        _mockAccountService.IsAuthenticated().Returns(true);

        // Act
        var result = _controller.Index();

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Index");
        redirectResult.ControllerName.Should().Be("Extratos");
    }

    [Fact]
    public void Index_ReturnsLoginView_WhenUserIsNotAuthenticatedAndNoReturnUrl()
    {
        // Arrange
        _mockAccountService.IsAuthenticated().Returns(false);

        // Act
        var result = _controller.Index();

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.ViewName.Should().Be("Login");
        viewResult.ViewData.Should().NotContainKey("Mensagem");
        viewResult.ViewData["ReturnUrl"].Should().BeNull();
    }

    [Fact]
    public void Index_ReturnsLoginViewWithMessageAndReturnUrl_WhenNotAuthenticatedAndValidReturnUrl()
    {
        // Arrange
        _mockAccountService.IsAuthenticated().Returns(false);
        var returnUrl = "/some/protected/page";

        // Act
        var result = _controller.Index(returnUrl);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.ViewName.Should().Be("Login");
        viewResult.ViewData.Should().ContainKey("Mensagem");
        viewResult.ViewData["Mensagem"].Should().Be("Você precisa estar logado para acessar essa página.");
        viewResult.ViewData["ReturnUrl"].Should().Be(returnUrl);
    }

    [Fact]
    public void Index_ReturnsLoginViewWithoutMessage_WhenNotAuthenticatedAndInvalidReturnUrl()
    {
        // Arrange
        _mockAccountService.IsAuthenticated().Returns(false);
        var returnUrl = "http://malicious.com";

        _controller.Url.IsLocalUrl(returnUrl).Returns(false);

        // Act
        var result = _controller.Index(returnUrl);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.ViewName.Should().Be("Login");
        viewResult.ViewData.Should().NotContainKey("Mensagem");
        viewResult.ViewData["ReturnUrl"].Should().Be(returnUrl);
    }

    // --- Testes para o método Login (POST) ---

    [Fact]
    public async Task LoginPost_ReturnsLoginViewWithModel_WhenModelStateIsInvalid()
    {
        // Arrange
        var model = new LoginVM { Email = "invalid", Senha = "" };
        _controller.ModelState.AddModelError("Email", "Email inválido.");
        _controller.ModelState.AddModelError("Senha", "Senha é obrigatória.");

        // Act
        var result = await _controller.Login(model);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.ViewName.Should().Be("Login");
        viewResult.Model.Should().BeOfType<LoginVM>().And.BeEquivalentTo(model);
        viewResult.ViewData.ModelState.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task LoginPost_ReturnsRedirectToExtratosIndex_WhenLoginIsSuccessfulAndNoReturnUrl()
    {
        // Arrange
        var model = new LoginVM { Email = "test@example.com", Senha = "Password123" };
        _mockAccountService.LoginAsync(model.Email, model.Senha).Returns(true);

        // Act
        var result = await _controller.Login(model);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Index");
        redirectResult.ControllerName.Should().Be("Extratos");
        await _mockAccountService.Received(1).LoginAsync(model.Email, model.Senha);
    }

    [Fact]
    public async Task LoginPost_ReturnsRedirectToReturnUrl_WhenLoginIsSuccessfulAndValidReturnUrl()
    {
        // Arrange
        var model = new LoginVM { Email = "test@example.com", Senha = "Password123" };
        var returnUrl = "/dashboard";
        _mockAccountService.LoginAsync(model.Email, model.Senha).Returns(true);

        // Act
        var result = await _controller.Login(model, returnUrl);

        // Assert
        result.Should().BeOfType<RedirectResult>();
        var redirectResult = result.As<RedirectResult>();
        redirectResult.Url.Should().Be(returnUrl);
        await _mockAccountService.Received(1).LoginAsync(model.Email, model.Senha);
    }

    [Fact]
    public async Task LoginPost_ReturnsRedirectToExtratosIndex_WhenLoginIsSuccessfulAndInvalidReturnUrl()
    {
        // Arrange
        var model = new LoginVM { Email = "test@example.com", Senha = "Password123" };
        var returnUrl = "http://malicious.com";
        _mockAccountService.LoginAsync(model.Email, model.Senha).Returns(true);

        _controller.Url.IsLocalUrl(returnUrl).Returns(false);

        // Act
        var result = await _controller.Login(model, returnUrl);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Index");
        redirectResult.ControllerName.Should().Be("Extratos");
        await _mockAccountService.Received(1).LoginAsync(model.Email, model.Senha);
    }

    [Fact]
    public async Task LoginPost_ReturnsLoginViewWithError_WhenLoginFails()
    {
        // Arrange
        var model = new LoginVM { Email = "wrong@example.com", Senha = "WrongPassword" };
        _mockAccountService.LoginAsync(model.Email, model.Senha).Returns(false);
        _controller.ViewData["Mensagem"] = null;

        // Act
        var result = await _controller.Login(model);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.ViewName.Should().Be("Login");
        viewResult.Model.Should().BeOfType<LoginVM>().And.BeEquivalentTo(model);
        viewResult.ViewData.ModelState.IsValid.Should().BeFalse();
        viewResult.ViewData.ModelState.Should().ContainSingle(m => m.Value.Errors.Any(e => e.ErrorMessage == "Email ou senha inválidos."));
        viewResult.ViewData.Should().ContainKey("Mensagem");
        viewResult.ViewData["Mensagem"].Should().Be("Email ou senha inválidos.");
        await _mockAccountService.Received(1).LoginAsync(model.Email, model.Senha);
    }

    [Fact]
    public async Task LoginPost_ReturnsLoginViewWithError_WhenArgumentNullExceptionOccurs()
    {
        // Arrange
        var model = new LoginVM { Email = "test@example.com", Senha = "Password123" };
        // CORREÇÃO: AQUI ESTÁ A MENSAGEM EXATA QUE A ArgumentNullException GERA
        var expectedErrorMessage = "Value cannot be null. (Parameter 'Email ou senha não podem ser nulos.')";
        _mockAccountService.LoginAsync(Arg.Any<string>(), Arg.Any<string>()).ThrowsAsync(new ArgumentNullException("Email ou senha não podem ser nulos."));
        // NOTA: A ArgumentNullException gerará a mensagem completa automaticamente se você fornecer o nome do parâmetro.
        // Se você quer que a mensagem seja EXATAMENTE "Email ou senha não podem ser nulos.",
        // você teria que usar uma Exception genérica ou uma Exception customizada,
        // ou ajustar a lógica do controlador para extrair só a parte da mensagem que você quer.
        // No momento, estamos ajustando o TESTE para o comportamento REAL da ArgumentNullException.


        // Act
        var result = await _controller.Login(model);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.ViewName.Should().Be("Login");
        viewResult.Model.Should().BeOfType<LoginVM>().And.BeEquivalentTo(model);
        viewResult.ViewData.ModelState.IsValid.Should().BeFalse();
        viewResult.ViewData.ModelState.Should().ContainSingle(
            m => m.Key == "" && m.Value.Errors.Any(e => e.ErrorMessage == expectedErrorMessage)
        );
        // Mensagem no ViewData["Mensagem"] não é definida neste catch, então não a verificamos.
    }

    [Fact]
    public async Task LoginPost_ReturnsLoginViewWithGenericError_WhenGeneralExceptionOccurs()
    {
        // Arrange
        var model = new LoginVM { Email = "test@example.com", Senha = "Password123" };
        _mockAccountService.LoginAsync(Arg.Any<string>(), Arg.Any<string>()).Throws(new Exception("Erro inesperado do serviço."));

        // Act
        var result = await _controller.Login(model);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.ViewName.Should().Be("Login");
        viewResult.Model.Should().BeOfType<LoginVM>().And.BeEquivalentTo(model);
        viewResult.ViewData.ModelState.IsValid.Should().BeFalse();
        viewResult.ViewData.ModelState.Should().ContainSingle(m => m.Value.Errors.Any(e => e.ErrorMessage == "Ocorreu um erro inesperado durante o login."));
    }

    // --- Testes para o método Logout (POST) ---

    [Fact]
    public async Task Logout_ReturnsRedirectToLoginIndex_WhenSuccessful()
    {
        // Arrange
        _mockAccountService.LogoutAsync().Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Logout();

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Index");
        redirectResult.ControllerName.Should().Be("Login");
        await _mockAccountService.Received(1).LogoutAsync();
    }

    [Fact]
    public async Task Logout_ShouldHandleExceptionsGracefully_IfServiceThrows()
    {
        // Arrange
        _mockAccountService.LogoutAsync().Throws(new Exception("Erro no logout."));

        // Act
        var result = await _controller.Logout();

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Index");
        redirectResult.ControllerName.Should().Be("Login");
    }

    // --- Testes para o método Cadastrar (GET) ---

    [Fact]
    public void Cadastrar_ReturnsRedirectToUsuariosCreate()
    {
        // Arrange

        // Act
        var result = _controller.Cadastrar();

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Create");
        redirectResult.ControllerName.Should().Be("Usuarios");
    }
}