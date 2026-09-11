using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SanatorioHMS.Application.Auth;
using SanatorioHMS.Application.ClinicalCare;
using SanatorioHMS.Application.Core;
using SanatorioHMS.Application.Diagnostics;
using SanatorioHMS.Application.PatientRegistry;
using SanatorioHMS.Application.Scheduling;
using SanatorioHMS.Domain.Auth.Entities;
using SanatorioHMS.Domain.ClinicalCare.Entities;
using SanatorioHMS.Domain.Core;
using SanatorioHMS.Domain.Diagnostics.Entities;
using SanatorioHMS.Domain.PatientRegistry.Entities;
using SanatorioHMS.Domain.Scheduling.Entities;

namespace SanatorioHMS.Api;

public sealed class ApiJwtOptions
{
    public string Issuer { get; init; } = "SanatorioHMS";
    public string Audience { get; init; } = "SanatorioHMS.Api";
    public string SigningKey { get; init; } = "development-signing-key-change-in-production-1234567890";
}

public sealed class InMemoryStore : IPatientRegistryRepository, ISchedulingRepository, ITurnRepository, IClinicalCareRepository, IDiagnosticsRepository, IUserAuthRepository
{
    private readonly ConcurrentDictionary<Guid, Patient> patients = new();
    private readonly ConcurrentDictionary<Guid, Agenda> agendas = new();
    private readonly ConcurrentDictionary<Guid, Turno> turns = new();
    private readonly ConcurrentDictionary<Guid, Episode> episodes = new();
    private readonly ConcurrentDictionary<Guid, Study> studies = new();
    private readonly ConcurrentDictionary<Guid, DiagnosticOrder> orders = new();
    private readonly ConcurrentDictionary<Guid, DiagnosticResult> results = new();
    private readonly ConcurrentDictionary<string, User> users = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim bookingLock = new(1, 1);
    public IEnumerable<Agenda> Agendas => agendas.Values;
    public IEnumerable<Turno> Turns => turns.Values;
    public IEnumerable<Episode> Episodes => episodes.Values;

    public InMemoryStore()
    {
        var admin = new User(Guid.Parse("00000000-0000-0000-0000-000000000001"), "admin@sanatorio.local");
        admin.SetPasswordHash(Hash("Admin123!"));
        admin.AssignRole(new Role(Guid.NewGuid(), "Admin"));
        users[admin.Username] = admin;
        var receptionist = new User(Guid.Parse("00000000-0000-0000-0000-000000000002"), "reception@sanatorio.local");
        receptionist.SetPasswordHash(Hash("Reception123!"));
        receptionist.AssignRole(new Role(Guid.NewGuid(), "Receptionist"));
        users[receptionist.Username] = receptionist;
    }

