using Microsoft.EntityFrameworkCore;
using SanatorioHMS.Infrastructure.Data;
using SanatorioHMS.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<HmsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SanatorioHMS")));
builder.Services.AddDbContext<PatientRegistryDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("SanatorioHMS")));
builder.Services.AddDbContext<SchedulingDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("SanatorioHMS")));
builder.Services.AddDbContext<ClinicalCareDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("SanatorioHMS")));
builder.Services.AddDbContext<DiagnosticsDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("SanatorioHMS")));
builder.Services.AddDbContext<AuthDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("SanatorioHMS")));
builder.Services.AddHmsIdentity();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<AuditMiddleware>();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("GetHealth")
    .WithOpenApi();

app.Run();
