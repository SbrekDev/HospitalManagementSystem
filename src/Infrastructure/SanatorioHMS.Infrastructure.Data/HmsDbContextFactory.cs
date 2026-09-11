using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SanatorioHMS.Infrastructure.Data;

public sealed class HmsDbContextFactory : IDesignTimeDbContextFactory<HmsDbContext>
{
    public HmsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<HmsDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=SanatorioHMS;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new HmsDbContext(options);
    }
}
