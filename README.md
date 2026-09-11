# Hospital Management System (HMS)

> Sistema de Gestión Hospitalaria - Sanatorio Adventista del Plata

[![Build Status](https://github.com/SbrekDev/HospitalManagementSystem/actions/workflows/ci.yml/badge.svg?branch=develop)](https://github.com/YOUR_USERNAME/HospitalManagementSystem/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-12-blue)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-blue)](https://www.microsoft.com/sql-server)
[![ITAES](https://img.shields.io/badge/ITAES-2026--2029-green)](https://www.sanatorioadventista.org.ar/)

## 📋 Tabla de Contenidos

- [Descripción](#-descripción)
- [Características](#-características)
- [Stack Tecnológico](#-stack-tecnológico)
- [Arquitectura](#-arquitectura)
- [Estructura del Proyecto](#-estructura-del-proyecto)
- [Primeros Pasos](#-primeros-pasos)
- [Base de Datos](#-base-de-datos)
- [Testing](#-testing)
- [CI/CD](#-cicd)
- [Contribuir](#-contribuir)
- [Roadmap](#-roadmap)
- [Licencia](#-licencia)

---

## 📝 Descripción

El **Hospital Management System (HMS)** es un sistema integral de gestión hospitalaria desarrollado para el **Sanatorio Adventista del Plata**, una institución médica polivalente de alta complejidad ubicada en Entre Ríos, Argentina, con **118 años de historia** y más de **200,000 consultas anuales**.

El sistema abarca la gestión de:
- 📅 Turnos y Agenda de Profesionales
- 👥 Registro de Pacientes e Historia Clínica
- 🏥 Atención Ambulatoria y Episodios Clínicos
- 🔬 Diagnósticos por Imágenes y Laboratorio
- 🔐 Authentication y Control de Acceso

---

## ✨ Características

### Módulos Principales

| Módulo | Descripción | Estado |
|--------|-------------|--------|
| **Scheduling** | Gestión de turnos y agendas de profesionales | ✅ Implementando |
| **Patient Registry** | Registro de pacientes e información demográfica | ✅ Implementando |
| **Clinical Care** | Episodios clínicos, atenciones y notas | ✅ Implementando |
| **Diagnostics** | Órdenes y resultados de estudios | ✅ Implementando |
| **Auth** | Autenticación y autorización RBAC | ✅ Implementando |
| **Pharmacy** | Dispensación de medicamentos | 🔜 Próxima fase |
| **Billing** | Facturación y cuentas corrientes | 🔜 Próxima fase |
| **Inventory** | Gestión de inventarios | 🔜 Próxima fase |

### Características Técnicas

- 🏗️ **Clean Architecture** con DDD (Domain-Driven Design)
- ⚡ **CQRS** con MediatR para commands y queries
- 🗄️ **Entity Framework Core 8** con SQL Server 2022
- 🖥️ **WPF Desktop App** para staff interno
- 🌐 **REST API** para integración futura
- 🧪 **xUnit** + **Moq** + **FluentAssertions** para testing
- 📊 **GitHub Actions** para CI/CD
- 🔒 **JWT** + **RBAC** para seguridad
- 📝 **Auditoría completa** de todas las operaciones

---

## 🛠️ Stack Tecnológico

| Componente | Tecnología | Versión |
|------------|------------|---------|
| **Runtime** | .NET | 8.0 |
| **Lenguaje** | C# | 12.0 |
| **Base de Datos** | SQL Server | 2022 |
| **ORM** | Entity Framework Core | 8.0 |
| **API** | ASP.NET Core Web API | 8.0 |
| **Desktop** | WPF | .NET 8 |
| **Testing** | xUnit + Moq + FluentAssertions | Latest |
| **CI/CD** | GitHub Actions | - |
| **Arquitectura** | Clean Architecture + DDD + CQRS | - |

---

## 🏗️ Arquitectura

### Clean Architecture Layers

```
┌─────────────────────────────────────────────────────────────┐
│                    PRESENTATION LAYER                        │
│              (WPF Desktop App, Web API)                      │
├─────────────────────────────────────────────────────────────┤
│                      API LAYER                               │
│            (ASP.NET Core Web API - Controllers)              │
├─────────────────────────────────────────────────────────────┤
│                   APPLICATION LAYER                          │
│         (Commands, Queries, Services, DTOs)                   │
├─────────────────────────────────────────────────────────────┤
│                     DOMAIN LAYER                             │
│      (Entities, Value Objects, Aggregates, Events)            │
├─────────────────────────────────────────────────────────────┤
│                  INFRASTRUCTURE LAYER                        │
│    (EF Core, Repositories, External Services, Identity)       │
└─────────────────────────────────────────────────────────────┘
```

### Domain-Driven Design Bounded Contexts

```
┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│   Patient     │ │  Scheduling  │ │   Clinical   │ │  Diagnostics │
│   Registry    │ │              │ │    Care      │ │              │
└──────────────┘ └──────────────┘ └──────────────┘ └──────────────┘
```

---

## 📁 Estructura del Proyecto

```
HospitalManagementSystem/
├── src/                          # Código fuente
│   ├── SanatorioHMS.sln          # Solution file
│   ├── Domain/                    # Domain Layer
│   │   ├── SanatorioHMS.Domain.Core/
│   │   ├── SanatorioHMS.Domain.PatientRegistry/
│   │   ├── SanatorioHMS.Domain.Scheduling/
│   │   ├── SanatorioHMS.Domain.ClinicalCare/
│   │   └── SanatorioHMS.Domain.Diagnostics/
│   ├── Application/               # Application Layer
│   │   ├── SanatorioHMS.Application.Core/
│   │   ├── SanatorioHMS.Application.PatientRegistry/
│   │   ├── SanatorioHMS.Application.Scheduling/
│   │   ├── SanatorioHMS.Application.ClinicalCare/
│   │   └── SanatorioHMS.Application.Diagnostics/
│   ├── Infrastructure/            # Infrastructure Layer
│   │   ├── SanatorioHMS.Infrastructure.Core/
│   │   ├── SanatorioHMS.Infrastructure.Data/
│   │   └── SanatorioHMS.Infrastructure.Identity/
│   ├── Api/                      # API Layer
│   │   ├── SanatorioHMS.Api/
│   │   └── SanatorioHMS.Api.Client/
│   └── Presentation/             # Presentation Layer
│       ├── SanatorioHMS.Desktop/  # WPF Desktop App
│       └── SanatorioHMS.Web/      # Blazor Web App (futuro)
├── tests/                        # Testing
│   ├── SanatorioHMS.UnitTests/
│   └── SanatorioHMS.IntegrationTests/
├── database/                     # Base de datos
│   └── migrations/               # EF Core migrations
├── docs/                         # Documentación
├── .github/workflows/            # GitHub Actions
├── .gitignore
├── README.md
└── LICENSE
```

---

## 🚀 Primeros Pasos

### Prerrequisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server 2022](https://www.microsoft.com/sql-server/sql-server-downloads) (o SQL Server Express)
- [Visual Studio 2022](https://visualstudio.microsoft.com/downloads/) o [VS Code](https://code.visualstudio.com/) con extensiones C#
- [Git](https://git-scm.com/downloads)

### Clonar el Repositorio

```bash
git clone https://github.com/SbrekDev/HospitalManagementSystem.git
cd HospitalManagementSystem
```

### Configurar la Cadena de Conexión

1. Copiar `appsettings.Development.json` de ejemplo:

```bash
# En la carpeta src/SanatorioHMS.Api
cp appsettings.Development.json.example appsettings.Development.json
```

2. Editar la cadena de conexión:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=SanatorioHMS;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

### Restaurar y Compilar

```bash
# Restaurar paquetes
dotnet restore src/SanatorioHMS.sln

# Compilar
dotnet build src/SanatorioHMS.sln

# Compilar en Release
dotnet build src/SanatorioHMS.sln --configuration Release
```

### Ejecutar la Aplicación

```bash
# API (desarrollo)
cd src/SanatorioHMS.Api
dotnet run

# WPF Desktop
cd src/SanatorioHMS.Desktop
dotnet run
```

---

## 🗄️ Base de Datos

### Migraciones con EF Core

```bash
# Crear una nueva migración
dotnet ef migrations add InitialCreate --project src/Infrastructure/SanatorioHMS.Infrastructure.Data --startup-project src/Api/SanatorioHMS.Api

# Aplicar migraciones
dotnet ef database update --project src/Infrastructure/SanatorioHMS.Infrastructure.Data --startup-project src/Api/SanatorioHMS.Api

# Generar script SQL
dotnet ef migrations script --project src/Infrastructure/SanatorioHMS.Infrastructure.Data --output database/migrations/script.sql
```

### Scripts de Base de Datos

Los scripts SQL se encuentran en `database/scripts/`:

- `database/scripts/seed/` - Datos iniciales
- `database/scripts/upgrade/` - Scripts de upgrade
- `database/scripts/maintenance/` - Scripts de mantenimiento

### Diagrama de Entidades (Principales)

```
Patients ──────< Episodes ──────< Encounters
  │                                    │
  │                                    ├──── ClinicalNotes
  │                                    ├──── Orders
  │                                    └──── DiagnosticOrders
  │
Professionals ───< ProfessionalSpecialties >── Specialties
  │
  └────< Agendas >────< Turns
```

---

## 🧪 Testing

### Ejecutar Tests

```bash
# Todos los tests
dotnet test src/SanatorioHMS.sln

# Con cobertura
dotnet test src/SanatorioHMS.sln --collect:"XPlat Code Coverage"

# Solo unit tests
dotnet test tests/SanatorioHMS.UnitTests

# Solo integration tests
dotnet test tests/SanatorioHMS.IntegrationTests
```

### Estándares de Testing

- **Cobertura mínima**: 80%
- **Cada feature**: unit tests + integration tests
- **Naming convention**: `[Method]_[Scenario]_[ExpectedResult]`

```csharp
[Fact]
public void CreatePatient_WithValidData_ReturnsPatientDto()
{
    // Arrange
    var command = new CreatePatientCommand { /* ... */ };
    
    // Act
    var result = _handler.Handle(command);
    
    // Assert
    result.Should().NotBeNull();
    result.Id.Should().NotBeEmpty();
}
```

---

## 🔄 CI/CD

### GitHub Actions Workflows

| Workflow | Trigger | Descripción |
|----------|---------|-------------|
| `ci.yml` | push/PR a develop y main | Build, test, analyze |
| `code-quality.yml` | schedule (daily) | Sonar analysis |
| `deploy-dev.yml` | push a develop | Deploy a ambiente dev |
| `deploy-staging.yml` | release | Deploy a staging |
| `deploy-production.yml` | tag v*.*.* | Deploy a producción |

### Pipeline de CI

```yaml
1. Checkout
2. Setup .NET 8
3. Restore packages
4. Build (Debug|Release)
5. Run tests
6. Code coverage (coverlet)
7. Analyze (dotnet format)
8. Publish artifacts
```

### Ramas y Flujo Git

```
main (production)
  │
  └── develop (integration)
        │
        ├── feature/HMS-XXX-descripcion
        ├── feature/HMS-XXX-otra-feature
        │
        └── (periódicamente) ──→ PR ──→ main (release)
```

**Convenciones de Commits** (Conventional Commits):

```bash
feat(APP): agregar búsqueda de pacientes por documento
fix(GEM): corregir error al guardar indicación
docs(API): actualizar Swagger para módulo de turnos
refactor(FAC): extraer lógica de cálculo de IVA
test(LAB): agregar tests unitarios para resultados
ci: agregar pipeline de deploy a staging
```

---

## 🤝 Contribuir

### Flujo de Contribución

1. **Fork** el repositorio
2. Crear una rama desde `develop`: `git checkout -b feature/HMS-XXX-descripcion`
3. **Commit** tus cambios siguiendo [Conventional Commits](#-commits-conventional-commits)
4. **Push** a tu fork: `git push origin feature/HMS-XXX-descripcion`
5. Abrir un **Pull Request** hacia `develop`

### Requisitos para Merge

- [ ] Code Review aprobado (mínimo 1 approval)
- [ ] Todos los tests pasando
- [ ] Cobertura >= 80%
- [ ] Sin conflictos con `develop`
- [ ] Commits descriptivos
- [ ] Documentación actualizada (si aplica)

### Configuración de IDE

**Visual Studio 2022**:
- Extensiones recomendadas:
  - [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)
  - [EF Core Power Tools](https://marketplace.visualstudio.com/items?itemName=ErikEJ.EFCorePowerTools)
  - [SonarLint](https://marketplace.visualstudio.com/items?itemName=SonarSource.SonarLintforVisualStudio2022)
- Configuración de formatter: `.editorconfig` incluido

**VS Code**:
- Extensiones:
  - `ms-dotnettools.csharp`
  - `ms-dotnettools.vscode-dotnet-runtime`
  - `dbaeumer.vscode-eslint`
  - `esbenp.prettier-vscode`

---

## 🗺️ Roadmap

### v1.0.0 - MVP (En desarrollo)
- [x] Estructura del proyecto (WU-01)
- [ ] Domain entities (WU-02, WU-03)
- [ ] Infrastructure (WU-04)
- [ ] Application layer - CQRS (WU-05)
- [ ] API REST (WU-06)
- [ ] WPF Desktop App (WU-07)
- [ ] Testing y cobertura (WU-08)

### v1.1.0 - Fase 2
- [ ] Módulo de Farmacia
- [ ] Módulo de Facturación
- [ ] Portal del Paciente

### v1.2.0 - Fase 3
- [ ] Módulo de Internación
- [ ] Módulo de Emergencias
- [ ] Integración con sistemas externos

---

## 📋 Compliance y Regulación

Este sistema cumple con las regulaciones argentinas de salud:

| Ley/Regulación | Descripción |
|----------------|-------------|
| **Ley 26.529** | Derechos del Paciente en su relación con los profesionales e instituciones de salud |
| **Ley 25.326** | Protección de Datos Personales |
| **Disp. 60-E/2016** | Regulaciones para sistemas de historia clínica electrónica |
| **Ley 26.657** | Salud Mental |
| **Ley 27.553** | Prescripción de medicamentos |
| **ITAES** | Instituto Técnico para la Acreditación de Establecimientos de Salud (2026-2029) |

---

## 📞 Contacto

| Rol | Contacto |
|-----|----------|
| **Líder Técnico** | [Tu Nombre](mailto:tu.email@sanatorioadventista.org.ar) |
| **Dirección Médica** | Dr. Edson Velozo - Director Médico |
| **Administración** | Prof. Arnoldo Schlemper - Director Financiero |

**Sanatorio Adventista del Plata**
- 🌐 https://www.sanatorioadventista.org.ar/
- 📞 +54 343 4200220
- 📍 Entre Ríos, Argentina

---

## 📄 Licencia

Este proyecto está bajo la Licencia MIT - ver el archivo [LICENSE](LICENSE) para más detalles.

---

<div align="center">

**Hospital Management System** - Sanatorio Adventista del Plata

*Más de 118 años de cuidado de la salud*

</div>
