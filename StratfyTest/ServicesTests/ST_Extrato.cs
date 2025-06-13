using Xunit;
using NSubstitute;
using FluentAssertions;
using NSubstitute.ExceptionExtensions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.IO;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using System.Threading;

// Imports das suas classes de projeto
using STRATFY.Interfaces.IRepositories;
using STRATFY.Interfaces.IContexts;
using STRATFY.Interfaces.IServices;
using STRATFY.Models;
using STRATFY.Services;


namespace StratfyTest.ServicesTests
{
    public class ExtratoServiceTests
    {
        private readonly IRepositoryExtrato _mockExtratoRepository;
        private readonly IMovimentacaoService _mockMovimentacaoService;
        private readonly ICategoriaService _mockCategoriaService;
        private readonly ICsvExportService _mockCsvExportService;
        private readonly IHttpClientFactory _mockHttpClientFactory;
        private readonly IUsuarioContexto _mockUsuarioContexto;
        private readonly ExtratoService _extratoService;

        public ExtratoServiceTests()
        {
            _mockExtratoRepository = Substitute.For<IRepositoryExtrato>();
            _mockMovimentacaoService = Substitute.For<IMovimentacaoService>();
            _mockCategoriaService = Substitute.For<ICategoriaService>();
            _mockCsvExportService = Substitute.For<ICsvExportService>();
            _mockHttpClientFactory = Substitute.For<IHttpClientFactory>();
            _mockUsuarioContexto = Substitute.For<IUsuarioContexto>();

            _extratoService = new ExtratoService(
                _mockExtratoRepository,
                _mockMovimentacaoService,
                _mockCategoriaService,
                _mockCsvExportService,
                _mockHttpClientFactory,
                _mockUsuarioContexto
            );
        }

        // --- Auxiliares para Mocks ---
        public class MockFormFile : IFormFile
        {
            private readonly MemoryStream _stream;
            private readonly string _fileName;
            private readonly string _contentType;

            public MockFormFile(string content, string fileName, string contentType = "text/csv")
            {
                _stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
                _fileName = fileName;
                _contentType = contentType;
            }

            public string ContentType => _contentType;
            public string ContentDisposition => $"form-data; name=\"file\"; filename=\"{_fileName}\"";
            public IHeaderDictionary Headers => new HeaderDictionary();
            public long Length => _stream.Length;
            public string Name => "file";
            public string FileName => _fileName;

            public void CopyTo(Stream targetStream)
            {
                _stream.Seek(0, SeekOrigin.Begin);
                _stream.CopyTo(targetStream);
            }

            public async Task CopyToAsync(Stream targetStream, CancellationToken cancellationToken = default)
            {
                _stream.Seek(0, SeekOrigin.Begin);
                await _stream.CopyToAsync(targetStream, cancellationToken);
            }

            public Stream OpenReadStream()
            {
                _stream.Seek(0, SeekOrigin.Begin);
                return _stream;
            }
        }

        public class MockHttpMessageHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode _statusCode;
            private readonly string _responseContent;
            private readonly bool _throwException;
            private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _sendAsyncAction;

            // Construtor para casos simples de sucesso/falha
            public MockHttpMessageHandler(HttpStatusCode statusCode, string responseContent, bool throwException = false)
            {
                _statusCode = statusCode;
                _responseContent = responseContent;
                _throwException = throwException;
                _sendAsyncAction = null; // Usará a lógica padrão
            }

