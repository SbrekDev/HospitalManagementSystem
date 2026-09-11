using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SanatorioHMS.Application.Auth;
using SanatorioHMS.Application.ClinicalCare;
using SanatorioHMS.Application.Diagnostics;
using SanatorioHMS.Application.PatientRegistry;
using SanatorioHMS.Application.Scheduling;
using SanatorioHMS.Domain.Core;
using SanatorioHMS.Domain.Auth.Entities;
using SanatorioHMS.Domain.ClinicalCare.Entities;
using SanatorioHMS.Domain.Diagnostics.Entities;
using SanatorioHMS.Domain.PatientRegistry.Entities;
using SanatorioHMS.Domain.Scheduling.Entities;
using SanatorioHMS.Infrastructure.Core;

namespace SanatorioHMS.Infrastructure.Data;

public sealed class PatientRepository(PatientRegistryDbContext db) : Repository<Patient>(db), IPatientRegistryRepository
{
    public async Task<bool> HasDocumentAsync(DocumentType type, string number, CancellationToken cancellationToken = default) =>
        await db.Set<Patient>().Include(p => p.Documents).AnyAsync(p => p.Documents.Any(d => d.DocumentType == type && d.DocumentNumber == number), cancellationToken);

    public async Task<IReadOnlyList<Patient>> SearchAsync(string? query, Guid? patientId, CancellationToken cancellationToken = default)
    {
        var set = db.Set<Patient>().Include(p => p.Documents).Include(p => p.Contacts).AsQueryable();
        if (patientId.HasValue) set = set.Where(p => p.Id == patientId);
        if (!string.IsNullOrWhiteSpace(query))
            set = set.Where(p => p.Id.ToString().Contains(query) || (p.Name + " " + p.Surname).Contains(query) || p.Documents.Any(d => d.DocumentNumber.Contains(query)) || p.Contacts.Any(c => c.Value.Contains(query)));
        return await set.ToListAsync(cancellationToken);
    }
}
public sealed class SchedulingRepository(SchedulingDbContext db) : Repository<Agenda>(db), ISchedulingRepository
{
    public async Task<IReadOnlyList<Agenda>> GetAgendasAsync(CancellationToken cancellationToken = default) => await db.Set<Agenda>().AsNoTracking().ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<DateTime>> GetAvailableSlotsAsync(Guid agendaId, CancellationToken cancellationToken = default)
    {
        var agenda = await db.Set<Agenda>().FirstOrDefaultAsync(x => x.Id == agendaId, cancellationToken);
        if (agenda is null || !agenda.Activo) return [];

        var booked = await db.Set<Turno>()
            .Where(x => x.AgendaId == agendaId && x.Estado != "Cancelado" && x.Estado != "NoAsistio")
            .Select(x => x.FechaHora)
            .ToListAsync(cancellationToken);
        var slots = new List<DateTime>();
        for (var start = agenda.Fecha.ToDateTime(agenda.HoraInicio); start.AddMinutes(agenda.DuracionTurnoMinutos) <= agenda.Fecha.ToDateTime(agenda.HoraFin); start = start.AddMinutes(agenda.DuracionTurnoMinutos))
        {
            if (!booked.Contains(start)) slots.Add(start);
        }
        return slots;
    }
}
public sealed class ClinicalCareRepository(ClinicalCareDbContext db) : Repository<Episode>(db), IClinicalCareRepository
{
    public async Task<IReadOnlyList<Episode>> GetEpisodesAsync(CancellationToken cancellationToken = default) => await db.Set<Episode>().AsNoTracking().ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<Encounter>> GetEncountersAsync(Guid episodeId, CancellationToken cancellationToken = default) =>
        await db.Set<Encounter>().Where(x => x.EpisodioId == episodeId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Order>> GetOrdersAsync(Guid episodeId, CancellationToken cancellationToken = default) =>
        await db.Set<Order>().Where(x => x.EpisodioId == episodeId).Include(x => x.Items).ToListAsync(cancellationToken);

    public Task<bool> HasIncompleteRequiredOrdersAsync(Guid episodeId, CancellationToken cancellationToken = default) =>
        db.Set<Order>().AnyAsync(x => x.EpisodioId == episodeId && x.Estado != "Completada" && x.Estado != "Cancelada", cancellationToken);
}

public sealed class DiagnosticsRepository(DiagnosticsDbContext db) : Repository<DiagnosticOrder>(db), IDiagnosticsRepository
{
    public Task<Study?> GetStudyAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Set<Study>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Study>> GetStudiesAsync(string? search, CancellationToken cancellationToken = default)
    {
        var query = db.Set<Study>().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Code.Contains(search) || x.Name.Contains(search));
        return await query.ToListAsync(cancellationToken);
    }

    public Task<DiagnosticResult?> GetResultAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Set<DiagnosticResult>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<DiagnosticOrder>> GetPendingOrdersAsync(Guid? patientId, CancellationToken cancellationToken = default)
    {
        var query = db.Set<DiagnosticOrder>().Where(x => !x.Fulfilled).AsQueryable();
        if (patientId.HasValue) query = query.Where(x => x.PatientId == patientId);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task AddResultAsync(DiagnosticResult result, CancellationToken cancellationToken = default) =>
        await db.Set<DiagnosticResult>().AddAsync(result, cancellationToken);

    public void UpdateResult(DiagnosticResult result) => db.Set<DiagnosticResult>().Update(result);
}
public sealed class AuthRepository(AuthDbContext db) : Repository<User>(db), IUserAuthRepository
{
    public async Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        await db.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

    public async Task<IReadOnlyList<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var userExists = await db.Users.AnyAsync(u => u.Id == userId, cancellationToken);
        if (!userExists) return [];
        return await (from userRole in db.Set<UserRole>()
                      join rolePermission in db.Set<RolePermission>() on userRole.RoleId equals rolePermission.RoleId
                      join permission in db.Permissions on rolePermission.PermissionId equals permission.Id
                      where userRole.UserId == userId
                      select permission.Code).ToListAsync(cancellationToken);
    }

    public void UpdateUser(User user) => db.Users.Update(user);
}
public sealed class TurnRepository(SchedulingDbContext db) : Repository<Turno>(db), ITurnRepository
{
    public async Task<IReadOnlyList<Turno>> GetTurnsAsync(CancellationToken cancellationToken = default) => await db.Set<Turno>().AsNoTracking().ToListAsync(cancellationToken);
    public Task<Turno?> FindSlotAsync(Guid agendaId, DateTime start, CancellationToken cancellationToken = default) =>
        db.Set<Turno>().FromSqlInterpolated($"SELECT * FROM [sched].[Turns] WITH (UPDLOCK, ROWLOCK) WHERE [AgendaId] = {agendaId} AND [FechaHora] = {start}").SingleOrDefaultAsync(cancellationToken);
}
