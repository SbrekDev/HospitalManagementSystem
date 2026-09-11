# Design: Hospital Management System Foundation

## Architecture Overview

Modular monolith sliced by bounded context, layered with Clean Architecture + DDD + CQRS. Every module is a vertical slice spanning the four layers; shared `Core` projects carry cross-cutting primitives.

| Layer | Responsibility | Dependency rule |
|-------|---------------|-----------------|
| **Domain** | Entities, aggregates, value objects, domain events, repository interfaces | No external deps |
| **Application** | CQRS commands/queries (MediatR), use cases, DTOs, validators, ports | → Domain only |
| **Infrastructure** | EF Core `DbContext`s, repository impls, identity, migrations, audit | → Application + Domain |
| **Presentation** | ASP.NET Core Web API + WPF desktop (MVVM) | → Application |

Dependencies point inward. The API and the WPF app both consume the same `Application` layer — the API is the single integration surface for the future patient portal.

## Solution Structure

```
src/
├── SanatorioHMS.sln
├── Presentation/
│   ├── SanatorioHMS.Desktop/              # WPF (.NET 8), MVVM, Spanish UI
│   └── SanatorioHMS.Web/                  # Blazor (future phase)
├── Api/
│   ├── SanatorioHMS.Api/                  # ASP.NET Core 8 Web API + JWT
│   └── SanatorioHMS.Api.Client/           # Typed HTTP client for Desktop
├── Application/
│   ├── SanatorioHMS.Application.Core/     # MediatR pipeline, ports, DTO base
│   ├── SanatorioHMS.Application.PatientRegistry/
│   ├── SanatorioHMS.Application.Scheduling/
│   ├── SanatorioHMS.Application.ClinicalCare/
│   ├── SanatorioHMS.Application.Diagnostics/
│   └── SanatorioHMS.Application.Auth/
├── Domain/
│   ├── SanatorioHMS.Domain.Core/          # Entity/ValueObject/AggregateRoot bases
│   ├── SanatorioHMS.Domain.PatientRegistry/
│   ├── SanatorioHMS.Domain.Scheduling/
│   ├── SanatorioHMS.Domain.ClinicalCare/
│   ├── SanatorioHMS.Domain.Diagnostics/
│   └── SanatorioHMS.Domain.Auth/
└── Infrastructure/
    ├── SanatorioHMS.Infrastructure.Core/  # DI registrations, unit of work
    ├── SanatorioHMS.Infrastructure.Data/  # EF Core DbContexts, migrations, repos
    └── SanatorioHMS.Infrastructure.Identity/ # JWT, RBAC, audit
```

## Domain Model

Aggregate roots are marked **AR**; value objects are immutable.

| Context | Entities | Notes |
|---------|----------|-------|
| **PatientRegistry** | `Patient` **AR**, `PatientContact`, `HealthCoverage`, `PatientDocument`, `GuardianRelationship` | VO: `IdentityDocument` (type DNI/LC/LE/Passport + number, uniqueness enforced). `CoverageType`: ObraSocial/Prepaga/Particular |
| **Scheduling** | `Professional`, `Specialty`, `ProfessionalSpecialty`, `Agenda` **AR**, `Turn` **AR**, `Room` | VO: `TimeSlot`. `TurnStatus` state machine: Reserved→Confirmed→Attended; Reserved/Confirmed→Cancelled/NoShow |
| **ClinicalCare** | `Episode` **AR**, `Encounter`, `ClinicalNote`, `Order` **AR**, `OrderItem` | `OrderType`: Medication/Study/Procedure/Diet. Notes and orders are append-only |
| **Diagnostics** | `Study` (catalog, versioned), `DiagnosticOrder` **AR**, `DiagnosticResult` | `AuthorizationStatus`: NotRequired/Pending/Approved/Denied |
| **Auth** | `User`, `Role`, `Permission`, `RolePermission`, `UserRole`, `Session`, `AuditLog` | `AuditLog` is tamper-evident (append-only, hash-chained) |

**Domain events**: `PatientRegistered`, `TurnBooked`, `TurnCancelled`, `EpisodeClosed`, `DiagnosticResultValidated`, `AuditRecorded`. Cross-context integration via in-process MediatR notifications.

**Coding extension points**: `Study.Code` (LOINC), diagnosis code (CIE-10/11), practice code (NOMIVARC) — seeded catalogs, expert-validated per proposal.

## API Design

Base path `/api/v1`. JSON (camelCase). Auth via `Authorization: Bearer`.

