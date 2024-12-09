using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PoS_Placeholder.Server.Data;

namespace PoS_Placeholder.Server.Repositories;

public class Repository<T> where T : class
{
    protected readonly ApplicationDbContext _db;

    protected Repository(ApplicationDbContext db)
    {
        _db = db;
    }
    
    public T GetById(int id)
    {
        return _db.Set<T>().Find(id);
    }
    
    public async Task<T?> GetByIdAsync(int id)
    {
        return await _db.Set<T>().FindAsync(id);
    }

    public IEnumerable<T> GetAll()
    {
        return _db.Set<T>().ToList();
    }
    
    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _db.Set<T>().ToListAsync();
    }

    public IEnumerable<T> GetWhere(Expression<Func<T, bool>> expression)
    {
        return _db.Set<T>().Where(expression).ToList();
    }
    
    public async Task<IEnumerable<T>> GetWhereAsync(Expression<Func<T, bool>> expression)
    {
        return await _db.Set<T>().Where(expression).ToListAsync();
    }

    public void Add(T entity)
    {
        _db.Set<T>().Add(entity);
    }

    public void AddRange(IEnumerable<T> entities)
    {
        _db.Set<T>().AddRange(entities);
    }

    public void Remove(T entity)
    {
        _db.Set<T>().Remove(entity);
    }

    public void Update(T entity)
    {
        _db.Set<T>().Update(entity);
    }
    
    public void BulkUpdate(IEnumerable<T> entity)
    {
        _db.Set<T>().UpdateRange(entity);
    }
    
    public int SaveChanges()
    {
        return _db.SaveChanges();
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _db.SaveChangesAsync();
    }
}