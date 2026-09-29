# AGENTS.md
# Document Template System

This file defines implementation rules for coding agents and contributors.

The project is an OOD / Design Pattern assignment. The required architecture and patterns are part of the project requirements and must remain visible in both class design and code.

## 1. Project Goal

Build a Document Template System where:

- Admin manages Categories, Templates, Template Versions, Placeholders, Users, and Audit Logs.
- Users browse active Templates and create Documents from a selected Template Version.
- Placeholder values are validated before a Document is rendered.
- A new Document is created as an independent copy from a Template Version.
- Users may edit the cloned Document without modifying the source Template Version.
- Draft Documents may be edited.
- Finalized Documents are read-only.
- Existing Documents must not change when a Template or Template Version is updated.
- Saved Documents retain historical Placeholder information.

Required Design Patterns:

1. Creational: Prototype
2. Structural: Composite
3. Behavioral: Strategy

---

## 2. Technology Stack

### Frontend

Use:

- Next.js
- TypeScript
- App Router
- Tailwind CSS
- shadcn/ui
- TanStack Query
- Axios
- React Hook Form
- Zod

Rules:

- Use local component state for local UI state.
- Use TanStack Query for server state.
- Do not add another global state-management library unless there is a clear requirement.

### Backend

Use:

- .NET
- ASP.NET Core REST API
- C#
- Entity Framework Core
- PostgreSQL
- Swagger / OpenAPI
- JWT Authentication
- Role-based authorization

The backend exposes REST APIs only.

---

## 3. Repository Structure

Use one repository:

```text
document-template-system/
├── AGENTS.md
├── README.md
├── .gitignore
├── docker-compose.yml
├── frontend/
└── backend/
```

Do not split frontend and backend into separate repositories unless explicitly requested.

---

## 4. Backend Architecture

Use:

```text
backend/
├── DocumentTemplateSystem.sln
├── src/
│   ├── DocumentTemplateSystem.Api/
│   ├── DocumentTemplateSystem.Application/
│   ├── DocumentTemplateSystem.Domain/
│   └── DocumentTemplateSystem.Infrastructure/
└── tests/
    ├── DocumentTemplateSystem.UnitTests/
    └── DocumentTemplateSystem.IntegrationTests/
```

Dependency direction:

```text
Api
 ↓
Application
 ↓
Domain

Infrastructure
 ├── implements Domain/Application interfaces
 └── contains persistence/external concerns
```

Rules:

- Domain must not depend on Api.
- Domain must not depend on EF Core.
- Controllers must not contain business logic.
- Application services coordinate use cases.
- Infrastructure handles persistence and external concerns.
- Domain contains entities, enums, interfaces, and required design-pattern abstractions.

---

## 5. Backend Folder Structure

Recommended:

```text
DocumentTemplateSystem.Domain/
├── Entities/
│   ├── Category.cs
│   ├── Template.cs
│   ├── TemplateVersion.cs
│   ├── Placeholder.cs
│   ├── Document.cs
│   ├── DocumentPlaceholderValue.cs
│   ├── User.cs
│   └── AuditLog.cs
├── Enums/
├── Patterns/
│   ├── Prototype/
│   ├── Composite/
│   └── Strategy/
├── Interfaces/
└── Exceptions/
```

```text
DocumentTemplateSystem.Application/
├── DTOs/
├── Interfaces/
├── Services/
├── Validators/
├── Mappings/
└── UseCases/
```

```text
DocumentTemplateSystem.Infrastructure/
├── Persistence/
│   ├── AppDbContext.cs
│   ├── Configurations/
│   └── Migrations/
├── Repositories/
├── Authentication/
├── Rendering/
└── Export/
```

```text
DocumentTemplateSystem.Api/
├── Controllers/
├── Middleware/
├── Extensions/
└── Program.cs
```

Folder names may be adjusted when necessary, but layer boundaries must remain intact.

---

## 6. Frontend Structure

Use a feature-oriented structure:

```text
frontend/
├── app/
│   ├── (auth)/
│   ├── templates/
│   ├── documents/
│   ├── admin/
│   └── layout.tsx
├── components/
│   ├── ui/
│   └── shared/
├── features/
│   ├── auth/
│   ├── templates/
│   ├── documents/
│   ├── placeholders/
│   └── admin/
├── hooks/
├── lib/
├── services/
├── types/
└── schemas/
```

Frontend rules:

