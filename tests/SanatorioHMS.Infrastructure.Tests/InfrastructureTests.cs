using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SanatorioHMS.Domain.Auth.Entities;
using SanatorioHMS.Infrastructure.Data;
using Xunit;

namespace SanatorioHMS.Infrastructure.Tests;

public sealed class InfrastructureTests
{
    [Fact]
    public void PasswordHasherDoesNotStorePlaintext()
    {
        var user = new User(Guid.NewGuid(), "operator");
        var hash = new PasswordHasher<User>().HashPassword(user, "Correct horse battery staple");
        Assert.NotEqual("Correct horse battery staple", hash);
        Assert.StartsWith("AQAAAA", hash);
    }

    [Fact]
    public void AuthModelContainsHashChainedAuditAndSessionTables()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using var db = new AuthDbContext(options);
        Assert.NotNull(db.Model.FindEntityType(typeof(AuditLog)));
        Assert.NotNull(db.Model.FindEntityType(typeof(Session)));
        Assert.Equal("auth", db.Model.FindEntityType(typeof(AuditLog))!.GetSchema());
    }
}
