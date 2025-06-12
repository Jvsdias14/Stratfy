using System.Security.Claims;

namespace STRATFY.Interfaces.IContexts
{
    public interface IUsuarioContexto
    {
        int ObterUsuarioId();
        string ObterUsuarioEmail();
        string ObterUsuarioNome();
        
    }
}
