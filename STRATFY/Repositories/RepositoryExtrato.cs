using Microsoft.EntityFrameworkCore;
using STRATFY.Helpers;
using STRATFY.Models;

namespace STRATFY.Repositories
{
    public class RepositoryExtrato : RepositoryBase<Extrato>, IDisposable
    {
        private readonly UsuarioContexto usuarioContexto;
        public RepositoryExtrato(AppDbContext context, UsuarioContexto usuariocontexto, bool pSaveChanges = true) : base(context, pSaveChanges)
        {
            usuarioContexto = usuariocontexto;
        }

        public Extrato CarregarExtratoCompleto(int extratoId)
        {
            var extrato = contexto.Extratos
                .Include(e => e.Usuario)
                .Include(e => e.Movimentacaos)
                .ThenInclude(m => m.Categoria)
                .FirstOrDefault(e => e.Id == extratoId);

            return extrato;
        }

        public async Task<List<Extrato>> SelecionarTodosDoUsuarioAsync()
        {
            var usuarioId = usuarioContexto.ObterUsuarioId();
            return await contexto.Set<Extrato>()
                .Where(e => e.UsuarioId == usuarioId)
                .OrderByDescending(e => e.Id)
                .ToListAsync();
        }
        public void Dispose()
        {
        }
    }
}
