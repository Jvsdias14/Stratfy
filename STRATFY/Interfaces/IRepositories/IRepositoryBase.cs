namespace STRATFY.Interfaces.IRepositories
{
    public interface IRepositoryBase<T> where T : class
    {
        //Assincronos
        Task<List<T>> SelecionarTodosAsync();
        Task<T> SelecionarChaveAsync(params object[] variavel);
        Task<T> IncluirAsync(T entity);
        Task<T> AlterarAsync(T entity);
        Task ExcluirAsync(T entity);

        //Sincronos

        List<T> SelecionarTodos();
        T SelecionarChave(params object[] variavel);
        T Incluir(T entity);
        T Alterar(T entity);
        void Excluir(T entity);
    }
}
