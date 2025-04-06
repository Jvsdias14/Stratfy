using Microsoft.EntityFrameworkCore;
using STRATFY.Models;

namespace STRATFY.Repositories
{
    public class RepositoryLogin : RepositoryBase<Usuario>, IDisposable
    {
        public RepositoryLogin(AppDbContext context, bool pSaveChanges = true) : base(context, pSaveChanges)
        {
        }

        public async Task<Usuario> Login(string email, string senha)
        {
            return await contexto.Set<Usuario>().FirstOrDefaultAsync(u => u.Email == email && u.Senha == senha);
        }

        public void Dispose()
        {
        }
    }
}
