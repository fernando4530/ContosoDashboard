---
description: "Task list for Document Upload and Management"
---

# Tasks: Document Upload and Management

**Input**: Design documents from `specs/001-document-upload-management/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`
**Tests**: Included because the specification and constitution require automated validation for validation, storage, authorization, persistence, compensation, and IDOR behavior.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare the application and test project for the document feature.

- [X] T001 Create `ContosoDashboard.Tests/ContosoDashboard.Tests.csproj` targeting `net10.0`, referencing the web project, and adding xUnit, test SDK, EF Core SQLite, and ASP.NET Core test dependencies.
- [X] T002 [P] Add `ContosoDashboard.Tests/` to the repository solution or test discovery configuration and document `dotnet test` execution in `README.md`.
- [X] T003 [P] Add document storage and scanner configuration sections with local defaults outside `wwwroot` in `ContosoDashboard/appsettings.json` and `appsettings.Development.json`.
- [X] T004 [P] Add the configured local upload root and scanner mode options to `ContosoDashboard/Properties/launchSettings.json` without committing environment-specific secrets.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish persistence, replaceable infrastructure, and authorization plumbing required by every user story.

**Critical**: No user story work can begin until this phase is complete.

- [X] T005 Add `Document`, `DocumentShare`, and `DocumentActivity` entities plus project/task association navigation properties under `ContosoDashboard/Models/`, using integer IDs, text categories/actions, required title/category, 1 to 25 MiB size validation, unique opaque `StorageKey`, `Ready`/`Pending`/`Failed` status, nullable document IDs on activity, and exactly one user-or-department share recipient.
- [X] T006 Configure document, share, activity, project, task, and user relationships, restricted/nullifying deletes, required/optional fields, and indexes for uploader/date, project/date, category, status, file type, active shares, and activity date/action in `ContosoDashboard/Data/ApplicationDbContext.cs`.
- [X] T007 Define `IFileStorageService`, `IMalwareScanner`, and document result/query contracts in `ContosoDashboard/Services/`, preserving the storage contract methods `WriteTemporaryAsync`, `PromoteAsync`, `OpenReadAsync`, and `DeleteAsync`.
- [X] T008 Implement local storage in `ContosoDashboard/Services/FileStorageService.cs` with a configurable root outside `wwwroot`, server-generated keys, root containment checks, rejection of absolute/traversal keys, and a hard 25 MiB stream limit.
- [X] T009 Implement the local/configured malware scanner in `ContosoDashboard/Services/MalwareScanner.cs`, including explicit available, rejected, and unavailable outcomes so unavailable scanning rejects the upload rather than exposing a file.
- [X] T010 Implement combined authorization primitives in `ContosoDashboard/Services/DocumentAuthorizationService.cs` for owner, administrator, authorized project membership, department/team membership, explicit user share, and active department share, re-evaluating revoked shares and current membership on every request.
- [X] T011 Register storage, scanner, authorization, and document services in `ContosoDashboard/Program.cs`, configure document options, and ensure the existing mock authentication and role policies remain the identity source.
- [X] T012 Create shared test fixtures and fake storage/scanner implementations in `ContosoDashboard.Tests/Infrastructure/` for temporary SQLite databases, seeded users/projects/memberships, compensation assertions, and controllable scanner outcomes.

**Checkpoint**: Persistence, file boundaries, scanner gating, and combined authorization are available through DI and can be tested without the UI.

---

## Phase 3: User Story 1 - Upload and Organize Documents (Priority: P1) 🎯 MVP

**Goal**: Authenticated employees can upload supported documents with required metadata, retain valid results from multi-file uploads, and find accepted documents in their personal list.

**Independent Test**: Upload a valid file as an authenticated employee, verify metadata and a file outside `wwwroot`, then mix valid, oversized, unsupported, scanner-rejected, and scanner-unavailable files and verify independent outcomes.

### Tests for User Story 1

