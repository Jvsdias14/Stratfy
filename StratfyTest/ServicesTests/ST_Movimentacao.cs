// StratfyTest/ServicesTests/ST_Movimentacao.cs
using NSubstitute;
using FluentAssertions;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using STRATFY.Models;
using STRATFY.Interfaces.IRepositories;
using STRATFY.Interfaces.IServices;
using System;
using STRATFY.Services; // Certifique-se de que este using está presente

namespace StratfyTest.ServicesTests
{
    public class ST_Movimentacao
    {
        private readonly MovimentacaoService _movimentacaoService;
        private readonly IRepositoryMovimentacao _mockMovimentacaoRepository;
        private readonly ICategoriaService _mockCategoriaService;

        public ST_Movimentacao()
        {
            _mockMovimentacaoRepository = Substitute.For<IRepositoryMovimentacao>();
            _mockCategoriaService = Substitute.For<ICategoriaService>();
            _movimentacaoService = new MovimentacaoService(_mockMovimentacaoRepository, _mockCategoriaService);
        }

        // --- Testes para ImportarMovimentacoesDoCsvAsync ---

        [Fact]
        public async Task ImportarMovimentacoesDoCsvAsync_ShouldReturn_WhenMovimentacoesImportadasIsNull()
        {
            // Arrange
            List<Movimentacao> movimentacoes = null;
            var extratoId = 1;

            // Act
            await _movimentacaoService.ImportarMovimentacoesDoCsvAsync(movimentacoes, extratoId);

            // Assert
            // Agora o serviço de categoria não é chamado para criar/obter, mas sim para listar todas.
            // Para este caso (lista nula), ele não deveria ser chamado.
            await _mockCategoriaService.DidNotReceive().ObterTodasCategoriasParaSelectListAsync();
            _mockMovimentacaoRepository.DidNotReceive().Incluir(Arg.Any<Movimentacao>());
            _mockMovimentacaoRepository.DidNotReceive().Salvar();
        }

        
        [Fact]
        public async Task ImportarMovimentacoesDoCsvAsync_ShouldImportMovimentacoesAndAssignCategory_WhenCategoryExists()
        {
            // Arrange
            var extratoId = 1;
            var categoriaNome = "Alimentação";
            var categoriaExistente = new Categoria { Id = 10, Nome = categoriaNome };
            var categoriaOutros = new Categoria { Id = 99, Nome = "Outros" };

            var movimentacoesImportadas = new List<Movimentacao>
            {
                new Movimentacao { Descricao = "Lanche", Valor = 10.0m, Tipo = "Despesa", DataMovimentacao = DateOnly.FromDateTime(DateTime.Now), Categoria = new Categoria { Nome = categoriaNome } }
            };

            // Setup para o CategoriaService mockado: ele agora retorna a lista completa de categorias
            _mockCategoriaService.ObterTodasCategoriasParaSelectListAsync().Returns(Task.FromResult<IEnumerable<Categoria>>(
                new List<Categoria> { categoriaExistente, categoriaOutros }
            ));

            // Act
            await _movimentacaoService.ImportarMovimentacoesDoCsvAsync(movimentacoesImportadas, extratoId);

            // Assert
            await _mockCategoriaService.Received(1).ObterTodasCategoriasParaSelectListAsync(); // Chamado apenas uma vez para obter todas
            _mockMovimentacaoRepository.Received(1).Incluir(Arg.Is<Movimentacao>(m =>
                m.ExtratoId == extratoId &&
                m.CategoriaId == categoriaExistente.Id &&
                m.Categoria == null &&
                m.Descricao == "Lanche" &&
                m.Tipo == "Despesa"
            ));
            _mockMovimentacaoRepository.Received(1).Salvar();
        }

