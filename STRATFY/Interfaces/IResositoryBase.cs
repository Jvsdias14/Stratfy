namespace STRATFY.Interfaces
{
    public interface IRepositoryBase<T> where T : class
    {
        Task<List<T>> SelecionarTodos();
        Task<T> SelecionarPorId(int id);
        Task Criar(T entity);
        Task Editar(T entity);
        Task Excluir(int id);
    }
}
