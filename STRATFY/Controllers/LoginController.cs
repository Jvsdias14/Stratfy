using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using STRATFY.Models;
using STRATFY.Repositories;
using System.Security.Claims;

namespace STRATFY.Controllers
{
    public class LoginController : Controller
    {
        private readonly RepositoryLogin RepositoryLogin;
        private readonly AppDbContext _context;

        public LoginController(AppDbContext context, RepositoryLogin repositoryLogin)
        {
            _context = context;
            RepositoryLogin = repositoryLogin;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Index(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(returnUrl))
            {
                ViewData["Mensagem"] = "Você precisa estar logado para acessar essa página.";
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View("Login");
        }


        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginVM model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View("Login", model);

            var usuario = await RepositoryLogin.Login(model.Email, model.Senha);

            if (usuario == null)
            {
                ModelState.AddModelError("", "Email ou senha inválidos.");
                return View("Login", model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, usuario.Nome),
                new Claim(ClaimTypes.Email, usuario.Email)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // Se returnUrl estiver preenchida, redireciona pra ela
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index");
        }

    }
}
