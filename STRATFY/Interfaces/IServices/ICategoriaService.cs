using STRATFY.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace STRATFY.Interfaces.IServices
{
    public interface ICategoriaService
    {
        Task<Categoria> ObterOuCriarCategoriaAsync(string nomeCategoria);
        IEnumerable<Categoria> ObterTodasCategoriasParaSelectList();
    }
}