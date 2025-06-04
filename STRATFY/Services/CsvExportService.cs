using CsvHelper;
using CsvHelper.Configuration;
using System.Collections.Generic;
using System.Globalization;
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
            var csvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ";",
                Encoding = Encoding.UTF8
            };

             using (var csvWriter = new CsvWriter(streamWriter, csvConfiguration))
            {
                var records = movimentacoes.Select(m => new MovimentacaoCsvModel
                {

                    DataMovimentacao = m.DataMovimentacao.Value.ToString("dd/MM/yyyy"),
                    Descricao = m.Descricao,
                    Valor = m.Valor,
                    Tipo = m.Tipo, 
                    Categoria = m.Categoria?.Nome 
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