- API calls belong in `services/` or feature API modules.
- Do not call Axios directly from large page components.
- Use React Hook Form + Zod for forms.
- TanStack Query owns remote server state.
- Prefer feature-local components.
- Use `"use client"` only where required.

---

## 7. Approved Core Domain Model

### Category

Fields:

```text
Id
Name
IsActive
CreatedBy
CreatedAt
```

Relationship:

```text
Category 1 -> 0..* Template
```

### Template

Fields:

```text
Id
Name
CategoryId
Status
CreatedBy
CreatedAt
```

Relationship:

```text
Template 1 -> 1..* TemplateVersion
```

A newly created Template should create its initial Draft TemplateVersion unless the project requirements are changed.

### TemplateVersion

Fields:

```text
Id
TemplateId
VersionNumber
Content
ContentFormat
Status
IsCurrent
CreatedBy
PublishedBy?
PublishedAt?
CreatedAt
UpdatedAt
```

Relationships:

```text
TemplateVersion 1 -> 0..* Placeholder
TemplateVersion 1 -> 0..* Document
```

### Placeholder

Fields:

```text
Id
TemplateVersionId
Key
Label
DataType
IsRequired
DefaultValue?
```

Initial data types:

```text
Text
Number
Date
Email
```

### Document

Fields:

```text
Id
TemplateVersionId
CreatedBy
Title
Content
Status
CreatedAt
UpdatedAt
FinalizedAt?
```

Lifecycle:

```text
Draft -> Finalized
```

### DocumentPlaceholderValue

Fields:

```text
Id
DocumentId
PlaceholderId?
PlaceholderKeySnapshot
LabelSnapshot
DataTypeSnapshot
Value
```

Snapshot fields must be retained for historical integrity.

### User

Fields:

```text
Id
Username
PasswordHash
FullName
Email
Role
IsActive
CreatedAt
```

Roles:

```text
Admin
User
```

### AuditLog

Fields:

```text
Id
PerformedBy
ActionType
EntityType
EntityId
Description
CreatedAt
```

AuditLog may use `EntityType + EntityId` as a polymorphic reference.

---

## 8. Business Rules

### Authentication and Authorization

- Users must authenticate before protected actions.
- Roles are initially `Admin` and `User`.
- Admin has User capabilities plus administration capabilities.
- Only Admin may manage Templates, Template Versions, Categories, and Users.
- Authorization must be enforced on the backend.

### Template Rules

- Every Template has a name.
- Every Template belongs to one Category.
- Template statuses:
  - Draft
  - Active
  - Inactive
- Only Active Templates may create new Documents.
- Draft Templates are editable by Admin.
- Inactive Templates cannot create new Documents.
- Deactivating a Template must not affect existing Documents.
- Do not hard-delete Templates referenced by historical data.

### Template Version Rules

- A Template has one or more Template Versions.
- `(TemplateId, VersionNumber)` must be unique.
- Published Template Versions must not be modified in place.
- Editing a Published Template creates a new Template Version.
- Only Published versions may become Current.
- A Template may have at most one Current Version.
- A Draft version must not be Current.
- Existing Documents retain their original `TemplateVersionId`.
- Publishing a new version must not mutate old Documents.
- Old versions remain available for history/reference.

### Placeholder Rules

- Every Placeholder belongs to one Template Version.
- `(TemplateVersionId, Key)` must be unique.
- Placeholder has Key, Label, DataType, IsRequired, and optional DefaultValue.
- Required Placeholders require valid data before finalization.
- Values must match the configured DataType.
- Changing Placeholders on a Published Template Version creates a new Template Version.

### Document Rules

- A Document is created from one specific Template Version.
- A Document retains `TemplateVersionId`.
- A Document is independent from its source Template Version after creation.
- Updating Document content must never modify Template Version content.
- Draft Documents may be edited.
- Finalized Documents are read-only.
- Draft Documents may be saved multiple times.
- Finalization changes lifecycle state; saving alone does not.
- Document Placeholder values must be persisted.
- Existing Documents never change automatically after Template updates.

### Preview Rules

- Preview reflects current Placeholder values and editable Document content.
- Invalid required values must be reported before finalization.
- Preview must never modify the source Template Version.
- A user may return from Preview to continue editing a Draft Document.

### History Rules

- Users may view Documents they are authorized to access.
- Historical Documents retain source Template Version, creation date, stored content, and Placeholder snapshots.
- Template updates/deactivation must not destroy Document history.

