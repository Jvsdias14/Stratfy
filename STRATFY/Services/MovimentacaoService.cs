// STRATFY.Services/MovimentacaoService.cs
using STRATFY.Interfaces.IRepositories;
using STRATFY.Interfaces.IServices;
using STRATFY.Models;
using System.Collections.Generic;
using System.Linq; // Para Where, Select, Any, ToList
using System.Threading.Tasks;

namespace STRATFY.Services
{
    public class MovimentacaoService : IMovimentacaoService
    {
        private readonly IRepositoryMovimentacao _movimentacaoRepository;
        private readonly ICategoriaService _categoriaService; // Injetar CategoriaService

        public MovimentacaoService(IRepositoryMovimentacao movimentacaoRepository, ICategoriaService categoriaService)
        {
            _movimentacaoRepository = movimentacaoRepository;
            _categoriaService = categoriaService;
        }

        public async Task ImportarMovimentacoesDoCsvAsync(List<Movimentacao> movimentacoesImportadas, int extratoId)
        {
            if (movimentacoesImportadas == null || !movimentacoesImportadas.Any())
            {
                return; // Nada para importar
            }

            foreach (var mov in movimentacoesImportadas)
            {
                mov.ExtratoId = extratoId; // Vincula ao extrato

                // Lógica de categoria: delegar para CategoriaService
                // O CSV pode vir com Categoria.Id (0 para nova) ou Categoria.Nome
                // Assumindo que a CategoriaService vai lidar com isso de forma inteligente
                // Se o JSON do CSV já traz um Categoria.Nome, usaremos ele.
                // Se o JSON do CSV traz Categoria.Id e Categoria.Nome, podemos usar o nome para buscar/criar.
                if (mov.Categoria != null && !string.IsNullOrEmpty(mov.Categoria.Nome))
                {
                    var categoriaProcessada = await _categoriaService.ObterOuCriarCategoriaAsync(mov.Categoria.Nome);
                    mov.CategoriaId = categoriaProcessada.Id; // Vincula pelo ID
                    mov.Categoria = null; // Evita que o EF tente inserir a categoria novamente
                }
                else
                {
                    // Tratar caso onde a categoria não tem nome (ou definir uma categoria padrão)
                    // Por exemplo, vincular a uma categoria "Outros"
                    var categoriaPadrao = await _categoriaService.ObterOuCriarCategoriaAsync("Outros");
                    mov.CategoriaId = categoriaPadrao.Id;
                    mov.Categoria = null;
                }

                _movimentacaoRepository.Incluir(mov);
            }

            await SalvarAlteracoesAsync(); // Salva todas as movimentações importadas de uma vez
        }

        public async Task AtualizarMovimentacoesDoExtratoAsync(List<Movimentacao> movimentacoesRecebidas, List<Movimentacao> movimentacoesExistentesNoBanco, int extratoId)
        {
            // Remover movimentações que não estão mais na lista recebida
            var idsRecebidos = movimentacoesRecebidas.Select(m => m.Id).ToList();
            var movimentacoesParaRemover = movimentacoesExistentesNoBanco.Where(m => m.Id != 0 && !idsRecebidos.Contains(m.Id)).ToList(); // Exclui IDs 0 (novas)
            RemoverMovimentacoes(movimentacoesParaRemover);

            foreach (var movRecebida in movimentacoesRecebidas)
            {
                // Validação de categoria, a Service deve garantir que a categoria existe e está válida
                if (movRecebida.Categoria == null || movRecebida.Categoria.Id <= 0)
                {
                    throw new ArgumentException($"A categoria é obrigatória para a movimentação com descrição '{movRecebida.Descricao}'.");
                }

                if (movRecebida.Id == 0) // Nova movimentação
                {
                    var novaMovimentacao = new Movimentacao
                    {
                        Descricao = movRecebida.Descricao,
                        Valor = movRecebida.Valor,
                        Tipo = movRecebida.Tipo,
                        CategoriaId = movRecebida.Categoria.Id,
                        DataMovimentacao = movRecebida.DataMovimentacao,
                        ExtratoId = extratoId
                    };
                    novaMovimentacao.Categoria = null; // Evita que o EF tente inserir a categoria novamente
                    IncluirMovimentacao(novaMovimentacao);
                }
                else // Movimentação existente
                {
                    var movBanco = ObterMovimentacaoPorId(movRecebida.Id); // Busca a movimentação pelo ID
                    if (movBanco != null)
                    {
                        movBanco.Descricao = movRecebida.Descricao;
                        movBanco.Valor = movRecebida.Valor;
                        movBanco.Tipo = movRecebida.Tipo;
                        movBanco.CategoriaId = movRecebida.Categoria.Id; // Atualiza o ID da categoria
                        movBanco.DataMovimentacao = movRecebida.DataMovimentacao;
                        AtualizarMovimentacao(movBanco); // Marca para atualização (se necessário)
                    }
                }
            }

            await SalvarAlteracoesAsync(); // Salva todas as alterações de uma vez
        }

        public void RemoverMovimentacoes(List<Movimentacao> movimentacoes)
        {
            _movimentacaoRepository.RemoverVarias(movimentacoes);
        }

        public void IncluirMovimentacao(Movimentacao movimentacao)
        {
            _movimentacaoRepository.Incluir(movimentacao);
        }

        public void AtualizarMovimentacao(Movimentacao movimentacao)
        {
            // O IRepositoryBase<T> não tem um método Alterar específico.
            // Se o objeto 'movimentacao' já foi obtido do contexto e modificado (tracked),
            // SalvarAlteracoesAsync() será suficiente.
            // Se o objeto não estiver tracked (ex: foi criado fora do contexto e você quer attachar),
            // você precisaria de: _movimentacaoRepository.Contexto.Entry(movimentacao).State = EntityState.Modified;
            // ou um método Alterar no IRepositoryBase.
            // Por enquanto, assumimos que 'movBanco' já está tracked ou que Incluir/Remover são suficientes.
            // Se _movimentacaoRepository.SelecionarChave() retorna um objeto tracked, as alterações serão salvas.
            // Se for um novo objeto Movimentacao com o mesmo ID, pode precisar de um método de Attach/Update.
        }

        public Movimentacao ObterMovimentacaoPorId(int movimentacaoId)
        {
            return _movimentacaoRepository.SelecionarChave(movimentacaoId);
        }

        public async Task SalvarAlteracoesAsync()
        {
            // Chama o Salvar do repositório para persistir todas as mudanças pendentes
            // no contexto compartilhado.
            _movimentacaoRepository.Salvar();
            await Task.CompletedTask; // Para satisfazer o Task do método
        }
    }
}