- [X] T013 [P] [US1] Add validation tests in `ContosoDashboard.Tests/DocumentUploadValidationTests.cs` for the six categories, required title/category, supported PDF/Word/Excel/PowerPoint/TXT/JPEG/PNG formats, exact 25 MiB maximum, mismatched extension/MIME/content, and specific rejection reasons.
- [X] T014 [P] [US1] Add storage tests in `ContosoDashboard.Tests/FileStorageServiceTests.cs` for temporary writes, promotion, root containment, traversal rejection, generated keys, stream limits, and cleanup outside `ContosoDashboard/wwwroot`.
- [X] T015 [P] [US1] Add upload workflow tests in `ContosoDashboard.Tests/DocumentServiceUploadTests.cs` for scanner rejection/unavailability, independent multi-file results, successful metadata persistence, and filesystem/SQLite compensation after persistence failure.
- [X] T016 [P] [US1] Add authorization and persistence integration tests in `ContosoDashboard.Tests/DocumentAuthorizationTests.cs` for owner/project visibility, inaccessible projects, user-supplied ID/path attempts, and `Ready`-only listability.

### Implementation for User Story 1

- [X] T017 [US1] Implement upload DTOs, category values, per-file result states, and allowlist/content-signature validation in `ContosoDashboard/Services/DocumentContracts.cs` and `ContosoDashboard/Services/DocumentValidationService.cs`.
- [X] T018 [US1] Implement `DocumentService` upload and personal-list operations in `ContosoDashboard/Services/DocumentService.cs`, processing each `IBrowserFile` independently through validate, temporary write, scan, promote, persist, and compensation stages.
- [X] T019 [US1] Add upload/list page state, per-file progress and success/failure messaging, required metadata controls, category/project/tag inputs, and clear empty/error states to `ContosoDashboard/Pages/Documents.razor`.
- [X] T020 [US1] Add the authenticated employee document route and service-backed upload/list handlers in `ContosoDashboard/Pages/Documents.razor` or `ContosoDashboard/Endpoints/DocumentEndpoints.cs`, ensuring the page never queries `ApplicationDbContext` directly.
- [X] T021 [US1] Add document navigation and shared page imports for the new document workflow in `ContosoDashboard/Shared/NavMenu.razor`, `ContosoDashboard/Pages/_Imports.razor`, and `ContosoDashboard/Shared/_Imports.razor`.

**Checkpoint**: The P1 upload slice is independently usable, preserves valid partial successes, and never exposes rejected or partially persisted files.

---

## Phase 4: User Story 2 - Find, View, and Manage Accessible Documents (Priority: P1)

**Goal**: Users can search, filter, sort, preview, download, edit, replace, and delete only documents authorized by the combined permission rules.

**Independent Test**: Seed accessible and inaccessible documents, then verify authorized list/search/filter/sort/preview/download operations, owner metadata replacement/deletion, project-manager deletion, and denied IDOR requests.

### Tests for User Story 2

- [X] T022 [P] [US2] Add query tests in `ContosoDashboard.Tests/DocumentQueryTests.cs` for title/description/tag/uploader/project search, category/project/date filters, title/date/category/size sorting, empty terms, no-match states, and a 500-document result set.
- [X] T023 [P] [US2] Add endpoint security tests in `ContosoDashboard.Tests/DocumentEndpointTests.cs` for authorized download, PDF/image inline preview, unsupported preview, missing IDs, denied IDs, and responses that never reveal physical storage keys.
- [X] T024 [P] [US2] Add management tests in `ContosoDashboard.Tests/DocumentManagementTests.cs` for owner metadata edits/replacement, replacement validation, owner deletion, project-manager deletion, team-lead restrictions, and file cleanup.

### Implementation for User Story 2

