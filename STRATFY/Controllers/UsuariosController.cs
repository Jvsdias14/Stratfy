using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using STRATFY.Models;
using STRATFY.Interfaces.IServices; // Usar a interface da Service
using STRATFY.Interfaces.IContexts;

namespace STRATFY.Controllers
{
    [Authorize]
    public class UsuariosController : Controller
    {
        private readonly IUsuarioService _usuarioService; // Injetar a Service
        private readonly IAccountService _accountService; // Para login após cadastro

        public UsuariosController(IUsuarioService usuarioService, IAccountService accountService)
        {
            _usuarioService = usuarioService;
            _accountService = accountService;
        }

        // GET: Usuarios
        public async Task<IActionResult> Index()
        {
            try
            {
                var usuarios = await _usuarioService.ObterTodosUsuariosAsync();
                return View(usuarios);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ocorreu um erro ao carregar os usuários: " + ex.Message;
                return View(new List<Usuario>());
            }
        }

        // GET: Usuarios/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var usuario = await _usuarioService.ObterUsuarioPorIdAsync(id.Value); // Delega para a Service
                if (usuario == null)
                {
                    return NotFound();
                }
                return View(usuario);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ocorreu um erro ao carregar os detalhes do usuário: " + ex.Message;
                return NotFound();
            }
        }

        // GET: Usuarios/Create
        [AllowAnonymous]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nome,Email,Senha")] Usuario usuario) // Senha ainda no bind, mas será hashada
        {

            if (ModelState.IsValid)
            {
                try
                {
                    // A Service irá lidar com a criptografia da senha e persistência
                    var novoUsuario = await _usuarioService.CriarUsuarioAsync(usuario, usuario.Senha);

                    // Após o cadastro, você pode logar o usuário automaticamente
                    await _accountService.LoginAsync(novoUsuario.Email, usuario.Senha); // Senha original antes do hash

                    TempData["SuccessMessage"] = "Usuário cadastrado com sucesso!";
                    return RedirectToAction("Index", "Extratos");
                }
                catch (ApplicationException ex)
                {
                    // Captura erros de negócio da Service (ex: e-mail já existe)
                    ModelState.AddModelError("", ex.Message);
                }
                catch (ArgumentException ex)
                {
                    // Captura erros de argumentos inválidos (ex: senha vazia)
                    ModelState.AddModelError("", ex.Message);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Ocorreu um erro inesperado ao cadastrar o usuário.");
                    // Logar o erro completo
                }
            }
            return View(usuario);
        }

        // GET: Usuarios/Edit/5 (Editando o próprio perfil do usuário logado)
        public async Task<IActionResult> Edit()
        {
            try
            {
                var user = await _usuarioService.ObterUsuarioLogadoAsync(); // Método para obter o ID do usuário logado
                var usuario = await _usuarioService.ObterUsuarioPorIdAsync(user.Id); // Assumindo que este método existe

                if (usuario == null)
                {
                    TempData["ErrorMessage"] = "Usuário não encontrado.";
                    return RedirectToAction("Index", "Extratos"); // Ou redirecionar para uma página de erro
                }

                // Mapeia a entidade Usuario para a ViewModel para exibir no formulário
                var model = new UsuarioEditVM
                {
                    Id = usuario.Id,
                    Nome = usuario.Nome,
                    Email = usuario.Email
                    // Não mapeie a senha aqui por segurança!
                };

                return View(model);
            }
            catch (UnauthorizedAccessException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Login", "Account"); // Ou Index de Login, dependendo da rota
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ocorreu um erro ao carregar os dados do usuário.";
                // Logar o erro
                return View("Error"); // Ou uma view de erro genérica
            }
        }

        // [HttpPost] Edit - Para processar o formulário submetido
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UsuarioEditVM model) // <<<< Aceita a ViewModel
        {
            // Remove a validação de Senha e ConfirmarNovaSenha se elas não foram preenchidas,
            // para que não causem erros de validação desnecessários quando o usuário não quiser mudar a senha.
            // A validação de stringLength (MinLength) para NovaSenha já cuidará se ela for preenchida
            // mas for muito curta.
            if (string.IsNullOrEmpty(model.NovaSenha) && string.IsNullOrEmpty(model.ConfirmarNovaSenha))
            {
                ModelState.Remove(nameof(model.NovaSenha));
                ModelState.Remove(nameof(model.ConfirmarNovaSenha));
            }


            if (!ModelState.IsValid)
            {
                // Se houver erros de validação, retorna a View com a ViewModel e os erros
                return View(model);
            }

            try
            {
                await _usuarioService.AtualizarUsuarioAsync(model); // <<<< Passa a ViewModel para o serviço
                TempData["SuccessMessage"] = "Dados do perfil atualizados com sucesso!";
                return RedirectToAction("Index", "Extratos"); // Redireciona para GET Edit para exibir a mensagem de sucesso
            }
            catch (UnauthorizedAccessException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Login", "Login");
            }
            catch (ApplicationException ex)
            {
                ModelState.AddModelError("", ex.Message); // Adiciona erro específico ao ModelState
                return View(model); // Retorna a View com a ViewModel e a mensagem de erro
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ocorreu um erro inesperado ao atualizar o perfil.";
                // Logar o erro
                return View(model); // Retorna a View com a ViewModel e a mensagem de erro genérica
            }
        }

        // GET: Usuarios/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            // Esta ação geralmente é para administradores, ou o próprio usuário pode se "deletar".
            // Para simplicidade, vou considerar que é para o próprio usuário ou para admin.
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var usuario = await _usuarioService.ObterUsuarioPorIdAsync(id.Value);
                if (usuario == null)
                {
                    return NotFound();
                }
                return View(usuario);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ocorreu um erro ao carregar o usuário para exclusão: " + ex.Message;
                return NotFound();
            }
        }

        // POST: Usuarios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _usuarioService.ExcluirUsuarioAsync(id); // Delega para a Service
                await _accountService.LogoutAsync(); // Se o próprio usuário se excluiu, faça logout
                TempData["SuccessMessage"] = "Usuário excluído com sucesso!";
                return RedirectToAction("Index", "Login"); // Redireciona para login após exclusão
            }
            catch (ApplicationException ex)
            {
                TempData["DeleteError"] = ex.Message; // Ex: "Usuário possui extratos vinculados"
                return RedirectToAction(nameof(Delete), new { id });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                TempData["DeleteError"] = "Ocorreu um erro inesperado ao excluir o usuário: " + ex.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        // O método UsuarioExists() não é mais necessário na Controller, a Service fará essa verificação.
        // private bool UsuarioExists(int id)
        // {
        //     return _context.Usuarios.Any(e => e.Id == id);
        // }
    }
}