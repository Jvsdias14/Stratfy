using STRATFY.Models;
using STRATFY.Repositories;

namespace STRATFY.Interfaces.IRepositories
{
    public interface IRepositoryLogin : IRepositoryBase<Usuario>
    {
        Task<Usuario> Login(string email, string senha);
    }
}