- [X] T025 [US2] Implement authorized query, sort/filter/search, metadata edit, replacement, and deletion operations in `ContosoDashboard/Services/DocumentService.cs`, applying the same authorization predicate before materializing every result.
- [X] T026 [US2] Implement authenticated download and preview endpoints in `ContosoDashboard/Endpoints/DocumentEndpoints.cs` for `GET /documents/{documentId}/download` and `/preview`, using only integer IDs, validated MIME types, `attachment`/`inline` disposition, and authorization before opening storage.
- [X] T027 [US2] Add audit creation hooks for download, replacement, metadata edit, and deletion operations in `ContosoDashboard/Services/DocumentService.cs`, preserving document activity after deletion by allowing nullable `DocumentId`.
- [X] T028 [US2] Complete the browse/search/filter/sort, preview, download, metadata edit, replacement, confirmation deletion, unauthorized, and unsupported-preview states in `ContosoDashboard/Pages/Documents.razor`.
- [X] T029 [US2] Add the authorized shared-document browsing shell in `ContosoDashboard/Pages/SharedDocuments.razor` so later sharing can reuse the same view, preview, download, and empty-state components.

**Checkpoint**: Users can locate and manage accessible documents while unauthorized metadata, files, and physical paths remain undiscoverable.

---

## Phase 5: User Story 3 - Share Documents with People and Teams (Priority: P2)

**Goal**: Owners can grant and revoke explicit user/department shares, recipients can use `Shared with Me`, and access changes are reflected immediately.

**Independent Test**: Share a ready document with a user and department, verify recipient visibility and one notification per recipient, revoke membership/share, and verify non-recipients are denied while owner/admin access remains.

### Tests for User Story 3

- [ ] T030 [P] [US3] Add share authorization tests in `ContosoDashboard.Tests/DocumentSharingTests.cs` for owner-only sharing, exactly-one recipient validation, active/revoked shares, department membership changes, cumulative project-plus-share access, and non-recipient denial.
- [ ] T031 [P] [US3] Add notification integration tests in `ContosoDashboard.Tests/DocumentNotificationTests.cs` for one understandable notification per recipient and project-member notifications without exposing other recipients.

### Implementation for User Story 3

- [ ] T032 [US3] Implement share, revoke, recipient listing, and `Shared with Me` operations in `ContosoDashboard/Services/DocumentService.cs`, restricting sharing to owners, requiring `Ready` documents, and re-evaluating active permissions at request time.
- [ ] T033 [US3] Integrate `INotificationService` with share and project-document events in `ContosoDashboard/Services/DocumentService.cs`, creating recipient-scoped in-app notifications through the existing `Notification` model.
- [ ] T034 [US3] Add owner share/revoke controls, user/department recipient selection, current-share state, and shared-document actions to `ContosoDashboard/Pages/SharedDocuments.razor` and `ContosoDashboard/Pages/Documents.razor`.
- [ ] T035 [US3] Add share/revoke activity records and authorization-aware failure states to `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Pages/SharedDocuments.razor`.

**Checkpoint**: Explicit sharing expands access without removing inherited permissions, and revocation or membership changes take effect on the next request.

---

## Phase 6: User Story 4 - Use Documents from Projects, Tasks, and Dashboard (Priority: P2)

**Goal**: Authorized project/task users and dashboard users can access documents within their existing workflows.

**Independent Test**: Associate a document with a project and task, verify project/task visibility and authorized download, then verify five recent uploads and total count on the dashboard plus notifications.

### Tests for User Story 4

- [ ] T036 [P] [US4] Add project/task association tests in `ContosoDashboard.Tests/DocumentAssociationTests.cs` for task-project consistency, project-member authorization, project-manager upload, task attachment, and inaccessible project/task denial.
- [ ] T037 [P] [US4] Add dashboard aggregation tests in `ContosoDashboard.Tests/DashboardDocumentTests.cs` for five most recent uploads, total count, authorization filtering, and project/share notifications.

### Implementation for User Story 4

