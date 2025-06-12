// STRATFY.Repositories/RepositoryMovimentacao.cs
using Microsoft.EntityFrameworkCore; // Certifique-se que está usando
using STRATFY.Models;
using STRATFY.Interfaces.IRepositories; // Para IRepositoryMovimentacao
using System.Collections.Generic; // Para List<Movimentacao>
using System.Linq; // Para Where, etc.

namespace STRATFY.Repositories
{
    public class RepositoryMovimentacao : RepositoryBase<Movimentacao>, IRepositoryMovimentacao, IDisposable
    {
        public RepositoryMovimentacao(AppDbContext context, bool pSaveChanges = true) : base(context, pSaveChanges)
        {
        }

        public Movimentacao SelecionarChave(int id)
        {
            return contexto.Set<Movimentacao>().Find(id);
            //return contexto.Set<Movimentacao>().Include(m => m.Categoria).FirstOrDefault(m => m.Id == id);
        }

        // Implementação do método RemoverVarias da IRepositoryMovimentacao
        public void RemoverVarias(List<Movimentacao> movimentacoes)
        {
            if (movimentacoes != null && movimentacoes.Any())
            {
                // Remove as entidades do DbSet. Elas serão excluídas do banco
                // quando o SaveChanges for chamado (pelo Salvar() ou SalvarAsync() no Repositório ou Service).
                contexto.Set<Movimentacao>().RemoveRange(movimentacoes);
                // Não chama SaveChanges aqui, pois a lógica de negócio (Service)
                // deve decidir quando persistir as mudanças em lote.
            }
        }
    }
}