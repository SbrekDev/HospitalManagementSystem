using SanatorioHMS.Domain.Auth.Entities;
using SanatorioHMS.Domain.Diagnostics.Entities;

namespace SanatorioHMS.UnitTests.Domain;

public sealed class DiagnosticsAndAuditTests
{
    [Theory]
    [InlineData(AuthorizationStatus.Pending)]
    [InlineData(AuthorizationStatus.Denied)]
    public void AuthorizationGateBlocksFulfillment(AuthorizationStatus status)
    {
        var study = Study.Create("LOINC-1", "Hemograma", StudyModality.Laboratory);
        var order = DiagnosticOrder.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), study, status);
        Assert.Throws<InvalidOperationException>(order.Fulfill);
    }

    [Fact]
    public void ApprovedAuthorizationAllowsFulfillmentOnce()
    {
        var study = Study.Create("LOINC-2", "Radiografía", StudyModality.Imaging);
        var order = DiagnosticOrder.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), study, AuthorizationStatus.Pending);
        order.ApproveAuthorization();
        order.Fulfill();
        Assert.Throws<InvalidOperationException>(order.Fulfill);
    }

    [Fact]
    public void AuditEntriesFormHashChain()
    {
        var first = AuditLog.Record(Guid.NewGuid(), "Create", "Episode", "Success");
        var second = AuditLog.Record(Guid.NewGuid(), "Close", "Episode", "Success", first.Hash);
        Assert.True(second.LinksTo(first));
        Assert.NotEqual(first.Hash, second.Hash);
    }
}
