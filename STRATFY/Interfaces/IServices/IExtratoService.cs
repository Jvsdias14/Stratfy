// STRATFY.Interfaces.IServices/IExtratoService.cs
using STRATFY.Models;
using Microsoft.AspNetCore.Http; // Para IFormFile
using System.Collections.Generic;
using System.IO; // Para Stream
using System.Threading.Tasks;

namespace STRATFY.Interfaces.IServices
{
    public interface IExtratoService
    {
        // Removido: int userId
        Task<List<ExtratoIndexViewModel>> ObterExtratosDoUsuarioParaIndexAsync();

        Task<Extrato> ObterExtratoDetalhesAsync(int extratoId);

        // Removido: int userId
        Task<int> CriarExtratoComMovimentacoesAsync(Extrato extrato, IFormFile csvFile);

        Task<ExtratoEdicaoViewModel> ObterExtratoParaEdicaoAsync(int extratoId);

        Task AtualizarExtratoEMovimentacoesAsync(ExtratoEdicaoViewModel model);

        Task<bool> ExcluirExtratoAsync(int extratoId);

        Task<Stream> ExportarMovimentacoesDoExtratoParaCsvAsync(int extratoId);
    }
}