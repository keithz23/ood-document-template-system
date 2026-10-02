# Document Template System

Document Template System is an academic OOD and Design Patterns project for
creating documents from governed, versioned templates. Authors can browse
active templates, create independent drafts, validate placeholders, preview,
finalize, reopen, and download HTML documents. Administrators manage
categories, templates, versions, placeholders, users, and read-only audit logs.

This repository is at a **post-Phase 6G.1 implementation checkpoint** and is
ready for a separately scoped Phase 7B QA/deployment-readiness pass. It is not
declared feature-complete or production-ready, and it has not been deployed.
Registration and local logout, client Document rich-text editing, administrator
user creation, and a fixed permission-based authorization matrix are
implemented. Custom permissions and production authentication hardening remain
outside this checkpoint.

## Architecture

- `frontend/` — Next.js App Router, TypeScript, Tailwind CSS, shadcn/ui,
  TanStack Query, Axios, React Hook Form, Zod, and a shared TipTap editor for
  Admin TemplateVersions and author Documents.
- `backend/` — ASP.NET Core REST API with Domain, Application,
  Infrastructure, and Api projects plus unit and integration tests.
- `docker-compose.yml` — local PostgreSQL 16 service only.
- `docs/` — API contract, architecture, deployment preparation, and checkpoint
  report.

The required Prototype, Composite, and Strategy patterns remain explicit in
the Domain layer. See [Architecture](docs/ARCHITECTURE.md),
[API contract](docs/API_CONTRACT.md), [product definition](PRODUCT.md), and
[design system](DESIGN.md).

## Prerequisites

- Node.js 22 or later and npm
- .NET SDK 10
- Docker with Docker Compose
- `dotnet-ef` 10 for applying migrations

## Run locally

1. Create local environment files:

   ```bash
   cp .env.example .env
   cp frontend/.env.example frontend/.env.local
   cp backend/.env.example backend/.env
   ```

   The .NET development settings already contain matching localhost defaults.
   Export values from `backend/.env` when overriding them; `dotnet run` does not
   automatically load that file.

2. Start PostgreSQL and apply the migration:

   ```bash
   docker compose up -d postgres
   dotnet ef database update \
     --project backend/src/DocumentTemplateSystem.Infrastructure \
     --startup-project backend/src/DocumentTemplateSystem.Api
   ```

3. Install packages and run the frontend in one terminal:

   ```bash
   cd frontend
   npm install
   npm run dev
   ```

4. Run the API in another terminal:

   ```bash
   set -a
   source backend/.env
   set +a
   dotnet run --project backend/src/DocumentTemplateSystem.Api
   ```

Open the frontend at [http://localhost:3000](http://localhost:3000). The API is
at `http://localhost:5000`, its liveness endpoint is `/health`, and Swagger UI
is available at `/swagger` in Development.

### Deterministic development accounts

Development seeding runs only when the environment is `Development` and
`SeedData:Enabled` is true.

| Role | Username | Email | Password |
|---|---|---|---|
| Admin | `admin` | `admin@example.test` | `Admin123!` |
| User | `author` | `author@example.test` | `User123!` |

These are local demo credentials, not production secrets. Disable development
seeding and provide a new JWT key outside Development.

## Verification

```bash
npm --prefix frontend run lint
npm --prefix frontend run build
dotnet build backend/DocumentTemplateSystem.sln
dotnet test backend/DocumentTemplateSystem.sln
git diff --check
```

The current automated backend suite contains 45 unit tests and 46 integration
tests. Browser regression coverage is currently manual; see the
[Phase 7 checkpoint](docs/PHASE_7_CHECKPOINT.md).

## Deployment status

No deployment is performed or implied by this checkpoint. The repository does
not yet include production application images, infrastructure-as-code, or a
CI/CD release pipeline. See [Deployment preparation](docs/DEPLOYMENT.md) for
prerequisites, risks, and the recommended future deployment order.