| Module | Endpoints |
|--------|-----------|
| PatientRegistry | `GET/POST/PUT /patients`, `GET /patients/{id}`, `GET /patients/search?q=...` |
| Scheduling | `GET/POST /agendas`, `GET/POST /turns`, `PUT /turns/{id}/status` |
| ClinicalCare | `GET/POST /episodes`, `GET/POST /encounters`, `GET/POST /orders` |
| Diagnostics | `GET/POST /diagnostic-orders`, `GET /studies`, `POST /results` |
| Auth | `POST /auth/login`, `POST /auth/refresh`, `GET /auth/me` |

**Status codes**: `201` created (Location header), `200` read/update, `204` no content, `400` validation, `401` unauthenticated, `403` forbidden, `404` not found, `409` conflict (duplicate document / double booking / invalid status transition).

**Errors**: RFC 7807 problem details — `type`, `title`, `status`, `detail`, never leak account existence or protected data.

## Database Design

SQL Server 2022. One schema per bounded context (`patient`, `sched`, `clinical`, `diag`, `auth`). Additive-only migrations.

| Context | Tables |
|---------|--------|
| PatientRegistry | `Patients`, `PatientContacts`, `HealthCoverages`, `PatientDocuments`, `GuardianRelationships` |
| Scheduling | `Professionals`, `Specialties`, `ProfessionalSpecialties`, `Agendas`, `Turns`, `Rooms` |
| ClinicalCare | `Episodes`, `Encounters`, `ClinicalNotes`, `Orders`, `OrderItems` |
| Diagnostics | `Studies`, `DiagnosticOrders`, `DiagnosticResults` |
| Auth | `Users`, `Roles`, `Permissions`, `RolePermissions`, `UserRoles`, `Sessions`, `AuditLogs` |

**Key relationships**: `Patient 1—* Turn`, `Professional *—* Specialty`, `Agenda 1—* Turn`, `Episode 1—* Encounter 1—* ClinicalNote`, `Episode 1—* Order 1—* OrderItem`, `Episode 1—* DiagnosticOrder 1—* DiagnosticResult`, `Study 1—* DiagnosticOrder`.

**Indexes & constraints**: unique `(DocumentType, DocumentNumber)`; unique `Turns(AgendaId, StartTime)` + `rowversion` concurrency token to prevent double booking; `Patients(Name, Surname)`, `Turns(Status, StartTime)`, `DiagnosticOrders(PatientId, AuthorizationStatus)`. FK `ON DELETE RESTRICT` for clinical/audit tables (no history erasure).

## Security Architecture

**Authentication (JWT)**: ASP.NET Core Identity + `PasswordHasher` (no plaintext, never logged). `POST /auth/login` verifies credentials → issues short-lived access token + rotated refresh token → persists `Session`. Refresh rotation invalidates prior token. Session expiry, logout, and account disable/reset invalidate sessions (spec FR-003/004).

**Authorization (RBAC)**: `User → UserRole → Role → RolePermission → Permission`, enforced via `[Authorize(Policy=…)]` and a MediatR authorization behavior. Roles: Admin, Doctor, Nurse, Receptionist, LabTech (extensible). Least privilege per spec NFR.

**Audit**: a MediatR pipeline behavior + auth-failure middleware append `AuditLog(actor, action, target, timestamp, outcome)` — append-only, hash-chained, readable only by Admin.

## Infrastructure

- **EF Core 8**: one `DbContext` per context, each with its own `IEntityTypeConfiguration`; migrations in `Infrastructure.Data`.
- **Repositories**: generic `IRepository<T>` + context-specific repositories (`ITurnRepository` with slot-locking query). Unit of work scoped per request.
- **DI**: `Infrastructure.Core` exposes `AddDomain`, `AddApplication`, `AddInfrastructure`, `AddAuth` extension methods; API composes them. MediatR registered by scanning `Application.*`.

## Architecture Decisions

| Decision | Option A | Option B | Choice & rationale |
|----------|----------|----------|---------------------|
| Topology | Microservices | **Modular monolith** | Chosen: single team, ~550/day consultations; monolith reduces ops cost while context slices preserve extraction paths |
| Context slicing | Layer-first (one Domain/App project) | **Vertical slice per context** | Chosen: matches DDD bounded contexts, enables per-module feature flags + git branches (rollback plan) |
| Query model | Full CQRS write/read | **CQRS + MediatR** | Chosen: MediatR handlers centralize cross-cutting audit/validation; declared in config |
| Double-booking guard | App-level lock only | **DB unique index + rowversion** | Chosen: storage-level guarantee is race-proof; app maps conflict → `409` |
| Clinical immutability | Editable rows | **Append-only + correction records** | Chosen: Ley 26.529 requires traceable corrections, not overwrites |
| Audit integrity | Plain log table | **Append-only hash-chained** | Chosen: Ley 25.326 / Disp. 60-E/2016 require tamper-evident audit |
| Coding standards | Hardcode local codes | **Extension points (LOINC/CIE-10/11/NOMIVARC)** | Chosen: seeded local catalogs now, standard codes as columns from day one |

