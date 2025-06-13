// STRATFY.Interfaces.IServices/IMovimentacaoService.cs
using STRATFY.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System; // Para ArgumentException

namespace STRATFY.Interfaces.IServices
{
    public interface IMovimentacaoService
    {
        Task ImportarMovimentacoesDoCsvAsync(List<Movimentacao> movimentacoesImportadas, int extratoId);

        Task AtualizarMovimentacoesDoExtratoAsync(List<Movimentacao> movimentacoesRecebidas, List<Movimentacao> movimentacoesExistentesNoBanco, int extratoId);
    }
}