using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SanatorioHMS.Api;
using SanatorioHMS.Application.Auth;
using SanatorioHMS.Application.ClinicalCare;
using SanatorioHMS.Application.Core;
using SanatorioHMS.Application.Diagnostics;
using SanatorioHMS.Application.PatientRegistry;
using SanatorioHMS.Application.Scheduling;
using SanatorioHMS.Domain.Auth.Entities;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<ApiJwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<InMemoryStore>();
builder.Services.AddScoped<IPatientRegistryRepository>(sp => sp.GetRequiredService<InMemoryStore>());
builder.Services.AddScoped<ISchedulingRepository>(sp => sp.GetRequiredService<InMemoryStore>());
builder.Services.AddScoped<ITurnRepository>(sp => sp.GetRequiredService<InMemoryStore>());
builder.Services.AddScoped<IClinicalCareRepository>(sp => sp.GetRequiredService<InMemoryStore>());
builder.Services.AddScoped<IDiagnosticsRepository>(sp => sp.GetRequiredService<InMemoryStore>());
builder.Services.AddScoped<IUserAuthRepository>(sp => sp.GetRequiredService<InMemoryStore>());
builder.Services.AddScoped<IUserCredentialService, ApiCredentialService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IAuthTokenService, ApiTokenService>();
builder.Services.AddScoped<SanatorioHMS.Application.Core.IAuthorizationService, RequestAuthorization>();
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(typeof(CreatePatient).Assembly, typeof(Login).Assembly, typeof(OpenEpisode).Assembly, typeof(GetStudies).Assembly, typeof(ReserveTurn).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(AuthorizationBehavior<,>));
});
var jwt = builder.Configuration.GetSection("Jwt").Get<ApiJwtOptions>() ?? new ApiJwtOptions();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = true, ValidIssuer = jwt.Issuer, ValidateAudience = true, ValidAudience = jwt.Audience, ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)), ValidateLifetime = true, ClockSkew = TimeSpan.Zero };
    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new ProblemDetails { Type = "https://httpstatuses.com/401", Title = "Unauthorized", Status = 401, Detail = "Authentication is required." }));
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new ProblemDetails { Type = "https://httpstatuses.com/403", Title = "Forbidden", Status = 403, Detail = "The current role cannot perform this operation." }));
        }
    };
});
builder.Services.AddAuthorizationBuilder().AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseExceptionHandler(error => error.Run(async context => { context.Response.StatusCode = 500; await Results.Problem(statusCode: 500, title: "Internal Server Error", type: "https://httpstatuses.com/500").ExecuteAsync(context); }));
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).WithName("GetHealth").WithOpenApi();
app.Run();

public partial class Program { }
