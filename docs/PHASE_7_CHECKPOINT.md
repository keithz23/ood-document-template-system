# Phase 7A Checkpoint and Phase 6E–6H Follow-up

Date: 2026-10-02
Status: **superseded by the completed Phase 7B readiness checkpoint**

Phase 7A was the earlier QA, cleanup, and documentation checkpoint. This update
records the subsequently approved Phase 6E–6H implementation and its focused
regression evidence. It does not deploy the application, declare it
feature-complete, or approve a production release. A separate Phase 7B pass
was subsequently completed. See `docs/PHASE_7B_READINESS.md` for the current
evidence and deployment-readiness verdict. Neither checkpoint deploys the
application or approves a public production release.

## Scope verified

- registration, JWT login, current-user loading, local logout, `401` handling,
  profile editing, password change/recovery, fixed permission claims, and
  Admin/User boundaries
- active Template gallery, detail, current version, and Placeholders
- Document creation, edit, placeholder persistence, preview, finalize, history,
  reopen, and HTML download
- Admin Category, Template, TemplateVersion, Placeholder, User creation,
  identity editing and management, and read-only Audit Log screens and APIs
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
| Unit tests | 47 passed, 0 failed, 0 skipped |
| Integration tests | 58 passed, 0 failed, 0 skipped |
| EF migration verification | Existing database upgraded and clean temporary database migrated successfully |
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
existing responsive and accessibility conventions. The fresh device-width
browser pass was subsequently completed in Phase 7B.

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
editor toolbar, and responsive dialogs. Phase 7B subsequently repeated the full
device matrix; the focused Phase 6H evidence below remains historical context.

Phase 6H received an additional focused browser pass on 2026-10-02. Desktop
verification covered author sign-in, `/profile`, a persisted name/email change,
immediate shell-identity refresh, local logout, the generic forgot-password
response, the generated Development reset link, and the Admin user detail/edit
dialog. The profile and Admin user detail screens were also inspected at a
390×844 emulated viewport: forms stacked cleanly, the mobile navigation trigger
remained available, and controls stayed within the viewport. The valid profile
route emitted no application console errors. Reset consumption and password
change success/logout are covered by integration tests; browser automation did
not submit the final password-change action. The deterministic author identity
and password were restored after verification.

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

The Phase 6H focused browser pass created one reset token and five related
audit rows while exercising profile, reset, and Admin identity editing. Those
exact verification-only rows were removed after their identifiers and
timestamps were inspected; the removal is not recoverable, and deterministic
seed records were preserved.

## Known limitations

- Registration, local logout, profile editing, password changes, and one-time
  password recovery are implemented. Refresh, access-token revocation,
  production email delivery, and server-side sessions are not.
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

This Phase 7A risk list has been superseded by the detailed Phase 7B assessment.
Phase 7B added baseline public-auth rate limiting, API security headers, stricter
image-source sanitization, and separate database readiness. Remaining principal
risks include browser-local JWT storage, no token revocation or lockout,
development-only reset-link delivery, no approved stronger password policy,
process-local rather than distributed limiting, and absent production
containers, CI/CD, observability, backups, and restore drills. See
`docs/PHASE_7B_READINESS.md` and `docs/DEPLOYMENT.md` for the current list.

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
| Password/auth hardening | stronger password policy, production email adapter, recovery abuse controls/retention, access-token invalidation, threat model, and tests |
| Custom roles/runtime permissions | domain/persistence model, authorization policies, JWT claims, endpoint matrix, Admin navigation, migration, and tests |
| Additional content/export formats | DTOs, sanitizer, Composite/rendering/export boundaries, editor/preview parity, and deployment dependencies |
| Production hardening | distributed/edge abuse controls, proxy/header review, automated browser E2E, CI/CD, monitoring, backups, and rollback |

`AGENTS.md`, `docs/API_CONTRACT.md`, `docs/ARCHITECTURE.md`,
`docs/DEPLOYMENT.md`, and this report must be updated together when those
decisions become approved implementation scope.

## Phase 7B follow-up

Phase 7B reran the desktop and device-width browser matrix, performed a focused
console inspection, repeated migration verification against both an existing
database and a clean temporary database, and recorded the final checkpoint
decision in `docs/PHASE_7B_READINESS.md`. That result is readiness evidence for
the current academic/demo scope, not production approval.
