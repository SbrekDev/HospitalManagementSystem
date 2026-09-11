# Tasks: Hospital Management System Foundation

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~2,600–3,200 (greenfield: 18 projects, 23 tables, API + WPF + tests) |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | 8 work units (PR 1 → PR 8); original 6-slice split exceeded budget at domain and WPF units |
| Delivery strategy | feature-branch-chain |
| Chain strategy | feature-branch-chain with GitFlow feature branches into develop and releases to main |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: feature-branch-chain with GitFlow feature branches into develop and releases to main
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Focused test command | Runtime harness | Rollback boundary |
|------|------|-----------|----------------------|---------------|-------------------|
| WU-1 | Solution scaffold, Domain.Core, git, CI, DB project | PR 1 (~380) | `dotnet build src/SanatorioHMS.sln && dotnet test tests/` | Local SQL Server reachability check (`dotnet ef migrations script` dry-run) | Remove `src/`+`tests/` scaffold; nothing depends on it |
| WU-2 | Domain entities: Scheduling + ClinicalCare | PR 2 (~360) | `dotnet test tests/SanatorioHMS.Domain.Tests` | N/A — pure unit tests | Revert Domain.Scheduling/ClinicalCare additions |
| WU-3 | Domain entities: Diagnostics + Auth | PR 3 (~340) | `dotnet test tests/SanatorioHMS.Domain.Tests` | N/A — pure unit tests | Revert Domain.Diagnostics/Auth additions |
| WU-4 | DbContexts, migrations, repositories, JWT, audit | PR 4 (~400) | `dotnet test tests/SanatorioHMS.Infrastructure.Tests` | SQL Server LocalDB/Testcontainers | Revert Infrastructure.* projects + drop pending migrations |
| WU-5 | CQRS handlers/validators for all modules | PR 5 (~390) | `dotnet test tests/SanatorioHMS.Application.Tests` | N/A — Moq unit tests | Revert Application.* features; lower layers unused |
| WU-6 | REST API controllers, RBAC policies, typed client | PR 6 (~400) | `dotnet test tests/SanatorioHMS.Api.Tests` | WebApplicationFactory + SQL Server | Revert Api/ projects; WPF unaffected |
| WU-7 | WPF shell, navigation, registry + scheduling views | PR 7 (~380) | `dotnet test tests/SanatorioHMS.Desktop.Tests` | Manual: launch WPF, login → patient CRUD → booking | Revert Desktop view modules |
| WU-8 | Clinical + diagnostics views, coverage ≥80%, CI gate | PR 8 (~400) | `dotnet test /p:CollectCoverage=true` | Manual: episode → order → result loop in WPF | Revert views + test-only diff |

## Phase 1: Project Foundation (WU-1)

### T-001: Create .NET solution structure
**Module**: Infrastructure | **Type**: chore | **Estimation**: 3h | **PR**: 1
- [x] `src/SanatorioHMS.sln` + all 18 projects per design tree (Domain ×6, Application ×6, Infrastructure ×3, Api ×2, Desktop); project references enforce inward dependency rule
- [x] `dotnet build` green; Directory.Build.props pins net8.0 + analyzers

### T-002: Configure Git repository and .gitignore
**Module**: Infrastructure | **Type**: chore | **Estimation**: 1h | **PR**: 1
- [x] `git init` + .NET standard .gitignore (bin/, obj/, .vs/, user secrets)

### T-003: Set up CI/CD pipeline with GitHub Actions
**Module**: Infrastructure | **Type**: chore | **Estimation**: 4h | **PR**: 1
- [x] build → `dotnet format --verify-no-changes` → `dotnet test` with coverlet; fail under 80% when code exists (informational in PR 1)

### T-004: Create database project with EF Core migrations
**Module**: Infrastructure | **Type**: chore | **Estimation**: 4h | **PR**: 1
- [x] `Infrastructure.Data` wired to SQL Server 2022; per-context schemas (`patient`,`sched`,`clinical`,`diag`,`auth`); migration tooling + first empty baseline; additive-only convention documented

## Phase 2: Domain Layer (WU-1…WU-3)

### T-010: Implement Domain.Core shared primitives
**Module**: Infrastructure | **Type**: feature | **Estimation**: 3h | **PR**: 1
- [ ] `Entity<T>`, `AggregateRoot<T>`, `ValueObject`, `IDomainEvent`, `IRepository<T>`, `Result`
- [ ] Test: value-object equality by components

### T-011: Implement PatientRegistry domain entities
**Module**: PatientRegistry | **Type**: feature | **Estimation**: 6h | **PR**: 1
- [ ] `Patient` AR + `PatientContact`, `HealthCoverage`, `PatientDocument`, `GuardianRelationship`; VO `IdentityDocument` (DNI/LC/LE/Passport); `CoverageType`
- [ ] Tests: duplicate document rejected; obra social/prepaga without payer rejected; minor completion without guardian blocked — **Related**: patient-registry FR-001…004, edge cases

