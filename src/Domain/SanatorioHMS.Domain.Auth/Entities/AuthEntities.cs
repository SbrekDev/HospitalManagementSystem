using System.Security.Cryptography;
using System.Text;
using SanatorioHMS.Domain.Auth.Events;
using SanatorioHMS.Domain.Core;

namespace SanatorioHMS.Domain.Auth.Entities;

#pragma warning disable CA1711 // Permission and RolePermission are domain names from the RBAC model.

public sealed class User : AggregateRoot<Guid>
{
    public User(Guid id, string username) : base(id) { if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("Username is required."); Username = username.Trim(); }
    public string Username { get; }
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public ICollection<UserRole> UserRoles { get; } = new List<UserRole>();
    public ICollection<Session> Sessions { get; } = new List<Session>();
    public void Disable() => IsActive = false;
    public void Enable() => IsActive = true;
    public void SetPasswordHash(string passwordHash) => PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
    public void AssignRole(Role role) { if (UserRoles.All(x => x.RoleId != role.Id)) UserRoles.Add(new UserRole(Id, role.Id)); }
}

public sealed class Role : Entity<Guid>
{
    public Role(Guid id, string name) : base(id) { if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Role name is required."); Name = name.Trim(); }
    public string Name { get; }
    public ICollection<RolePermission> Permissions { get; } = new List<RolePermission>();
}

public sealed class Permission : Entity<Guid>
{
    public Permission(Guid id, string code) : base(id) { if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Permission code is required."); Code = code.Trim(); }
    public string Code { get; }
}

public sealed record RolePermission(Guid RoleId, Guid PermissionId);
public sealed record UserRole(Guid UserId, Guid RoleId);

public sealed class Session : Entity<Guid>
{
    public Session(Guid id, Guid userId, DateTime expiresAt, string? refreshTokenHash = null) : base(id) { UserId = userId; ExpiresAt = expiresAt; RefreshTokenHash = refreshTokenHash; }
    public Guid UserId { get; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public string? RefreshTokenHash { get; private set; }
    public bool IsValid(DateTime now) => !IsRevoked && ExpiresAt > now;
    public void Revoke() => IsRevoked = true;
    public void Rotate(string refreshTokenHash, DateTime expiresAt) { RefreshTokenHash = refreshTokenHash; ExpiresAt = expiresAt; }
}

public sealed class AuditLog : AggregateRoot<Guid>
{
    private AuditLog(Guid id) : base(id) { }
    public Guid? ActorId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string Target { get; private set; } = string.Empty;
    public DateTime RecordedAt { get; private set; }
    public string Outcome { get; private set; } = string.Empty;
    public string? PreviousHash { get; private set; }
    public string Hash { get; private set; } = string.Empty;
    public static AuditLog Record(Guid? actorId, string action, string target, string outcome, string? previousHash = null)
    {
        if (string.IsNullOrWhiteSpace(action) || string.IsNullOrWhiteSpace(target)) throw new ArgumentException("Audit action and target are required.");
        var entry = new AuditLog(Guid.NewGuid()) { ActorId = actorId, Action = action, Target = target, Outcome = outcome, RecordedAt = DateTime.UtcNow, PreviousHash = previousHash };
        entry.Hash = ComputeHash(entry);
        entry.AddDomainEvent(new AuditRecorded(entry.Id, entry.ActorId, entry.Action, entry.RecordedAt));
        return entry;
    }
    public bool LinksTo(AuditLog previous) => PreviousHash == previous.Hash;
    private static string ComputeHash(AuditLog entry)
    {
        var payload = $"{entry.Id:N}|{entry.ActorId}|{entry.Action}|{entry.Target}|{entry.RecordedAt:O}|{entry.Outcome}|{entry.PreviousHash}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }
}

#pragma warning restore CA1711
