# Implementation Plan: Document Upload and Management

**Branch**: `001-document-upload-management` | **Date**: 2026-09-15 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/001-document-upload-management/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Agregar un repositorio documental local y seguro al dashboard existente. La implementación
extenderá el proyecto Blazor Server con entidades EF Core para documentos, compartidos,
asociaciones y auditoría; un `DocumentService` que concentre validación y autorización; una
frontera `IFileStorageService` con almacenamiento local fuera de `wwwroot`; y páginas/handlers
autorizados para carga, búsqueda, preview, descarga, gestión y reportes. Cada archivo se
procesará de forma independiente y el flujo filesystem/SQLite usará temporales, promoción y
compensación porque no existe una transacción atómica entre ambos recursos.

## Technical Context

<!--
  ACTION REQUIRED: Replace the content in this section with the technical details
  for the project. The structure here is presented in advisory capacity to guide
  the iteration process.
-->

**Language/Version**: C# / .NET 10.0  
**Primary Dependencies**: ASP.NET Core Blazor Server, EF Core 10 SQLite, `IBrowserFile`, cookie mock authentication  
**Storage**: SQLite metadata plus local filesystem configurable under `AppData/uploads`, outside `wwwroot`  
**Testing**: xUnit test project with fake storage/scanner and temporary SQLite integration checks; build and documented manual checks  
**Target Platform**: Linux training environment and local web browser  
**Project Type**: Single web application  
**Performance Goals**: Lists/searches up to 500 authorized documents within 2 seconds; previews within 3 seconds; uploads up to 25 MB within 30 seconds excluding scanner downtime  
**Constraints**: Offline-first; 25 MiB per file; allowlisted formats; malware scan required; no user-controlled filesystem paths; existing mock identity and integer keys  
**Scale/Scope**: Existing seeded users/projects, P1 core lifecycle first, with sharing, integrations and administrative reporting in the same feature

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

* **I. Layered architecture**: PASS. Models/context, services, storage/scanner interfaces and
  Blazor/HTTP presentation remain separated and DI-registered.
* **II. Security by default**: PASS. Service queries authorize before returning metadata or
  streams; files remain outside static content; retrieval uses `DocumentId` lookup, never a
  supplied path. Combined access is owner, administrator, project/team membership or explicit
  share, and revoked membership/share is re-evaluated at request time.
* **III. Verifiable behavior**: PASS. The test plan covers allowed/denied access, validation,
  partial multi-upload, compensation and endpoint IDOR attempts.
* **IV. Offline-first**: PASS. Local storage and a local/configured scanner implementation are
  the default; future cloud storage is an interface-only replacement.
* **V. Simplicity/accessibility/traceability**: PASS. Existing Bootstrap/Blazor conventions are
  reused, error and empty states are specified, and upload/download/delete/share actions are
  audited.
* **Technical constraints**: PASS. Integer document IDs and textual categories are explicit;
  unique server-side storage keys and validated streams are required.

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)
<!--
  ACTION REQUIRED: Replace the placeholder tree below with the concrete layout
  for this feature. Delete unused options and expand the chosen structure with
  real paths (e.g., apps/admin, packages/something). The delivered plan must
  not include Option labels.
-->

```text
ContosoDashboard/
├── Data/ApplicationDbContext.cs
├── Models/
│   ├── Document.cs
│   ├── DocumentShare.cs
│   └── DocumentActivity.cs
├── Services/
│   ├── DocumentService.cs
│   ├── DocumentAuthorizationService.cs
│   ├── FileStorageService.cs
│   └── MalwareScanner.cs
├── Pages/
│   ├── Documents.razor
│   ├── SharedDocuments.razor
│   └── DocumentReports.razor
├── Endpoints/DocumentEndpoints.cs
├── Program.cs
└── appsettings.json
ContosoDashboard.Tests/
├── DocumentServiceTests.cs
├── FileStorageServiceTests.cs
└── DocumentAuthorizationTests.cs
```

**Structure Decision**: Se mantiene el único proyecto web existente y sus carpetas `Models`,
`Data`, `Services` y `Pages`; se agrega un proyecto de pruebas separado para no introducir
dependencias de test en la aplicación. Los endpoints de archivo viven en el mismo host, detrás
de autenticación/autorización, y no exponen almacenamiento estático.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Equipo derivado por `Department` | El modelo actual no tiene entidad Team; `UserService` ya define el equipo por departamento y la especificación exige shares a equipos. | Agregar una entidad Team ampliaría el dominio y las migraciones sin una fuente de membresía existente; se documenta el límite y se reevalúa si el dominio real incorpora equipos. |
