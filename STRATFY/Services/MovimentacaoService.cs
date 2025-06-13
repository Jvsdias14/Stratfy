// STRATFY.Services/MovimentacaoService.cs
using STRATFY.Interfaces.IRepositories;
using STRATFY.Interfaces.IServices;
using STRATFY.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace STRATFY.Services
{
    public class MovimentacaoService : IMovimentacaoService
    {
        private readonly IRepositoryMovimentacao _movimentacaoRepository;
        private readonly ICategoriaService _categoriaService;

        public MovimentacaoService(IRepositoryMovimentacao movimentacaoRepository, ICategoriaService categoriaService)
        {
            _movimentacaoRepository = movimentacaoRepository;
            _categoriaService = categoriaService;
        }

        public async Task ImportarMovimentacoesDoCsvAsync(List<Movimentacao> movimentacoesImportadas, int extratoId)
        {
            if (movimentacoesImportadas == null || !movimentacoesImportadas.Any())
            {
                return;
            }

            var todasCategorias = (await _categoriaService.ObterTodasCategoriasParaSelectListAsync()).ToList();

            // Crie um dicionário para busca rápida de categorias por nome (ignorando case e espaços)
            var categoriasPorNome = todasCategorias.ToDictionary(
                c => c.Nome.Trim().ToLower(),
                c => c.Id
            );

            // Obter o ID da categoria "Outros". Se não existir, lançar uma exceção clara.
            // É fundamental que a categoria "Outros" exista para o funcionamento da importação.
            if (!categoriasPorNome.TryGetValue("outros", out int categoriaOutrosId))
            {
                throw new InvalidOperationException("A categoria 'Outros' é obrigatória para a importação de movimentações e não foi encontrada no sistema. Por favor, certifique-se de que ela está configurada no banco de dados.");
            }

            foreach (var mov in movimentacoesImportadas)
            {
                mov.ExtratoId = extratoId;
                mov.Descricao = mov.Descricao?.Trim();
                var categoriaNomeCsv = mov.Categoria?.Nome?.Trim().ToLower();

                if (string.IsNullOrWhiteSpace(categoriaNomeCsv) || !categoriasPorNome.TryGetValue(categoriaNomeCsv, out int categoriaId))
                {
                    mov.CategoriaId = categoriaOutrosId; // Atribui "Outros" se o nome estiver vazio/nulo ou não for encontrado
                }
                else
                {
                    mov.CategoriaId = categoriaId;
                }

                mov.Categoria = null; // Garante que a entidade de navegação não seja incluída/atualizada
                _movimentacaoRepository.Incluir(mov);
            }

            _movimentacaoRepository.Salvar();
        }

        public async Task AtualizarMovimentacoesDoExtratoAsync(List<Movimentacao> movimentacoesRecebidas, List<Movimentacao> movimentacoesExistentesNoBanco, int extratoId)
        {
            if (movimentacoesRecebidas == null)
            {
                movimentacoesRecebidas = new List<Movimentacao>();
            }
            if (movimentacoesExistentesNoBanco == null)
            {
                movimentacoesExistentesNoBanco = new List<Movimentacao>();
            }

            var idsRecebidos = new HashSet<int>(movimentacoesRecebidas.Where(m => m.Id > 0).Select(m => m.Id));

            var movimentacoesParaRemover = movimentacoesExistentesNoBanco
                .Where(m => !idsRecebidos.Contains(m.Id))
                .ToList();

            if (movimentacoesParaRemover.Any())
            {
                _movimentacaoRepository.RemoverVarias(movimentacoesParaRemover);
            }

            // Pré-carregar todas as categorias válidas para validação eficiente.
            var categoriasValidasIds = new HashSet<int>((await _categoriaService.ObterTodasCategoriasParaSelectListAsync()).Select(c => c.Id));

            foreach (var movRecebida in movimentacoesRecebidas)
            {
                if (movRecebida == null)
                {
                    throw new InvalidOperationException("Uma movimentação nula foi encontrada na lista de movimentações recebidas.");
                }

                movRecebida.Descricao = movRecebida.Descricao?.Trim();

                // Validação para garantir que a CategoriaId é válida (não 0 e existe na lista de IDs válidos)
                if (movRecebida.Categoria.Id == 0 || !categoriasValidasIds.Contains(movRecebida.Categoria.Id))
                {
                    throw new ArgumentException($"A categoria selecionada para a movimentação com descrição '{movRecebida.Descricao?.Trim()}' é inválida ou não foi selecionada.");
                }

                if (movRecebida.Id == 0) // Nova movimentação
                {
                    movRecebida.ExtratoId = extratoId;
                    movRecebida.CategoriaId = movRecebida.Categoria.Id; // Limpa a navegação para evitar problemas de tracking
                    movRecebida.Categoria = null; // Garante que a instância de Categoria da ViewModel não seja rastreada
                    movRecebida.Extrato = null;
                    _movimentacaoRepository.Incluir(movRecebida);
                }
                else // Movimentação existente, precisa ser atualizada
                {
                    // Nota: O método SelecionarChave geralmente é síncrono e retorna a entidade rastreada.
                    // Se você tiver uma versão async (SelecionarChaveAsync), use-a.
                    var movBanco = _movimentacaoRepository.SelecionarChave(movRecebida.Id);

                    if (movBanco != null)
                    {
                        // Atualiza as propriedades do objeto que já está sendo rastreado pelo DbContext
                        movBanco.Descricao = movRecebida.Descricao;
                        movBanco.Valor = movRecebida.Valor;
                        movBanco.Tipo = movRecebida.Tipo;
                        movBanco.CategoriaId = movRecebida.Categoria.Id;
                        movBanco.DataMovimentacao = movRecebida.DataMovimentacao;
                        // Não é necessário chamar _movimentacaoRepository.Alterar(movBanco) se movBanco
                        // já foi obtido e está sendo rastreado pelo contexto. Salvar() no final fará o trabalho.
                    }
                    else
                    {
                        throw new InvalidOperationException($"Movimentação com Id {movRecebida.Id} esperada para atualização não encontrada no repositório.");
                    }
                }
            }

            _movimentacaoRepository.Salvar();
        }

        public void RemoverMovimentacoes(List<Movimentacao> movimentacoes)
        {
            if (movimentacoes != null && movimentacoes.Any())
            {
                _movimentacaoRepository.RemoverVarias(movimentacoes);
                _movimentacaoRepository.Salvar(); // Adicionei Salvar aqui também, pois é uma operação de persistência
            }
        }

        // Recomendação: Mude de 'async void' para 'async Task' para melhor manuseio de erros e assincronicidade.
        public async Task IncluirMovimentacao(Movimentacao movimentacao)
        {
            if (movimentacao == null)
            {
                throw new ArgumentNullException(nameof(movimentacao), "A movimentação a ser incluída não pode ser nula.");
            }

            // Validação de categoria para inclusão única
            var categoriasValidasIds = new HashSet<int>((await _categoriaService.ObterTodasCategoriasParaSelectListAsync()).Select(c => c.Id));
            if (movimentacao.CategoriaId == 0 || !categoriasValidasIds.Contains(movimentacao.CategoriaId))
            {
                throw new ArgumentException("Categoria inválida para inclusão de movimentação.");
            }
            movimentacao.Descricao = movimentacao.Descricao?.Trim();
            _movimentacaoRepository.Incluir(movimentacao);
            _movimentacaoRepository.Salvar(); // Salvar após a inclusão
        }

        // Recomendação: Mude de 'async void' para 'async Task' para melhor manuseio de erros e assincronicidade.
        public async Task AtualizarMovimentacao(Movimentacao movimentacao)
        {
            if (movimentacao == null)
            {
                throw new ArgumentNullException(nameof(movimentacao), "A movimentação a ser atualizada não pode ser nula.");
            }

            // Validação de categoria para atualização única
            var categoriasValidasIds = new HashSet<int>((await _categoriaService.ObterTodasCategoriasParaSelectListAsync()).Select(c => c.Id));
            if (movimentacao.CategoriaId == 0 || !categoriasValidasIds.Contains(movimentacao.CategoriaId))
            {
                throw new ArgumentException("Categoria inválida para atualização de movimentação.");
            }

            var movBanco = _movimentacaoRepository.SelecionarChave(movimentacao.Id);
            if (movBanco == null)
            {
                throw new InvalidOperationException($"Movimentação com Id {movimentacao.Id} não encontrada para atualização.");
            }

            movBanco.Descricao = movimentacao.Descricao?.Trim();
            movBanco.Valor = movimentacao.Valor;
            movBanco.Tipo = movimentacao.Tipo;
            movBanco.CategoriaId = movimentacao.CategoriaId;
            movBanco.DataMovimentacao = movimentacao.DataMovimentacao;
            // _movimentacaoRepository.Alterar(movBanco); // Não é necessário se o objeto é rastreado
            _movimentacaoRepository.Salvar(); // Salvar após a atualização
        }

        public Movimentacao ObterMovimentacaoPorId(int movimentacaoId)
        {
            return _movimentacaoRepository.SelecionarChave(movimentacaoId);
        }
    }
}