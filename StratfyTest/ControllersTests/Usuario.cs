using Xunit;
using NSubstitute;
using FluentAssertions;
using Bogus;
using STRATFY.Controllers;
using STRATFY.Interfaces.IServices;
using STRATFY.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using NSubstitute.ExceptionExtensions;

public class UsuariosControllerTests
{
    private readonly IUsuarioService _mockUsuarioService;
    private readonly IAccountService _mockAccountService;
    private readonly UsuariosController _controller;
    private readonly ITempDataDictionary _mockTempData;
    private readonly Faker<Usuario> _usuarioFaker;

    public UsuariosControllerTests()
    {
        _mockUsuarioService = Substitute.For<IUsuarioService>();
        _mockAccountService = Substitute.For<IAccountService>();
        _mockTempData = Substitute.For<ITempDataDictionary>();

        _controller = new UsuariosController(_mockUsuarioService, _mockAccountService);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Email, "teste@teste.com")
            }, "TestAuth"))
            },
        };
        _controller.TempData = _mockTempData;

        _usuarioFaker = new Faker<Usuario>()
            .RuleFor(u => u.Id, f => f.IndexFaker + 1)
            .RuleFor(u => u.Nome, f => f.Person.FullName)
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.Senha, f => f.Internet.Password());
    }

    // --- Testes para o método Index ---
    [Fact]
    public async Task Index_ReturnsViewWithListOfUsers_WhenServiceReturnsUsers()
    {
        // Arrange
        var usuarios = _usuarioFaker.Generate(3);
        _mockUsuarioService.ObterTodosUsuariosAsync().Returns(usuarios);

        // Act
        var result = await _controller.Index();

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.Model.Should().BeAssignableTo<IEnumerable<Usuario>>();
        viewResult.Model.As<IEnumerable<Usuario>>().Should().HaveCount(3);
    }

    [Fact]
    public async Task Index_ReturnsViewWithEmptyListAndSetsErrorMessage_WhenServiceThrowsException()
    {
        // Arrange
        _mockUsuarioService.ObterTodosUsuariosAsync().Throws(new Exception("Erro de teste."));
        _mockTempData["ErrorMessage"] = null;

        // Act
        var result = await _controller.Index();

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.Model.Should().BeAssignableTo<IEnumerable<Usuario>>();
        viewResult.Model.As<IEnumerable<Usuario>>().Should().BeEmpty();
        _mockTempData["ErrorMessage"].Should().NotBeNull().And.BeOfType<string>().Which.Should().NotBeNullOrEmpty();
    }

    // --- Testes para o método Details ---
    [Fact]
    public async Task Details_ReturnsNotFound_WhenIdIsNull()
    {
        // Arrange
        int? id = null;

        // Act
        var result = await _controller.Details(id);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Details_ReturnsNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        _mockUsuarioService.ObterUsuarioPorIdAsync(Arg.Any<int>()).Returns((Usuario)null);

        // Act
        var result = await _controller.Details(1);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Details_ReturnsViewWithUser_WhenUserExists()
    {
        // Arrange
        var usuario = _usuarioFaker.Generate();
        _mockUsuarioService.ObterUsuarioPorIdAsync(usuario.Id).Returns(usuario);

        // Act
        var result = await _controller.Details(usuario.Id);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.Model.Should().BeOfType<Usuario>();
        viewResult.Model.As<Usuario>().Id.Should().Be(usuario.Id);
    }

    [Fact]
    public async Task Details_ReturnsNotFoundAndSetsErrorMessage_WhenServiceThrowsException()
    {
        // Arrange
        _mockUsuarioService.ObterUsuarioPorIdAsync(Arg.Any<int>()).Throws(new Exception("Erro de detalhes."));
        _mockTempData["ErrorMessage"] = null;

        // Act
        var result = await _controller.Details(1);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        _mockTempData["ErrorMessage"].Should().NotBeNull().And.BeOfType<string>().Which.Should().NotBeNullOrEmpty();
    }

    // --- Testes para o método Create (GET) ---
    [Fact]
    public void Create_ReturnsView()
    {
        // Arrange

        // Act
        var result = _controller.Create();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    // --- Testes para o método Create (POST) ---
    [Fact]
    public async Task Create_ReturnsRedirectToAction_WhenModelStateIsValidAndUserIsCreated()
    {
        // Arrange
        var usuario = _usuarioFaker.Generate();
        var senhaOriginal = "SenhaSegura123"; // Senha válida para o cenário de sucesso

        _mockUsuarioService.CriarUsuarioAsync(Arg.Any<Usuario>(), Arg.Any<string>()).Returns(usuario);
        _mockAccountService.LoginAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(true));
        _mockTempData["SuccessMessage"] = null;

        // Act
        var result = await _controller.Create(new Usuario { Nome = usuario.Nome, Email = usuario.Email, Senha = senhaOriginal });

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Index");
        redirectResult.ControllerName.Should().Be("Extratos");
        _mockTempData["SuccessMessage"].Should().NotBeNull().And.BeOfType<string>().Which.Should().NotBeNullOrEmpty();

        await _mockUsuarioService.Received(1).CriarUsuarioAsync(Arg.Is<Usuario>(u => u.Email == usuario.Email), senhaOriginal);
        await _mockAccountService.Received(1).LoginAsync(usuario.Email, senhaOriginal);
    }

    [Fact]
    public async Task Create_ReturnsViewWithModelError_WhenEmailAlreadyExists()
    {
        // Arrange
        var usuario = _usuarioFaker.Generate();
        var senhaOriginal = "SenhaSegura123";
        _mockUsuarioService.CriarUsuarioAsync(Arg.Any<Usuario>(), Arg.Any<string>())
                           .Throws(new ApplicationException("Este e-mail já está cadastrado."));

        // Act
        var result = await _controller.Create(new Usuario { Nome = usuario.Nome, Email = usuario.Email, Senha = senhaOriginal });

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.ViewData.ModelState.Should().ContainSingle(m => m.Value.Errors.Any(e => e.ErrorMessage == "Este e-mail já está cadastrado."));
        viewResult.Model.Should().BeOfType<Usuario>();
    }

    [Fact]
    public async Task Create_ReturnsViewWithModelError_WhenPasswordIsEmpty()
    {
        // Arrange
        var usuario = _usuarioFaker.Generate();
        var senhaVazia = "";
        _mockUsuarioService.CriarUsuarioAsync(Arg.Any<Usuario>(), Arg.Any<string>())
                           .Throws(new ArgumentException("A senha não pode ser vazia."));

        // Act
        var result = await _controller.Create(new Usuario { Nome = usuario.Nome, Email = usuario.Email, Senha = senhaVazia });

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.ViewData.ModelState.Should().ContainSingle(m => m.Value.Errors.Any(e => e.ErrorMessage == "A senha não pode ser vazia."));
        viewResult.Model.Should().BeOfType<Usuario>();
    }

    [Fact]
    public async Task Create_ReturnsViewWithModelError_WhenGeneralExceptionOccurs()
    {
        // Arrange
        var usuario = _usuarioFaker.Generate();
        var senhaOriginal = "SenhaSegura123";
        _mockUsuarioService.CriarUsuarioAsync(Arg.Any<Usuario>(), Arg.Any<string>())
                           .Throws(new Exception("Erro inesperado."));

        // Act
        var result = await _controller.Create(new Usuario { Nome = usuario.Nome, Email = usuario.Email, Senha = senhaOriginal });

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.ViewData.ModelState.Should().ContainSingle(m => m.Value.Errors.Any(e => e.ErrorMessage == "Ocorreu um erro inesperado ao cadastrar o usuário."));
        viewResult.Model.Should().BeOfType<Usuario>();
    }

    [Fact]
    public async Task Create_ReturnsViewWithModel_WhenModelStateIsInvalid()
    {
        // Arrange
        var usuarioInvalido = new Usuario { Nome = "A", Email = "invalido", Senha = "123" };
        _controller.ModelState.AddModelError("Nome", "Nome muito curto.");

        // Act
        var result = await _controller.Create(usuarioInvalido);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.Model.Should().BeOfType<Usuario>();
        viewResult.ViewData.ModelState.IsValid.Should().BeFalse();
    }

    // --- Testes para o método Edit (GET) ---
    [Fact]
    public async Task Edit_ReturnsViewWithViewModel_WhenUserIsFound()
    {
        // Arrange
        var usuarioLogado = _usuarioFaker.Generate();
        _mockUsuarioService.ObterUsuarioLogadoAsync().Returns(usuarioLogado);
        _mockUsuarioService.ObterUsuarioPorIdAsync(usuarioLogado.Id).Returns(usuarioLogado);

        // Act
        var result = await _controller.Edit();

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.Model.Should().BeOfType<UsuarioEditVM>();
        var model = viewResult.Model.As<UsuarioEditVM>();
        model.Id.Should().Be(usuarioLogado.Id);
        model.Nome.Should().Be(usuarioLogado.Nome);
        model.Email.Should().Be(usuarioLogado.Email);
    }

    [Fact]
    public async Task EditGet_DeveRetornarViewDeErro_QuandoUsuarioLogadoNaoForEncontrado() // Novo nome para refletir a expectativa
    {
        // Arrange
        // Configura o mock para ObterUsuarioLogadoAsync retornar null
        _mockUsuarioService.ObterUsuarioLogadoAsync().Returns((Usuario)null);
        _mockTempData["ErrorMessage"] = null;

        // Act
        var result = await _controller.Edit();

        // Assert
        // O controller deve retornar um ViewResult (View("Error")) neste cenário
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.ViewName.Should().Be("Error"); // Verifica se é a View de erro

        // Confere se ObterUsuarioPorIdAsync NÃO foi chamado, pois o ObterUsuarioLogadoAsync já retornou null
        await _mockUsuarioService.DidNotReceive().ObterUsuarioPorIdAsync(Arg.Any<int>());

        // Confere a mensagem de erro no TempData
        _mockTempData["ErrorMessage"].Should().NotBeNull().And.BeOfType<string>().Which.Should().NotBeNullOrEmpty();
        //_mockTempData["ErrorMessage"].Should().Contain("Ocorreu um erro ao carregar os dados do usuário."); // Mensagem específica do seu catch genérico
    }


    [Fact]
    public async Task Edit_ReturnsRedirectToLogin_WhenUnauthorizedAccessOccurs()
    {
        // Arrange
        _mockUsuarioService.ObterUsuarioLogadoAsync().Throws(new UnauthorizedAccessException("Acesso negado."));
        _mockTempData["ErrorMessage"] = null;

        // Act
        var result = await _controller.Edit();

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Login");
        redirectResult.ControllerName.Should().Be("Account");
        _mockTempData["ErrorMessage"].Should().NotBeNull().And.BeOfType<string>().Which.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Edit_ReturnsErrorView_WhenGeneralExceptionOccurs()
    {
        // Arrange
        _mockUsuarioService.ObterUsuarioLogadoAsync().Throws(new Exception("Erro inesperado no GET Edit."));
        _mockTempData["ErrorMessage"] = null;

        // Act
        var result = await _controller.Edit();

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.ViewName.Should().Be("Error");
        _mockTempData["ErrorMessage"].Should().NotBeNull().And.BeOfType<string>().Which.Should().NotBeNullOrEmpty();
    }

    // --- Testes para o método Edit (POST) ---
    [Fact]
    public async Task EditPost_ReturnsRedirectToAction_WhenUpdateIsSuccessfulAndPasswordIsNotChanged()
    {
        // Arrange
        var model = new UsuarioEditVM
        {
            Id = 1,
            Nome = "Novo Nome",
            Email = "novo@email.com",
            NovaSenha = null,
            ConfirmarNovaSenha = null
        };
        _mockTempData["SuccessMessage"] = null;

        // Act
        var result = await _controller.Edit(model);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Index");
        redirectResult.ControllerName.Should().Be("Extratos");
        _mockTempData["SuccessMessage"].Should().NotBeNull().And.BeOfType<string>().Which.Should().NotBeNullOrEmpty();
        await _mockUsuarioService.Received(1).AtualizarUsuarioAsync(model);
    }

    [Fact]
    public async Task EditPost_ReturnsRedirectToAction_WhenUpdateIsSuccessfulAndPasswordIsChanged()
    {
        // Arrange
        var model = new UsuarioEditVM
        {
            Id = 1,
            Nome = "Novo Nome",
            Email = "novo@email.com",
            NovaSenha = "NovaSenha123",
            ConfirmarNovaSenha = "NovaSenha123"
        };
        _mockTempData["SuccessMessage"] = null;

        // Act
        var result = await _controller.Edit(model);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Index");
        redirectResult.ControllerName.Should().Be("Extratos");
        _mockTempData["SuccessMessage"].Should().NotBeNull().And.BeOfType<string>().Which.Should().NotBeNullOrEmpty();
        await _mockUsuarioService.Received(1).AtualizarUsuarioAsync(model);
    }

    [Fact]
    public async Task EditPost_ReturnsViewWithModelError_WhenModelStateIsInvalid()
    {
        // Arrange
        var model = new UsuarioEditVM
        {
            Id = 1,
            Nome = "N",
            Email = "emailinvalido",
            NovaSenha = "123",
            ConfirmarNovaSenha = "123"
        };
        _controller.ModelState.AddModelError("Nome", "Nome muito curto.");
        _controller.ModelState.AddModelError("Email", "Formato de e-mail inválido.");
        _controller.ModelState.AddModelError("NovaSenha", "A senha deve ter entre 6 e 100 caracteres.");

        // Act
        var result = await _controller.Edit(model);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.Model.Should().BeOfType<UsuarioEditVM>();
        viewResult.ViewData.ModelState.IsValid.Should().BeFalse();
        viewResult.ViewData.ModelState["Nome"].Errors.Should().NotBeEmpty();
        viewResult.ViewData.ModelState["Email"].Errors.Should().NotBeEmpty();
        viewResult.ViewData.ModelState["NovaSenha"].Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task EditPost_RemovesPasswordValidationErrors_WhenPasswordFieldsAreEmpty()
    {
        // Arrange
        var model = new UsuarioEditVM
        {
            Id = 1,
            Nome = "Nome Válido",
            Email = "valido@email.com",
            NovaSenha = null,
            ConfirmarNovaSenha = null
        };

        _controller.ModelState.AddModelError(nameof(model.NovaSenha), "Erro de validação de senha.");
        _controller.ModelState.AddModelError(nameof(model.ConfirmarNovaSenha), "Erro de validação de confirmação de senha.");


        // Act
        var result = await _controller.Edit(model);

        // Assert
        _controller.ModelState.IsValid.Should().BeTrue();
        _controller.ModelState.Should().NotContainKey(nameof(model.NovaSenha));
        _controller.ModelState.Should().NotContainKey(nameof(model.ConfirmarNovaSenha));

        result.Should().BeOfType<RedirectToActionResult>();
    }


    [Fact]
    public async Task EditPost_ReturnsViewWithModelError_WhenApplicationExceptionOccurs()
    {
        // Arrange
        var model = new UsuarioEditVM
        {
            Id = 1,
            Nome = "Nome",
            Email = "email@valido.com",
            NovaSenha = null,
            ConfirmarNovaSenha = null
        };
        _mockUsuarioService.AtualizarUsuarioAsync(Arg.Any<UsuarioEditVM>()).Throws(new ApplicationException("E-mail já em uso."));

        // Act
        var result = await _controller.Edit(model);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.Model.Should().BeOfType<UsuarioEditVM>();
        viewResult.ViewData.ModelState.Should().ContainSingle(m => m.Value.Errors.Any(e => e.ErrorMessage == "E-mail já em uso."));
    }

    // Dentro do seu UsuariosControllerTests.cs
    [Fact]
    public async Task EditPost_ReturnsViewWithGenericErrorMessage_WhenGeneralExceptionOccurs()
    {
        // Arrange
        var model = new UsuarioEditVM // Este é o modelo que é passado para o controller
        {
            Id = 1,
            Nome = "Nome",
            Email = "email@valido.com",
            NovaSenha = null,
            ConfirmarNovaSenha = null
        };
        _mockUsuarioService.AtualizarUsuarioAsync(Arg.Any<UsuarioEditVM>()).Throws(new Exception("Erro geral."));
        _mockTempData["ErrorMessage"] = null;

        // Act
        var result = await _controller.Edit(model);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.ViewName.Should().BeNull();

        // --- CORREÇÃO AQUI NO TESTE ---
        // Espera que o modelo seja do tipo UsuarioEditVM (o mesmo que foi passado para o controller)
        viewResult.Model.Should().BeOfType<UsuarioEditVM>();
        viewResult.Model.As<UsuarioEditVM>().Id.Should().Be(model.Id); // Ou outras propriedades para verificar que é o mesmo modelo
                                                                       // Não mais .Should().BeNull();

        // Verifica que o ModelState ESTÁ vazio (conforme a implementação do controller)
        viewResult.ViewData.ModelState.Should().BeEmpty();

        // Verifica que a mensagem de erro está no TempData
        _mockTempData["ErrorMessage"].Should().NotBeNull().And.BeOfType<string>().Which.Should().NotBeNullOrEmpty();
        //_mockTempData["ErrorMessage"].Should().Contain("Ocorreu um erro inesperado ao atualizar o usuário.");
    }


    // --- Testes para o método Delete (GET) ---
    [Fact]
    public async Task Delete_ReturnsNotFound_WhenIdIsNull()
    {
        // Act
        var result = await _controller.Delete(null);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        _mockUsuarioService.ObterUsuarioPorIdAsync(Arg.Any<int>()).Returns((Usuario)null);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_ReturnsViewWithUser_WhenUserExists()
    {
        // Arrange
        var usuario = _usuarioFaker.Generate();
        _mockUsuarioService.ObterUsuarioPorIdAsync(usuario.Id).Returns(usuario);

        // Act
        var result = await _controller.Delete(usuario.Id);

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.Model.Should().BeOfType<Usuario>();
        viewResult.Model.As<Usuario>().Id.Should().Be(usuario.Id);
    }

    [Fact]
    public async Task Delete_ReturnsNotFoundAndSetsErrorMessage_WhenServiceThrowsException()
    {
        // Arrange
        _mockUsuarioService.ObterUsuarioPorIdAsync(Arg.Any<int>()).Throws(new Exception("Erro ao buscar para deletar."));
        _mockTempData["ErrorMessage"] = null;

        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        _mockTempData["ErrorMessage"].Should().NotBeNull().And.BeOfType<string>().Which.Should().NotBeNullOrEmpty();
    }

    // --- Testes para o método DeleteConfirmed (POST) ---
    [Fact]
    public async Task DeleteConfirmed_ReturnsRedirectToLogin_WhenDeletionIsSuccessful()
    {
        // Arrange
        _mockUsuarioService.ExcluirUsuarioAsync(Arg.Any<int>()).Returns(Task.CompletedTask);
        _mockAccountService.LogoutAsync().Returns(Task.CompletedTask);
        _mockTempData["SuccessMessage"] = null;

        // Act
        var result = await _controller.DeleteConfirmed(1);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be("Index");
        redirectResult.ControllerName.Should().Be("Login");
        _mockTempData["SuccessMessage"].Should().NotBeNull().And.BeOfType<string>().Which.Should().NotBeNullOrEmpty();
        await _mockUsuarioService.Received(1).ExcluirUsuarioAsync(1);
        await _mockAccountService.Received(1).LogoutAsync();
    }

    [Fact]
    public async Task DeleteConfirmed_ReturnsRedirectToDeleteWithErrorMessage_WhenApplicationExceptionOccurs()
    {
        // Arrange
        _mockUsuarioService.ExcluirUsuarioAsync(Arg.Any<int>()).Throws(new ApplicationException("Usuário possui extratos vinculados."));
        _mockTempData["DeleteError"] = null;

        // Act
        var result = await _controller.DeleteConfirmed(1);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be(nameof(UsuariosController.Delete));
        redirectResult.RouteValues["id"].Should().Be(1);
        _mockTempData["DeleteError"].Should().NotBeNull().And.BeOfType<string>().Which.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DeleteConfirmed_ReturnsForbid_WhenUnauthorizedAccessOccurs()
    {
        // Arrange
        _mockUsuarioService.ExcluirUsuarioAsync(Arg.Any<int>()).Throws(new UnauthorizedAccessException());

        // Act
        var result = await _controller.DeleteConfirmed(1);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task DeleteConfirmed_ReturnsRedirectToDeleteWithGenericErrorMessage_WhenGeneralExceptionOccurs()
    {
        // Arrange
        _mockUsuarioService.ExcluirUsuarioAsync(Arg.Any<int>()).Throws(new Exception("Erro inesperado na exclusão."));
        _mockTempData["DeleteError"] = null;

        // Act
        var result = await _controller.DeleteConfirmed(1);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result.As<RedirectToActionResult>();
        redirectResult.ActionName.Should().Be(nameof(UsuariosController.Delete));
        redirectResult.RouteValues["id"].Should().Be(1);
        _mockTempData["DeleteError"].Should().NotBeNull().And.BeOfType<string>().Which.Should().NotBeNullOrEmpty();
    }
}