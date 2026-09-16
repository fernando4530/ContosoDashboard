<!--
Sync Impact Report
- Version change: scaffold with no version -> 1.0.0
- Modified principles: five placeholder principles -> five ContosoDashboard principles
- Added sections: Technical Constraints; Development Workflow
- Removed sections: none
- Follow-up TODOs: none
-->

# ContosoDashboard Constitution

## Core Principles

### I. Layered Architecture and Abstractions
Features MUST preserve the separation between Pages, Services, Data, and Models. Business
logic MUST live in services, dependencies MUST be registered through dependency injection,
and infrastructure that may change between offline and cloud environments MUST be accessed
through interfaces. This keeps training implementations replaceable without rewriting the
application behavior.

### II. Security and Authorization by Default
Every protected page and service operation MUST enforce authentication and authorization at
the point where data is accessed. Queries and mutations MUST prevent cross-user and
cross-project access, including IDOR scenarios. Secrets MUST NOT be committed, uploaded
files MUST remain outside web-accessible directories, and user-controlled paths MUST never
be used directly for storage or retrieval.

### III. Verifiable Behavior
Every feature or defect fix MUST define observable acceptance criteria and MUST be validated
with the narrowest relevant automated test, build, or documented manual check. Security,
authorization, persistence, and file-handling changes MUST include checks for both allowed
and denied paths. A change is not complete when it only compiles; its user-visible behavior
and failure handling must be demonstrated.

### IV. Offline-First Training Scope
The application MUST run locally without cloud services or external service dependencies.
New infrastructure integrations MUST have a local implementation and an explicit interface
boundary. Training-only authentication, seeded data, and simplified persistence MUST remain
clearly identified so they are not presented as production security or operational guarantees.

### V. Simplicity, Accessibility, and Traceability
Changes MUST use the smallest design that satisfies the requirement and MUST avoid duplicating
business rules across UI and services. User-facing workflows MUST provide clear validation,
success, and error states and remain usable with the existing application conventions. Actions
that affect security, documents, or shared data MUST produce sufficient logs or audit records
for the intended training scenario.

## Technical Constraints

The project MUST target the configured .NET framework and use ASP.NET Core, Blazor Server,
Entity Framework Core, and SQLite consistently with the existing application. Database keys
and persisted values MUST follow existing model conventions unless a feature specification
explicitly approves a migration. File uploads MUST validate size and type, generate a unique
server-side path before persistence, save outside `wwwroot`, and apply authorization on every
download or preview path. Documentation MUST state when a behavior is mock, local-only, or
unsuitable for production.

## Development Workflow

Each change MUST start from a written requirement or reproducible defect and MUST identify
its affected layer and authorization impact. Implementation MUST preserve existing public
contracts unless a breaking change is explicitly documented. Before review, contributors MUST
run the project build and the most relevant automated checks, inspect warnings and failures,
and document any unavailable checks. Reviews MUST verify security boundaries, persistence
behavior, error handling, and consistency with this constitution.

## Governance
<!-- Example: Constitution supersedes all other practices; Amendments require documentation, approval, migration plan -->

This constitution supersedes conflicting project guidance. Amendments MUST state the reason,
affected principles or sections, compatibility impact, and any migration or follow-up work.
The amendment MUST update the Sync Impact Report, version, and last-amended date. Versioning
uses semantic rules: MAJOR for incompatible governance changes or removals, MINOR for new or
materially expanded principles, and PATCH for clarifications or non-semantic corrections.
Every implementation plan and review MUST check compliance with the constitution; exceptions
MUST be documented with an owner, rationale, and expiration or removal condition.

**Version**: 1.0.0 | **Ratified**: 2025-12-15 | **Last Amended**: 2026-09-15
<!-- Example: Version: 2.1.1 | Ratified: 2025-06-13 | Last Amended: 2025-07-16 -->
