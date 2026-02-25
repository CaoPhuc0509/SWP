using System.Linq;

namespace eyewearshop_data.Interfaces;

public interface IRepository<TEntity> where TEntity : class
{
    IQueryable<TEntity> Query();

    Task AddAsync(TEntity entity, CancellationToken ct = default);

    void Update(TEntity entity);

    void Remove(TEntity entity);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

