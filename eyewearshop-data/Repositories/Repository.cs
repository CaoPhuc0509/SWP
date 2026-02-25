using eyewearshop_data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eyewearshop_data.Repositories;

public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
{
    private readonly EyewearShopDbContext _dbContext;
    private readonly DbSet<TEntity> _dbSet;

    public Repository(EyewearShopDbContext dbContext)
    {
        _dbContext = dbContext;
        _dbSet = _dbContext.Set<TEntity>();
    }

    public IQueryable<TEntity> Query() => _dbSet.AsQueryable();

    public async Task AddAsync(TEntity entity, CancellationToken ct = default)
    {
        await _dbSet.AddAsync(entity, ct);
    }

    public void Update(TEntity entity)
    {
        _dbSet.Update(entity);
    }

    public void Remove(TEntity entity)
    {
        _dbSet.Remove(entity);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return _dbContext.SaveChangesAsync(ct);
    }
}

