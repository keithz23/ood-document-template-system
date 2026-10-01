# Phase 7 QA Checkpoint

Date: 2026-10-01  
Status: **checkpoint passed with known limitations**

This is a QA, cleanup, documentation, and deployment-readiness checkpoint. It
does not deploy the application, declare it feature-complete, or approve a
production release.

## Scope verified

- JWT login, current-user loading, `401` handling, and Admin/User boundaries
- active Template gallery, detail, current version, and Placeholders
- Document creation, edit, placeholder persistence, preview, finalize, history,
  reopen, and HTML download
- Admin Category, Template, TemplateVersion, Placeholder, User, and read-only
  Audit Log screens and APIs
- Admin rich TemplateVersion editor, including Published read-only behavior
- historical safeguards, Draft/Finalized lifecycle, Published immutability, and
  one-current-version enforcement
- desktop and mobile shell, navigation, editor layout, loading/empty/error/
  disabled states, and keyboard-accessible semantics visible in the audited
  flows

## Verification results

| Check | Result |
|---|---|
| `npm run lint` | Passed |
| `npm run build` | Passed; all App Router routes compiled |
| `dotnet build DocumentTemplateSystem.sln` | Passed; 0 warnings, 0 errors |
| Unit tests | 45 passed, 0 failed, 0 skipped |
| Integration tests | 37 passed, 0 failed, 0 skipped |
| EF migration verification | Database already up to date |
| Browser console | No warnings or errors in the regression tab |
| `git diff --check` | Passed |

The default parallel .NET build path stalled after compiling Domain and reached
the command runner's five-minute limit without a compiler diagnostic. A clean
single-node build with shared compilation and build servers disabled completed
successfully. This is a local build-server contention issue to watch in CI, not
a source compilation failure.

The Impeccable static detector reported only the two uses of Arial in global
styles. Arial is the explicitly approved body family in `DESIGN.md`, so these
were retained. Its URL scanner could not launch its own browser in this local
environment; equivalent responsive and console checks were completed in the
in-app browser.

## Browser regression evidence

The local frontend and API were run against PostgreSQL and checked at desktop
and mobile breakpoints. The verified author path was:

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

The Finalized Document rendered as read-only and its edit/save/finalize controls
were unavailable. Admin navigation and screens were verified with an Admin;
direct Admin access as a normal User showed the forbidden state. Published
TemplateVersion content rendered with `contenteditable=false` and
`aria-readonly=true`. Desktop tables/two-pane editing and mobile navigation,
stacked records, and pane switching remained usable.

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

Two QA-only Documents, their six stored values, two unreferenced Draft
TemplateVersions, their six Placeholders, and six QA audit entries were removed
from the local database. This deletion is not recoverable from that database,
but the records were verification-only; deterministic seed data was preserved.

## Known limitations

- Authentication currently provides login only. Registration and logout are
  planned for Phase 6E; refresh, revocation, recovery, and profile editing are
  not implemented.
- Author-created Documents use plain content editing. Rich-text behavior is
  planned for Phase 6F; the existing TipTap editor is Admin TemplateVersion
  scope only.
- Admins can manage existing users but cannot create users. That workflow is
  planned for Phase 6G.
- Authorization is the current Admin/User role model. Optional fine-grained
  permissions have not been approved or designed.
- HTML is the only download format; PDF, DOCX, and JSON editing are absent.
- Search/filter behavior is client-side for current MVP lists. Server
  pagination and filtering are absent.
- Browser regression is manual; there is no automated frontend component or
  end-to-end suite.
- Backend integration tests use the application test host and test doubles/model
  inspection rather than a disposable real PostgreSQL instance.

## Production risks and prerequisites

The principal risks are browser-local JWT storage, no token revocation, no
login rate limiting/lockout, no explicit HTML sanitization/CSP policy, a
liveness-only health endpoint, and absent production containers, CI/CD,
observability, backups, and restore drills. Development credentials, seeding,
and the local JWT key must never be used outside a local demo.

Before a future deployment, provision and back up PostgreSQL, provide external
secrets, disable seeding, apply migrations explicitly, deploy and smoke-test the
API, build/deploy the frontend with the target API URL, then verify the full
HTTPS/CORS/auth/author/Admin path. Detailed prerequisites and order are in
`docs/DEPLOYMENT.md`.

## Documentation and architecture revisit points

After each planned phase, revisit these areas:

| Planned scope | Required documentation/architecture review |
|---|---|
| Phase 6E register/logout | `PRODUCT.md`, auth API DTOs/endpoints, session storage, route guards, security risks, README/demo-account flow, auth diagrams and tests |
| Phase 6F client rich text | `DESIGN.md`, Document DTO semantics, sanitization/CSP, Composite/rendering boundary, preview/download parity, accessibility and responsive editor tests |
| Phase 6G Admin user creation | API contract, password initialization, audit actions, Admin form/accessibility, seed assumptions and user tests |
| Optional permissions | approved domain/claim model, authorization policies, endpoint matrix, Admin navigation, forbidden states and boundary tests |

`AGENTS.md`, `docs/API_CONTRACT.md`, `docs/ARCHITECTURE.md`,
`docs/DEPLOYMENT.md`, and this report must be updated together when those
decisions become approved implementation scope.
