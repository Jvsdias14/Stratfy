using Microsoft.EntityFrameworkCore;
using STRATFY.Helpers;
using STRATFY.Models;

namespace STRATFY.Repositories
{
    public class RepositoryDashboard : RepositoryBase<Dashboard>, IDisposable
    {
        private readonly UsuarioContexto usuarioContexto;
        public RepositoryDashboard(AppDbContext context, UsuarioContexto usuariocontexto, bool pSaveChanges = true) : base(context, pSaveChanges)
        {
            usuarioContexto = usuariocontexto;
        }

        public async Task<List<Dashboard>> SelecionarTodosDoUsuarioAsync()
        {
            var usuarioId = usuarioContexto.ObterUsuarioId();
            return await contexto.Set<Dashboard>()
                .Where(e => e.Extrato.UsuarioId == usuarioId)
                .Include(e => e.Cartoes)
                .Include(e => e.Graficos)
                .OrderByDescending(e => e.Id)
                .ToListAsync();
        }
        public void Dispose()
        {
        }
    }
}
