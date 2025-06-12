using STRATFY.Models;
using STRATFY.DTOs; // Para o método da API
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering; // Para SelectListItem

namespace STRATFY.Interfaces.IServices
{
    public interface IDashboardService
    {
        Task<List<Dashboard>> ObterTodosDashboardsDoUsuarioAsync();
        Task<Dashboard> ObterDashboardPorIdAsync(int dashboardId); // Retorna a entidade para a View
        Task<Dashboard> CriarDashboardAsync(DashboardVM model);
        Task<Dashboard> CriarDashboardPadraoAsync(string nome, int extratoId);
        Task AtualizarDashboardAsync(DashboardVM model);
        Task ExcluirDashboardAsync(int dashboardId);
        Task<DashboardDetailsDTO> ObterDadosDashboardParaApiAsync(int dashboardId); // Retorna o DTO para a API
        Task<List<SelectListItem>> ObterExtratosDisponiveisParaUsuarioAsync();
    }
}