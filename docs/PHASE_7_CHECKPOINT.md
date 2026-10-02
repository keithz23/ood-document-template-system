# Phase 7A Checkpoint and Phase 6E–6G.1 Follow-up

Date: 2026-10-02
Status: **implementation verification passed; final Phase 7B pass pending**

Phase 7A was the earlier QA, cleanup, and documentation checkpoint. This update
records the subsequently approved Phase 6E–6G.1 implementation and its focused
regression evidence. It does not deploy the application, declare it
feature-complete, or approve a production release. A separate Phase 7B pass is
still required before any deployment decision.

## Scope verified

- registration, JWT login, current-user loading, local logout, `401` handling,
  fixed permission claims, and Admin/User boundaries
- active Template gallery, detail, current version, and Placeholders
- Document creation, edit, placeholder persistence, preview, finalize, history,
  reopen, and HTML download
- Admin Category, Template, TemplateVersion, Placeholder, User creation and
  management, and read-only Audit Log screens and APIs
- shared Admin TemplateVersion and author Document rich editor, including
  Published/Finalized read-only behavior and server HTML sanitization
- historical safeguards, Draft/Finalized lifecycle, Published immutability, and
  one-current-version enforcement
- desktop shell, navigation, editor layout, loading/empty/error/disabled states,
  and keyboard-accessible semantics visible in the audited flows

## Verification results

| Check | Result |
|---|---|
| `npm run lint` | Passed |
| `npm run build` | Passed; all App Router routes compiled |
| `dotnet build DocumentTemplateSystem.sln` | Passed; 0 warnings, 0 errors |
| Unit tests | 45 passed, 0 failed, 0 skipped |
| Integration tests | 46 passed, 0 failed, 0 skipped |
| EF migration verification | Database already up to date |
| Browser runtime | No new browser-side error was emitted after the final logout fix |
| `git diff --check` | Passed |

The default parallel .NET build path stalled after compiling Domain and reached
the command runner's five-minute limit without a compiler diagnostic. A clean
single-node build with shared compilation and build servers disabled completed
successfully. This is a local build-server contention issue to watch in CI, not
a source compilation failure.

The Impeccable static detector reported only the two uses of Arial in global
styles. Arial is the explicitly approved body family in `DESIGN.md`, so these
were retained. The shared editor and new forms were also reviewed against the
existing responsive and accessibility conventions. A fresh device-width
browser pass remains an explicit Phase 7B item.

## Browser regression evidence

The local frontend and API were run against PostgreSQL. The desktop author path
verified during this implementation was:

```text
login
→ templates
→ template detail
→ fill placeholders
→ create Draft
→ edit and save
→ preview
→ finalize with confirmation
→ history
→ reopen Finalized Document
→ HTML download
```

The Finalized Document rendered as read-only and its save/finalize controls were
unavailable, while server preview and HTML download remained available. Admin
navigation and Category, Template, Published version, User, and Audit Log
screens were verified with an Admin; direct Admin access as a normal User showed
the forbidden state. Logout cleared persistent credentials and auth-sensitive
query state, landed on plain `/login`, and browser back navigation exposed only
the guarded login flow. Published TemplateVersion and Finalized Document content
rendered read-only. Registration, permission claims, Admin user creation,
role/state changes, inactive-user rejection, and audit entries were additionally
verified against the live local API.

The Phase 7A mobile findings remain useful, and the implementation retains its
mobile shell, stacked table alternatives, editor pane switcher, wrapping rich
editor toolbar, and responsive dialogs. The newly changed flows were not rerun
in a dedicated device-width browser during this follow-up; that check remains
required in Phase 7B.

## Static and data-integrity review

- The retired frontend mock data/type modules were removed; source searches find
  no mock-data imports.
- API calls remain outside route/page components and server state remains in
  TanStack Query hooks.
- EF relationships that preserve history use `Restrict`; the optional live
  Placeholder reference uses `SetNull` while snapshot values remain.
- No public workflow hard-deletes historical Templates, TemplateVersions,
  Documents, Users, Categories, or AuditLogs.
- The database has a filtered unique current-version index and a check that a
  Current version is Published. Domain and Application tests cover the same
  invariant.
- The deterministic local database contains 2 active users, 4 categories, 4
  active Templates, 4 Published/current versions, 0 Documents, and 0 AuditLogs.
  A direct invariant query returned no Template with zero/multiple current
  versions or a non-Published current version.

The earlier Phase 7A verification records remain removed. This follow-up also
removed one browser-QA Document with four stored values, one browser-QA user,
two API-smoke users, and five associated audit entries. These deletions are not
recoverable from the local database, but every target was synthetic and was
resolved by exact identifier before deletion; deterministic seed data was
preserved.

## Known limitations

- Registration and local logout are implemented, but refresh, revocation,
  recovery, password changes, and profile editing are not.
- Rich editing supports the approved HTML subset; there is no file upload,
  collaborative editing, or JSON content editor.
- Admin-created users receive a required initial password, but there is no
  first-login password-change or password-expiry workflow.
- Authorization uses a fixed Admin/User permission matrix. Custom roles,
  persisted grants, and runtime permission management are not implemented.
- HTML is the only download format; PDF, DOCX, and JSON editing are absent.
- Search/filter behavior is client-side for current MVP lists. Server
  pagination and filtering are absent.
- Browser regression is manual; there is no automated frontend component or
  end-to-end suite.
- Backend integration tests use the application test host and test doubles/model
  inspection rather than a disposable real PostgreSQL instance.

## Production risks and prerequisites

The principal risks are browser-local JWT storage, no token revocation, no
login rate limiting/lockout, no production CSP/security-header policy, a
liveness-only health endpoint, and absent production containers, CI/CD,
observability, backups, and restore drills. Rich HTML is allowlist-sanitized and
previewed in a sandboxed iframe, but that does not replace deployment-level CSP
or downloaded-file threat review. Development credentials, seeding, and the
local JWT key must never be used outside a local demo.

Before a future deployment, provision and back up PostgreSQL, provide external
secrets, disable seeding, apply migrations explicitly, deploy and smoke-test the
API, build/deploy the frontend with the target API URL, then verify the full
HTTPS/CORS/auth/author/Admin path. Detailed prerequisites and order are in
`docs/DEPLOYMENT.md`.

## Documentation and architecture revisit points

Revisit these areas when future scope is approved:

| Future scope | Required documentation/architecture review |
|---|---|
| Refresh/revocation/server sessions | auth DTOs/endpoints, token storage, logout semantics, threat model, deployment secrets, diagrams, and tests |
| Password recovery/change | approved password policy, recovery token lifecycle, Admin boundaries, audit behavior, UX, and tests |
| Custom roles/runtime permissions | domain/persistence model, authorization policies, JWT claims, endpoint matrix, Admin navigation, migration, and tests |
| Additional content/export formats | DTOs, sanitizer, Composite/rendering/export boundaries, editor/preview parity, and deployment dependencies |
| Production hardening | CSP/security headers, rate limiting, readiness, real-PostgreSQL tests, automated browser E2E, CI/CD, monitoring, backups, and rollback |

`AGENTS.md`, `docs/API_CONTRACT.md`, `docs/ARCHITECTURE.md`,
`docs/DEPLOYMENT.md`, and this report must be updated together when those
decisions become approved implementation scope.

## Phase 7B readiness

The implementation is ready to enter Phase 7B. That pass must rerun the full
desktop and device-width browser matrix, perform a dedicated console/network
inspection, repeat clean-database migration verification, and make the final
deployment-readiness decision. Readiness to begin Phase 7B is not production
approval.