### T-012: Implement Scheduling domain entities
**Module**: Scheduling | **Type**: feature | **Estimation**: 8h | **PR**: 2
- [x] `Agenda` AR (non-overlapping intervals), `Turn` AR with state machine Reserved→Confirmed→Attended / →Cancelled/NoShow; `Professional`, `Specialty`, `ProfessionalSpecialty`, `Room`; events `TurnBooked`, `TurnCancelled`
- [x] Tests: invalid transition rejected; room required in-person, destination required teleconsultation — **Related**: scheduling FR-001…004, edges

### T-013: Implement ClinicalCare domain entities
**Module**: ClinicalCare | **Type**: feature | **Estimation**: 8h | **PR**: 2
- [x] `Episode` AR (Active→Closed only when requirements met; edits after closure blocked), `Encounter`, append-only `ClinicalNote` (evolution/procedure), `Order` AR + `OrderItem` (Medication/Study/Procedure/Diet); event `EpisodeClosed`; corrections = traceable additions
- [x] Tests: closure guard; append-only enforcement; duplicate submission idempotence — **Related**: clinical-care FR-001…004, edges

### T-014: Implement Diagnostics domain entities
**Module**: Diagnostics | **Type**: feature | **Estimation**: 6h | **PR**: 3
- [x] Versioned `Study` catalog (LOINC column, retired studies block new orders), `DiagnosticOrder` AR with `AuthorizationStatus` gate (Pending/Denied block processing), `DiagnosticResult` + validation/correction; events `DiagnosticOrderCreated`, `DiagnosticResultValidated`
- [x] Tests: denied authorization blocks fulfillment; result correction never overwrites original — **Related**: diagnostics FR-001…004, edges

### T-015: Implement Auth domain entities
**Module**: Auth | **Type**: feature | **Estimation**: 4h | **PR**: 3
- [x] `User`, `Role`, `Permission`, `RolePermission`, `UserRole`, `Session`, append-only hash-chained `AuditLog`; event `AuditRecorded`
- [x] Tests: audit hash chain links previous entry — **Related**: auth FR-002, FR-005, NFR-002

## Phase 3: Infrastructure Layer (WU-4)

### T-020: Set up EF Core DbContexts
**Module**: Infrastructure | **Type**: feature | **Estimation**: 6h | **PR**: 4
- [ ] One DbContext per context; `IEntityTypeConfiguration` per entity; unique (DocumentType,DocumentNumber), unique Turns(AgendaId,StartTime)+rowversion, FK ON DELETE RESTRICT (clinical/audit); migrations generated
- [ ] Test: migration applies to clean LocalDB; double-booking surfaces DB-level conflict

### T-021: Implement repositories
**Module**: Infrastructure | **Type**: feature | **Estimation**: 6h | **PR**: 4
- [ ] Generic `Repository<T>` + per-context repos; `ITurnRepository.FindSlotAsync` with slot locking; unit of work per request
- [ ] Test: concurrent reserve on same slot → exactly one success

### T-022: Set up JWT authentication
**Module**: Auth | **Type**: feature | **Estimation**: 8h | **PR**: 4
- [ ] ASP.NET Core Identity + PasswordHasher; access token + rotated refresh token; `Session` persistence; expiry/logout/disable/reset invalidate sessions
- [ ] Test: no plaintext password at rest; reset consumes token and kills prior sessions — **Related**: auth FR-001, FR-003, FR-004, NFR-001

### T-023: Configure audit logging
**Module**: Auth | **Type**: feature | **Estimation**: 6h | **PR**: 4
- [ ] MediatR pipeline behavior + auth-failure middleware appending AuditLog(actor, action, target, timestamp, outcome); Admin-only read; RBAC policy registration (User→Role→Permission)
- [ ] Test: failed login audited without leaking account existence — **Related**: auth FR-002, FR-005

## Phase 4: Application Layer (WU-5)

### T-030: PatientRegistry commands/queries
**Module**: PatientRegistry | **Type**: feature | **Estimation**: 6h | **PR**: 5
 - [x] Create/Update patient, Search (id/document/name/contact), history+coverage+guardian commands with FluentValidation handlers/DTOs
 - [x] Test: validators reject spec edge inputs — **Related**: patient-registry FR-001…003, NFR-002

### T-031: Scheduling commands/queries
**Module**: Scheduling | **Type**: feature | **Estimation**: 8h | **PR**: 5
 - [x] Agenda publish/query, Turn reserve/cancel/status-change commands; 409 mapping for slot conflict
 - [x] Test: reservation emits `TurnBooked`; invalid status change rejected — **Related**: scheduling FR-001…004

### T-032: ClinicalCare commands/queries
**Module**: ClinicalCare | **Type**: feature | **Estimation**: 8h | **PR**: 5
 - [x] Episode open/close, note append, order issue handlers
 - [x] Test: closure blocked while requirements incomplete — **Related**: clinical-care FR-001…004

### T-033: Diagnostics commands/queries
**Module**: Diagnostics | **Type**: feature | **Estimation**: 6h | **PR**: 5
 - [x] Order create, result register/validate, catalog query by specialty handlers
 - [x] Test: processing blocked when authorization absent/denied (409 path) — **Related**: diagnostics FR-002…004

