using Microsoft.EntityFrameworkCore;
using STRATFY.Interfaces;
using STRATFY.Models;

namespace STRATFY.Repositories
{
    public class Repository<T> : IRepositoryBase<T> where T : class
    {
        private readonly AppDbContext _context;
        private readonly DbSet<T> _dbSet;

        public Repository(AppDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task<IEnumerable<T>> SelecionarTodos() => await _dbSet.ToListAsync();
        public async Task<T> GetById(int id) => await _dbSet.FindAsync(id);
        public async Task Add(T entity) { _dbSet.Add(entity); await _context.SaveChangesAsync(); }
        public async Task Update(T entity) { _dbSet.Update(entity); await _context.SaveChangesAsync(); }
        public async Task Delete(int id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity != null) { _dbSet.Remove(entity); await _context.SaveChangesAsync(); }
        }

        public async Task<List<T>> SelecionarTodos() => await _dbSet.ToListAsync();

        public async Task<T> SelecionarPorId(int id) => await _dbSet.FindAsync(id);

        public Task Criar(T entity)
        {
            throw new NotImplementedException();
        }

        public Task Editar(T entity)
        {
            throw new NotImplementedException();
        }

        public Task Excluir(int id)
        {
            throw new NotImplementedException();
        }
    }
}

