using Microsoft.EntityFrameworkCore;
using STRATFY.Models;

namespace STRATFY.Repositories
{
    public class RepositoryExtrato : RepositoryBase<Extrato>, IDisposable
    {
        public RepositoryExtrato(AppDbContext context, bool pSaveChanges = true) : base(context, pSaveChanges)
        {
        }

        public Extrato CarregarExtratoCompleto(int extratoId)
        {
            var extrato = contexto.Extratos
                .Include(e => e.Usuario)
                .Include(e => e.Movimentacaos)
                .ThenInclude(m => m.Categoria)
                .FirstOrDefault(e => e.Id == extratoId);

            return extrato;
        }
        public void Dispose()
        {
        }
    }
}
