using Microsoft.EntityFrameworkCore;
using SanatorioHMS.Domain.Core;
using SanatorioHMS.Domain.Auth.Entities;
using SanatorioHMS.Domain.ClinicalCare.Entities;
using SanatorioHMS.Domain.Diagnostics.Entities;
using SanatorioHMS.Domain.PatientRegistry.Entities;
using SanatorioHMS.Domain.Scheduling.Entities;
using SanatorioHMS.Infrastructure.Core;

namespace SanatorioHMS.Infrastructure.Data;

public interface IPatientRepository : IRepository<Patient> { }
public interface ISchedulingRepository : IRepository<Agenda> { }
public interface IClinicalCareRepository : IRepository<Episode> { }
public interface IDiagnosticsRepository : IRepository<DiagnosticOrder> { }
public interface IAuthRepository : IRepository<User> { }
public interface ITurnRepository : IRepository<Turno> { Task<Turno?> FindSlotAsync(Guid agendaId, DateTime start, CancellationToken cancellationToken = default); }

public sealed class PatientRepository(PatientRegistryDbContext db) : Repository<Patient>(db), IPatientRepository;
public sealed class SchedulingRepository(SchedulingDbContext db) : Repository<Agenda>(db), ISchedulingRepository;
public sealed class ClinicalCareRepository(ClinicalCareDbContext db) : Repository<Episode>(db), IClinicalCareRepository;
public sealed class DiagnosticsRepository(DiagnosticsDbContext db) : Repository<DiagnosticOrder>(db), IDiagnosticsRepository;
public sealed class AuthRepository(AuthDbContext db) : Repository<User>(db), IAuthRepository;
public sealed class TurnRepository(SchedulingDbContext db) : Repository<Turno>(db), ITurnRepository
{
    public Task<Turno?> FindSlotAsync(Guid agendaId, DateTime start, CancellationToken cancellationToken = default) =>
        db.Set<Turno>().FromSqlInterpolated($"SELECT * FROM [sched].[Turns] WITH (UPDLOCK, ROWLOCK) WHERE [AgendaId] = {agendaId} AND [FechaHora] = {start}").SingleOrDefaultAsync(cancellationToken);
}
