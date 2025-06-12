// STRATFY.Interfaces.IRepositories/IRepositoryUsuario.cs
using STRATFY.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace STRATFY.Interfaces.IRepositories
{
    public interface IRepositoryUsuario : IRepositoryBase<Usuario>
    {
        Task<Usuario> ObterUsuarioPorEmailAsync(string email);
        Task<bool> ExisteUsuarioComEmailAsync(string email);
        // Se ObterUsuarioLogado ainda for necessário (para pegar um objeto Usuario completo)
        // e não apenas o ID, ele pode ficar aqui, mas o Ideal é que IUsuarioContexto
        // só retorne o ID e a Service use esse ID para buscar o usuário no repositório.
        // A lógica de claims deve estar na IAccountService, não em IRepositoryUsuario.
        // Task<Usuario> ObterUsuarioLogadoAsync(); // Provavelmente será removido após IUsuarioContexto
    }
}