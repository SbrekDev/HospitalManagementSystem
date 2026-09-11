using Microsoft.EntityFrameworkCore;
using SanatorioHMS.Domain.Auth.Entities;
using SanatorioHMS.Domain.ClinicalCare.Entities;
using SanatorioHMS.Domain.Diagnostics.Entities;
using SanatorioHMS.Domain.PatientRegistry.Entities;
using SanatorioHMS.Domain.Scheduling.Entities;

namespace SanatorioHMS.Infrastructure.Data;

public sealed class PatientRegistryDbContext(DbContextOptions<PatientRegistryDbContext> options) : DbContext(options)
{ protected override void OnModelCreating(ModelBuilder b) { b.HasDefaultSchema("patient"); b.ApplyConfiguration(new PatientConfiguration()); b.ApplyConfiguration(new PatientDocumentConfiguration()); b.ApplyConfiguration(new PatientContactConfiguration()); b.ApplyConfiguration(new HealthCoverageConfiguration()); b.ApplyConfiguration(new GuardianConfiguration()); } }
public sealed class SchedulingDbContext(DbContextOptions<SchedulingDbContext> options) : DbContext(options)
{ protected override void OnModelCreating(ModelBuilder b) { b.HasDefaultSchema("sched"); b.ApplyConfiguration(new ProfessionalConfiguration()); b.ApplyConfiguration(new SpecialtyConfiguration()); b.ApplyConfiguration(new ProfessionalSpecialtyConfiguration()); b.ApplyConfiguration(new RoomConfiguration()); b.ApplyConfiguration(new AgendaConfiguration()); b.ApplyConfiguration(new SchedulingConfiguration()); } }
public sealed class ClinicalCareDbContext(DbContextOptions<ClinicalCareDbContext> options) : DbContext(options)
{ protected override void OnModelCreating(ModelBuilder b) { b.HasDefaultSchema("clinical"); b.ApplyConfiguration(new EpisodeConfiguration()); b.ApplyConfiguration(new EncounterConfiguration()); b.ApplyConfiguration(new ClinicalNoteConfiguration()); b.ApplyConfiguration(new OrderConfiguration()); b.ApplyConfiguration(new OrderItemConfiguration()); } }
public sealed class DiagnosticsDbContext(DbContextOptions<DiagnosticsDbContext> options) : DbContext(options)
{ protected override void OnModelCreating(ModelBuilder b) { b.HasDefaultSchema("diag"); b.ApplyConfiguration(new StudyConfiguration()); b.ApplyConfiguration(new DiagnosticOrderConfiguration()); b.ApplyConfiguration(new DiagnosticResultConfiguration()); } }
public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{ public DbSet<User> Users => Set<User>(); public DbSet<Role> Roles => Set<Role>(); public DbSet<Permission> Permissions => Set<Permission>(); public DbSet<Session> Sessions => Set<Session>(); public DbSet<AuditLog> AuditLogs => Set<AuditLog>(); protected override void OnModelCreating(ModelBuilder b) { b.HasDefaultSchema("auth"); b.ApplyConfiguration(new UserConfiguration()); b.ApplyConfiguration(new RoleConfiguration()); b.ApplyConfiguration(new PermissionConfiguration()); b.ApplyConfiguration(new SessionConfiguration()); b.ApplyConfiguration(new AuditLogConfiguration()); b.Entity<UserRole>().HasKey(x => new { x.UserId, x.RoleId }); b.Entity<RolePermission>().HasKey(x => new { x.RoleId, x.PermissionId }); } }