- [ ] T038 [US4] Implement project and task association, project-manager upload, and task attachment operations in `ContosoDashboard/Services/DocumentService.cs`, validating that an attached task belongs to the selected project.
- [ ] T039 [US4] Add authorized project document display and project-manager upload controls to `ContosoDashboard/Pages/ProjectDetails.razor`, delegating all reads and mutations to `IDocumentService`.
- [ ] T040 [US4] Add task document attachment/upload and authorized document display to `ContosoDashboard/Pages/Tasks.razor`, preserving the task's project context.
- [ ] T041 [US4] Add five recent documents and document count aggregation to `ContosoDashboard/Services/DashboardService.cs` and render the results in `ContosoDashboard/Pages/Index.razor`.
- [ ] T042 [US4] Add project-document notification dispatch for affected authorized members in `ContosoDashboard/Services/DocumentService.cs` and verify it uses the existing in-app notification preferences.

**Checkpoint**: Documents are available where project, task, and dashboard users already work, with access checks still centralized in the service layer.

---

## Phase 7: User Story 5 - Audit Document Activity (Priority: P3)

**Goal**: Administrators can inspect document activity and aggregate reports while non-administrators are denied.

**Independent Test**: Perform upload, download, deletion, and share actions, then generate administrator reports for document types, uploaders, and access patterns and verify employee denial.

### Tests for User Story 5

- [ ] T043 [P] [US5] Add audit persistence tests in `ContosoDashboard.Tests/DocumentActivityTests.cs` for upload, download, deletion, and share actor/document/action/timestamp records, including records retained after document deletion.
- [ ] T044 [P] [US5] Add report authorization and aggregation tests in `ContosoDashboard.Tests/DocumentReportTests.cs` for administrator-only access, document type frequency, active uploaders, and access patterns by selected scope.

### Implementation for User Story 5

- [ ] T045 [US5] Implement activity queries and aggregate report DTOs in `ContosoDashboard/Services/DocumentService.cs` or `ContosoDashboard/Services/DocumentReportService.cs`, restricting report operations to administrators and excluding sensitive physical paths.
- [ ] T046 [US5] Add administrator-only report endpoint/page data handlers in `ContosoDashboard/Endpoints/DocumentEndpoints.cs` and render filters, summaries, and empty/error states in `ContosoDashboard/Pages/DocumentReports.razor`.
- [ ] T047 [US5] Add the administrator document-report navigation entry and explicit access-denied behavior in `ContosoDashboard/Shared/NavMenu.razor` and `ContosoDashboard/Pages/DocumentReports.razor`.

**Checkpoint**: All required document lifecycle actions are reviewable by administrators and reporting cannot be accessed by employees.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Validate the complete feature, document local-only behavior, and address cross-cutting quality concerns.

- [ ] T048 [P] Add a documented temporary-file cleanup/reconciliation operation for startup or administration in `ContosoDashboard/Services/FileStorageService.cs` and describe its local-only crash-recovery limits in `README.md`.
- [ ] T049 [P] Review document pages and components for keyboard accessibility, labels, progress/error/empty states, and consistency with existing Bootstrap conventions in `ContosoDashboard/Pages/Documents.razor`, `ContosoDashboard/Pages/SharedDocuments.razor`, and `ContosoDashboard/Pages/DocumentReports.razor`.
- [ ] T050 [P] Add configuration and security documentation for offline local storage, scanner availability, storage outside `wwwroot`, mock authentication, and the future `IFileStorageService` replacement boundary in `README.md`.
- [ ] T051 Run `dotnet test` from the repository root for `ContosoDashboard.Tests/ContosoDashboard.Tests.csproj` and resolve feature test failures without weakening authorization, validation, compensation, or audit assertions.
- [ ] T052 Run `dotnet build` from the repository root for `ContosoDashboard/ContosoDashboard.csproj` and `ContosoDashboard.Tests/ContosoDashboard.Tests.csproj`, resolving new compiler warnings or errors in the application and test project.
- [ ] T053 Run every manual scenario in `specs/001-document-upload-management/quickstart.md`, recording any unavailable environment check and confirming the 25 MiB, 500-document, preview, and unauthorized-access acceptance paths.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; T001-T004 can begin immediately, with T002 depending on the test project being created by T001.
- **Foundational (Phase 2)**: Depends on Phase 1; blocks all user stories because it supplies the schema, storage boundary, scanner gate, authorization, DI, and test fixtures.
- **User Stories (Phases 3-7)**: Depend on Phase 2. US1 and US2 are both P1 and share the document model/service surface; complete US1 before finalizing US2 query and management behavior. US3 depends on ready documents and authorization from US1/US2. US4 reuses document operations and can begin after the shared service contracts exist, but integration validation follows US2. US5 depends on activity records produced by US1-US3.
- **Polish (Phase 8)**: Depends on the desired user stories being implemented; T048-T050 can proceed alongside final story work, while T051-T053 are final validation tasks.