## Data Flow

Diagnostic order→result loop (complex flow per config rules):

```
 Doctor ──► Api ──► MediatR ──► DiagnosticOrderHandler ──► Domain (DiagnosticOrder)
    │                             │                              │
    │  201 + prep instructions    │  publish DiagnosticOrderCreated
    │◄────────────────────────────┘                              │
 LabTech ──► POST /results  ──► register result (Pending)         │
    │                             │  authorization gate: Denied/Pending → 409
 Doctor ──► validate result ──► DiagnosticResultValidated ────────┘
    │                             │
    │  200 + validated result     │  append AuditLog (actor/action/target/outcome)
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/SanatorioHMS.sln` | Create | Solution root |
| `src/Domain/*` (7 projects) | Create | Aggregates, VOs, domain events, repo interfaces |
| `src/Application/*` (6 projects) | Create | CQRS handlers, DTOs, validators, ports |
| `src/Infrastructure/*` (3 projects) | Create | EF Core DbContexts, migrations, repos, JWT/RBAC/audit |
| `src/Api/*` (2 projects) | Create | REST controllers, auth pipeline, typed client |
| `src/Presentation/SanatorioHMS.Desktop` | Create | WPF MVVM shell + Patient/Turno views |
| `tests/*` (unit + integration) | Create | xUnit + Moq + FluentAssertions + WebApplicationFactory |

## Interfaces / Contracts

```csharp
public interface ITurnRepository : IRepository<Turn>
{
    Task<Turn?> FindSlotAsync(Guid agendaId, DateTime start, CancellationToken ct);
}

public record IdentityDocument(DocumentType Type, string Number);

public sealed class DiagnosticOrder : AggregateRoot<Guid>
{
    public AuthorizationStatus Authorization { get; private set; }
    public Result RegisterResult(ResultData data, Guid staffId);   // blocks if not Approved/NotRequired
}
```

## Technology Choices

| Component | Choice | Rationale |
|-----------|--------|-----------|
| ORM | EF Core 8 | SQL Server native support, migrations |
| API | ASP.NET Core 8 | Modern, cross-platform, JWT/RBAC built-in |
| Desktop | WPF (.NET 8) | Rich internal client; MVVM |
| Auth | JWT + RBAC | Standard, auditable |
| CQRS | MediatR | Clean command/query + behavior pipeline |
| Tests | xUnit + Moq + FluentAssertions | Declared in config |
| CI | GitHub Actions | Declared; `dotnet build`/`dotnet test` + coverlet (80%) |

## Testing Strategy

| Layer | What to test | Approach |
|-------|-------------|----------|
| Unit | Aggregates (Turn status machine, Order authorization gate, Episode closure), validators | xUnit + Moq, no DB |
| Integration | Repositories, DbContext mappings, migrations, concurrency (`409` on double booking) | WebApplicationFactory + SQL Server LocalDB/testcontainer |
| API | Auth/RBAC enforcement, audit emission, status codes, problem details | WebApplicationFactory, in-memory auth |
| E2E | Patient CRUD → booking → episode → order → result loop | Manual WPF smoke + API scripted suite (future) |

Coverage target 80% (coverlet). RED tests for applicable spec edge cases (duplicate document, invalid transitions, rejected authorization).

## Threat Matrix

N/A — no routing, shell, subprocess, VCS/PR automation, executable-file classification, or process-integration boundary. This is a CRUD/domain application; no external process execution or file classification is introduced.

## Migration / Rollout

No data migration (greenfield). Rollout: per-module feature flags + additive-only migrations; per-module git branches enable selective revert. Full rollback = abandon branch.

## Open Questions

- [ ] Confirm the existing patient portal is integrated (adapter) or replaced — affects `Api.Client` scope.
- [ ] Confirm seed-catalog validation owner (which domain experts) before production data load.
- [ ] Confirm AAIP database registration timing in the rollout plan (Ley 25.326).
