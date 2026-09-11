using Microsoft.EntityFrameworkCore;
using SanatorioHMS.Domain.Core;

namespace SanatorioHMS.Infrastructure.Core;

public class Repository<T>(DbContext context) : IRepository<T> where T : class
{
    protected DbSet<T> Set => context.Set<T>();
    public Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default) => Set.FindAsync([id], cancellationToken).AsTask();
    public Task AddAsync(T entity, CancellationToken cancellationToken = default) => Set.AddAsync(entity, cancellationToken).AsTask();
    public void Update(T entity) => Set.Update(entity);
    public void Remove(T entity) => Set.Remove(entity);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed class UnitOfWork(DbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => context.SaveChangesAsync(cancellationToken);
}
