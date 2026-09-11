using FluentValidation;
using MediatR;
using Moq;
using SanatorioHMS.Application.Core;
using SanatorioHMS.Domain.Core;

namespace SanatorioHMS.Application.Tests;

public sealed class PipelineCoverageTests
{
    private sealed record SecuredRequest(string Value) : IRequest<Result<string>>, IRequirePermission
    {
        public string Permission => "Test.Read";
    }

    [Fact]
    public async Task ValidationBehaviorReturnsFailureBeforeHandlerAndPassesValidRequests()
    {
        var validator = new InlineValidator<SecuredRequest>();
        validator.RuleFor(x => x.Value).NotEmpty();
        var behavior = new ValidationBehavior<SecuredRequest, Result<string>>([validator]);
        var request = new SecuredRequest("");
        var called = false;
        var failed = await behavior.Handle(request, () => { called = true; return Task.FromResult(Result<string>.Success("ok")); }, default);
        Assert.False(failed.IsSuccess);
        Assert.False(called);

        var success = await behavior.Handle(new SecuredRequest("value"), () => Task.FromResult(Result<string>.Success("ok")), default);
        Assert.True(success.IsSuccess);
    }

    [Fact]
    public async Task AuthorizationBehaviorCoversDeniedAndAllowedRequests()
    {
        var authorization = new Mock<IAuthorizationService>();
        var behavior = new AuthorizationBehavior<SecuredRequest, Result<string>>(authorization.Object);
        authorization.Setup(x => x.AuthorizeAsync("Test.Read", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var denied = await behavior.Handle(new SecuredRequest("x"), () => Task.FromResult(Result<string>.Success("ok")), default);
        Assert.False(denied.IsSuccess);
        authorization.Setup(x => x.AuthorizeAsync("Test.Read", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        Assert.True((await behavior.Handle(new SecuredRequest("x"), () => Task.FromResult(Result<string>.Success("ok")), default)).IsSuccess);
    }

    [Fact]
    public async Task AuditBehaviorWritesSuccessAndFailureOutcomes()
    {
        var audit = new Mock<IAuditWriter>();
        var behavior = new AuditBehavior<SecuredRequest, Result<string>>(audit.Object);
        await behavior.Handle(new SecuredRequest("x"), () => Task.FromResult(Result<string>.Success("ok")), default);
        await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.Handle(new SecuredRequest("x"), () => throw new InvalidOperationException("boom"), default));
        audit.Verify(x => x.WriteAsync(nameof(SecuredRequest), nameof(SecuredRequest), "Success", It.IsAny<CancellationToken>()), Times.Once);
        audit.Verify(x => x.WriteAsync(nameof(SecuredRequest), nameof(SecuredRequest), "Failure", It.IsAny<CancellationToken>()), Times.Once);
    }
}
