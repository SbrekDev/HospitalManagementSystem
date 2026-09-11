using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SanatorioHMS.Application.Auth;
using SanatorioHMS.Application.ClinicalCare;
using SanatorioHMS.Application.Diagnostics;
using SanatorioHMS.Application.PatientRegistry;
using SanatorioHMS.Application.Scheduling;
using SanatorioHMS.Infrastructure.Core;

namespace SanatorioHMS.Infrastructure.Data;

public static class InfrastructureRegistration
{
    public static IServiceCollection AddHmsData(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<PatientRegistryDbContext>(o => o.UseSqlServer(connectionString));
        services.AddDbContext<SchedulingDbContext>(o => o.UseSqlServer(connectionString));
        services.AddDbContext<ClinicalCareDbContext>(o => o.UseSqlServer(connectionString));
        services.AddDbContext<DiagnosticsDbContext>(o => o.UseSqlServer(connectionString));
        services.AddDbContext<AuthDbContext>(o => o.UseSqlServer(connectionString));
        services.AddScoped<IPatientRegistryRepository, PatientRepository>();
        services.AddScoped<ISchedulingRepository, SchedulingRepository>();
        services.AddScoped<ITurnRepository, TurnRepository>();
        services.AddScoped<IClinicalCareRepository, ClinicalCareRepository>();
        services.AddScoped<IDiagnosticsRepository, DiagnosticsRepository>();
        services.AddScoped<IUserAuthRepository, AuthRepository>();
        services.AddScoped<SanatorioHMS.Application.Core.IUnitOfWork, RequestUnitOfWork>();
        return services;
    }
}

public sealed class RequestUnitOfWork(
    PatientRegistryDbContext patient,
    SchedulingDbContext scheduling,
    ClinicalCareDbContext clinical,
    DiagnosticsDbContext diagnostics,
    AuthDbContext auth) : SanatorioHMS.Application.Core.IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var changes = 0;
        changes += await patient.SaveChangesAsync(cancellationToken);
        changes += await scheduling.SaveChangesAsync(cancellationToken);
        changes += await clinical.SaveChangesAsync(cancellationToken);
        changes += await diagnostics.SaveChangesAsync(cancellationToken);
        changes += await auth.SaveChangesAsync(cancellationToken);
        return changes;
    }
}
