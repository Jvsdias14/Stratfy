using Xunit;
using NSubstitute;
using FluentAssertions;
using NSubstitute.ExceptionExtensions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

// Importe suas classes de projeto
using STRATFY.Interfaces.IRepositories;
using STRATFY.Interfaces.IContexts;
using STRATFY.Interfaces.IServices;
using STRATFY.Models;
using STRATFY.Services; // Sua UsuarioService
using STRATFY.Helpers; // Seu PasswordHasher

namespace StratfyTest.ServicesTests
{
    public class UsuarioServiceTests
    {
        private readonly IRepositoryUsuario _mockUsuarioRepository;
        private readonly IUsuarioContexto _mockUsuarioContexto;
        private readonly UsuarioService _usuarioService;

        public UsuarioServiceTests()
        {
            _mockUsuarioRepository = Substitute.For<IRepositoryUsuario>();
            _mockUsuarioContexto = Substitute.For<IUsuarioContexto>();
            _usuarioService = new UsuarioService(_mockUsuarioRepository, _mockUsuarioContexto);
        }

        // --- Testes para ObterTodosUsuariosAsync ---

        [Fact]
        public async Task ObterTodosUsuariosAsync_ShouldReturnListOfUsers_WhenUsersExist()
        {
            // Arrange
            var usuarios = new List<Usuario>
            {
                new Usuario { Id = 1, Nome = "User One", Email = "one@test.com" },
                new Usuario { Id = 2, Nome = "User Two", Email = "two@test.com" }
            };
            _mockUsuarioRepository.SelecionarTodosAsync().Returns(usuarios);

            // Act
            var result = await _usuarioService.ObterTodosUsuariosAsync();

            // Assert
            result.Should().BeEquivalentTo(usuarios);
            await _mockUsuarioRepository.Received(1).SelecionarTodosAsync();
        }

        [Fact]
        public async Task ObterTodosUsuariosAsync_ShouldReturnEmptyList_WhenNoUsersExist()
        {
            // Arrange
            _mockUsuarioRepository.SelecionarTodosAsync().Returns(new List<Usuario>());

            // Act
            var result = await _usuarioService.ObterTodosUsuariosAsync();

            // Assert
            result.Should().BeEmpty();
            await _mockUsuarioRepository.Received(1).SelecionarTodosAsync();
        }

        // --- Testes para ObterUsuarioPorIdAsync ---

        [Fact]
        public async Task ObterUsuarioPorIdAsync_ShouldReturnUser_WhenUserExists()
        {
            // Arrange
            var userId = 1;
            var usuario = new Usuario { Id = userId, Nome = "Test User", Email = "test@test.com" };
            _mockUsuarioRepository.SelecionarChaveAsync(Arg.Is<object[]>(a => (int)a[0] == userId)).Returns(usuario);

            // Act
            var result = await _usuarioService.ObterUsuarioPorIdAsync(userId);

            // Assert
            result.Should().BeEquivalentTo(usuario);
            await _mockUsuarioRepository.Received(1).SelecionarChaveAsync(Arg.Is<object[]>(a => (int)a[0] == userId));
        }

        [Fact]
        public async Task ObterUsuarioPorIdAsync_ShouldReturnNull_WhenUserDoesNotExist()
        {
            // Arrange
            var userId = 99;
            _mockUsuarioRepository.SelecionarChaveAsync(Arg.Is<object[]>(a => (int)a[0] == userId)).Returns((Usuario)null);

            // Act
            var result = await _usuarioService.ObterUsuarioPorIdAsync(userId);

            // Assert
            result.Should().BeNull();
            await _mockUsuarioRepository.Received(1).SelecionarChaveAsync(Arg.Is<object[]>(a => (int)a[0] == userId));
        }

        // --- Testes para ObterUsuarioLogadoAsync ---

