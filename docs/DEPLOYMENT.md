# Deployment Preparation

Status: **pre-deployment guidance only; Phase 7B checkpoint complete**. The
checkpoint does not deploy the system or declare it production-ready.

## Runtime prerequisites

- PostgreSQL 16-compatible database
- .NET 10 runtime for the ASP.NET Core API
- Node.js 22-compatible runtime for the Next.js frontend
- HTTPS termination and a trusted public origin for each application
- a secret/configuration provider outside source control

The root Compose file can build and run the API, frontend, and PostgreSQL for a
single-VM academic demo. It publishes the app images to Docker Hub when configured
with `.env.vm`; deploy HTTPS through a reverse proxy/domain before using real
accounts publicly. Backups, monitoring, and CI/CD are not included.

## Single-VM Docker Compose demo

1. Create the two Docker Hub repositories and copy `.env.vm.example` to
   `.env.vm`. Set the Docker Hub namespace, image tag, VM public URL, database
   password, JWT key, and Gmail App Password. Do not commit `.env.vm`.
2. Build and push from the repository root:

   ```bash
   docker login --username <DOCKERHUB_USERNAME>
   docker compose --env-file .env.vm build api frontend
   docker compose --env-file .env.vm push api frontend
   ```

3. On the VM, install Docker Engine and the Compose plugin. Copy
   `docker-compose.yml`, the `nginx/` directory, and a protected `.env.vm` to
   the VM, then pull and start:

   ```bash
   chmod 600 .env.vm
   docker login --username <DOCKERHUB_USERNAME>
   docker compose --env-file .env.vm pull
   docker compose --env-file .env.vm up -d --no-build
   docker compose --env-file .env.vm ps
   ```

   The VM firewall should allow SSH only from your IP and HTTP/HTTPS ports
   80/443. Nginx routes `/` to the frontend and `/api/` to the API. PostgreSQL
   is bound to VM loopback only. Do not expose application ports 3000/8080 or
   port 5432 publicly.

4. Apply EF Core migrations before first use. Compose does not run migrations
   automatically. The PostgreSQL host port is bound to VM loopback; create an
   SSH tunnel from the build machine and run `dotnet ef database update` using
   `Host=127.0.0.1` and the database credentials in `.env.vm`.

Set `DOMAIN_NAME=chiendos.jiramisu.tech`, `FRONTEND_ORIGIN=https://chiendos.jiramisu.tech`,
and `NEXT_PUBLIC_API_URL=/api` in `.env.vm`. The frontend and API share the same
origin, so the frontend uses the relative `/api` path.

### Enable HTTPS with Let's Encrypt

The Nginx container serves HTTP first and switches to HTTPS after the
certificate exists. Ensure the domain's A record points to this VM and Azure's
NSG allows inbound TCP 80 and 443. Let's Encrypt HTTP-01 validation requires
public access to port 80.

1. After copying the updated Compose file and `nginx/` directory to the VM,
   update `.env.vm` with the domain and HTTPS frontend origin above, then start
   or recreate the stack:

   ```bash
   sudo docker compose --env-file .env.vm up -d --no-build
   ```

2. Request the certificate (replace the email with an address you can access):

   ```bash
   sudo docker compose --env-file .env.vm --profile certbot run --rm certbot \
     certonly --webroot --webroot-path /var/www/certbot \
     --email you@example.com --agree-tos --no-eff-email \
     -d chiendos.jiramisu.tech
   ```

3. Restart Nginx so its startup script loads the new certificate and HTTPS
   configuration:

   ```bash
   sudo docker compose --env-file .env.vm restart nginx
   sudo docker compose --env-file .env.vm logs --tail 50 nginx
   ```

4. Open `https://chiendos.jiramisu.tech`. HTTP requests redirect to HTTPS.
   Certbot certificates expire, so schedule renewal and reload Nginx. For
   example, add this daily cron entry with `sudo crontab -e`, replacing the
   directory with the location of `docker-compose.yml` and `.env.vm` on the VM:

   ```cron
   17 3 * * * cd /home/azureuser/ood-document-template-system && docker compose --env-file .env.vm --profile certbot run --rm certbot renew --quiet && docker compose --env-file .env.vm restart nginx
   ```

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
| `DevelopmentEmail__FrontendBaseUrl` | development only | Frontend origin used to compose local reset URLs |
| `DevelopmentEmail__ExposePasswordResetUrlInLogs` | yes | Must be `false` outside an isolated local Development environment |
| `Email__Provider` | yes | `Development` (default) or `Smtp`; use `Smtp` only when SMTP secrets are configured |
| `Email__SmtpHost` | SMTP only | SMTP hostname; Gmail uses `smtp.gmail.com` |
| `Email__SmtpPort` | SMTP only | SMTP port; Gmail uses `587` |
| `Email__UseStartTls` | SMTP only | Enable STARTTLS; use `true` for Gmail port 587 |
| `Email__SmtpUser` | SMTP only | SMTP account email; store as a secret |
| `Email__SmtpAppPassword` | SMTP only | Provider app password; store as a secret, never as an account password |
| `Email__FromAddress` | SMTP only | Verified sender address; for Gmail demo, use the same Gmail address |
| `Email__FromName` | no | Display name for outgoing email |
| `Email__FrontendBaseUrl` | SMTP only | Public frontend origin used in password reset links |
| `RateLimiting__PublicAuthentication__PermitLimit` | yes | Per-window request limit for each remote-IP/path partition; must be positive |
| `RateLimiting__PublicAuthentication__WindowSeconds` | yes | Fixed-window duration in seconds; must be positive |

