using STRATFY.Models;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace STRATFY.Interfaces.IServices
{
    public interface IMovimentacaoService
    {

        Task ImportarMovimentacoesDoCsvAsync(List<Movimentacao> movimentacoesImportadas, int extratoId);

        Task AtualizarMovimentacoesDoExtratoAsync(List<Movimentacao> movimentacoesRecebidas, List<Movimentacao> movimentacoesExistentesNoBanco, int extratoId);

        void RemoverMovimentacoes(List<Movimentacao> movimentacoes); // Isso será usado internamente em AtualizarMovimentacoesDoExtratoAsync
        void IncluirMovimentacao(Movimentacao movimentacao); // Usado internamente em ImportarMovimentacoesDoCsvAsync e AtualizarMovimentacoesDoExtratoAsync
        void AtualizarMovimentacao(Movimentacao movimentacao); // Usado internamente em AtualizarMovimentacoesDoExtratoAsync
        Movimentacao ObterMovimentacaoPorId(int movimentacaoId); // Pode ser útil para testes ou cenários específicos
        Task SalvarAlteracoesAsync();
    }
}