using STRATFY.Models;

namespace STRATFY.Interfaces.IRepositories
{
    public interface IRepositoryExtrato : IRepositoryBase<Extrato>
    {
        Extrato CarregarExtratoCompleto(int extratoId);
        Task<List<Extrato>> SelecionarTodosDoUsuarioAsync(int userId);

        Task<Extrato> CarregarExtratoCompletoAsync(int extratoId); // Adicionando a versão Async
    }
}