            // Construtor para casos mais complexos, onde você define a lógica de resposta
            public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsyncAction)
            {
                _sendAsyncAction = sendAsyncAction;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (_sendAsyncAction != null)
                {
                    return _sendAsyncAction(request, cancellationToken);
                }

                if (_throwException)
                {
                    throw new HttpRequestException("Simulated network error.");
                }

                var response = new HttpResponseMessage(_statusCode)
                {
                    Content = new StringContent(_responseContent)
                };
                return Task.FromResult(response);
            }
        }

        // --- Testes para ObterExtratosDoUsuarioParaIndexAsync --- (Mantidos)

        [Fact]
        public async Task ObterExtratosDoUsuarioParaIndexAsync_ShouldReturnListOfViewModels_WhenExtratosExist()
        {
            // Arrange
            var userId = 1;
            _mockUsuarioContexto.ObterUsuarioId().Returns(userId);

            var extratos = new List<Extrato>
            {
                new Extrato
                {
                    Id = 1,
                    Nome = "Extrato Mensal",
                    DataCriacao = new DateOnly(2023, 1, 1),
                    UsuarioId = userId,
                    Movimentacaos = new List<Movimentacao>
                    {
                        new Movimentacao { DataMovimentacao = new DateOnly(2023, 1, 15) },
                        new Movimentacao { DataMovimentacao = new DateOnly(2023, 1, 20) }
                    }
                },
                new Extrato
                {
                    Id = 2,
                    Nome = "Extrato Semanal",
                    DataCriacao = new DateOnly(2023, 1, 10),
                    UsuarioId = userId,
                    Movimentacaos = new List<Movimentacao>
                    {
                        new Movimentacao { DataMovimentacao = new DateOnly(2023, 1, 10) }
                    }
                }
            };
            _mockExtratoRepository.SelecionarTodosDoUsuarioAsync(userId).Returns(extratos);

            // Act
            var result = await _extratoService.ObterExtratosDoUsuarioParaIndexAsync();

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().HaveCount(2);

            result[0].Id.Should().Be(2);
            result[1].Id.Should().Be(1);

            result[0].Nome.Should().Be("Extrato Semanal");
            result[0].DataCriacao.Should().Be(new DateOnly(2023, 1, 10));
            result[0].DataInicioMovimentacoes.Should().Be(new DateOnly(2023, 1, 10));
            result[0].DataFimMovimentacoes.Should().Be(new DateOnly(2023, 1, 10));
            result[0].TotalMovimentacoes.Should().Be(1);

            result[1].Nome.Should().Be("Extrato Mensal");
            result[1].DataCriacao.Should().Be(new DateOnly(2023, 1, 1));
            result[1].DataInicioMovimentacoes.Should().Be(new DateOnly(2023, 1, 15));
            result[1].DataFimMovimentacoes.Should().Be(new DateOnly(2023, 1, 20));
            result[1].TotalMovimentacoes.Should().Be(2);

            _mockUsuarioContexto.Received(1).ObterUsuarioId();
            await _mockExtratoRepository.Received(1).SelecionarTodosDoUsuarioAsync(userId);
        }

        [Fact]
        public async Task ObterExtratosDoUsuarioParaIndexAsync_ShouldReturnEmptyList_WhenNoExtratosExist()
        {
            // Arrange
            var userId = 1;
            _mockUsuarioContexto.ObterUsuarioId().Returns(userId);
            _mockExtratoRepository.SelecionarTodosDoUsuarioAsync(userId).Returns(new List<Extrato>());

            // Act
            var result = await _extratoService.ObterExtratosDoUsuarioParaIndexAsync();

            // Assert
            result.Should().NotBeNull().And.BeEmpty();
            _mockUsuarioContexto.Received(1).ObterUsuarioId();
            await _mockExtratoRepository.Received(1).SelecionarTodosDoUsuarioAsync(userId);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task ObterExtratosDoUsuarioParaIndexAsync_ShouldThrowUnauthorizedAccessException_WhenUserIdIsInvalid(int invalidUserId)
        {
            // Arrange
            _mockUsuarioContexto.ObterUsuarioId().Returns(invalidUserId);

            // Act
            Func<Task> act = async () => await _extratoService.ObterExtratosDoUsuarioParaIndexAsync();

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                     .WithMessage("Usuário não autenticado ou ID inválido para obter extratos.");
            _mockUsuarioContexto.Received(1).ObterUsuarioId();
            await _mockExtratoRepository.DidNotReceive().SelecionarTodosDoUsuarioAsync(Arg.Any<int>());
        }
    }
}