// STRATFY.Interfaces/IServices/ICategoriaService.cs
using STRATFY.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace STRATFY.Interfaces.IServices
{
    public interface ICategoriaService
    {
        Task<IEnumerable<Categoria>> ObterTodasCategoriasParaSelectListAsync();
        Task<Categoria> ObterCategoriaPorIdAsync(int categoriaId);
        Task<int> ObterCategoriaIdPorNomeAsync(string nomeCategoria);
    }
}