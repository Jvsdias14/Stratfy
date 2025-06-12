using STRATFY.Models;

namespace STRATFY.Interfaces.IRepositories
{
    public interface IRepositoryMovimentacao : IRepositoryBase<Movimentacao>
    {
        void RemoverVarias(List<Movimentacao> movimentacoes);
        Movimentacao SelecionarChave(int id); // Para o Edit
        
    }
}
