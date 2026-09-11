namespace SanatorioHMS.Domain.Core;

public abstract class Entity<TId> : IEquatable<Entity<TId>> where TId : notnull
{
    protected Entity(TId id) => Id = id;
    public TId Id { get; protected init; }
    public bool Equals(Entity<TId>? other) => other is not null && GetType() == other.GetType() && EqualityComparer<TId>.Default.Equals(Id, other.Id);
    public override bool Equals(object? obj) => obj is Entity<TId> other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}

public abstract class AggregateRoot<TId> : Entity<TId> where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = new();
    protected AggregateRoot(TId id) : base(id) { }
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public IReadOnlyCollection<IDomainEvent> DequeueDomainEvents()
    {
        var events = _domainEvents.ToArray();
        _domainEvents.Clear();
        return events;
    }
}

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Remove(T entity);
}

public abstract record ValueObject
{
    protected abstract IEnumerable<object?> GetEqualityComponents();
    public virtual bool Equals(ValueObject? other) => other is not null && GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    public override int GetHashCode() => GetEqualityComponents().Aggregate(17, (hash, value) => hash * 31 + (value?.GetHashCode() ?? 0));
}

public interface IDomainEvent { DateTime OccurredAt { get; } }
public readonly record struct Result(bool IsSuccess, string? Error = null)
{
    public static Result Success() => new(true);
    public static Result Failure(string error) => new(false, error);
}

#pragma warning disable CA1000 // Result<T> uses the same ergonomic factory API as Result.
public readonly record struct Result<T>(bool IsSuccess, T? Value = default, string? Error = null)
{
    public static Result<T> Success(T value) => new(true, value);
    public static Result<T> Failure(string error) => new(false, default, error);
}
#pragma warning restore CA1000
