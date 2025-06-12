using Xunit;
using NSubstitute;
using FluentAssertions;
using NSubstitute.ExceptionExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

// Importe as suas classes de projeto
using STRATFY.Interfaces.IRepositories;
using STRATFY.Interfaces.IServices;
using STRATFY.Models;
using STRATFY.Services; // Sua AccountService
using STRATFY.Helpers; // Seu PasswordHasher (você pode mockar ele se preferir)

public class AccountServiceTests
{
    private readonly IRepositoryUsuario _mockUsuarioRepository;
    private readonly IHttpContextAccessor _mockHttpContextAccessor;
    private readonly AccountService _accountService;

    // Mocks adicionais para o HttpContextAccessor
    private readonly HttpContext _mockHttpContext;
    private readonly IAuthenticationService _mockAuthenticationService; // Mock para SignInAsync/SignOutAsync

    public AccountServiceTests()
    {
        _mockUsuarioRepository = Substitute.For<IRepositoryUsuario>();
        _mockHttpContextAccessor = Substitute.For<IHttpContextAccessor>();

        // Configurar o HttpContext e AuthenticationService mocks
        _mockHttpContext = Substitute.For<HttpContext>();
        _mockAuthenticationService = Substitute.For<IAuthenticationService>();

        // Lembre-se que _mockHttpContextAccessor.HttpContext deve retornar _mockHttpContext
        _mockHttpContextAccessor.HttpContext.Returns(_mockHttpContext);

        // Como o AuthenticationService é acessado via HttpContext.RequestServices, precisamos mockar isso.
        // Isso é um pouco mais complexo e pode ser feito de algumas maneiras.
        // A mais comum é mockar IServiceProvider e GetService.
        var mockServiceProvider = Substitute.For<IServiceProvider>();
        mockServiceProvider.GetService(typeof(IAuthenticationService))
                           .Returns(_mockAuthenticationService);

        _mockHttpContext.RequestServices.Returns(mockServiceProvider);


        _accountService = new AccountService(_mockUsuarioRepository, _mockHttpContextAccessor);
    }

    // --- Testes para LoginAsync ---