---

## 9. Required Design Patterns

These patterns are mandatory.

Do not remove, bypass, or replace them without explicit approval.

### 9.1 Prototype Pattern

Purpose:

Create a new `Document` from an existing `TemplateVersion`.

Required abstraction:

```csharp
public interface IPrototype<T>
{
    T Clone();
}
```

Conceptual flow:

```text
TemplateVersion
      |
      | Clone()
      v
Document
```

Rules:

- New Documents must use the approved Prototype workflow.
- Do not mutate the source Template Version.
- A cloned Document receives a new identity.
- A cloned Document retains source `TemplateVersionId`.
- Mutable nested structures must not share mutable references with the source.
- Clone logic must be testable.
- Do not replace Prototype with ad-hoc Controller mapping.

Typical clone behavior:

```text
Document.Id                 -> new value
Document.CreatedBy          -> current user
Document.CreatedAt          -> current time
Document.Content            -> copied from source
Document.TemplateVersionId  -> source version id
```

### 9.2 Composite Pattern

Purpose:

Represent Document content using a tree of components.

Required structure:

```text
DocumentComponent
├── TextComponent
├── ImageComponent
└── SectionComponent
      └── Children: List<DocumentComponent>
```

Suggested abstraction:

```csharp
public abstract class DocumentComponent
{
    public abstract string Render();
    public abstract DocumentComponent Clone();
}
```

Rules:

- `TextComponent`, `ImageComponent`, and `SectionComponent` inherit from `DocumentComponent`.
- `SectionComponent` may contain zero or more `DocumentComponent` children.
- Leaf and composite components must share the same abstraction.
- Recursive operations belong in the Composite model.
- Composite cloning must deep-copy child components.
- Do not replace Composite with large nested conditional logic.

Persistence:

- The database does not need one table per component type.
- `TemplateVersion.Content` and `Document.Content` remain persisted content fields.
- Composite may be serialized/deserialized or used in memory during rendering.
- Do not redesign the ERD solely to mirror Composite classes unless explicitly requested.

### 9.3 Strategy Pattern

Purpose:

Validate Placeholder values based on Placeholder DataType.

Required interface:

```csharp
public interface IPlaceholderValidationStrategy
{
    bool Validate(string value);
}
```

Initial strategies:

```text
TextValidationStrategy
NumberValidationStrategy
DateValidationStrategy
EmailValidationStrategy
```

Use a `PlaceholderValidator` context to resolve and execute the proper strategy.

Rules:

- Placeholder validation must use Strategy.
- Each supported DataType has its own strategy.
- Do not implement all validation in one large `switch` or `if/else`.
- Adding a new DataType should require adding a new strategy with minimal changes.
- Strategy resolution should be centralized.

---

## 10. Content Format Policy

The domain currently allows:

```text
Html
Json
```

For the initial implementation:

- Use `Html` as the default persisted content format.
- Do not implement full JSON editor support unless explicitly requested.
- Keep `ContentFormat` extensible.
- Composite may render to HTML for persistence and preview.
- Do not build two complete rendering systems during the MVP.

---

## 11. Database Constraints

Enforce at least:

```text
User.Username UNIQUE
User.Email UNIQUE
TemplateVersion(TemplateId, VersionNumber) UNIQUE
Placeholder(TemplateVersionId, Key) UNIQUE
```

Also:

- Each Template has at most one Current Template Version.
- Configure foreign keys explicitly.
- Prefer soft-deactivation over destructive deletion where history exists.
- Avoid cascade delete when it could destroy historical Documents or Template Versions.
- Add indexes for foreign keys and common lookup fields.

---

## 12. Entity Framework Core Rules

Prefer `IEntityTypeConfiguration<T>` configurations.

Recommended:

```text
Infrastructure/Persistence/Configurations/
├── CategoryConfiguration.cs
├── TemplateConfiguration.cs
├── TemplateVersionConfiguration.cs
├── PlaceholderConfiguration.cs
├── DocumentConfiguration.cs
├── DocumentPlaceholderValueConfiguration.cs
├── UserConfiguration.cs
└── AuditLogConfiguration.cs
```

Rules:

- Do not place every persistence concern in entity classes.
- Do not create migrations until entity/configuration changes are internally consistent.
- Do not manually edit generated migrations without a specific reason.

---

## 13. Repository Rules

Possible repository abstractions:

```text
ITemplateRepository
IDocumentRepository
IPlaceholderRepository
IUserRepository
IAuditLogRepository
```

Rules:

- Do not create a repository for every class automatically.
- Avoid unnecessary generic-repository complexity.
- Repositories should provide useful persistence abstraction.

---

## 14. Application Services

Expected services may include:

```text
TemplateService
DocumentService
PlaceholderValidator
AuthenticationService
AuditLogService
```

Example responsibilities:

### TemplateService

```text
GetTemplates
GetTemplateById
CreateTemplate
CreateNewVersion
PublishVersion
SetCurrentVersion
DeactivateTemplate
```

### DocumentService

```text
CreateDocument
UpdateDocument
SetPlaceholderValue
PreviewDocument
FinalizeDocument
GetDocumentById
GetUserDocuments
```

Do not place these workflows directly in Controllers.

---

## 15. API Conventions

Base prefix:

```text
/api
```

Typical groups:

```text
/api/auth
/api/templates
/api/template-versions
/api/documents
/api/categories
/api/users
/api/admin
```

HTTP semantics:

```text
GET     read
POST    create/action
PUT     full update when appropriate
PATCH   partial update
DELETE  only when deletion is truly allowed
```

Use standard status codes:

```text
200 OK
201 Created
204 No Content
400 Bad Request
401 Unauthorized
403 Forbidden
404 Not Found
409 Conflict
422 Unprocessable Entity
500 Internal Server Error
```

Rules:

- Use consistent response contracts.
- Validation errors should be explicit and machine-readable.
- Do not expose stack traces.
- Do not invent feature endpoints before that feature's API contract is approved.

---

## 16. Authentication and Security

- Passwords must be securely hashed.
- Never store plaintext passwords.
- JWT secrets come from environment/configuration.
- Never commit secrets.
- Backend authorization is mandatory.
- Admin endpoints require Admin role.
- Validate all client input.
- Frontend validation is UX, not security.
- Do not build SQL from user input.

---

## 17. Validation

Use validation at multiple appropriate layers:

```text
API DTO validation
Business-rule validation
Placeholder Strategy validation
Database constraints
```

Do not rely on only one layer.

Placeholder type validation must follow Strategy Pattern rules.

---

## 18. Audit Logging

Important Admin actions should create AuditLog records.

At minimum consider:

```text
Create Template
Update Template
Deactivate Template
Create Template Version
Publish Template Version
Set Current Version
Manage Category
Manage User
```

AuditLog should capture:

```text
PerformedBy
ActionType
EntityType
EntityId
Description
CreatedAt
```

Prefer centralized application/service behavior over duplicated Controller logging.

---

## 19. Testing Requirements

### Prototype tests

Verify:

- clone creates a different Document instance
- new Document receives a new Id
- TemplateVersion content is not mutated
- TemplateVersionId is retained
- mutable nested content is copied independently

### Composite tests

Verify:

- leaf components render correctly
- Section renders children
- nested sections render recursively
- Section cloning deep-copies children

### Strategy tests

Verify:

```text
Text validation
Number validation
Date validation
Email validation
Required field behavior
```

### Business-rule tests

Examples:

```text
Published TemplateVersion cannot be edited in place
Only Published version may become Current
A Template cannot have two Current versions
Finalized Document cannot be edited
Inactive Template cannot create a new Document
```

Do not write tests only for getters/setters.

---

## 20. Coding Conventions

### C#

- Enable nullable reference types.
- Use async APIs for I/O.
- Suffix async methods with `Async`.
- Use dependency injection.
- Prefer small focused classes.
- Avoid static mutable state.
- Avoid giant service classes.
- Use meaningful domain names.
- Do not swallow exceptions.

### TypeScript

- Do not use `any` unless documented.
- Prefer explicit API/domain types.
- Keep UI components focused.
- Keep server-state logic in TanStack Query hooks/services.
- Avoid duplicated API transformation logic.

---

## 21. Error Handling

Use centralized backend error handling.

Useful error/domain cases may include:

```text
TemplateNotFound
TemplateVersionNotFound
InvalidPlaceholderValue
TemplateInactive
DocumentFinalized
VersionConflict
UnauthorizedDocumentAccess
```

Do not scatter repetitive try/catch blocks across Controllers.

---

## 22. Logging

Use structured logging.

Do not log:

- passwords
- JWT tokens
- secret keys
- sensitive raw authentication data

Useful context:

