// STRATFY.Services/AccountService.cs
using STRATFY.Interfaces.IRepositories;
using STRATFY.Interfaces.IServices;
using STRATFY.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using STRATFY.Helpers; // Para PasswordHasher

namespace STRATFY.Services
{
    public class AccountService : IAccountService
    {
        private readonly IRepositoryUsuario _usuarioRepository; // Usaremos IRepositoryUsuario agora
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AccountService(IRepositoryUsuario usuarioRepository, IHttpContextAccessor httpContextAccessor)
        {
            _usuarioRepository = usuarioRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<bool> LoginAsync(string email, string senha, bool isPersistent = false)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(senha))
            {
                throw new ArgumentNullException("Email e senha são obrigatórios.");
            }

            // Acesso ao repositório para obter o usuário por email
            var usuario = await _usuarioRepository.ObterUsuarioPorEmailAsync(email);

            if (usuario == null)
            {
                return false; // Usuário não encontrado
            }

            // VERIFICAÇÃO DA SENHA HASHADA
            if (!PasswordHasher.VerifyPassword(senha, usuario.Senha))
            {
                return false; // Senha incorreta
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Nome),
                new Claim(ClaimTypes.Email, usuario.Email)
                // Adicione outras claims conforme necessário (ex: roles)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = isPersistent,
            };

            await _httpContextAccessor.HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProperties);

            return true;
        }

        public async Task LogoutAsync()
        {
            await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        public bool IsAuthenticated()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
        }
    }
}