// STRATFY.Repositories/RepositoryDashboard.cs
using Microsoft.EntityFrameworkCore;
using STRATFY.Interfaces.IRepositories;
using STRATFY.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using STRATFY.Interfaces.IContexts; // Para IUsuarioContexto

namespace STRATFY.Repositories
{
    public class RepositoryDashboard : RepositoryBase<Dashboard>, IRepositoryDashboard
    {
        private readonly IUsuarioContexto _usuarioContexto;

        public RepositoryDashboard(AppDbContext context, IUsuarioContexto usuarioContexto, bool pSaveChanges = true) : base(context, pSaveChanges)
        {
            _usuarioContexto = usuarioContexto;
        }

        public async Task<List<Dashboard>> SelecionarTodosDoUsuarioAsync(int usuarioId)
        {
            // Assumindo que Dashboard tenha um UserId ou que Extrato tenha um UserId
            // Se o Dashboard não tiver UserId direto, você pode precisar incluir o Extrato para filtrar pelo UserId
            return await contexto.Set<Dashboard>()
                                 .Include(d => d.Extrato) // Inclui o Extrato se necessário.Include(d => d.Extrato)
                                 .Include(d => d.Graficos) // Inclui os gráficos associados
                                 .Include(d => d.Cartoes) // Inclui os cartões associados
                                 .Where(d => d.Extrato.UsuarioId == usuarioId) // Adapte conforme seu modelo
                                 .ToListAsync();
        }

        public async Task<Dashboard> SelecionarDashboardCompletoPorIdAsync(int dashboardId)
        {
            return await contexto.Set<Dashboard>()
                                 .Include(d => d.Extrato)
                                 .ThenInclude(e => e.Movimentacaos)
                                 .ThenInclude(m => m.Categoria)
                                 .Include(d => d.Graficos)
                                 .Include(d => d.Cartoes)
                                 .FirstOrDefaultAsync(d => d.Id == dashboardId);
        }
    }
}