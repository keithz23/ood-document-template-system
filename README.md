# Document Template System

Phase 2 domain foundation for the OOD Document Template System described in
[`AGENTS.md`](./AGENTS.md). The repository includes the approved domain model,
EF Core persistence mappings, and the required Prototype, Composite, and
Strategy implementations. Feature controllers and frontend API integration are
intentionally deferred to later phases.

## Repository layout

- `frontend/` — Next.js App Router, TypeScript, Tailwind CSS, shadcn/ui,
  TanStack Query, Axios, React Hook Form, and Zod.
- `backend/` — ASP.NET Core REST API with Domain, Application,
  Infrastructure, API, unit-test, and integration-test projects. The Domain
  project contains the Phase 2 entities, invariants, and design patterns;
  Infrastructure contains the PostgreSQL EF Core model.
- `docker-compose.yml` — local PostgreSQL service.

## Prerequisites

- Node.js 22 or later
- .NET SDK 10
- Docker with Docker Compose

## Local setup

1. Copy `.env.example` to `.env`, then start PostgreSQL:

   ```bash
   docker compose up -d postgres
   ```

2. Copy `frontend/.env.example` to `frontend/.env.local`, install packages,
   and start the frontend:

   ```bash
   cd frontend
   npm install
   npm run dev
   ```

3. Copy `backend/.env.example` values into your shell environment, then start
   the API:

   ```bash
   dotnet run --project backend/src/DocumentTemplateSystem.Api
   ```

The API exposes `/health`. In Development, Swagger UI is available at
`/swagger`.

## Verification

```bash
npm --prefix frontend run build
dotnet build backend/DocumentTemplateSystem.sln
dotnet test backend/DocumentTemplateSystem.sln
```
