using Microsoft.EntityFrameworkCore;
using STRATFY.Helpers;
using STRATFY.Interfaces.IContexts; // Para IUsuarioContexto, se for injetado
using STRATFY.Interfaces.IRepositories;
using STRATFY.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace STRATFY.Repositories
{
    // Removi IDisposable daqui porque RepositoryBase já implementa
    public class RepositoryExtrato : RepositoryBase<Extrato>, IRepositoryExtrato
    {

        public RepositoryExtrato(AppDbContext context, bool pSaveChanges = true) : base(context, pSaveChanges)
        {
        }

        // Seu método existente (síncrono)
        public Extrato CarregarExtratoCompleto(int extratoId)
        {
            var extrato = contexto.Extratos
                .Include(e => e.Usuario)
                .Include(e => e.Movimentacaos)
                .ThenInclude(m => m.Categoria)
                .FirstOrDefault(e => e.Id == extratoId);

            return extrato;
        }

        // Nova versão assíncrona para CarregarExtratoCompleto
        public async Task<Extrato> CarregarExtratoCompletoAsync(int extratoId)
        {
            var extrato = await contexto.Extratos
                .Include(e => e.Usuario)
                .Include(e => e.Movimentacaos)
                .ThenInclude(m => m.Categoria)
                .FirstOrDefaultAsync(e => e.Id == extratoId);

            return extrato;
        }

        public async Task<List<Extrato>> SelecionarTodosDoUsuarioAsync(int usuarioId)
        {
            return await contexto.Set<Extrato>()
                .Where(e => e.UsuarioId == usuarioId)
                .Include(e => e.Movimentacaos) // Inclui o usuário associado
                .OrderByDescending(e => e.Id)
                .ToListAsync();
        }

    }
}