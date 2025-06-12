using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STRATFY.Interfaces.IServices; // Usar a interface da Service
using STRATFY.Models;
using System; // Para Exception
using System.Threading.Tasks;

namespace STRATFY.Controllers
{
    public class LoginController : Controller
    {
        private readonly IAccountService _accountService;

        public LoginController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Index(string returnUrl = null)
        {
            if (_accountService.IsAuthenticated()) // Delega para a Service
            {
                return RedirectToAction("Index", "Extratos");
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
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
            {
                return View("Login", model);
            }

            try
            {
                // Delega a lógica de autenticação para a Service
                var loginSuccess = await _accountService.LoginAsync(model.Email, model.Senha);

                if (!loginSuccess)
                {
                    ViewData["Mensagem"] = "Email ou senha inválidos.";
                    ModelState.AddModelError("", "Email ou senha inválidos.");
                    return View("Login", model);
                }

                // Se returnUrl estiver preenchida, redireciona para ela
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Extratos");
            }
            catch (ArgumentNullException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View("Login", model);
            }
            catch (Exception ex)
            {
                // Logar o erro (ex: com um logger)
                Console.WriteLine($"Erro inesperado durante o login: {ex.Message}"); // Apenas para depuração ou substituir por um logger real
                ModelState.AddModelError("", "Ocorreu um erro inesperado durante o login.");
                return View("Login", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try // ADICIONADO O TRY-CATCH AQUI
            {
                await _accountService.LogoutAsync(); // Delega para a Service
            }
            catch (Exception ex)
            {
                // Logar a exceção. Mesmo com erro, o usuário deve ser redirecionado para a página de login.
                // Não há necessidade de exibir uma mensagem de erro específica na UI para o logout em si,
                // pois o objetivo é sempre levar o usuário para a tela de login após tentar deslogar.
                Console.WriteLine($"Erro durante o logout: {ex.Message}"); // Apenas para depuração ou substituir por um logger real
            }
            return RedirectToAction("Index", "Login");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Cadastrar()
        {
            return RedirectToAction("Create", "Usuarios");
        }
    }
}