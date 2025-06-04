using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using global::STRATFY.Models;

namespace STRATFY.Interfaces.IServices

{
    public interface ICsvExportService
    {
        Task<MemoryStream> ExportMovimentacoesToCsvAsync(IEnumerable<Movimentacao> movimentacoes);
    }
}