using STRATFY.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace STRATFY.Interfaces.IRepositories
{
    public interface IRepositoryDashboard : IRepositoryBase<Dashboard>
    {
        Task<List<Dashboard>> SelecionarTodosDoUsuarioAsync(int usuarioId);
        Task<Dashboard> SelecionarDashboardCompletoPorIdAsync(int dashboardId);
    }
}