
using STRATFY.Models; // Se LoginVM estiver em Models ou ViewsModels
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication; // Para AuthenticationProperties

namespace STRATFY.Interfaces.IServices
{
    public interface IAccountService
    {
        Task<bool> LoginAsync(string email, string senha, bool isPersistent = false);

        Task LogoutAsync();

        // Opcional: Método para verificar se o usuário está autenticado
        bool IsAuthenticated();

        // Opcional: Métodos para registro de usuário, recuperação de senha, etc.
        // Task<Usuario> RegisterUserAsync(RegisterVM model);
        // Task<bool> ForgotPasswordAsync(string email);
    }
}