### T-034: Auth commands/queries
**Module**: Auth | **Type**: feature | **Estimation**: 6h | **PR**: 5
 - [x] Login/refresh/logout/me handlers; MediatR behavior ordering validation→authorization→audit
 - [x] Test: unauthorized request denied without exposing protected data — **Related**: auth FR-001…003

## Phase 5: API Layer (WU-6)

### T-040: PatientRegistry controller
**Module**: PatientRegistry | **Type**: feature | **Estimation**: 4h | **PR**: 6
- [x] `GET/POST/PUT /api/v1/patients`, `GET /patients/{id}`, `GET /patients/search`; RFC 7807 problem details
- [x] Integration test: duplicate document → 409 with existing id hint — **Related**: patient-registry FR-002, edge

### T-041: Scheduling controller
**Module**: Scheduling | **Type**: feature | **Estimation**: 5h | **PR**: 6
- [x] `GET/POST /agendas`, `GET/POST /turns`, `PUT /turns/{id}/status`
- [x] Integration test: concurrent booking race → one 201, rest 409 — **Related**: scheduling NFR-001, edge

### T-042: ClinicalCare controller
**Module**: ClinicalCare | **Type**: feature | **Estimation**: 5h | **PR**: 6
- [x] `GET/POST /episodes`, `/encounters`, `/orders`
- [x] Integration test: edit closed episode → 403/409 — **Related**: clinical-care FR-004

### T-043: Diagnostics controller
**Module**: Diagnostics | **Type**: feature | **Estimation**: 5h | **PR**: 6
- [x] `GET/POST /diagnostic-orders`, `GET /studies`, `POST /results`
- [x] Integration test: unvalidated vs validated result distinction — **Related**: diagnostics FR-001, FR-004

### T-044: Auth controller
**Module**: Auth | **Type**: feature | **Estimation**: 6h | **PR**: 6
- [x] `POST /auth/login|refresh`, `GET /auth/me`; `[Authorize(Policy)]` per endpoint; `SanatorioHMS.Api.Client` typed client
- [x] Integration test: 401/403 matrix per role; invalid credentials never leak existence — **Related**: auth FR-001…003, edges

## Phase 6: WPF Desktop Application (WU-7, WU-8)

### T-050: Set up WPF shell and navigation
**Module**: Infrastructure | **Type**: feature | **Estimation**: 8h | **PR**: 7
- [ ] MVVM shell (CommunityToolkit), login view, role-driven nav, Api.Client DI; Spanish UI / English code
- [ ] ViewModel test: nav items reflect granted permissions — **Related**: auth FR-002

### T-051: PatientRegistry views
**Module**: PatientRegistry | **Type**: feature | **Estimation**: 8h | **PR**: 7
- [ ] Patient search, registration, coverage/guardian editing forms
- [ ] ViewModel test: duplicate document shows 409 problem-detail message — **Related**: patient-registry FR-001…004

### T-052: Scheduling views
**Module**: Scheduling | **Type**: feature | **Estimation**: 10h | **PR**: 7
- [ ] Agenda grid with slots, booking dialog with room/teleconsultation allocation, status actions
- [ ] ViewModel test: full slot disables reserve; conflict shows reschedule prompt — **Related**: scheduling FR-001…004

### T-053: ClinicalCare views
**Module**: ClinicalCare | **Type**: feature | **Estimation**: 8h | **PR**: 8
- [ ] Episode workspace: notes (evolution/procedure), order entry, closure action
- [ ] ViewModel test: closed episode disables edit — **Related**: clinical-care FR-002…004

### T-054: Diagnostics views
**Module**: Diagnostics | **Type**: feature | **Estimation**: 8h | **PR**: 8
- [ ] Order creation w/ prep instructions, LabTech result registration, doctor validation screen
- [ ] ViewModel test: Pending/Denied authorization disables fulfillment with reason — **Related**: diagnostics FR-002…004

## Phase 7: Testing & Verification (WU-8)

### T-060: Unit tests for Domain layer
**Module**: Infrastructure | **Type**: chore | **Estimation**: 6h | **PR**: 8
- [ ] Turn state machine, Order authorization gate, Episode closure rules, audit hash chain ≥90% on Domain

### T-061: Unit tests for Application layer
**Module**: Infrastructure | **Type**: chore | **Estimation**: 6h | **PR**: 8
- [ ] All handlers happy + failure paths with Moq; validator rule coverage per spec edge cases

### T-062: Integration tests for API
**Module**: Infrastructure | **Type**: chore | **Estimation**: 6h | **PR**: 8
- [ ] WebApplicationFactory: status-code matrix (201/200/204/400/401/403/404/409), RBAC per role, audit emission, concurrent-booking 409

### T-063: WPF view model tests and coverage gate
**Module**: Infrastructure | **Type**: chore | **Estimation**: 4h | **PR**: 8
- [ ] ViewModel tests for all views; total coverage ≥80% enforced in CI; success criteria checklist from proposal verified
