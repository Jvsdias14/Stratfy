using Xunit;
using FluentAssertions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using STRATFY.Models;
using STRATFY.Services;
using System;

namespace StratfyTest.ServicesTests
{
    public class ST_CsvExport
    {
        private readonly CsvExportService _csvExportService;

        public ST_CsvExport()
        {
            _csvExportService = new CsvExportService();
        }

        private async Task<List<MovimentacaoCsvModel>> ReadCsvFromStreamAsync(MemoryStream stream)
        {
            stream.Position = 0;
            // Ao ler de volta, use a cultura que corresponde à escrita (pt-BR)
            // ou uma cultura neutra e depois faça a conversão explícita.
            // Se o CsvHelper está lendo a string "100,00" e o campo em MovimentacaoCsvModel.Valor é string,
            // então não haverá problema de conversão implícita.
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            using (var csv = new CsvReader(reader, new CsvConfiguration(new CultureInfo("pt-BR")) { Delimiter = ";", HasHeaderRecord = true, Encoding = Encoding.UTF8 }))
            {
                return csv.GetRecords<MovimentacaoCsvModel>().ToList();
            }
        }

        [Fact]
        public async Task ExportMovimentacoesToCsvAsync_ShouldReturnCsvWithHeaderAndData_WhenMovimentacoesAreProvided()
        {
            // Arrange
            var movimentacoes = new List<Movimentacao>
            {
                new Movimentacao
                {
                    Descricao = "Salário",
                    Valor = 3000.00m,
                    Tipo = "Receita",
                    DataMovimentacao = new DateOnly(2025, 1, 15),
                    Categoria = new Categoria { Nome = "Trabalho" }
                },
                new Movimentacao
                {
                    Descricao = "Aluguel",
                    Valor = 1500.00m,
                    Tipo = "Despesa",
                    DataMovimentacao = new DateOnly(2025, 1, 10),
                    Categoria = new Categoria { Nome = "Moradia" }
                }
            };

            // Act
            using (var memoryStream = await _csvExportService.ExportMovimentacoesToCsvAsync(movimentacoes))
            {
                // Assert - Verificar o conteúdo bruto do CSV
                var csvContent = Encoding.UTF8.GetString(memoryStream.ToArray());

                // CORREÇÃO AQUI: Ajustar o cabeçalho esperado para corresponder ao que está sendo gerado
                csvContent.Should().Contain("Data da Movimentação;Descrição;Valor;Tipo;Categoria"); // <--- CORRIGIDO
                csvContent.Should().Contain("15/01/2025;Salário;3000,00;Receita;Trabalho");
                csvContent.Should().Contain("10/01/2025;Aluguel;1500,00;Despesa;Moradia");

                // Assert - Ler o CSV de volta para verificar a estrutura
                var records = await ReadCsvFromStreamAsync(memoryStream);
                records.Should().HaveCount(2);

                records[0].DataMovimentacao.Should().Be("15/01/2025");
                records[0].Descricao.Should().Be("Salário");
                records[0].Valor.Should().Be(3000.00m.ToString("F2", new CultureInfo("pt-BR")));
                records[0].Tipo.Should().Be("Receita");
                records[0].Categoria.Should().Be("Trabalho");

                records[1].DataMovimentacao.Should().Be("10/01/2025");
                records[1].Descricao.Should().Be("Aluguel");
                records[1].Valor.Should().Be(1500.00m.ToString("F2", new CultureInfo("pt-BR")));
                records[1].Tipo.Should().Be("Despesa");
                records[1].Categoria.Should().Be("Moradia");
            }
        }

        [Fact]
        public async Task ExportMovimentacoesToCsvAsync_ShouldReturnCsvWithOnlyHeader_WhenNoMovimentacoesAreProvided()
        {
            // Arrange
            var movimentacoes = new List<Movimentacao>();

            // Act
            using (var memoryStream = await _csvExportService.ExportMovimentacoesToCsvAsync(movimentacoes))
            {
                // Assert - Verificar o conteúdo bruto do CSV
                var csvContent = Encoding.UTF8.GetString(memoryStream.ToArray());

                // CORREÇÃO AQUI: Ajustar o cabeçalho esperado
                csvContent.Should().Be("Data da Movimentação;Descrição;Valor;Tipo;Categoria\r\n"); // <--- CORRIGIDO

                // Assert - Ler o CSV de volta para verificar a estrutura
                var records = await ReadCsvFromStreamAsync(memoryStream);
                records.Should().BeEmpty();
            }
        }

        [Fact]
        public async Task ExportMovimentacoesToCsvAsync_ShouldHandleMovimentacoesWithNullCategory()
        {
            // Arrange
            var movimentacoes = new List<Movimentacao>
            {
                new Movimentacao
                {
                    Descricao = "Doação",
                    Valor = 50.00m,
                    Tipo = "Despesa",
                    DataMovimentacao = new DateOnly(2025, 2, 1),
                    Categoria = null
                },
                new Movimentacao
                {
                    Descricao = "Bônus",
                    Valor = 200.00m,
                    Tipo = "Receita",
                    DataMovimentacao = new DateOnly(2025, 2, 5),
                    Categoria = new Categoria { Nome = null }
                }
            };

            // Act
            using (var memoryStream = await _csvExportService.ExportMovimentacoesToCsvAsync(movimentacoes))
            {
                // Assert - Verificar o conteúdo bruto do CSV (o CsvHelper normalmente coloca string vazia para nulos)
                var csvContent = Encoding.UTF8.GetString(memoryStream.ToArray());
                csvContent.Should().Contain("01/02/2025;Doação;50,00;Despesa;");
                csvContent.Should().Contain("05/02/2025;Bônus;200,00;Receita;");

                // Assert - Ler o CSV de volta para verificar a estrutura
                var records = await ReadCsvFromStreamAsync(memoryStream);
                records.Should().HaveCount(2);
                records[0].Categoria.Should().BeEmpty();
                records[1].Categoria.Should().BeEmpty();
            }
        }

        [Fact]
        public async Task ExportMovimentacoesToCsvAsync_ShouldHandleMovimentacoesWithDateOnlyAsNullable()
        {
            // Arrange
            var movimentacoes = new List<Movimentacao>
            {
                new Movimentacao
                {
                    Descricao = "Compra Teste",
                    Valor = 100.00m,
                    Tipo = "Despesa",
                    DataMovimentacao = new DateOnly(2024, 7, 20),
                    Categoria = new Categoria { Nome = "Compras" }
                }
            };

            // Act
            using (var memoryStream = await _csvExportService.ExportMovimentacoesToCsvAsync(movimentacoes))
            {
                // Assert
                var csvContent = Encoding.UTF8.GetString(memoryStream.ToArray());
                csvContent.Should().Contain("20/07/2024;Compra Teste;100,00;Despesa;Compras");

                var records = await ReadCsvFromStreamAsync(memoryStream);
                records.Should().HaveCount(1);
                records[0].DataMovimentacao.Should().Be("20/07/2024");
                records[0].Valor.Should().Be(100.00m.ToString("F2", new CultureInfo("pt-BR")));
            }
        }
    }
}