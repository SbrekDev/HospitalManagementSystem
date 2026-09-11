using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SanatorioHMS.Application.Auth;
using SanatorioHMS.Application.ClinicalCare;
using SanatorioHMS.Application.Diagnostics;
using SanatorioHMS.Application.PatientRegistry;
using SanatorioHMS.Application.Scheduling;
using SanatorioHMS.Domain.Diagnostics.Entities;

namespace SanatorioHMS.Api;

[ApiController]
[Route("api/v1")]
[Authorize]
public abstract class ApiControllerBase : ControllerBase
{
    protected ApiControllerBase(IMediator mediator) => Mediator = mediator;
    protected IMediator Mediator { get; }
    protected IActionResult Reply<T>(SanatorioHMS.Domain.Core.Result<T> result, string? location = null)
    {
        if (result.IsSuccess) return location is null ? Ok(result.Value) : Created(location, result.Value);
        var status = result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true ? 404 : result.Error?.Contains("conflict", StringComparison.OrdinalIgnoreCase) == true || result.Error?.Contains("already exists", StringComparison.OrdinalIgnoreCase) == true ? 409 : result.Error?.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase) == true || result.Error?.Contains("Invalid credentials", StringComparison.OrdinalIgnoreCase) == true || result.Error?.Contains("Invalid refresh token", StringComparison.OrdinalIgnoreCase) == true ? 401 : 400;
        return Problem(statusCode: status, title: status == 409 ? "Conflict" : "Request failed", detail: result.Error, type: $"https://httpstatuses.com/{status}");
    }
}

[Route("api/v1/patients")]
public sealed class PatientsController(IMediator mediator) : ApiControllerBase(mediator)
{
    [HttpGet, HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] Guid? id) => Reply(await Mediator.Send(new SearchPatients(q, id)));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id) => Reply(await Mediator.Send(new SearchPatients(null, id)));
    [HttpPost] public async Task<IActionResult> Create(CreatePatientRequest request) => Reply(await Mediator.Send(new CreatePatient(request)), $"/api/v1/patients");
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id, UpdatePatientRequest request) => Reply(await Mediator.Send(new UpdatePatient(request with { PatientId = id })));
}

[Route("api/v1/agendas")]
public sealed class AgendasController(IMediator mediator, InMemoryStore store) : ApiControllerBase(mediator)
{
    [HttpGet] public IActionResult List() => Ok(store.Agendas);
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id) => Reply(await Mediator.Send(new GetAgenda(id)));
    [HttpPost] public async Task<IActionResult> Publish(PublishAgendaRequest request) => Reply(await Mediator.Send(new PublishAgenda(request)), $"/api/v1/agendas/{request.AgendaId}");
}

[Route("api/v1/turns")]
public sealed class TurnsController(IMediator mediator, InMemoryStore store) : ApiControllerBase(mediator)
{
    [HttpGet] public IActionResult List() => Ok(store.Turns);
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id) => Reply(await Mediator.Send(new GetTurnDetails(id)));
    [HttpPost] public async Task<IActionResult> Reserve(ReserveTurnRequest request) => Reply(await Mediator.Send(new ReserveTurn(request)), "/api/v1/turns");
    [HttpPut("{id:guid}/status")] public async Task<IActionResult> Status(Guid id, ChangeTurnStatusRequest request) => Reply(await Mediator.Send(new ChangeTurnStatus(request with { TurnId = id })));
}

[Route("api/v1/episodes")]
public sealed class EpisodesController(IMediator mediator, InMemoryStore store) : ApiControllerBase(mediator)
{
    [HttpGet] public IActionResult List() => Ok(store.Episodes);
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id) => Reply(await Mediator.Send(new GetEpisode(id)));
    [HttpPost] public async Task<IActionResult> Open(OpenEpisodeRequest request) => Reply(await Mediator.Send(new OpenEpisode(request)), "/api/v1/episodes");
    [HttpPut("{id:guid}/close")] public async Task<IActionResult> Close(Guid id) => Reply(await Mediator.Send(new CloseEpisode(new CloseEpisodeRequest(id))));
    [HttpPost("{id:guid}/notes")] public async Task<IActionResult> Note(Guid id, AppendClinicalNoteRequest request) => Reply(await Mediator.Send(new AppendClinicalNote(request with { EpisodeId = id })));
    [HttpPost("{id:guid}/orders")] public async Task<IActionResult> Order(Guid id, IssueOrderRequest request) => Reply(await Mediator.Send(new IssueOrder(request with { EpisodeId = id })));
}

[Route("api/v1/studies")]
public sealed class StudiesController(IMediator mediator) : ApiControllerBase(mediator)
{
    [HttpGet] public async Task<IActionResult> List([FromQuery] string? q) => Reply(await Mediator.Send(new GetStudies(q)));
}

[Route("api/v1/diagnostic-orders")]
public sealed class DiagnosticOrdersController(IMediator mediator) : ApiControllerBase(mediator)
{
    [HttpGet] public async Task<IActionResult> List([FromQuery] Guid? patientId) => Reply(await Mediator.Send(new GetPendingOrders(patientId)));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id) => Reply(await Mediator.Send(new GetOrderDetails(id)));
    [HttpPost] public async Task<IActionResult> Create(CreateDiagnosticOrderRequest request) => Reply(await Mediator.Send(new CreateDiagnosticOrder(request)), "/api/v1/diagnostic-orders");
}

[Route("api/v1/results")]
public sealed class ResultsController(IMediator mediator) : ApiControllerBase(mediator)
{
    [HttpPost] public async Task<IActionResult> Register(RegisterDiagnosticResultRequest request) => Reply(await Mediator.Send(new RegisterDiagnosticResult(request)), "/api/v1/results");
    [HttpPut("{id:guid}/validate")] public async Task<IActionResult> Validate(Guid id, ValidateDiagnosticResultRequest request) => Reply(await Mediator.Send(new ValidateDiagnosticResult(request with { ResultId = id })));
}

[Route("api/v1/auth")]
[AllowAnonymous]
public sealed class AuthController(IMediator mediator) : ApiControllerBase(mediator)
{
    [HttpPost("login")] public async Task<IActionResult> Login(LoginRequest request) => Reply(await Mediator.Send(new Login(request)));
    [HttpPost("refresh")] public async Task<IActionResult> Refresh(RefreshTokenRequest request) => Reply(await Mediator.Send(new RefreshToken(request)));
    [Authorize, HttpPost("logout")] public async Task<IActionResult> Logout([FromBody] Guid sessionId) => Reply(await Mediator.Send(new Logout(sessionId)));
    [Authorize, HttpGet("me")] public async Task<IActionResult> Me() => Guid.TryParse(User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value, out var id) ? Reply(await Mediator.Send(new GetCurrentUser(id))) : Problem(detail: "A valid subject claim is required.", statusCode: 401, title: "Unauthorized");
}
