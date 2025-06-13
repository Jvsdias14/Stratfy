using FluentAssertions;
using NSubstitute;
using STRATFY.Interfaces.IRepositories;
using STRATFY.Models;
using STRATFY.Services;

namespace StratfyTest.ServicesTests
{
    public class ST_Categoria
    {
        private readonly CategoriaService _categoriaService;
        private readonly IRepositoryBase<Categoria> _mockCategoriaRepository;

        public ST_Categoria()
        {
            _mockCategoriaRepository = Substitute.For<IRepositoryBase<Categoria>>();
            _categoriaService = new CategoriaService(_mockCategoriaRepository);
        }

        [Fact]
        public async Task ObterCategoriaIdPorNomeAsync_ShouldReturnId_WhenCategoryExists()
        {
            // Arrange
            var nomeCategoria = "Alimentação";
            var categoriaExistente = new Categoria { Id = 1, Nome = nomeCategoria };

            // CORREÇÃO: Forneça a lista diretamente. NSubstitute vai lidar com o Task.
            _mockCategoriaRepository.SelecionarTodosAsync().Returns(new List<Categoria> { categoriaExistente });

            // Act
            var result = await _categoriaService.ObterCategoriaIdPorNomeAsync(nomeCategoria);

            // Assert
            result.Should().Be(categoriaExistente.Id);
            await _mockCategoriaRepository.Received(1).SelecionarTodosAsync();
            _mockCategoriaRepository.DidNotReceive().Incluir(Arg.Any<Categoria>());
            _mockCategoriaRepository.DidNotReceive().Salvar();
        }

        [Fact]
        public async Task ObterCategoriaIdPorNomeAsync_ShouldThrowException_WhenCategoryDoesNotExist()
        {
            // Arrange
            var nomeCategoria = "Transporte";
            // CORREÇÃO: Forneça a lista vazia diretamente.
            _mockCategoriaRepository.SelecionarTodosAsync().Returns(new List<Categoria>());

            // Act
            Func<Task> act = async () => await _categoriaService.ObterCategoriaIdPorNomeAsync(nomeCategoria);

            // Assert
            await act.Should().ThrowAsync<ApplicationException>()
                            .WithMessage($"Categoria '{nomeCategoria}' não encontrada no sistema de categorias fixas.");
            await _mockCategoriaRepository.Received(1).SelecionarTodosAsync();
        }

        [Fact]
        public async Task ObterCategoriaIdPorNomeAsync_ShouldHandleCaseInsensitiveMatching()
        {
            // Arrange
            var nomeCategoriaOriginal = "serviços";
            var nomeCategoriaBusca = "Serviços";
            var categoriaExistente = new Categoria { Id = 2, Nome = nomeCategoriaOriginal };

            // CORREÇÃO: Forneça a lista diretamente.
            _mockCategoriaRepository.SelecionarTodosAsync().Returns(new List<Categoria> { categoriaExistente });

            // Act
            var result = await _categoriaService.ObterCategoriaIdPorNomeAsync(nomeCategoriaBusca);

            // Assert
            result.Should().Be(categoriaExistente.Id);
            await _mockCategoriaRepository.Received(1).SelecionarTodosAsync();
        }

        [Fact]
        public async Task ObterTodasCategoriasParaSelectListAsync_ShouldReturnAllCategories()
        {
            // Arrange
            var categorias = new List<Categoria>
            {
                new Categoria { Id = 1, Nome = "Moradia" },
                new Categoria { Id = 2, Nome = "Transporte" },
                new Categoria { Id = 3, Nome = "Lazer" }
            };

            // CORREÇÃO: Forneça a lista diretamente.
            _mockCategoriaRepository.SelecionarTodosAsync().Returns(categorias);

            // Act
            var result = await _categoriaService.ObterTodasCategoriasParaSelectListAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.Should().BeEquivalentTo(categorias);
            await _mockCategoriaRepository.Received(1).SelecionarTodosAsync();
        }

        [Fact]
        public async Task ObterTodasCategoriasParaSelectListAsync_ShouldReturnEmptyList_WhenNoCategoriesExist()
        {
            // Arrange
            // CORREÇÃO: Forneça a lista vazia diretamente.
            _mockCategoriaRepository.SelecionarTodosAsync().Returns(new List<Categoria>());

            // Act
            var result = await _categoriaService.ObterTodasCategoriasParaSelectListAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
            await _mockCategoriaRepository.Received(1).SelecionarTodosAsync();
        }

        [Fact]
        public async Task ObterCategoriaPorIdAsync_ShouldReturnCategory_WhenCategoryExists()
        {
            // Arrange
            var categoriaId = 5;
            var categoriaExistente = new Categoria { Id = categoriaId, Nome = "Saúde" };
            _mockCategoriaRepository.SelecionarChaveAsync(categoriaId).Returns(Task.FromResult(categoriaExistente));

            // Act
            var result = await _categoriaService.ObterCategoriaPorIdAsync(categoriaId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(categoriaId);
            result.Nome.Should().Be(categoriaExistente.Nome);
            await _mockCategoriaRepository.Received(1).SelecionarChaveAsync(categoriaId);
        }

        [Fact]
        public async Task ObterCategoriaPorIdAsync_ShouldThrowException_WhenCategoryDoesNotExist()
        {
            // Arrange
            var categoriaId = 99;
            _mockCategoriaRepository.SelecionarChaveAsync(categoriaId).Returns(Task.FromResult<Categoria>(null));

            // Act
            Func<Task> act = async () => await _categoriaService.ObterCategoriaPorIdAsync(categoriaId);

            // Assert
            await act.Should().ThrowAsync<ApplicationException>()
                            .WithMessage($"Categoria com ID {categoriaId} não encontrada.");
            await _mockCategoriaRepository.Received(1).SelecionarChaveAsync(categoriaId);
        }
    }
}