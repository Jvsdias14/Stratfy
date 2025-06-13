using CsvHelper;
using CsvHelper.Configuration;
using System.Collections.Generic;
using System.Globalization; // Necessário para CultureInfo
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using STRATFY.Models;
using STRATFY.Interfaces.IServices;

namespace STRATFY.Services
{
    public class CsvExportService : ICsvExportService
    {
        public async Task<MemoryStream> ExportMovimentacoesToCsvAsync(IEnumerable<Movimentacao> movimentacoes)
        {
            var memoryStream = new MemoryStream();
            var streamWriter = new StreamWriter(memoryStream, leaveOpen: true);

            // 1. Alterar CultureInfo para "pt-BR" no CsvConfiguration
            // Isso afeta como o CsvHelper trata os números e datas por padrão
            var csvConfiguration = new CsvConfiguration(new CultureInfo("pt-BR")) // <--- CORREÇÃO AQUI
            {
                HasHeaderRecord = true,
                Delimiter = ";",
                Encoding = Encoding.UTF8
            };

            using (var csvWriter = new CsvWriter(streamWriter, csvConfiguration))
            {
                var records = movimentacoes.Select(m => new MovimentacaoCsvModel
                {
                    // 2. Tratar DataMovimentacao como nula e formatar com InvariantCulture para consistência
                    DataMovimentacao = m.DataMovimentacao?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? string.Empty, // <--- CORREÇÃO AQUI (DateOnly?)
                    // Se DataMovimentacao não pode ser nula, remova o '?' e '?? string.Empty'
                    // Mas o teste mencionou "DateOnlyAsNullable", então é bom ter esse tratamento.

                    Descricao = m.Descricao,

                    // 3. Formatar o Valor para string usando CultureInfo "pt-BR" explicitamente
                    Valor = m.Valor.ToString("F2", new CultureInfo("pt-BR")), // <--- CORREÇÃO AQUI

                    Tipo = m.Tipo,
                    Categoria = m.Categoria?.Nome ?? string.Empty // Lida com Categoria e Nome nulos
                }).ToList();

                csvWriter.WriteHeader<MovimentacaoCsvModel>();
                await csvWriter.NextRecordAsync();
                await csvWriter.WriteRecordsAsync(records);
            }

            memoryStream.Position = 0;
            return memoryStream;
        }
    }
}