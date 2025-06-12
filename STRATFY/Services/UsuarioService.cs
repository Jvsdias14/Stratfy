using STRATFY.Interfaces.IRepositories;
using STRATFY.Interfaces.IServices;
using STRATFY.Interfaces.IContexts;
using STRATFY.Models;
using STRATFY.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace STRATFY.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IRepositoryUsuario _usuarioRepository;
        private readonly IUsuarioContexto _usuarioContexto;

        public UsuarioService(IRepositoryUsuario usuarioRepository, IUsuarioContexto usuarioContexto)
        {
            _usuarioRepository = usuarioRepository;
            _usuarioContexto = usuarioContexto;
        }

        public async Task<List<Usuario>> ObterTodosUsuariosAsync()
        {
            return await _usuarioRepository.SelecionarTodosAsync();
        }

        public async Task<Usuario> ObterUsuarioPorIdAsync(int id)
        {
            // O SelecionarChaveAsync(params object[] variavel) espera um array de objetos.
            // A chamada correta seria: new object[] { id }
            return await _usuarioRepository.SelecionarChaveAsync(new object[] { id });
        }

        public async Task<Usuario> ObterUsuarioLogadoAsync()
        {
            var userId = _usuarioContexto.ObterUsuarioId();
            if (userId <= 0)
            {
                return null;
            }
            // A chamada correta seria: new object[] { userId }
            return await _usuarioRepository.SelecionarChaveAsync(new object[] { userId });
        }

        public async Task<Usuario> CriarUsuarioAsync(Usuario usuario, string senha)
        {
            if (string.IsNullOrEmpty(senha))
            {
                throw new ArgumentException("A senha é obrigatória.");
            }

            if (await _usuarioRepository.ExisteUsuarioComEmailAsync(usuario.Email))
            {
                throw new ApplicationException("Já existe um usuário cadastrado com este e-mail.");
            }

            usuario.Senha = PasswordHasher.HashPassword(senha);

            // Se IncluirAsync já salva (pelo saveChanges = true), não precisa do Salvar() explícito aqui
            return await _usuarioRepository.IncluirAsync(usuario);
        }

        public async Task AtualizarUsuarioAsync(UsuarioEditVM model) // <<<< Recebe a ViewModel
        {
            var userIdLogado = _usuarioContexto.ObterUsuarioId();
            if (userIdLogado <= 0 || userIdLogado != model.Id) // Valida com o ID da ViewModel
            {
                throw new UnauthorizedAccessException("Você não tem permissão para atualizar este perfil.");
            }

            var usuarioExistente = await _usuarioRepository.SelecionarChaveAsync(new object[] { model.Id });
            if (usuarioExistente == null)
            {
                throw new ApplicationException("Usuário não encontrado para atualização.");
            }

            if (usuarioExistente.Email != model.Email && await _usuarioRepository.ExisteUsuarioComEmailAsync(model.Email))
            {
                throw new ApplicationException("O novo e-mail já está em uso por outro usuário.");
            }

            usuarioExistente.Nome = model.Nome;
            usuarioExistente.Email = model.Email;

            // --- TRATAMENTO DA NOVA SENHA ---
            if (!string.IsNullOrEmpty(model.NovaSenha)) // Se o usuário digitou uma nova senha
            {
                if (model.NovaSenha != model.ConfirmarNovaSenha)
                {
                    // Essa validação será feita na VM, mas é bom ter uma redundância aqui ou uma exceção mais específica
                    throw new ApplicationException("A nova senha e a confirmação não coincidem.");
                }
                // Hash da nova senha antes de salvar
                usuarioExistente.Senha = PasswordHasher.HashPassword(model.NovaSenha);
            }
            // Não toque em usuarioExistente.SenhaHash se NovaSenha estiver vazia

            await _usuarioRepository.AlterarAsync(usuarioExistente);
            // Salvar() é chamado automaticamente se pSaveChanges=true no construtor do repositório
            // _usuarioRepository.Salvar(); // Se seu repositório não salva automaticamente
        }

        public async Task ExcluirUsuarioAsync(int id)
        {
            var userIdLogado = _usuarioContexto.ObterUsuarioId();
            if (userIdLogado <= 0 || (userIdLogado != id /* && !_usuarioContexto.IsInRole("Admin") */))
            {
                throw new UnauthorizedAccessException("Você não tem permissão para excluir este usuário.");
            }

            var usuario = await _usuarioRepository.SelecionarChaveAsync(new object[] { id });
            if (usuario == null)
            {
                throw new ApplicationException("Usuário não encontrado para exclusão.");
            }

            // Se ExcluirAsync já salva (pelo saveChanges = true), não precisa do Salvar() explícito aqui
            await _usuarioRepository.ExcluirAsync(usuario);
        }

        public async Task<bool> ExisteEmail(string email)
        {
            return await _usuarioRepository.ExisteUsuarioComEmailAsync(email);
        }
    }
}