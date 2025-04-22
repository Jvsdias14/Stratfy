using Microsoft.EntityFrameworkCore;
using STRATFY.Helpers;
using STRATFY.Models;

namespace STRATFY.Repositories
{
    public class RepositoryUsuario : RepositoryBase<Usuario>, IDisposable
    {

        private readonly UsuarioContexto usuarioContexto;
        public RepositoryUsuario(AppDbContext context, UsuarioContexto usuariocontexto, bool pSaveChanges = true) : base(context, pSaveChanges)
        {
            usuarioContexto = usuariocontexto;
        }

        public async Task<Usuario> ObterUsuarioLogado()
        {
            return await contexto.Set<Usuario>().FirstOrDefaultAsync(u => u.Id == usuarioContexto.ObterUsuarioId());
        }

        public void Dispose()
        {
        }
    }
}
