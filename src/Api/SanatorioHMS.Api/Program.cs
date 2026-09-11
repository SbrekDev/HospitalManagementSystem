using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SanatorioHMS.Api;
using SanatorioHMS.Application.Auth;
using SanatorioHMS.Application.ClinicalCare;
using SanatorioHMS.Application.Core;
using SanatorioHMS.Application.Diagnostics;
using SanatorioHMS.Application.PatientRegistry;
using SanatorioHMS.Application.Scheduling;
using SanatorioHMS.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<ApiJwtOptions>(builder.Configuration.GetSection("Jwt"));
var connectionString = builder.Configuration.GetConnectionString("SanatorioHMS") ?? throw new InvalidOperationException("Connection string 'SanatorioHMS' not found.");
builder.Services.AddHmsData(connectionString);
builder.Services.AddScoped<IUserCredentialService, ApiCredentialService>();
builder.Services.AddScoped<IAuthTokenService, ApiTokenService>();
builder.Services.AddScoped<SanatorioHMS.Application.Core.IAuthorizationService, RequestAuthorization>();
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(typeof(CreatePatient).Assembly, typeof(Login).Assembly, typeof(OpenEpisode).Assembly, typeof(GetStudies).Assembly, typeof(ReserveTurn).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(AuthorizationBehavior<,>));
    cfg.AddOpenBehavior(typeof(PersistenceBehavior<,>));
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
// app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).WithName("GetHealth").WithOpenApi();
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var authDb = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await SeedData.SeedAsync(authDb);
    var schedulingDb = scope.ServiceProvider.GetRequiredService<SchedulingDbContext>();
    await SeedData.SeedCatalogsAsync(schedulingDb);
    var diagnosticsDb = scope.ServiceProvider.GetRequiredService<DiagnosticsDbContext>();
    await SeedData.SeedStudiesAsync(diagnosticsDb);
}
app.Run();

public partial class Program { }
