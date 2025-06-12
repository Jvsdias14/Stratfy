using STRATFY.Interfaces.IRepositories;
using STRATFY.Interfaces.IServices;
using STRATFY.Models;
using System.Collections.Generic;
using System.Linq; // Para ToList()
using System.Threading.Tasks;

namespace STRATFY.Services
{
    public class CategoriaService : ICategoriaService
    {
        private readonly IRepositoryBase<Categoria> _categoriaRepository;

        public CategoriaService(IRepositoryBase<Categoria> categoriaRepository)
        {
            _categoriaRepository = categoriaRepository;
        }

        public async Task<Categoria> ObterOuCriarCategoriaAsync(string nomeCategoria)
        {
            var categoriaExistente = _categoriaRepository.SelecionarTodos() // Seleciona todos os itens e filtra em memória (NÃO IDEAL PARA MUITOS DADOS)
                                           .FirstOrDefault(c => c.Nome.Equals(nomeCategoria, StringComparison.OrdinalIgnoreCase));
       

            if (categoriaExistente != null)
            {
                return categoriaExistente;
            }
            else
            {
                var novaCategoria = new Categoria
                {
                    Nome = nomeCategoria
                };
                await _categoriaRepository.IncluirAsync(novaCategoria);
                _categoriaRepository.Salvar(); // Salva a nova categoria imediatamente

                return novaCategoria;
            }
        }

        public IEnumerable<Categoria> ObterTodasCategoriasParaSelectList()
        {
            return _categoriaRepository.SelecionarTodos().ToList();
        }
    }
}