using FluentValidation;
using MediatR;
using SanatorioHMS.Application.Core;
using SanatorioHMS.Domain.Auth.Entities;
using SanatorioHMS.Domain.Core;

namespace SanatorioHMS.Application.Auth;

public sealed record AuthResponse(Guid UserId, string Username, string AccessToken, string RefreshToken, DateTime ExpiresAt, Guid SessionId);
public sealed record UserResponse(Guid Id, string Username, bool IsActive);
public sealed record LoginRequest(string Username, string Password);
public sealed record RefreshTokenRequest(Guid SessionId, string RefreshToken);
public sealed record ResetPasswordRequest(Guid UserId, string ResetToken, string NewPassword);
public sealed record Login(LoginRequest Data) : IRequest<Result<AuthResponse>>;
public sealed record RefreshToken(RefreshTokenRequest Data) : IRequest<Result<AuthResponse>>;
public sealed record Logout(Guid SessionId) : IRequest<Result<bool>>, IRequirePermission { public string Permission => "Auth.Logout"; }
public sealed record ResetPassword(ResetPasswordRequest Data) : IRequest<Result<bool>>;
public sealed record GetCurrentUser(Guid UserId) : IRequest<Result<UserResponse>>, IRequirePermission { public string Permission => "Auth.Me"; }
public sealed record GetUserPermissions(Guid UserId) : IRequest<Result<IReadOnlyList<string>>>, IRequirePermission { public string Permission => "Auth.Permissions.Read"; }

public interface IAuthTokenService
{
    AuthResponse Issue(User user);
    Task<AuthResponse?> RotateAsync(Guid sessionId, string refreshToken, CancellationToken ct = default);
    Task RevokeAsync(Guid sessionId, CancellationToken ct = default);
}
public interface IUserCredentialService
{
    bool Verify(User user, string password);
    Task<bool> ResetAsync(User user, string token, string newPassword, CancellationToken ct = default);
}
public interface IUserAuthRepository : IRepository<User>
{
    Task<User?> FindByUsernameAsync(string username, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetPermissionsAsync(Guid userId, CancellationToken ct = default);
    void UpdateUser(User user);
}
public sealed class LoginValidator : AbstractValidator<Login>
{ public LoginValidator() { RuleFor(x => x.Data.Username).NotEmpty(); RuleFor(x => x.Data.Password).NotEmpty(); } }
public sealed class RefreshTokenValidator : AbstractValidator<RefreshToken>
{ public RefreshTokenValidator() { RuleFor(x => x.Data.SessionId).NotEmpty(); RuleFor(x => x.Data.RefreshToken).NotEmpty(); } }
public sealed class ResetPasswordValidator : AbstractValidator<ResetPassword>
{ public ResetPasswordValidator() { RuleFor(x => x.Data.UserId).NotEmpty(); RuleFor(x => x.Data.ResetToken).NotEmpty(); RuleFor(x => x.Data.NewPassword).MinimumLength(12).Matches("[A-Z]").Matches("[a-z]").Matches("[0-9]"); } }

public sealed class LoginHandler(IUserAuthRepository repository, IUserCredentialService credentials, IAuthTokenService tokens) : IRequestHandler<Login, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(Login r, CancellationToken ct)
    { var user = await repository.FindByUsernameAsync(r.Data.Username, ct); if (user is null || !user.IsActive || !credentials.Verify(user, r.Data.Password)) return Result<AuthResponse>.Failure("Invalid credentials."); return Result<AuthResponse>.Success(tokens.Issue(user)); }
}
public sealed class RefreshTokenHandler(IAuthTokenService tokens) : IRequestHandler<RefreshToken, Result<AuthResponse>>
{ public async Task<Result<AuthResponse>> Handle(RefreshToken r, CancellationToken ct) => (await tokens.RotateAsync(r.Data.SessionId, r.Data.RefreshToken, ct)) is { } response ? Result<AuthResponse>.Success(response) : Result<AuthResponse>.Failure("Invalid refresh token."); }
public sealed class LogoutHandler(IAuthTokenService tokens) : IRequestHandler<Logout, Result<bool>>
{ public async Task<Result<bool>> Handle(Logout r, CancellationToken ct) { await tokens.RevokeAsync(r.SessionId, ct); return Result<bool>.Success(true); } }
public sealed class ResetPasswordHandler(IUserAuthRepository repository, IUserCredentialService credentials) : IRequestHandler<ResetPassword, Result<bool>>
{ public async Task<Result<bool>> Handle(ResetPassword r, CancellationToken ct) { var user = await repository.GetByIdAsync(r.Data.UserId, ct); if (user is null || !await credentials.ResetAsync(user, r.Data.ResetToken, r.Data.NewPassword, ct)) return Result<bool>.Failure("Reset request is invalid."); user.Sessions.ToList().ForEach(x => x.Revoke()); repository.UpdateUser(user); return Result<bool>.Success(true); } }
public sealed class GetCurrentUserHandler(IUserAuthRepository repository) : IRequestHandler<GetCurrentUser, Result<UserResponse>>
{ public async Task<Result<UserResponse>> Handle(GetCurrentUser r, CancellationToken ct) { var user = await repository.GetByIdAsync(r.UserId, ct); return user is null || !user.IsActive ? Result<UserResponse>.Failure("Unauthorized.") : Result<UserResponse>.Success(new UserResponse(user.Id, user.Username, user.IsActive)); } }
public sealed class GetUserPermissionsHandler(IUserAuthRepository repository) : IRequestHandler<GetUserPermissions, Result<IReadOnlyList<string>>>
{ public async Task<Result<IReadOnlyList<string>>> Handle(GetUserPermissions r, CancellationToken ct) => Result<IReadOnlyList<string>>.Success(await repository.GetPermissionsAsync(r.UserId, ct)); }
