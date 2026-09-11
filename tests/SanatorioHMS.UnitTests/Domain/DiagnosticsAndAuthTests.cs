using SanatorioHMS.Domain.Auth.Entities;
using SanatorioHMS.Domain.Diagnostics.Entities;

namespace SanatorioHMS.UnitTests.Domain;

public sealed class DiagnosticsAndAuthTests
{
    [Fact]
    public void DeniedAuthorizationBlocksFulfillment()
    {
        var study = Study.Create("24323-8", "Complete blood count", StudyModality.Laboratory);
        var order = DiagnosticOrder.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), study, AuthorizationStatus.Denied);
        Assert.Throws<InvalidOperationException>(order.Fulfill);
        Assert.False(order.Fulfilled);
    }

    [Fact]
    public void ResultCorrectionNeverOverwritesOriginal()
    {
        var original = DiagnosticResult.Register(Guid.NewGuid(), "10", Guid.NewGuid(), "mg/dL");
        var correction = original.Correct("12", Guid.NewGuid());
        Assert.Equal("10", original.Value);
        Assert.Equal("12", correction.Value);
        Assert.Equal(original.Id, correction.CorrectionOfId);
    }

    [Fact]
    public void AuditHashChainLinksPreviousEntry()
    {
        var first = AuditLog.Record(Guid.NewGuid(), "Login", "user", "Success");
        var second = AuditLog.Record(Guid.NewGuid(), "Read", "patient", "Success", first.Hash);
        Assert.True(second.LinksTo(first));
        Assert.NotEqual(first.Hash, second.Hash);
    }
}
