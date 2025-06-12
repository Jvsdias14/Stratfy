using STRATFY.Interfaces.IContexts;
using System.Security.Claims;

namespace STRATFY.Helpers
{
    public class UsuarioContexto : IUsuarioContexto
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public int UsuarioId { get; private set; }

        public UsuarioContexto(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int ObterUsuarioId()
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            //UsuarioId = claim != null ? int.Parse(claim.Value) : 0;
            return claim != null ? int.Parse(claim.Value) : 0;

        }

        public string ObterUsuarioEmail()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
        }

        public string ObterUsuarioNome()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
        }
    }
}
