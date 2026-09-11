using Microsoft.EntityFrameworkCore;
using SanatorioHMS.Infrastructure.Data;

const string connectionString =
    "Server=(localdb)\\MSSQLLocalDB;Database=SanatorioHMS;Trusted_Connection=True;TrustServerCertificate=True";

var options = new DbContextOptionsBuilder<AuthDbContext>()
    .UseSqlServer(connectionString)
    .Options;

await using var db = new AuthDbContext(options);
await db.Database.MigrateAsync();
await SeedData.SeedAsync(db);

var schedulingOptions = new DbContextOptionsBuilder<SchedulingDbContext>().UseSqlServer(connectionString).Options;
await using var schedulingDb = new SchedulingDbContext(schedulingOptions);
await schedulingDb.Database.MigrateAsync();
await SeedData.SeedCatalogsAsync(schedulingDb);

var diagnosticsOptions = new DbContextOptionsBuilder<DiagnosticsDbContext>().UseSqlServer(connectionString).Options;
await using var diagnosticsDb = new DiagnosticsDbContext(diagnosticsOptions);
await diagnosticsDb.Database.MigrateAsync();
await SeedData.SeedStudiesAsync(diagnosticsDb);

Console.WriteLine("Seed completed. Credentials: admin / Admin123!");
