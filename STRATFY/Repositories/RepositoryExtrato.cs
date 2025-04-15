using Microsoft.EntityFrameworkCore;
using STRATFY.Models;

namespace STRATFY.Repositories
{
    public class RepositoryExtrato : RepositoryBase<Extrato>, IDisposable
    {
        public RepositoryExtrato(AppDbContext context, bool pSaveChanges = true) : base(context, pSaveChanges)
        {
        }
        public async Task<Extrato> CarregarExtratoCompleto(int id)
        {
            return await contexto.Extratos
                .Include(e => e.Movimentacaos)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public void Dispose()
        {
        }
    }
}
