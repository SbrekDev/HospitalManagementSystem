using MediatR;
using SanatorioHMS.Domain.Core;

namespace SanatorioHMS.Application.Core;

public interface IAuthorizationService
{
    Task<bool> AuthorizeAsync(string permission, CancellationToken cancellationToken = default);
}

public interface IAuditWriter
{
    Task WriteAsync(string action, string target, string outcome, CancellationToken cancellationToken = default);
}

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<FluentValidation.IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var failures = (await Task.WhenAll(validators.Select(v => v.ValidateAsync(request, cancellationToken))))
            .SelectMany(x => x.Errors).ToArray();
        if (failures.Length > 0 && typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            return FailureResponse(string.Join("; ", failures.Select(x => x.ErrorMessage)));
        return await next();
    }

    private static TResponse FailureResponse(string error)
    {
        var valueType = typeof(TResponse).GetGenericArguments()[0];
        var resultType = typeof(Result<>).MakeGenericType(valueType);
        return (TResponse)resultType.GetMethod(nameof(Result<object>.Failure))!.Invoke(null, [error])!;
    }
}

public sealed class AuthorizationBehavior<TRequest, TResponse>(IAuthorizationService authorization)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is IRequirePermission secured && !await authorization.AuthorizeAsync(secured.Permission, cancellationToken))
        {
            if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
                return FailureResponse("Unauthorized.");
            throw new UnauthorizedAccessException();
        }
        return await next();
    }

    private static TResponse FailureResponse(string error)
    {
        var valueType = typeof(TResponse).GetGenericArguments()[0];
        var resultType = typeof(Result<>).MakeGenericType(valueType);
        return (TResponse)resultType.GetMethod(nameof(Result<object>.Failure))!.Invoke(null, [error])!;
    }
}

public sealed class AuditBehavior<TRequest, TResponse>(IAuditWriter audit)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try { var result = await next(); await audit.WriteAsync(typeof(TRequest).Name, typeof(TRequest).Name, "Success", cancellationToken); return result; }
        catch { await audit.WriteAsync(typeof(TRequest).Name, typeof(TRequest).Name, "Failure", cancellationToken); throw; }
    }
}

#pragma warning disable CA1711 // Permission is the domain term used by RBAC contracts.
public interface IRequirePermission { string Permission { get; } }
#pragma warning restore CA1711

public class Class1
{

}