        [Fact]
        public async Task ImportarMovimentacoesDoCsvAsync_ShouldAssignDefaultCategory_WhenCategoryDoesNotExist()
        {
            // Arrange
            var extratoId = 1;
            var categoriaNomeInexistente = "Lazer"; // Esta categoria não estará na lista retornada pelo mock
            var categoriaOutros = new Categoria { Id = 99, Nome = "Outros" };

            var movimentacoesImportadas = new List<Movimentacao>
            {
                new Movimentacao { Descricao = "Cinema", Valor = 50.0m, Tipo = "Despesa", DataMovimentacao = DateOnly.FromDateTime(DateTime.Now), Categoria = new Categoria { Nome = categoriaNomeInexistente } }
            };

            // Setup para o CategoriaService mockado: retorna apenas "Outros"
            _mockCategoriaService.ObterTodasCategoriasParaSelectListAsync().Returns(Task.FromResult<IEnumerable<Categoria>>(
                new List<Categoria> { categoriaOutros }
            ));

            // Act
            await _movimentacaoService.ImportarMovimentacoesDoCsvAsync(movimentacoesImportadas, extratoId);

            // Assert
            await _mockCategoriaService.Received(1).ObterTodasCategoriasParaSelectListAsync();
            _mockMovimentacaoRepository.Received(1).Incluir(Arg.Is<Movimentacao>(m =>
                m.ExtratoId == extratoId &&
                m.CategoriaId == categoriaOutros.Id && // Deve ser atribuído à categoria "Outros"
                m.Categoria == null &&
                m.Tipo == "Despesa"
            ));
            _mockMovimentacaoRepository.Received(1).Salvar();
        }

        [Fact]
        public async Task ImportarMovimentacoesDoCsvAsync_ShouldAssignDefaultCategory_WhenCategoryNameIsEmpty()
        {
            // Arrange
            var extratoId = 1;
            var categoriaPadraoNome = "Outros";
            var categoriaPadrao = new Categoria { Id = 99, Nome = categoriaPadraoNome };
            var movimentacoesImportadas = new List<Movimentacao>
            {
                new Movimentacao { Descricao = "Item sem categoria", Valor = 5.0m, Tipo = "Despesa", DataMovimentacao = DateOnly.FromDateTime(DateTime.Now), Categoria = new Categoria { Nome = "" } },
                new Movimentacao { Descricao = "Outro item sem categoria", Valor = 7.0m, Tipo = "Receita", DataMovimentacao = DateOnly.FromDateTime(DateTime.Now), Categoria = null }
            };

            // Setup para o CategoriaService mockado (apenas a categoria padrão)
            _mockCategoriaService.ObterTodasCategoriasParaSelectListAsync().Returns(Task.FromResult<IEnumerable<Categoria>>(
                new List<Categoria> { categoriaPadrao }
            ));

            // Act
            await _movimentacaoService.ImportarMovimentacoesDoCsvAsync(movimentacoesImportadas, extratoId);

            // Assert
            await _mockCategoriaService.Received(1).ObterTodasCategoriasParaSelectListAsync(); // Chamado apenas uma vez
            _mockMovimentacaoRepository.Received(2).Incluir(Arg.Any<Movimentacao>());
            _mockMovimentacaoRepository.Received(1).Salvar();

            _mockMovimentacaoRepository.Received(1).Incluir(Arg.Is<Movimentacao>(m =>
                m.Descricao == "Item sem categoria" && m.CategoriaId == categoriaPadrao.Id && m.Tipo == "Despesa"
            ));
            _mockMovimentacaoRepository.Received(1).Incluir(Arg.Is<Movimentacao>(m =>
                m.Descricao == "Outro item sem categoria" && m.CategoriaId == categoriaPadrao.Id && m.Tipo == "Receita"
            ));
        }

        // --- Testes para AtualizarMovimentacoesDoExtratoAsync ---

        
        

        
        [Fact]
        public async Task AtualizarMovimentacoesDoExtratoAsync_ShouldHandleEmptyReceivedAndExistingLists()
        {
            // Arrange
            var extratoId = 1;
            var categoriasDisponiveis = new List<Categoria> { new Categoria { Id = 99, Nome = "Outros" } }; // Pelo menos uma categoria para o mock

            var movimentacoesExistentes = new List<Movimentacao>();
            var movimentacoesRecebidas = new List<Movimentacao>();

            // Mock do CategoriaService
            _mockCategoriaService.ObterTodasCategoriasParaSelectListAsync()
                .Returns(Task.FromResult<IEnumerable<Categoria>>(categoriasDisponiveis));

            // Act
            await _movimentacaoService.AtualizarMovimentacoesDoExtratoAsync(movimentacoesRecebidas, movimentacoesExistentes, extratoId);

            // Assert
            await _mockCategoriaService.Received(1).ObterTodasCategoriasParaSelectListAsync(); // Pode ser chamado, dependendo da implementação do serviço
            _mockMovimentacaoRepository.DidNotReceive().RemoverVarias(Arg.Any<List<Movimentacao>>());
            _mockMovimentacaoRepository.DidNotReceive().Incluir(Arg.Any<Movimentacao>());
            _mockMovimentacaoRepository.Received(1).Salvar();
        }
    }
}