    [Fact]
    public async Task LoginAsync_ShouldReturnTrueAndSignIn_WhenValidCredentials()
    {
        // Arrange
        var email = "test@example.com";
        var senha = "Password123";
        var hashedPassword = PasswordHasher.HashPassword(senha); // Hash da senha para o mock

        var usuario = new Usuario { Id = 1, Nome = "Test User", Email = email, Senha = hashedPassword }; // Senha já hashada

        _mockUsuarioRepository.ObterUsuarioPorEmailAsync(email).Returns(usuario);

        // Configurar o mock de VerifyPassword do PasswordHasher
        // Como PasswordHasher é estático, não podemos mocká-lo diretamente com NSubstitute.
        // Você pode:
        // 1. Chamar o método real (como feito aqui, para simplificar).
        // 2. Encapsular PasswordHasher em uma interface e mockar a interface. (Mais robusto para TDD)
        // Por agora, presumimos que PasswordHasher.VerifyPassword funciona corretamente.

        // Act
        var result = await _accountService.LoginAsync(email, senha);

        // Assert
        result.Should().BeTrue();
        await _mockUsuarioRepository.Received(1).ObterUsuarioPorEmailAsync(email);

        // Verificar se SignInAsync foi chamado com os argumentos corretos
        await _mockAuthenticationService.Received(1).SignInAsync(
            _mockHttpContext,
            CookieAuthenticationDefaults.AuthenticationScheme,
            Arg.Is<ClaimsPrincipal>(p => p.Identity.Name == usuario.Nome && p.Identity.IsAuthenticated),
            Arg.Any<AuthenticationProperties>()
        );
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnTrueAndSignInWithPersistentCookie_WhenIsPersistentIsTrue()
    {
        // Arrange
        var email = "test@example.com";
        var senha = "Password123";
        var hashedPassword = PasswordHasher.HashPassword(senha);

        var usuario = new Usuario { Id = 1, Nome = "Test User", Email = email, Senha = hashedPassword };

        _mockUsuarioRepository.ObterUsuarioPorEmailAsync(email).Returns(usuario);

        // Act
        var result = await _accountService.LoginAsync(email, senha, isPersistent: true);

        // Assert
        result.Should().BeTrue();
        await _mockUsuarioRepository.Received(1).ObterUsuarioPorEmailAsync(email);

        await _mockAuthenticationService.Received(1).SignInAsync(
            _mockHttpContext,
            CookieAuthenticationDefaults.AuthenticationScheme,
            Arg.Is<ClaimsPrincipal>(p => p.Identity.Name == usuario.Nome),
            Arg.Is<AuthenticationProperties>(p => p.IsPersistent == true) // Verifica IsPersistent
        );
    }

    [Theory]
    [InlineData(null, "senha")]
    [InlineData("email", null)]
    [InlineData("", "senha")]
    [InlineData("email", "")]
    public async Task LoginAsync_ShouldThrowArgumentNullException_WhenEmailOrPasswordIsNullOrEmpty(string email, string senha)
    {
        // CORREÇÃO AQUI: Ajustando a mensagem esperada para o formato real da ArgumentNullException
        var expectedMessage = "Value cannot be null. (Parameter 'Email e senha são obrigatórios.')";

        // Act
        Func<Task> act = async () => await _accountService.LoginAsync(email, senha);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
                 .WithMessage(expectedMessage); // Agora espera a mensagem exata
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnFalse_WhenUserNotFound()
    {
        // Arrange
        var email = "nonexistent@example.com";
        var senha = "Password123";

        _mockUsuarioRepository.ObterUsuarioPorEmailAsync(email).Returns((Usuario)null); // Usuário não encontrado

        // Act
        var result = await _accountService.LoginAsync(email, senha);

        // Assert
        result.Should().BeFalse();
        await _mockUsuarioRepository.Received(1).ObterUsuarioPorEmailAsync(email);
        await _mockAuthenticationService.DidNotReceive().SignInAsync(
            Arg.Any<HttpContext>(), Arg.Any<string>(), Arg.Any<ClaimsPrincipal>(), Arg.Any<AuthenticationProperties>()
        );
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnFalse_WhenInvalidPassword()
    {
        // Arrange
        var email = "test@example.com";
        var senha = "WrongPassword";
        var correctHashedPassword = PasswordHasher.HashPassword("CorrectPassword");

        var usuario = new Usuario { Id = 1, Nome = "Test User", Email = email, Senha = correctHashedPassword };

        _mockUsuarioRepository.ObterUsuarioPorEmailAsync(email).Returns(usuario);

        // Act
        var result = await _accountService.LoginAsync(email, senha); // Passa a senha errada

        // Assert
        result.Should().BeFalse();
        await _mockUsuarioRepository.Received(1).ObterUsuarioPorEmailAsync(email);
        await _mockAuthenticationService.DidNotReceive().SignInAsync(
            Arg.Any<HttpContext>(), Arg.Any<string>(), Arg.Any<ClaimsPrincipal>(), Arg.Any<AuthenticationProperties>()
        );
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowException_WhenRepositoryThrowsException()
    {
        // Arrange
        var email = "test@example.com";
        var senha = "Password123";

        _mockUsuarioRepository.ObterUsuarioPorEmailAsync(email).ThrowsAsync(new Exception("Database error"));

        // Act
        Func<Task> act = async () => await _accountService.LoginAsync(email, senha);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Database error");
        await _mockUsuarioRepository.Received(1).ObterUsuarioPorEmailAsync(email);
        await _mockAuthenticationService.DidNotReceive().SignInAsync(
            Arg.Any<HttpContext>(), Arg.Any<string>(), Arg.Any<ClaimsPrincipal>(), Arg.Any<AuthenticationProperties>()
        );
    }

    // --- Testes para LogoutAsync ---

    [Fact]
    public async Task LogoutAsync_ShouldCallSignOutAsync()
    {
        // Arrange
        // Nenhuma configuração específica necessária, apenas garantir que o HttpContext e AuthenticationService são acessíveis

        // Act
        await _accountService.LogoutAsync();

        // Assert
        await _mockAuthenticationService.Received(1).SignOutAsync(
            _mockHttpContext,
            CookieAuthenticationDefaults.AuthenticationScheme,
            Arg.Any<AuthenticationProperties>()
        );
    }

    [Fact]
    public async Task LogoutAsync_ShouldHandleExceptionGracefully_WhenSignOutFails()
    {
        // Arrange
        // Mockar SignOutAsync para lançar uma exceção
        _mockAuthenticationService.SignOutAsync(
            _mockHttpContext,
            CookieAuthenticationDefaults.AuthenticationScheme,
            Arg.Any<AuthenticationProperties>()
        ).ThrowsAsync(new Exception("Sign out failed"));

        // Act
        Func<Task> act = async () => await _accountService.LogoutAsync();

        // Assert
        // Pelo código da sua AccountService, ela não tem um try-catch no LogoutAsync,
        // então a exceção será propagada. O teste deve verificar a exceção.
        await act.Should().ThrowAsync<Exception>().WithMessage("Sign out failed");
    }


    // --- Testes para IsAuthenticated ---

    [Fact]
    public void IsAuthenticated_ShouldReturnTrue_WhenUserIsAuthenticated()
    {
        // Arrange
        // Configurar um ClaimsPrincipal autenticado
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "test") }, "TestAuthType");
        _mockHttpContext.User = new ClaimsPrincipal(identity);

        // Act
        var result = _accountService.IsAuthenticated();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAuthenticated_ShouldReturnFalse_WhenUserIsNotAuthenticated()
    {
        // Arrange
        // Configurar um ClaimsPrincipal não autenticado (ou sem identidade)
        var identity = new ClaimsIdentity(); // Não autenticado
        _mockHttpContext.User = new ClaimsPrincipal(identity);

        // Act
        var result = _accountService.IsAuthenticated();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAuthenticated_ShouldReturnFalse_WhenHttpContextIsNull()
    {
        // Arrange
        _mockHttpContextAccessor.HttpContext.Returns((HttpContext)null); // HttpContext nulo

        // Act
        var result = _accountService.IsAuthenticated();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAuthenticated_ShouldReturnFalse_WhenHttpContextUserIsNull()
    {
        // Arrange
        _mockHttpContext.User = null; // HttpContext.User nulo

        // Act
        var result = _accountService.IsAuthenticated();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAuthenticated_ShouldReturnFalse_WhenHttpContextUserIdentityIsNull()
    {
        // Arrange
        _mockHttpContext.User = new ClaimsPrincipal(); // User existe, mas Identity é nula
        _mockHttpContext.User.Identity.Returns((ClaimsIdentity)null); // Explícitamente mockar Identity como null

        // Act
        var result = _accountService.IsAuthenticated();

        // Assert
        result.Should().BeFalse();
    }
}