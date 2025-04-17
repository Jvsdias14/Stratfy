using Microsoft.EntityFrameworkCore;
using STRATFY.Interfaces;
using STRATFY.Models;
namespace STRATFY.Repositories
{
    public class RepositoryMovimentacao : RepositoryBase<Movimentacao>, IDisposable
    {
        public RepositoryMovimentacao(AppDbContext context, bool pSaveChanges = true) : base(context, pSaveChanges)
        {
        }

        public void RemoverVarias(List<Movimentacao> Movimentacoes)
        {
            contexto.Movimentacaos.RemoveRange(Movimentacoes);
        }

        public Movimentacao CarregarMovimentacaoCompleta(int id)
        {
            var movimentacao = contexto.Movimentacaos
                .Include(m => m.Categoria)
                .Include(m => m.Extrato)
                .FirstOrDefault(m => m.Id == id);

            return movimentacao;
        }
        public void Dispose()
        {
        }
    }
}
