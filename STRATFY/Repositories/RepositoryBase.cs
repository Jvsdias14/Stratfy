using Microsoft.EntityFrameworkCore;
using STRATFY.Interfaces;
using STRATFY.Models;

namespace STRATFY.Repositories
{
    public class RepositoryBase<T> : IRepositoryBase<T>, IDisposable where T : class
    {
        public AppDbContext contexto;
        public bool saveChanges = true;
        DbSet<T> _dbSet;

        public RepositoryBase(AppDbContext context, bool pSaveChanges)
        {
            contexto = context;
            saveChanges = pSaveChanges;
        }
        public async Task<List<T>> SelecionarTodosAsync()
        {
            return await contexto.Set<T>().ToListAsync();
        }

        public async Task<T> SelecionarChaveAsync(params object[] variavel)
        {
            return await contexto.Set<T>().FindAsync(variavel);
        }

        public async Task<T> IncluirAsync(T entity)
        {
            await contexto.Set<T>().AddAsync(entity);
            if (saveChanges)
            {
                await contexto.SaveChangesAsync();
            }
            return entity;
        }

        public async Task<T> AlterarAsync(T entity)
        {
            contexto.Entry<T>(entity).State = EntityState.Modified;
            if (saveChanges)
            {
                await contexto.SaveChangesAsync();
            }
            return entity;
        }

        public async Task ExcluirAsync(T entity)
        {
            contexto.Entry<T>(entity).State = EntityState.Deleted;
            if (saveChanges)
            {
               await contexto.SaveChangesAsync();
            };
        }

        public List<T> SelecionarTodos()
        {
            return contexto.Set<T>().ToList();
        }

        public T SelecionarChave(params object[] variavel)
        {
            return contexto.Set<T>().Find(variavel);
        }

        public T Incluir(T entity)
        {
            contexto.Set<T>().Add(entity);
            if (saveChanges)
            {
                contexto.SaveChanges();
            }
            return entity;
        }

        public T Alterar(T entity)
        {
            contexto.Entry<T>(entity).State = EntityState.Modified;
            if (saveChanges)
            {
                contexto.SaveChanges();
            }
            return entity;
        }

        public void Excluir(T entity)
        {
            contexto.Entry<T>(entity).State = EntityState.Deleted;
            if (saveChanges)
            {
                contexto.SaveChanges();
            }
        }

        public void Dispose()
        {
            contexto.Dispose();
        }
    }
}















//public async Task<IEnumerable<T>> SelecionarTodos() => await _dbSet.ToListAsync();
//public async Task<T> GetById(int id) => await _dbSet.FindAsync(id);
//public async Task Add(T entity) { _dbSet.Add(entity); await _context.SaveChangesAsync(); }
//public async Task Update(T entity) { _dbSet.Update(entity); await _context.SaveChangesAsync(); }
//public async Task Delete(int id)
//{
//    var entity = await _dbSet.FindAsync(id);
//    if (entity != null) { _dbSet.Remove(entity); await _context.SaveChangesAsync(); }
//}