    Task<Patient?> IRepository<Patient>.GetByIdAsync(object id, CancellationToken ct) => Task.FromResult(patients.TryGetValue((Guid)id, out var value) ? value : null);
    public Task AddAsync(Patient entity, CancellationToken ct = default) { patients[entity.Id] = entity; return Task.CompletedTask; }
    public void Update(Patient entity) => patients[entity.Id] = entity;
    public void Remove(Patient entity) => patients.TryRemove(entity.Id, out _);
    public Task<bool> HasDocumentAsync(DocumentType type, string number, CancellationToken ct = default) => Task.FromResult(patients.Values.SelectMany(x => x.Documents).Any(x => x.DocumentType == type && x.DocumentNumber.Equals(number.Trim(), StringComparison.OrdinalIgnoreCase)));
    public Task<IReadOnlyList<Patient>> SearchAsync(string? query, Guid? patientId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Patient>>(patients.Values.Where(x => patientId is null || x.Id == patientId).Where(x => string.IsNullOrWhiteSpace(query) || x.Id.ToString().Contains(query!, StringComparison.OrdinalIgnoreCase) || $"{x.Name} {x.Surname}".Contains(query!, StringComparison.OrdinalIgnoreCase) || x.Documents.Any(d => d.DocumentNumber.Contains(query!, StringComparison.OrdinalIgnoreCase)) || x.Contacts.Any(c => c.Value.Contains(query!, StringComparison.OrdinalIgnoreCase))).ToArray());

    Task<Agenda?> IRepository<Agenda>.GetByIdAsync(object id, CancellationToken ct) => Task.FromResult(agendas.TryGetValue((Guid)id, out var value) ? value : null);
    public Task AddAsync(Agenda entity, CancellationToken ct = default) { agendas[entity.Id] = entity; return Task.CompletedTask; }
    public void Update(Agenda entity) => agendas[entity.Id] = entity;
    public void Remove(Agenda entity) => agendas.TryRemove(entity.Id, out _);
    public Task<IReadOnlyList<DateTime>> GetAvailableSlotsAsync(Guid id, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<DateTime>>(Array.Empty<DateTime>());
    Task<Turno?> IRepository<Turno>.GetByIdAsync(object id, CancellationToken ct) => Task.FromResult(turns.TryGetValue((Guid)id, out var value) ? value : null);
    public Task AddAsync(Turno entity, CancellationToken ct = default) { turns[entity.Id] = entity; bookingLock.Release(); return Task.CompletedTask; }
    public void Update(Turno entity) => turns[entity.Id] = entity;
    public void Remove(Turno entity) => turns.TryRemove(entity.Id, out _);
    public async Task<Turno?> FindSlotAsync(Guid agendaId, DateTime start, CancellationToken ct = default) { await bookingLock.WaitAsync(ct); var found = turns.Values.FirstOrDefault(x => x.AgendaId == agendaId && x.FechaHora == start && x.Estado is not "Cancelado" and not "NoAsistio"); if (found is not null) bookingLock.Release(); return found; }

    Task<Episode?> IRepository<Episode>.GetByIdAsync(object id, CancellationToken ct) => Task.FromResult(episodes.TryGetValue((Guid)id, out var value) ? value : null);
    public Task AddAsync(Episode entity, CancellationToken ct = default) { episodes[entity.Id] = entity; return Task.CompletedTask; }
    public void Update(Episode entity) => episodes[entity.Id] = entity;
    public void Remove(Episode entity) => episodes.TryRemove(entity.Id, out _);
    public Task<IReadOnlyList<Encounter>> GetEncountersAsync(Guid id, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Encounter>>(episodes.TryGetValue(id, out var e) ? e.Encounters.ToArray() : Array.Empty<Encounter>());
    public Task<IReadOnlyList<Order>> GetOrdersAsync(Guid id, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Order>>(episodes.TryGetValue(id, out var e) ? e.Orders.ToArray() : Array.Empty<Order>());
    public Task<bool> HasIncompleteRequiredOrdersAsync(Guid id, CancellationToken ct = default) => Task.FromResult(false);

    public Task<Study?> GetStudyAsync(Guid id, CancellationToken ct = default) => Task.FromResult(studies.TryGetValue(id, out var value) ? value : null);
    public Task<IReadOnlyList<Study>> GetStudiesAsync(string? search, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Study>>(studies.Values.Where(x => string.IsNullOrWhiteSpace(search) || x.Name.Contains(search!, StringComparison.OrdinalIgnoreCase) || x.Code.Contains(search!, StringComparison.OrdinalIgnoreCase)).ToArray());
    Task<DiagnosticOrder?> IRepository<DiagnosticOrder>.GetByIdAsync(object id, CancellationToken ct) => Task.FromResult(orders.TryGetValue((Guid)id, out var value) ? value : null);
    public Task AddAsync(DiagnosticOrder entity, CancellationToken ct = default) { orders[entity.Id] = entity; return Task.CompletedTask; }
    public void Update(DiagnosticOrder entity) => orders[entity.Id] = entity;
    public void Remove(DiagnosticOrder entity) => orders.TryRemove(entity.Id, out _);
    public Task<DiagnosticResult?> GetResultAsync(Guid id, CancellationToken ct = default) => Task.FromResult(results.TryGetValue(id, out var value) ? value : null);
    public Task<IReadOnlyList<DiagnosticOrder>> GetPendingOrdersAsync(Guid? patientId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<DiagnosticOrder>>(orders.Values.Where(x => patientId is null || x.PatientId == patientId).ToArray());
    public Task AddResultAsync(DiagnosticResult entity, CancellationToken ct = default) { results[entity.Id] = entity; return Task.CompletedTask; }
    public void UpdateResult(DiagnosticResult entity) => results[entity.Id] = entity;

    Task<User?> IRepository<User>.GetByIdAsync(object id, CancellationToken ct) => Task.FromResult(users.Values.FirstOrDefault(x => x.Id == (Guid)id));
    public Task AddAsync(User entity, CancellationToken ct = default) { users[entity.Username] = entity; return Task.CompletedTask; }
    public void Update(User entity) => users[entity.Username] = entity;
    public void Remove(User entity) => users.TryRemove(entity.Username, out _);
    public Task<User?> FindByUsernameAsync(string username, CancellationToken ct = default) => Task.FromResult(users.TryGetValue(username, out var value) ? value : null);
    public Task<IReadOnlyList<string>> GetPermissionsAsync(Guid userId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<string>>(new[] { "*" });
    public void UpdateUser(User user) => users[user.Username] = user;
    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}

public sealed class ApiCredentialService(IPasswordHasher<User> hasher) : IUserCredentialService
{
    public bool Verify(User user, string password) => hasher.VerifyHashedPassword(user, user.PasswordHash, password) == PasswordVerificationResult.Success;
    public Task<bool> ResetAsync(User user, string token, string newPassword, CancellationToken ct = default) => Task.FromResult(false);
}

public sealed class ApiTokenService(IOptions<ApiJwtOptions> options) : IAuthTokenService
{
    private readonly ApiJwtOptions settings = options.Value;
    public AuthResponse Issue(User user)
    {
        var refresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var session = Guid.NewGuid();
        var expiry = DateTime.UtcNow.AddMinutes(15);
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), new Claim(ClaimTypes.Name, user.Username), new Claim(ClaimTypes.Role, user.UserRoles.FirstOrDefault()?.RoleId == Guid.Empty ? "Staff" : user.Username.StartsWith("admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : "Receptionist") };
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(settings.Issuer, settings.Audience, claims, expires: expiry, signingCredentials: credentials);
        return new AuthResponse(user.Id, user.Username, new JwtSecurityTokenHandler().WriteToken(token), refresh, expiry, session);
    }
    public Task<AuthResponse?> RotateAsync(Guid sessionId, string refreshToken, CancellationToken ct = default) => Task.FromResult<AuthResponse?>(null);
    public Task RevokeAsync(Guid sessionId, CancellationToken ct = default) => Task.CompletedTask;
}

public sealed class RequestAuthorization(IHttpContextAccessor accessor) : IAuthorizationService
{
    public Task<bool> AuthorizeAsync(string permission, CancellationToken cancellationToken = default) => Task.FromResult(accessor.HttpContext?.User.Identity?.IsAuthenticated == true);
}
