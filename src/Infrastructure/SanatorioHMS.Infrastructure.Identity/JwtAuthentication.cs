using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SanatorioHMS.Domain.Auth.Entities;
using SanatorioHMS.Infrastructure.Data;

namespace SanatorioHMS.Infrastructure.Identity;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = "SanatorioHMS";
    public string Audience { get; init; } = "SanatorioHMS.Api";
    public string SigningKey { get; init; } = "change-this-development-key-to-a-secret-of-at-least-32-chars";
    public int AccessMinutes { get; init; } = 15;
    public int RefreshDays { get; init; } = 7;
}

public sealed record TokenPair(string AccessToken, string RefreshToken, DateTime ExpiresAt, Guid SessionId);

public interface ITokenService
{
    TokenPair Issue(User user);
    Task<TokenPair?> RotateAsync(Guid sessionId, string refreshToken, CancellationToken cancellationToken = default);
    Task RevokeAsync(Guid sessionId, CancellationToken cancellationToken = default);
}

public sealed class JwtTokenService(AuthDbContext db, IOptions<JwtOptions> options) : ITokenService
{
    private readonly JwtOptions settings = options.Value;
    public TokenPair Issue(User user)
    {
        var refresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var expires = DateTime.UtcNow.AddDays(settings.RefreshDays);
        var session = new Session(Guid.NewGuid(), user.Id, expires, Hash(refresh));
        db.Sessions.Add(session);
        var accessExpiry = DateTime.UtcNow.AddMinutes(settings.AccessMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey));
        var token = new JwtSecurityToken(settings.Issuer, settings.Audience, [new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), new Claim(ClaimTypes.Name, user.Username)], expires: accessExpiry, signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new TokenPair(new JwtSecurityTokenHandler().WriteToken(token), refresh, accessExpiry, session.Id);
    }
    public async Task<TokenPair?> RotateAsync(Guid sessionId, string refreshToken, CancellationToken cancellationToken = default)
    {
        var session = await db.Sessions.SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
        if (session is null || !session.IsValid(DateTime.UtcNow) || session.RefreshTokenHash != Hash(refreshToken)) return null;
        session.Revoke();
        var user = await db.Users.FindAsync([session.UserId], cancellationToken);
        if (user is null || !user.IsActive) return null;
        var pair = Issue(user);
        await db.SaveChangesAsync(cancellationToken);
        return pair;
    }
    public async Task RevokeAsync(Guid sessionId, CancellationToken cancellationToken = default)
    { var session = await db.Sessions.FindAsync([sessionId], cancellationToken); if (session is not null) { session.Revoke(); await db.SaveChangesAsync(cancellationToken); } }
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}

public static class IdentityRegistration
{
    public static IServiceCollection AddHmsIdentity(this IServiceCollection services, Action<JwtOptions>? configure = null)
    {
        services.AddOptions<JwtOptions>();
        if (configure is not null) services.Configure(configure);
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddMediatR(c => { c.RegisterServicesFromAssembly(typeof(IdentityRegistration).Assembly); c.AddOpenBehavior(typeof(AuditBehavior<,>)); });
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>().Configure<IOptions<JwtOptions>>((o, configured) =>
        {
            var settings = configured.Value;
            o.TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = true, ValidIssuer = settings.Issuer, ValidateAudience = true, ValidAudience = settings.Audience, ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)), ValidateLifetime = true, ClockSkew = TimeSpan.Zero };
        });
        services.AddAuthorizationBuilder().AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
        return services;
    }
}

public sealed class AuditMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, AuthDbContext db)
    {
        await next(context);
        if (context.Response.StatusCode is >= 401 and <= 403)
        {
            var actor = Guid.TryParse(context.User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : (Guid?)null;
            db.AuditLogs.Add(AuditLog.Record(actor, "AuthenticationFailure", context.Request.Path, "Denied"));
            await db.SaveChangesAsync();
        }
    }
}

public sealed class AuditBehavior<TRequest, TResponse>(AuthDbContext db) : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try { var response = await next(); db.AuditLogs.Add(AuditLog.Record(null, typeof(TRequest).Name, typeof(TRequest).Name, "Success")); await db.SaveChangesAsync(cancellationToken); return response; }
        catch { db.AuditLogs.Add(AuditLog.Record(null, typeof(TRequest).Name, typeof(TRequest).Name, "Failure")); await db.SaveChangesAsync(cancellationToken); throw; }
    }
}
