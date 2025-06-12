using STRATFY.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace STRATFY.Interfaces.IServices
{
    public interface IUsuarioService
    {
        Task<List<Usuario>> ObterTodosUsuariosAsync();
        Task<Usuario> ObterUsuarioPorIdAsync(int id);

        Task<Usuario> ObterUsuarioLogadoAsync();
        Task<Usuario> CriarUsuarioAsync(Usuario usuario, string senhaPura); // Aceita senha pura para hash
        Task AtualizarUsuarioAsync(UsuarioEditVM model); // <<< Alterado para UsuarioEditVM
        Task ExcluirUsuarioAsync(int id);
        // Não precisamos de GetUsuarioId aqui, pois a Controller já o obtém ou pode obtê-lo do contexto do usuário.
    }
}