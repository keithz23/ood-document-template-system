# Deployment Preparation

Status: **pre-deployment guidance only**. Phase 7 does not deploy the system or
declare it production-ready.

## Runtime prerequisites

- PostgreSQL 16-compatible database
- .NET 10 runtime for the ASP.NET Core API
- Node.js 22-compatible runtime for the Next.js frontend
- HTTPS termination and a trusted public origin for each application
- a secret/configuration provider outside source control

The root Compose file runs local PostgreSQL only. Production images, an ingress,
infrastructure-as-code, backups, monitoring, and CI/CD are not included.

## Environment variables

### API

| Variable | Required | Purpose |
|---|---:|---|
| `ASPNETCORE_ENVIRONMENT` | yes | Use a non-Development value in production |
| `ASPNETCORE_URLS` | platform-specific | API bind URL |
| `ConnectionStrings__DefaultConnection` | yes | PostgreSQL connection string |
| `Cors__AllowedOrigins__0` | yes | Exact frontend origin; add numbered entries for additional approved origins |
| `Jwt__Issuer` | yes | Expected JWT issuer |
| `Jwt__Audience` | yes | Expected JWT audience |
| `Jwt__Key` | yes | Random signing secret of at least 32 bytes, stored outside source control |
| `Jwt__ExpiresMinutes` | yes | Access-token lifetime |
| `SeedData__Enabled` | yes | Must be `false` outside local demonstration environments |

Swagger UI and deterministic seeding are restricted to Development by the
application. `appsettings.Development.json` contains local-only credentials and
a local JWT key; neither value is suitable for a shared or production system.

### Frontend

| Variable | Required | Purpose |
|---|---:|---|
| `NEXT_PUBLIC_API_URL` | yes | Browser-visible API base URL ending in `/api` |

This value is embedded into the client build. Build the frontend with the URL
for the target environment.

## Recommended future deployment order

1. Provision PostgreSQL, network access, backups, and credentials.
2. Configure production secrets with development seeding disabled.
3. Apply reviewed EF Core migrations as an explicit release step:

   ```bash
   dotnet ef database update \
     --project backend/src/DocumentTemplateSystem.Infrastructure \
     --startup-project backend/src/DocumentTemplateSystem.Api
   ```

4. Publish and start the API; verify `/health` and an authenticated smoke test.
5. Build the frontend with the production API URL, then publish and start it.
6. Verify CORS, login, author workflow, Admin authorization, HTML download, and
   audit-log creation through the public HTTPS origins.
7. Enable monitoring, alerting, backup verification, and rollback procedures
   before accepting production use.

The current `/health` route is a process liveness check only; it does not prove
database readiness. Migrations do not run automatically at API startup.

## Production risks to resolve or accept

- Registration, logout, token refresh/revocation, and account recovery are not
  implemented. Phase 6E will change the authentication surface.
- JWTs are stored in browser local storage, increasing impact if client-side
  script injection occurs. Session design should be reassessed with Phase 6E.
- Persisted HTML needs an explicit end-to-end sanitization and Content Security
  Policy review, particularly before Phase 6F expands author rich-text input.
- Login rate limiting, lockout, security headers, and operational secret
  rotation are not implemented.
- `/health` does not check PostgreSQL; readiness and liveness are not separated.
- Automated API tests do not currently exercise a real PostgreSQL container,
  and there is no automated browser end-to-end suite.
- Production containers, CI/CD, TLS configuration, observability, retention,
  database backup/restore drills, and disaster recovery are not supplied.
- List endpoints and Audit Logs have no server pagination; growth limits need an
  operational decision.
- Admin user creation is not implemented; Phase 6G must define secure initial
  password handling.
- Optional fine-grained permissions would require a revised authorization and
  token-claim model rather than a frontend-only change.

## Rollback and data safety

Back up PostgreSQL before applying a migration. Treat migrations and application
versions as one release unit. Historical entities use restrictive foreign keys
and soft activation states, but those safeguards do not replace tested database
backup and restore procedures. Never enable development seeding as a production
recovery mechanism.