Swagger UI and deterministic seeding are restricted to Development by the
application. `appsettings.Development.json` contains local-only credentials and
a local JWT key; neither value is suitable for a shared or production system.
The default email adapter is Development-only and never sends mail. For a small
academic demo, `Email__Provider=Smtp` enables the Gmail SMTP adapter. Use a
Google App Password (with 2-Step Verification enabled), store it only in local
User Secrets or the hosting platform's secret store, and expect Gmail sending
limits and account restrictions. This adapter is suitable for a controlled
demo, not a production transactional email service. Never expose password-reset
URLs in shared or production logs.

For local SMTP testing, set secrets on the API project (do not put values in
`appsettings.Development.json`):

```bash
cd backend/src/DocumentTemplateSystem.Api
dotnet user-secrets set "Email:Provider" "Smtp"
dotnet user-secrets set "Email:SmtpUser" "your-address@gmail.com"
dotnet user-secrets set "Email:SmtpAppPassword" "your-google-app-password"
dotnet user-secrets set "Email:FromAddress" "your-address@gmail.com"
dotnet user-secrets set "Email:FrontendBaseUrl" "http://localhost:3000"
```

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

4. Publish and start the API; verify `/health/live`, `/health/ready`, security
   headers, and an authenticated smoke test.
5. Build the frontend with the production API URL, then publish and start it.
6. Verify CORS, login, author workflow, Admin authorization, HTML download, and
   audit-log creation through the public HTTPS origins.
7. Enable monitoring, alerting, backup verification, and rollback procedures
   before accepting production use.

`/health` and `/health/live` are process liveness checks. `/health/ready`
returns healthy only when EF Core can connect to PostgreSQL. Readiness does not
prove that an expected migration version is installed, and migrations do not
run automatically at API startup.

## Implemented baseline hardening

- JWT issuer, audience, positive expiry, and a signing key of at least 32 bytes
  are validated on startup; invalid settings prevent the API from starting.
- Non-Development uses HSTS. API responses include a restrictive CSP,
  `X-Content-Type-Options`, `Referrer-Policy`, `X-Frame-Options`, and a
  restrictive `Permissions-Policy`. Development Swagger is excluded from this
  middleware because its interactive assets need a different policy.
- CORS allows only configured exact frontend origins, required headers/methods,
  and exposes only `Content-Disposition` for downloads. Empty or incorrect
  production origins fail closed from the browser's perspective.
- Public login, registration, forgot-password, and reset-password endpoints use
  a per-process, fixed-window limiter partitioned by remote IP and endpoint.
  Rejections use the normal JSON error contract with `429` and
  `RATE_LIMIT_EXCEEDED`.
- Persisted/rendered rich HTML is allowlist-sanitized. Image `src` values are
  retained only for absolute HTTP or HTTPS URLs. Downloads also carry the API
  security headers.

## Production risks to resolve or accept

- Registration, client-side logout, profile editing, password change, and
  one-time password recovery are implemented. Token refresh, access-token
  revocation, server-side sessions, production mail delivery, and a stronger
  approved password policy are not implemented. Existing JWTs remain valid
  until expiry after a password change/reset.
- JWTs are stored in browser local storage, increasing impact if client-side
  script injection occurs. Session storage and token lifecycle must be
  reassessed during production authentication hardening.
- Persisted and rendered rich HTML passes through a server allowlist sanitizer,
  previews use a sandboxed iframe, and the API emits a baseline CSP. A public
  deployment still needs a threat review for downloaded HTML, externally hosted
  images, frontend headers, and any reverse-proxy header overrides.
- The implemented authentication limiter is in-memory and process-local. There
  is no account lockout, CAPTCHA, distributed limiter, proxy-aware client-IP
  configuration, or abuse monitoring. A multi-instance/public deployment must
  provide those controls at the edge or through shared state.
- Reset tokens expire after 30 minutes and are stored as hashes, but production
  delivery, monitoring, retention/cleanup, and operational abuse response still
  require decisions.
- Readiness checks PostgreSQL connectivity only; it does not verify migration
  currency, storage capacity, mail delivery, or other future dependencies.
- Automated API tests do not currently exercise a real PostgreSQL container,
  and there is no automated browser end-to-end suite.
- Production containers, CI/CD, TLS configuration, observability, retention,
  database backup/restore drills, and disaster recovery are not supplied.
- List endpoints and Audit Logs have no server pagination; growth limits need an
  operational decision.
- Admin user creation hashes the required initial password and writes an audit
  record. There is no password-expiry/first-login-change workflow.
- Permission claims use a fixed Admin/User matrix. Custom roles, persisted
  grants, or runtime permission management would require a revised domain,
  authorization, and token-claim model rather than a frontend-only change.

## Deployment gates

The current repository is appropriate for a local academic/demo environment.
Before a public production release, at minimum provide and verify:

1. production API/frontend images or equivalent immutable build artifacts;
2. CI/CD with the full lint, build, test, migration, and smoke-test gates;
3. managed PostgreSQL, encrypted backups, restore drills, capacity alerts, and
   a reviewed migration/rollback runbook;
4. a production `IEmailService` and secret-managed provider credentials;
5. HTTPS ingress, proxy/IP-forwarding policy, edge/distributed abuse controls,
   and reviewed frontend/API security headers;
6. centralized structured logs, metrics, tracing where appropriate, health
   alerts, and an incident owner;
7. an approved token lifecycle/password policy and a decision on local-storage
   JWT risk, revocation, and password-change/reset invalidation;
8. automated browser E2E coverage for the critical author/Admin paths.

## Rollback and data safety

Back up PostgreSQL before applying a migration. Treat migrations and application
versions as one release unit. Historical entities use restrictive foreign keys
and soft activation states, but those safeguards do not replace tested database
backup and restore procedures. Never enable development seeding as a production
recovery mechanism.
