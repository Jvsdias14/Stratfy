// STRATFY.Services/CategoriaService.cs
using STRATFY.Interfaces.IRepositories;
using STRATFY.Interfaces.IServices;
using STRATFY.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace STRATFY.Services
{
    public class CategoriaService : ICategoriaService
    {
        private readonly IRepositoryBase<Categoria> _categoriaRepository;

        public CategoriaService(IRepositoryBase<Categoria> categoriaRepository)
        {
            _categoriaRepository = categoriaRepository;
        }

        public async Task<IEnumerable<Categoria>> ObterTodasCategoriasParaSelectListAsync()
        {
            return (await _categoriaRepository.SelecionarTodosAsync()).ToList();
        }

        public async Task<Categoria> ObterCategoriaPorIdAsync(int categoriaId)
        {
            var categoria = await _categoriaRepository.SelecionarChaveAsync(categoriaId);
            if (categoria == null)
            {
                throw new ApplicationException($"Categoria com ID {categoriaId} não encontrada.");
            }
            return categoria;
        }

        public async Task<int> ObterCategoriaIdPorNomeAsync(string nomeCategoria)
        {
            var categoria = (await _categoriaRepository.SelecionarTodosAsync())
                                .FirstOrDefault(c => c.Nome.Equals(nomeCategoria.Trim(), StringComparison.OrdinalIgnoreCase));

            if (categoria == null)
            {
                throw new ApplicationException($"Categoria '{nomeCategoria}' não encontrada no sistema de categorias fixas.");
            }
            return categoria.Id;
        }
    }
}