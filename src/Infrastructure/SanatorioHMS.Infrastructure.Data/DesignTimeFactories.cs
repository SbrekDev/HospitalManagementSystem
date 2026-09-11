using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SanatorioHMS.Infrastructure.Data;

public sealed class PatientRegistryDbContextFactory : IDesignTimeDbContextFactory<PatientRegistryDbContext>
{
    public PatientRegistryDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PatientRegistryDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=SanatorioHMS;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new PatientRegistryDbContext(options);
    }
}

public sealed class SchedulingDbContextFactory : IDesignTimeDbContextFactory<SchedulingDbContext>
{
    public SchedulingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SchedulingDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=SanatorioHMS;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new SchedulingDbContext(options);
    }
}

public sealed class ClinicalCareDbContextFactory : IDesignTimeDbContextFactory<ClinicalCareDbContext>
{
    public ClinicalCareDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ClinicalCareDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=SanatorioHMS;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new ClinicalCareDbContext(options);
    }
}

public sealed class DiagnosticsDbContextFactory : IDesignTimeDbContextFactory<DiagnosticsDbContext>
{
    public DiagnosticsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<DiagnosticsDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=SanatorioHMS;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new DiagnosticsDbContext(options);
    }
}

public sealed class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=SanatorioHMS;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new AuthDbContext(options);
    }
}
