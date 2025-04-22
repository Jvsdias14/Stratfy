using Microsoft.EntityFrameworkCore;
using STRATFY.Helpers;
using STRATFY.Interfaces;
using STRATFY.Models;
namespace STRATFY.Repositories
{
    public class RepositoryMovimentacao : RepositoryBase<Movimentacao>, IDisposable
    {
        private readonly UsuarioContexto usuarioContexto;
        public RepositoryMovimentacao(AppDbContext context, UsuarioContexto usuariocontexto, bool pSaveChanges = true) : base(context, pSaveChanges)
        {
        }

        public void RemoverVarias(List<Movimentacao> Movimentacoes)
        {
            contexto.Movimentacaos.RemoveRange(Movimentacoes);
        }

        public Movimentacao CarregarMovimentacaoCompleta(int id)
        {
            var movimentacao = contexto.Movimentacaos
                .Include(m => m.Categoria)
                .Include(m => m.Extrato)
                .FirstOrDefault(m => m.Id == id);

            return movimentacao;
        }

        public async Task<List<Movimentacao>> SelecionarTodosDoUsuarioAsync()
        {
            var usuarioId = usuarioContexto.ObterUsuarioId();
            return await contexto.Set<Movimentacao>()
                .Where(e => e.Extrato.UsuarioId == usuarioId)
                .ToListAsync();
        }
        public void Dispose()
        {
        }
    }
}
