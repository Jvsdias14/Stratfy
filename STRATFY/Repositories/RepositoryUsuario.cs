// STRATFY.Repositories/RepositoryUsuario.cs
using Microsoft.EntityFrameworkCore;
using STRATFY.Interfaces.IRepositories;
using STRATFY.Models;
using System.Threading.Tasks;

namespace STRATFY.Repositories
{
    // A interface IDisposable é herdada de IRepositoryBase, então não precisa ser explícita aqui.
    public class RepositoryUsuario : RepositoryBase<Usuario>, IRepositoryUsuario
    {
        // O AppDbContext já é acessível através do 'contexto' da classe base RepositoryBase
        // private readonly AppDbContext _context; // Não precisa desta declaração, já está na base

        public RepositoryUsuario(AppDbContext context, bool pSaveChanges = true) : base(context, pSaveChanges)
        {
            // O IUsuarioContexto e o método ObterUsuarioLogado()
            // foram removidos daqui e movidos para a camada de Service.
        }

        // Método específico para buscar usuário por email
        public async Task<Usuario> ObterUsuarioPorEmailAsync(string email)
        {
            return await contexto.Set<Usuario>().FirstOrDefaultAsync(u => u.Email == email);
        }

        // Método específico para verificar se um email já existe
        public async Task<bool> ExisteUsuarioComEmailAsync(string email)
        {
            return await contexto.Set<Usuario>().AnyAsync(u => u.Email == email);
        }

    }
}