### User Story Dependencies

- **US1 (P1)**: Depends on Phase 2; MVP starting point and source of upload/ready-document lifecycle.
- **US2 (P1)**: Depends on Phase 2 and the document lifecycle from US1; its query and endpoint tests must use the same authorization predicate.
- **US3 (P2)**: Depends on US1 ready documents and US2 authorized retrieval; sharing adds access but never removes project/owner/admin access.
- **US4 (P2)**: Depends on US1 upload and US2 authorized retrieval; project/task/dashboard integration remains independently testable through services.
- **US5 (P3)**: Depends on lifecycle activity from US1-US3 and the existing notification/user role model.

### Parallel Opportunities

- T002-T004 can run in parallel after T001; T007-T010 can run in parallel after the foundational entity shape is agreed.
- US1 tests T013-T016 can run in parallel, followed by T017-T021 where file/service work is separated from page/navigation work.
- US2 tests T022-T024 can run in parallel; endpoint, page, and management work can proceed in separate files after T025-T027 establish service behavior.
- US3 tests T030-T031 can run in parallel; UI and notification work can proceed after the share service contract is available.
- US4 tests T036-T037 can run in parallel; project, task, and dashboard integration tasks T039-T041 touch separate primary files.
- US5 tests T043-T044 can run in parallel; report UI and navigation work can proceed after the report service is defined.

## Parallel Example: User Story 1

```text
Task: T013 Validation tests in ContosoDashboard.Tests/DocumentUploadValidationTests.cs
Task: T014 Storage tests in ContosoDashboard.Tests/FileStorageServiceTests.cs
Task: T015 Upload workflow tests in ContosoDashboard.Tests/DocumentServiceUploadTests.cs
Task: T016 Authorization tests in ContosoDashboard.Tests/DocumentAuthorizationTests.cs
```

## Implementation Strategy

### MVP First

1. Complete Phase 1 and Phase 2.
2. Complete US1, including automated validation, storage, scanner, compensation, and personal-list checks.
3. Complete US2 to make the repository searchable and usable for daily retrieval.
4. Stop and validate the two P1 stories against `quickstart.md` before adding sharing or integrations.

### Incremental Delivery

1. Deliver US1 as the upload-and-organize increment.
2. Deliver US2 as the find/view/manage increment.
3. Deliver US3 for controlled sharing and notifications.
4. Deliver US4 for project, task, and dashboard integration.
5. Deliver US5 for administrator audit reporting.
6. Complete Phase 8 and rerun automated plus manual validation.

## Completion Criteria

- Every task uses the required checkbox, sequential ID, optional `[P]` marker, required story label in story phases, and an exact file path.
- Each user story has an independent test criterion and automated tests covering its security and persistence risks.
- The MVP is US1 plus US2 after Setup and Foundational phases.
- `dotnet test`, `dotnet build`, and the documented quickstart scenarios are executed before implementation is considered complete.