```text
TemplateId
TemplateVersionId
DocumentId
UserId
Action
```

---

## 23. Git and Commit Rules

Keep commits focused.

Suggested prefixes:

```text
feat:
fix:
refactor:
test:
docs:
chore:
```

Examples:

```text
chore: initialize frontend and backend architecture
feat: add template version domain model
feat: implement placeholder validation strategies
feat: create document using prototype pattern
test: add prototype deep-copy tests
```

---

## 24. Agent Workflow

For every substantial task:

1. Read this `AGENTS.md`.
2. Inspect the existing repository before creating files.
3. Identify affected layers.
4. Preserve approved domain rules and required patterns.
5. Make the smallest coherent change.
6. Add/update tests.
7. Run relevant build/test/format commands.
8. Report:
   - files changed
   - behavior added/changed
   - tests run
   - unresolved issues

Do not silently redesign the architecture.

---

## 25. First Implementation Order

### Phase 1 - Scaffold only

Create:

```text
Next.js frontend
ASP.NET Core backend solution
Domain/Application/Infrastructure/Api projects
unit and integration test projects
PostgreSQL configuration
EF Core
Swagger
CORS
health endpoint
environment templates
```

Do not implement business features yet.

### Phase 2 - Domain Foundation

Implement:

```text
Enums
Entities
Database constraints/configurations
Prototype interfaces/classes
Composite abstractions/classes
Strategy interfaces/classes
```

Add tests.

### Phase 3 - First Vertical Slice

Implement:

```text
View Active Templates
        ↓
Select Template
        ↓
Load Current TemplateVersion
        ↓
Load Placeholders
        ↓
Enter Placeholder Values
        ↓
Validate using Strategy
        ↓
Clone using Prototype
        ↓
Build/render Document
        ↓
Create Draft
        ↓
Preview/Edit
        ↓
Save
```

Do not implement the entire Admin system before this flow works end-to-end.

### Phase 4 - Administration

Then implement:

```text
Category management
Template management
Template Version management
Publish / Current Version
User management
Audit Log
```

### Phase 5 - Finalization and Export

Then implement:

```text
Finalize Document
Document History
Download / Export
additional ContentFormats if required
```

---

## 26. Things Agents Must NOT Do

Do not:

- remove Prototype, Composite, or Strategy
- bypass required pattern abstractions
- mutate Published Template Versions
- edit Template content when editing a Document
- allow Finalized Documents to be modified
- hard-delete historical data casually
- put business logic in Controllers
- put EF Core dependencies in Domain
- create one huge `switch` for Placeholder validation
- add unnecessary patterns only to appear sophisticated
- add microservices, message brokers, CQRS, or event sourcing unless explicitly requested
- create separate frontend/backend repositories
- add dependencies without a clear need
- commit secrets
- invent new business rules without approval
- rewrite existing architecture without explaining why

---

## 27. Decision Priority

When implementation choices conflict, use this priority:

```text
1. Approved Business Rules
2. Approved ERD / Class Diagram
3. Required Design Patterns
4. This AGENTS.md
5. Existing implementation conventions
6. Simplicity / minimal complexity
```

If a requested implementation conflicts with an approved rule or diagram, surface the conflict instead of silently choosing another design.

---

## 28. MVP Principle

The goal is a clear, defensible OOD project, not maximum architecture complexity.

Prefer:

```text
simple
explicit
testable
easy to explain
aligned with diagrams
```

over:

```text
clever
over-engineered
framework-heavy
pattern-heavy without a real use case
```

Every important class and abstraction should be explainable during the project presentation.

---

## 29. Recommended First Coding-Agent Prompt

After committing this file, use:

```text
Initialize the Document Template System repository according to AGENTS.md.

Create:
- a Next.js TypeScript frontend under /frontend
- an ASP.NET Core REST API solution under /backend
- Domain, Application, Infrastructure, and Api backend projects
- unit and integration test projects
- PostgreSQL + EF Core configuration
- Swagger/OpenAPI
- CORS configuration
- health-check endpoint
- .env.example / development configuration templates
- root docker-compose.yml for PostgreSQL

Do not implement business features yet.
Do not change the approved domain model or required design patterns.
Run available builds/tests and report the resulting repository structure.
```

---

## 30. Final Rule

The codebase must remain consistent with project documentation.

If the ERD, Class Diagram, Business Rules, and code disagree, do not silently choose one.

Identify the conflict and request a decision before making a breaking domain change.
