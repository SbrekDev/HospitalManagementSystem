# Proposal: Hospital Management System Foundation

## Intent

Greenfield HMS foundation for Sanatorio Adventista del Plata (~200k consultations/year, ~180 professionals, ~85 specialties, ~86 studies), replacing fragmented manual processes. MVP: Scheduling (Turnos), Patient Registry, Clinical Care (ambulatory), Diagnostics (lab/imaging), served via WPF desktop app for internal staff. Clean Architecture + DDD + CQRS on .NET 8, SQL Server 2022, EF Core 8.

## Scope

### In Scope (MVP)
- Scheduling: turnos/agendas, professional schedules, secretary booking
- Patient Registry: CRUD, search, guardianship for minors, coverage/payer
- Clinical Care: ambulatory episodes, encounters, clinical notes (Ley 26.529)
- Diagnostics: lab/imaging orders and results, authorization gating
- Auth: RBAC + audit logging (Ley 25.326, Disp. 60-E/2016)
- WPF desktop app (Spanish clinical UI, English code)
- Web API foundation for future own patient portal

### Out of Scope
- Pharmacy, blood bank, inpatient/ICU, emergency (Manchester), billing
- Patient portal implementation (later phase; API-ready now)
- Mobile apps, telemedicine, prevention program templates

## Capabilities

### New Capabilities
- `patient-registry`: patient lifecycle, search, guardianship, coverage
- `scheduling`: turnos/agendas, professional schedules, appointment lifecycle
- `clinical-care`: episodes, encounters, clinical notes, orders
- `diagnostics`: lab/imaging orders, authorization, results/reporting
- `auth`: staff authentication, RBAC, audit logging

### Modified Capabilities
- None (greenfield)

## Approach

Modular monolith sliced by bounded context. Clean Architecture layering (Domain/Application/Infrastructure/Presentation). CQRS/MediatR, repository pattern, EF Core 8 migrations. Coding extension points: CIE-10/11, LOINC, NOMIVARC. Catalogs seeded, then expert-validated (source data marketing-grade). Domain glossary prevents Spanish/English drift.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/SanatorioHMS.Domain.*` | New | Domain model per context |
| `src/SanatorioHMS.Application.*` | New | CQRS handlers, app services |
| `src/SanatorioHMS.Infrastructure` | New | EF Core, repositories, migrations |
| `src/SanatorioHMS.Api` | New | REST controllers, auth |
| `src/SanatorioHMS.Desktop` | New | WPF views/viewmodels |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Scope creep (85+ specialties) | High | Strict 4-context MVP; defer specialty flows |
| Regulatory exposure (Ley 25.326/26.529) | High | RBAC, audit, consent from day one |
| Catalog data quality | Medium | Expert-validate seeded catalogs |

## Rollback Plan

No production users until first release. Feature flags per module; additive-only migrations; per-module git branches enable selective revert. Full rollback = abandon branch; no data migration needed.

## Dependencies

- .NET 8 SDK, SQL Server 2022, Visual Studio 2022
- Domain experts for catalog validation

## Success Criteria

- [ ] Solution scaffolded, CI green
- [ ] Schema migrated; catalogs expert-validated
- [ ] Patient CRUD via API and WPF
- [ ] Turnos scheduling end-to-end
- [ ] Ambulatory clinical notes working
- [ ] Diagnostic order→result loop functional
- [ ] RBAC + audit logging active
- [ ] 80% test coverage (coverlet)