        [Fact]
        public async Task ObterUsuarioLogadoAsync_ShouldReturnUser_WhenUserIsLoggedIn()
        {
            // Arrange
            var userId = 1;
            var usuario = new Usuario { Id = userId, Nome = "Logged In User", Email = "logged@test.com" };
            _mockUsuarioContexto.ObterUsuarioId().Returns(userId);
            _mockUsuarioRepository.SelecionarChaveAsync(Arg.Is<object[]>(a => (int)a[0] == userId)).Returns(usuario);

            // Act
            var result = await _usuarioService.ObterUsuarioLogadoAsync();

            // Assert
            result.Should().BeEquivalentTo(usuario);
            _mockUsuarioContexto.Received(1).ObterUsuarioId();
            await _mockUsuarioRepository.Received(1).SelecionarChaveAsync(Arg.Is<object[]>(a => (int)a[0] == userId));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task ObterUsuarioLogadoAsync_ShouldReturnNull_WhenUserIdIsInvalid(int invalidUserId)
        {
            // Arrange
            _mockUsuarioContexto.ObterUsuarioId().Returns(invalidUserId);

            // Act
            var result = await _usuarioService.ObterUsuarioLogadoAsync();

            // Assert
            result.Should().BeNull();
            _mockUsuarioContexto.Received(1).ObterUsuarioId();
            await _mockUsuarioRepository.DidNotReceive().SelecionarChaveAsync(Arg.Any<object[]>()); // Não deve chamar o repositório
        }

        [Fact]
        public async Task ObterUsuarioLogadoAsync_ShouldReturnNull_WhenLoggedInUserDoesNotExistInRepo()
        {
            // Arrange
            var userId = 1;
            _mockUsuarioContexto.ObterUsuarioId().Returns(userId);
            _mockUsuarioRepository.SelecionarChaveAsync(Arg.Is<object[]>(a => (int)a[0] == userId)).Returns((Usuario)null);

            // Act
            var result = await _usuarioService.ObterUsuarioLogadoAsync();

            // Assert
            result.Should().BeNull();
            _mockUsuarioContexto.Received(1).ObterUsuarioId();
            await _mockUsuarioRepository.Received(1).SelecionarChaveAsync(Arg.Is<object[]>(a => (int)a[0] == userId));
        }

        // --- Testes para CriarUsuarioAsync ---

        [Fact]
        public async Task CriarUsuarioAsync_ShouldCreateUserAndHashPassword_WhenValidData()
        {
            // Arrange
            var usuario = new Usuario { Nome = "New User", Email = "new@test.com" };
            var senha = "SecurePassword123";

            _mockUsuarioRepository.ExisteUsuarioComEmailAsync(usuario.Email).Returns(false);

            // CORREÇÃO: Capturar o usuário passado para IncluirAsync para verificar a senha
            Usuario capturedUsuario = null;
            _mockUsuarioRepository.IncluirAsync(Arg.Do<Usuario>(u => capturedUsuario = u)).Returns(
                x =>
                {
                    var u = x.Arg<Usuario>();
                    u.Id = 1; // Simular a atribuição de ID pelo DB
                    return u;
                });

            // Act
            var result = await _usuarioService.CriarUsuarioAsync(usuario, senha);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(1);
            result.Nome.Should().Be(usuario.Nome);
            result.Email.Should().Be(usuario.Email);

            // CORREÇÃO: Verificar se a senha hashada pode ser verificada com a senha original
            // Isso é crucial porque PasswordHasher.HashPassword gera um salt diferente a cada vez.
            PasswordHasher.VerifyPassword(senha, capturedUsuario.Senha).Should().BeTrue();

            await _mockUsuarioRepository.Received(1).ExisteUsuarioComEmailAsync(usuario.Email);
            await _mockUsuarioRepository.Received(1).IncluirAsync(Arg.Any<Usuario>()); // Verificação mais flexível para o argumento
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")] // Este cenário agora disparará a exceção graças ao IsNullOrWhiteSpace
        public async Task CriarUsuarioAsync_ShouldThrowArgumentException_WhenPasswordIsNullOrEmpty(string senha)
        {
            // Arrange
            var usuario = new Usuario { Nome = "Invalid User", Email = "invalid@test.com" };

            // Act
            Func<Task> act = async () => await _usuarioService.CriarUsuarioAsync(usuario, senha);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                     .WithMessage("A senha é obrigatória.");
            await _mockUsuarioRepository.DidNotReceive().ExisteUsuarioComEmailAsync(Arg.Any<string>());
            await _mockUsuarioRepository.DidNotReceive().IncluirAsync(Arg.Any<Usuario>());
        }

        [Fact]
        public async Task CriarUsuarioAsync_ShouldThrowApplicationException_WhenEmailAlreadyExists()
        {
            // Arrange
            var usuario = new Usuario { Nome = "Existing User", Email = "existing@test.com" };
            var senha = "Password123";
            _mockUsuarioRepository.ExisteUsuarioComEmailAsync(usuario.Email).Returns(true);

            // Act
            Func<Task> act = async () => await _usuarioService.CriarUsuarioAsync(usuario, senha);

            // Assert
            await act.Should().ThrowAsync<ApplicationException>()
                     .WithMessage("Já existe um usuário cadastrado com este e-mail.");
            await _mockUsuarioRepository.Received(1).ExisteUsuarioComEmailAsync(usuario.Email);
            await _mockUsuarioRepository.DidNotReceive().IncluirAsync(Arg.Any<Usuario>());
        }

        [Fact]
        public async Task CriarUsuarioAsync_ShouldThrowException_WhenRepositoryFailsToInsert()
        {
            // Arrange
            var usuario = new Usuario { Nome = "Error User", Email = "error@test.com" };
            var senha = "Password123";
            _mockUsuarioRepository.ExisteUsuarioComEmailAsync(usuario.Email).Returns(false);
            _mockUsuarioRepository.IncluirAsync(Arg.Any<Usuario>()).ThrowsAsync(new Exception("DB insert error"));

            // Act
            Func<Task> act = async () => await _usuarioService.CriarUsuarioAsync(usuario, senha);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                     .WithMessage("DB insert error");
            await _mockUsuarioRepository.Received(1).ExisteUsuarioComEmailAsync(usuario.Email);
            await _mockUsuarioRepository.Received(1).IncluirAsync(Arg.Is<Usuario>(u => u.Email == usuario.Email));
        }

        // --- Testes para AtualizarUsuarioAsync ---

        [Fact]
        public async Task AtualizarUsuarioAsync_ShouldUpdateUser_WhenValidDataAndNoPasswordChange()
        {
            // Arrange
            var userId = 1;
            var originalHashedPassword = PasswordHasher.HashPassword("OldPassword123"); // Hash de uma senha de exemplo
            var existingUser = new Usuario { Id = userId, Nome = "Old Name", Email = "old@test.com", Senha = originalHashedPassword };
            var model = new UsuarioEditVM { Id = userId, Nome = "New Name", Email = "new@test.com", NovaSenha = "", ConfirmarNovaSenha = "" };

            _mockUsuarioContexto.ObterUsuarioId().Returns(userId);
            _mockUsuarioRepository.SelecionarChaveAsync(Arg.Is<object[]>(a => (int)a[0] == userId)).Returns(existingUser);
            _mockUsuarioRepository.ExisteUsuarioComEmailAsync(model.Email).Returns(false); // Novo email não existe

            // Act
            await _usuarioService.AtualizarUsuarioAsync(model);

            // Assert
            _mockUsuarioContexto.Received(1).ObterUsuarioId();
            await _mockUsuarioRepository.Received(1).SelecionarChaveAsync(Arg.Is<object[]>(a => (int)a[0] == userId));
            await _mockUsuarioRepository.Received(1).ExisteUsuarioComEmailAsync(model.Email);

            // CORREÇÃO: Capturar o usuário e verificar se a senha NÃO MUDOU
            await _mockUsuarioRepository.Received(1).AlterarAsync(
                Arg.Is<Usuario>(u =>
                    u.Id == userId &&
                    u.Nome == model.Nome &&
                    u.Email == model.Email &&
                    u.Senha == originalHashedPassword // A senha deve ser a original hashada
                )
            );
        }

        [Fact]
        public async Task AtualizarUsuarioAsync_ShouldUpdateUserAndPassword_WhenValidDataAndPasswordChange()
        {
            // Arrange
            var userId = 1;
            var originalHashedPassword = PasswordHasher.HashPassword("OldPassword123");
            var existingUser = new Usuario { Id = userId, Nome = "Old Name", Email = "old@test.com", Senha = originalHashedPassword };
            var newPassword = "NewSecurePassword456";
            var model = new UsuarioEditVM { Id = userId, Nome = "New Name", Email = "new@test.com", NovaSenha = newPassword, ConfirmarNovaSenha = newPassword };

            _mockUsuarioContexto.ObterUsuarioId().Returns(userId);
            _mockUsuarioRepository.SelecionarChaveAsync(Arg.Is<object[]>(a => (int)a[0] == userId)).Returns(existingUser);
            _mockUsuarioRepository.ExisteUsuarioComEmailAsync(model.Email).Returns(false);

            // CORREÇÃO: Mover a captura do argumento para DENTRO do Returns
            Usuario capturedUsuario = null; // Declare a variável aqui para que ela seja acessível no Assert

            _mockUsuarioRepository.AlterarAsync(Arg.Any<Usuario>()).Returns(x =>
            {
                // Capture o argumento aqui, onde ele é passado para o mock.
                capturedUsuario = x.Arg<Usuario>();
                // Retorne o Task<Usuario> que o método espera.
                return Task.FromResult(x.Arg<Usuario>());
            });


            // Act
            await _usuarioService.AtualizarUsuarioAsync(model);

            // Assert
            _mockUsuarioContexto.Received(1).ObterUsuarioId();
            await _mockUsuarioRepository.Received(1).SelecionarChaveAsync(Arg.Is<object[]>(a => (int)a[0] == userId));
            await _mockUsuarioRepository.Received(1).ExisteUsuarioComEmailAsync(model.Email);

            // Apenas verifique que o método foi chamado, a captura já ocorreu acima.
            await _mockUsuarioRepository.Received(1).AlterarAsync(Arg.Any<Usuario>());

            // Agora, capturedUsuario deve ter sido preenchido
            capturedUsuario.Should().NotBeNull("porque o AlterarAsync deveria ter sido chamado e capturado o argumento.");
            capturedUsuario.Nome.Should().Be(model.Nome);
            capturedUsuario.Email.Should().Be(model.Email);
            PasswordHasher.VerifyPassword(newPassword, capturedUsuario.Senha).Should().BeTrue();
        }

        [Theory]
        [InlineData(0, 1)] // userIdLogado inválido
        [InlineData(-1, 1)] // userIdLogado inválido
        [InlineData(2, 1)] // userIdLogado não corresponde ao model.Id
        public async Task AtualizarUsuarioAsync_ShouldThrowUnauthorizedAccessException_WhenUserNotAuthorized(int userIdLogado, int modelId)
        {
            // Arrange
            var model = new UsuarioEditVM { Id = modelId, Nome = "Any", Email = "any@test.com" };
            _mockUsuarioContexto.ObterUsuarioId().Returns(userIdLogado);

            // Act
            Func<Task> act = async () => await _usuarioService.AtualizarUsuarioAsync(model);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                     .WithMessage("Você não tem permissão para atualizar este perfil.");
            _mockUsuarioContexto.Received(1).ObterUsuarioId();
            await _mockUsuarioRepository.DidNotReceive().SelecionarChaveAsync(Arg.Any<object[]>());
        }

        [Fact]
        public async Task AtualizarUsuarioAsync_ShouldThrowApplicationException_WhenUserDoesNotExist()
        {
            // Arrange
            var userId = 1;
            var model = new UsuarioEditVM { Id = userId, Nome = "New Name", Email = "new@test.com" };
            _mockUsuarioContexto.ObterUsuarioId().Returns(userId);
            _mockUsuarioRepository.SelecionarChaveAsync(Arg.Is<object[]>(a => (int)a[0] == userId)).Returns((Usuario)null); // Usuário não encontrado

            // Act
            Func<Task> act = async () => await _usuarioService.AtualizarUsuarioAsync(model);

            // Assert
            await act.Should().ThrowAsync<ApplicationException>()
                     .WithMessage("Usuário não encontrado para atualização.");
            _mockUsuarioContexto.Received(1).ObterUsuarioId();
            await _mockUsuarioRepository.Received(1).SelecionarChaveAsync(Arg.Is<object[]>(a => (int)a[0] == userId));
            await _mockUsuarioRepository.DidNotReceive().ExisteUsuarioComEmailAsync(Arg.Any<string>());
            await _mockUsuarioRepository.DidNotReceive().AlterarAsync(Arg.Any<Usuario>());
        }

        [Fact]
        public async Task AtualizarUsuarioAsync_ShouldThrowApplicationException_WhenNewEmailAlreadyExists()
        {
            // Arrange
            var userId = 1;
            var existingUser = new Usuario { Id = userId, Nome = "Old Name", Email = "old@test.com" };
            var model = new UsuarioEditVM { Id = userId, Nome = "New Name", Email = "existing@test.com" }; // Tentando usar um email que já existe

            _mockUsuarioContexto.ObterUsuarioId().Returns(userId);
            _mockUsuarioRepository.SelecionarChaveAsync(Arg.Is<object[]>(a => (int)a[0] == userId)).Returns(existingUser);
            _mockUsuarioRepository.ExisteUsuarioComEmailAsync(model.Email).Returns(true); // O novo email já existe

            // Act
            Func<Task> act = async () => await _usuarioService.AtualizarUsuarioAsync(model);

            // Assert
            await act.Should().ThrowAsync<ApplicationException>()
                     .WithMessage("O novo e-mail já está em uso por outro usuário.");
            _mockUsuarioContexto.Received(1).ObterUsuarioId();
            await _mockUsuarioRepository.Received(1).SelecionarChaveAsync(Arg.Is<object[]>(a => (int)a[0] == userId));
            await _mockUsuarioRepository.Received(1).ExisteUsuarioComEmailAsync(model.Email);
            await _mockUsuarioRepository.DidNotReceive().AlterarAsync(Arg.Any<Usuario>());
        }

        [Fact]
        public async Task AtualizarUsuarioAsync_ShouldThrowApplicationException_WhenNewPasswordsDoNotMatch()
        {
            // Arrange
            var userId = 1;
            var existingUser = new Usuario { Id = userId, Nome = "Old Name", Email = "old@test.com", Senha = "hashedOldPassword" };
            var model = new UsuarioEditVM { Id = userId, Nome = "New Name", Email = "old@test.com", NovaSenha = "NewPassword1", ConfirmarNovaSenha = "NewPassword2" }; // Senhas não coincidem

            _mockUsuarioContexto.ObterUsuarioId().Returns(userId);
            _mockUsuarioRepository.SelecionarChaveAsync(Arg.Is<object[]>(a => (int)a[0] == userId)).Returns(existingUser);
            _mockUsuarioRepository.ExisteUsuarioComEmailAsync(model.Email).Returns(false); // Email não alterado, então não importa se existe

            // Act
            Func<Task> act = async () => await _usuarioService.AtualizarUsuarioAsync(model);

            // Assert
            await act.Should().ThrowAsync<ApplicationException>()
                     .WithMessage("A nova senha e a confirmação não coincidem.");
            _mockUsuarioContexto.Received(1).ObterUsuarioId();
            await _mockUsuarioRepository.Received(1).SelecionarChaveAsync(Arg.Is<object[]>(a => (int)a[0] == userId));
            await _mockUsuarioRepository.DidNotReceive().AlterarAsync(Arg.Any<Usuario>());
        }

        [Fact]
        public async Task AtualizarUsuarioAsync_ShouldThrowException_WhenRepositoryFailsToUpdate()
        {
            // Arrange
            var userId = 1;
            var existingUser = new Usuario { Id = userId, Nome = "Old Name", Email = "old@test.com", Senha = "hashedOldPassword" };
            var model = new UsuarioEditVM { Id = userId, Nome = "New Name", Email = "new@test.com" };

            _mockUsuarioContexto.ObterUsuarioId().Returns(userId);
            _mockUsuarioRepository.SelecionarChaveAsync(Arg.Is<object[]>(a => (int)a[0] == userId)).Returns(existingUser);
            _mockUsuarioRepository.ExisteUsuarioComEmailAsync(model.Email).Returns(false);
            _mockUsuarioRepository.AlterarAsync(Arg.Any<Usuario>()).ThrowsAsync(new Exception("DB update error"));

            // Act
            Func<Task> act = async () => await _usuarioService.AtualizarUsuarioAsync(model);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("DB update error");
            _mockUsuarioContexto.Received(1).ObterUsuarioId();
            await _mockUsuarioRepository.Received(1).SelecionarChaveAsync(Arg.Is<object[]>(a => (int)a[0] == userId));
            await _mockUsuarioRepository.Received(1).ExisteUsuarioComEmailAsync(model.Email);
            await _mockUsuarioRepository.Received(1).AlterarAsync(Arg.Is<Usuario>(u => u.Id == userId && u.Email == model.Email));
        }

        // --- Testes para ExcluirUsuarioAsync ---

        [Fact]
        public async Task ExcluirUsuarioAsync_ShouldDeleteUser_WhenAuthorizedAndUserExists()
        {
            // Arrange
            var userIdToDelete = 1;
            var loggedInUserId = 1; // Usuário logado é o mesmo que será excluído
            var usuario = new Usuario { Id = userIdToDelete, Nome = "User To Delete", Email = "delete@test.com" };

            _mockUsuarioContexto.ObterUsuarioId().Returns(loggedInUserId);
            _mockUsuarioRepository.SelecionarChaveAsync(Arg.Is<object[]>(a => (int)a[0] == userIdToDelete)).Returns(usuario);
            _mockUsuarioRepository.ExcluirAsync(usuario).Returns(Task.CompletedTask);

            // Act
            await _usuarioService.ExcluirUsuarioAsync(userIdToDelete);

            // Assert
            _mockUsuarioContexto.Received(1).ObterUsuarioId();
            await _mockUsuarioRepository.Received(1).SelecionarChaveAsync(Arg.Is<object[]>(a => (int)a[0] == userIdToDelete));
            await _mockUsuarioRepository.Received(1).ExcluirAsync(usuario);
        }

        [Theory]
        [InlineData(0, 1)] // userIdLogado inválido
        [InlineData(-1, 1)] // userIdLogado inválido
        [InlineData(2, 1)] // userIdLogado não corresponde ao id do usuário a ser excluído
        public async Task ExcluirUsuarioAsync_ShouldThrowUnauthorizedAccessException_WhenUserNotAuthorized(int loggedInUserId, int userIdToDelete)
        {
            // Arrange
            _mockUsuarioContexto.ObterUsuarioId().Returns(loggedInUserId);

            // Act
            Func<Task> act = async () => await _usuarioService.ExcluirUsuarioAsync(userIdToDelete);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                     .WithMessage("Você não tem permissão para excluir este usuário.");
            _mockUsuarioContexto.Received(1).ObterUsuarioId();
            await _mockUsuarioRepository.DidNotReceive().SelecionarChaveAsync(Arg.Any<object[]>());
            await _mockUsuarioRepository.DidNotReceive().ExcluirAsync(Arg.Any<Usuario>());
        }

        [Fact]
        public async Task ExcluirUsuarioAsync_ShouldThrowApplicationException_WhenUserDoesNotExist()
        {
            // Arrange
            var userIdToDelete = 1;
            var loggedInUserId = 1;
            _mockUsuarioContexto.ObterUsuarioId().Returns(loggedInUserId);
            _mockUsuarioRepository.SelecionarChaveAsync(Arg.Is<object[]>(a => (int)a[0] == userIdToDelete)).Returns((Usuario)null); // Usuário não encontrado

            // Act
            Func<Task> act = async () => await _usuarioService.ExcluirUsuarioAsync(userIdToDelete);

            // Assert
            await act.Should().ThrowAsync<ApplicationException>()
                     .WithMessage("Usuário não encontrado para exclusão.");
            _mockUsuarioContexto.Received(1).ObterUsuarioId();
            await _mockUsuarioRepository.Received(1).SelecionarChaveAsync(Arg.Is<object[]>(a => (int)a[0] == userIdToDelete));
            await _mockUsuarioRepository.DidNotReceive().ExcluirAsync(Arg.Any<Usuario>());
        }

        [Fact]
        public async Task ExcluirUsuarioAsync_ShouldThrowException_WhenRepositoryFailsToDelete()
        {
            // Arrange
            var userIdToDelete = 1;
            var loggedInUserId = 1;
            var usuario = new Usuario { Id = userIdToDelete, Nome = "User To Delete", Email = "delete@test.com" };

            _mockUsuarioContexto.ObterUsuarioId().Returns(loggedInUserId);
            _mockUsuarioRepository.SelecionarChaveAsync(Arg.Is<object[]>(a => (int)a[0] == userIdToDelete)).Returns(usuario);
            _mockUsuarioRepository.ExcluirAsync(usuario).ThrowsAsync(new Exception("DB delete error"));

            // Act
            Func<Task> act = async () => await _usuarioService.ExcluirUsuarioAsync(userIdToDelete);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("DB delete error");
            _mockUsuarioContexto.Received(1).ObterUsuarioId();
            await _mockUsuarioRepository.Received(1).SelecionarChaveAsync(Arg.Is<object[]>(a => (int)a[0] == userIdToDelete));
            await _mockUsuarioRepository.Received(1).ExcluirAsync(usuario);
        }

        // --- Testes para ExisteEmail ---

        [Fact]
        public async Task ExisteEmail_ShouldReturnTrue_WhenEmailExists()
        {
            // Arrange
            var email = "existing@test.com";
            _mockUsuarioRepository.ExisteUsuarioComEmailAsync(email).Returns(true);

            // Act
            var result = await _usuarioService.ExisteEmail(email);

            // Assert
            result.Should().BeTrue();
            await _mockUsuarioRepository.Received(1).ExisteUsuarioComEmailAsync(email);
        }

        [Fact]
        public async Task ExisteEmail_ShouldReturnFalse_WhenEmailDoesNotExist()
        {
            // Arrange
            var email = "nonexistent@test.com";
            _mockUsuarioRepository.ExisteUsuarioComEmailAsync(email).Returns(false);

            // Act
            var result = await _usuarioService.ExisteEmail(email);

            // Assert
            result.Should().BeFalse();
            await _mockUsuarioRepository.Received(1).ExisteUsuarioComEmailAsync(email);
        }
    }
}