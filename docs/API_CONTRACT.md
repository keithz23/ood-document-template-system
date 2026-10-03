# API Contract — Integrated Authoring and Administration

Status: **approved implementation contract**.

This document is the approved REST contract through Phase 6H and the completed
Phase 7B QA/deployment-readiness checkpoint. It covers the author workflow,
registration and local logout,
Category, Template, TemplateVersion, Placeholder, User, and read-only Audit Log
administration, Admin user creation/editing, self-profile management, password
change/recovery, and the fixed permission matrix. Phase 7 does not declare the
API feature-complete or production-ready.

Token refresh, custom roles, and runtime permission management remain outside
this contract. They require contract updates before implementation.

`AGENTS.md` section 7 is the only approved domain-model source currently present
in the repository. No separate ERD or Class Diagram file was found. If one is
added later and conflicts with this document, the decision priority in
`AGENTS.md` applies and this contract must be revised before implementation.

## 1. Contract boundaries

- Base path: `/api`
- Media type for JSON: `application/json`
- Authentication: JWT bearer token in `Authorization: Bearer <token>`
- Protected actions use the code-defined permission policies in section 18.
  An authenticated caller without the required permission receives
  `403 Forbidden`; an unauthenticated caller receives `401 Unauthorized`.
- Admin receives ordinary user capabilities here. This contract does **not**
  grant Admin cross-user document access.
- Dates and timestamps use ISO 8601. Timestamps are UTC, for example
  `2026-09-28T08:42:00Z`. Date placeholder values use `YYYY-MM-DD`.
- Identifiers are opaque JSON strings. Examples use UUIDs, but clients must not
  parse identifiers or assume a particular identifier format.
- Enum values are serialized as the exact strings shown in this document.
- Successful endpoints return their DTO directly; there is no generic `data`
  envelope.
- API DTOs are application-boundary contracts. They are not domain entities and
  must be mapped in the Application layer.
- Empty gallery/history results return `200 OK` with `[]`, not `404`.
- The current frontend filters template and document lists in memory. Search,
  category filtering, status filtering, sorting, and pagination are therefore
  intentionally absent from the server contract in this first slice.
- Public authentication mutations are rate-limited per remote-IP/path
  partition. A rejected request returns the shared error contract with
  `429 Too Many Requests`, code `RATE_LIMIT_EXCEEDED`, and may include a
  `Retry-After` response header.

## 2. Integration decisions and remaining constraints

The original frontend mock shape exposed several conflicts with the approved
domain. Integration resolved them at the DTO/UI boundary as recorded below;
the mock modules are no longer part of the application. These decisions must
not be silently re-encoded into entities or persistence.

1. **Template description:** the original mock summaries required
   `description`, but the approved `Template` and
   `TemplateVersion` models contain no such field. The normative DTOs below do
   not expose a description. The integrated frontend omits that copy unless the
   domain model is explicitly amended or an approved projection source is
   identified.
2. **Template updated date:** the original mock exposed `updatedAt`, but
   `Template` has no `UpdatedAt`. The contract exposes the current version's
   `updatedAt` under `currentVersion`; it must not be mislabeled as a template
   modification timestamp.
3. **Fixed categories:** the original mock hard-coded `Business`, `Finance`,
   `Human resources`, and `Operations` as a TypeScript union. The approved model
   treats Category as managed data. DTOs therefore return `{ id, name }`; the
   client must not assume a closed category enum.
4. **Placeholder examples:** the original mock had an `example` field, but
   the approved Placeholder has only `DefaultValue?`. The API does not expose
   `example`. A default value may be shown as a default, not relabeled as an
   example.
5. **Identifier shape:** original mock IDs were readable slugs. The
   approved model specifies only `Id`. API IDs remain opaque.
6. **Placeholder-value shape:** the original mock used
   `Record<placeholderKey, value>`.
   The approved model requires `PlaceholderId?`, key/label/data-type snapshots,
   and value. Document responses therefore return an array of snapshot DTOs.
   A frontend adapter may build a key/value map for form state but must retain
   the full DTO for history.
7. **Invalid finalized examples:** some original mocked Finalized documents had
   empty placeholder maps even though their source templates contain required
   fields. Such records violate the approved finalization rules and are not
   valid API examples.
8. **Preview behavior:** the original mock locally substituted values and left
   tokens visible when required values were absent. The approved rules require
   placeholder validation before rendering. The API preview operation therefore
   returns `422` rather than rendering invalid required or typed values.
9. **Content representation:** original mock content was plain text containing
   `{{placeholder_key}}` tokens while the approved initial persisted format is
   `Html`. The API uses `contentFormat: "Html"`. The Admin template editor now
   shares the approved TipTap surface with the author Document editor. Both use
   the same HTML transport format; author edits persist only to the cloned
   Document and never mutate the source TemplateVersion.
10. **Download format (resolved):** HTML is the only approved MVP export
    format. Downloads use the `.html` extension and
    `text/html; charset=utf-8`. PDF and DOCX are outside this slice.
11. **Download lifecycle (resolved):** both Draft and Finalized documents may
    be downloaded. The mock's former Finalized-only presentation is not a
    business rule.
12. **User identity:** `/api/auth/me` supplies the authenticated shell identity;
    the original hard-coded identity has been removed.
13. **Rich HTML safety:** author and Admin rich-text editors use the approved
    HTML subset. The API sanitizes persisted and rendered HTML at the trust
    boundary. Script, event-handler, unsafe URL, and unsupported markup are not
    preserved.
14. **Logout:** access tokens are stateless and there is no refresh-token store.
    Logout is therefore a client operation that clears the access token,
    identity, and authenticated query cache; no logout endpoint is defined.
15. **Permissions:** permissions are fixed in code and derived from `Admin` or
    `User`. They are returned by login, registration, and `/api/auth/me`; they
    are not persisted as a new domain model or editable through an API.
16. **Password rule:** the current project rule requires a non-empty password;
    Phase 6H reuses it consistently and does not silently introduce a new
    complexity policy. Production policy hardening remains documented work.
17. **Password-change session behavior:** successful self-service password
    change returns `204`; the frontend clears its JWT and authenticated cache
    and redirects to login. Existing stateless JWTs cannot be revoked by this
    operation and remain valid until expiry.
18. **Recovery privacy:** forgot-password returns the same generic response for
    unknown, inactive, and active accounts. Only an active matching account
    receives a reset token. Reset also requires the account to remain active.
19. **Rich-image sources:** server sanitization retains an image `src` only when
    it is an absolute HTTP or HTTPS URL. Relative, `data:`, scriptable, and
    unsupported image sources are removed before persistence or rendering.

## 3. Shared DTOs

### 3.1 ErrorResponseDto

All JSON errors use one machine-readable shape. Stack traces and sensitive
authentication details are never returned.

```json
{
  "status": 422,
  "code": "INVALID_PLACEHOLDER_VALUE",
  "title": "One or more placeholder values are invalid.",
  "detail": "Correct the reported values and try again.",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "errors": [
    {
      "field": "placeholderValues[client_email]",
      "code": "INVALID_EMAIL",
      "message": "Client email must be a valid email address.",
      "placeholderKey": "client_email"
    }
  ]
}
```

Fields:

| Field | Type | Required | Meaning |
|---|---|---:|---|
| `status` | integer | yes | HTTP status repeated for clients |
| `code` | string | yes | Stable application error code |
| `title` | string | yes | Short user-safe summary |
| `detail` | string or null | no | User-safe recovery detail |
| `traceId` | string | yes | Correlation identifier; never a stack trace |
| `errors` | `ValidationErrorDto[]` | no | Field-level errors |

`ValidationErrorDto` contains `field`, `code`, `message`, and optional
`placeholderKey`.

Rate-limit rejection example:

```json
{
  "status": 429,
  "code": "RATE_LIMIT_EXCEEDED",
  "title": "Too many authentication requests.",
  "detail": "Wait before trying again.",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "errors": null
}
```

### 3.2 Enum contracts

```text
Role: User | Admin
TemplateStatus: Draft | Active | Inactive
TemplateVersionStatus: Draft | Published
DocumentStatus: Draft | Finalized
PlaceholderDataType: Text | Number | Date | Email
ContentFormat: Html | Json
```

`Json` remains representable for forward compatibility but is not accepted for
new or updated content in this initial slice.

### 3.3 UserDto

```json
{
  "id": "8d1471f4-e867-44f0-876f-4008626fc51e",
  "username": "maya.chen",
  "fullName": "Maya Chen",
  "email": "maya@example.test",
  "role": "User",
  "permissions": [
    "Templates.View",
    "Documents.Create",
    "Documents.ViewOwn",
    "Documents.EditOwn"
  ]
}
```

`PasswordHash`, `IsActive`, and internal authentication data are never exposed.

### 3.4 CategoryReferenceDto

```json
{
  "id": "96fce3e3-cf1e-40c7-9bd0-a880d7e2cc46",
  "name": "Business"
}
```

### 3.5 CurrentTemplateVersionSummaryDto

```json
{
  "id": "f70b6a83-39a2-47ba-9963-adc13a4e1eb3",
  "versionNumber": 3,
  "status": "Published",
  "isCurrent": true,
  "contentFormat": "Html",
  "updatedAt": "2026-09-22T09:30:00Z",
  "placeholderCount": 6
}
```

### 3.6 TemplateGalleryItemDto

```json
{
  "id": "eb02cdda-021d-4b18-97cc-72c2a90d8618",
  "name": "Professional services agreement",
  "status": "Active",
  "category": {
    "id": "96fce3e3-cf1e-40c7-9bd0-a880d7e2cc46",
    "name": "Business"
  },
  "currentVersion": {
    "id": "f70b6a83-39a2-47ba-9963-adc13a4e1eb3",
    "versionNumber": 3,
    "status": "Published",
    "isCurrent": true,
    "contentFormat": "Html",
    "updatedAt": "2026-09-22T09:30:00Z",
    "placeholderCount": 6
  }
}
```

### 3.7 TemplateDetailDto

```json
{
  "id": "eb02cdda-021d-4b18-97cc-72c2a90d8618",
  "name": "Professional services agreement",
  "status": "Active",
  "category": {
    "id": "96fce3e3-cf1e-40c7-9bd0-a880d7e2cc46",
    "name": "Business"
  },
  "createdAt": "2026-08-01T10:00:00Z",
  "currentVersion": {
    "id": "f70b6a83-39a2-47ba-9963-adc13a4e1eb3",
    "versionNumber": 3,
    "status": "Published",
    "isCurrent": true,
    "contentFormat": "Html",
    "updatedAt": "2026-09-22T09:30:00Z",
    "placeholderCount": 6
  }
}
```

### 3.8 TemplateVersionDetailDto

```json
{
  "id": "f70b6a83-39a2-47ba-9963-adc13a4e1eb3",
  "templateId": "eb02cdda-021d-4b18-97cc-72c2a90d8618",
  "versionNumber": 3,
  "content": "<h1>Professional services agreement</h1><p>This agreement is made on {{effective_date}} between {{provider_name}} and {{client_name}}.</p>",
  "contentFormat": "Html",
  "status": "Published",
  "isCurrent": true,
  "publishedAt": "2026-09-22T09:00:00Z",
  "createdAt": "2026-09-20T08:00:00Z",
  "updatedAt": "2026-09-22T09:30:00Z"
}
```

### 3.9 PlaceholderDto

```json
{
  "id": "e12b0c4a-f75c-4e35-a9dd-b07db6d51cdf",
  "templateVersionId": "f70b6a83-39a2-47ba-9963-adc13a4e1eb3",
  "key": "effective_date",
  "label": "Effective date",
  "dataType": "Date",
  "isRequired": true,
  "defaultValue": null
}
```

### 3.10 PlaceholderValueInputDto

Request-only DTO:

```json
{
  "placeholderId": "e12b0c4a-f75c-4e35-a9dd-b07db6d51cdf",
  "value": "2026-09-30"
}
```

The client sends IDs, not labels or snapshot fields. The server creates and
maintains snapshots from the source Placeholder when values are persisted.

### 3.11 DocumentPlaceholderValueDto

```json
{
  "id": "89d4191d-8344-4367-8dd7-9dcd75cc16cc",
  "placeholderId": "e12b0c4a-f75c-4e35-a9dd-b07db6d51cdf",
  "placeholderKeySnapshot": "effective_date",
  "labelSnapshot": "Effective date",
  "dataTypeSnapshot": "Date",
  "value": "2026-09-30"
}
```

`placeholderId` is nullable for historical integrity if the source Placeholder
can no longer be referenced. Snapshot fields remain required.

### 3.12 DocumentSourceDto

```json
{
  "templateId": "eb02cdda-021d-4b18-97cc-72c2a90d8618",
  "templateName": "Professional services agreement",
  "templateVersionId": "f70b6a83-39a2-47ba-9963-adc13a4e1eb3",
  "versionNumber": 3
}
```

This is a read projection. It does not add duplicated fields to the Document
entity.

### 3.13 DocumentSummaryDto

```json
{
  "id": "b4382058-feb4-4cd4-b8bf-627a13822161",
  "title": "Acme consulting agreement",
  "status": "Draft",
  "source": {
    "templateId": "eb02cdda-021d-4b18-97cc-72c2a90d8618",
    "templateName": "Professional services agreement",
    "templateVersionId": "f70b6a83-39a2-47ba-9963-adc13a4e1eb3",
    "versionNumber": 3
  },
  "createdAt": "2026-09-28T08:00:00Z",
  "updatedAt": "2026-09-28T08:42:00Z",
  "finalizedAt": null
}
```

### 3.14 DocumentDetailDto

```json
{
  "id": "b4382058-feb4-4cd4-b8bf-627a13822161",
  "title": "Acme consulting agreement",
  "status": "Draft",
  "source": {
    "templateId": "eb02cdda-021d-4b18-97cc-72c2a90d8618",
    "templateName": "Professional services agreement",
    "templateVersionId": "f70b6a83-39a2-47ba-9963-adc13a4e1eb3",
    "versionNumber": 3
  },
  "content": "<h1>Professional services agreement</h1><p>This agreement is made on {{effective_date}} between {{provider_name}} and {{client_name}}.</p>",
  "contentFormat": "Html",
  "placeholderValues": [],
  "createdAt": "2026-09-28T08:00:00Z",
  "updatedAt": "2026-09-28T08:00:00Z",
  "finalizedAt": null
}
```

`contentFormat` is derived from the retained source TemplateVersion in this
approved model; it is not a new Document entity field.

## 4. Endpoint summary

| Capability | Method | Path |
|---|---|---|
| Sign in | `POST` | `/api/auth/login` |
| Register | `POST` | `/api/auth/register` |
| Forgot password | `POST` | `/api/auth/forgot-password` |
| Reset password | `POST` | `/api/auth/reset-password` |
| Current identity | `GET` | `/api/auth/me` |
| Update own profile | `PATCH` | `/api/users/me` |
| Change own password | `POST` | `/api/users/me/change-password` |
| Active template gallery | `GET` | `/api/templates` |
| Template detail | `GET` | `/api/templates/{templateId}` |
| Current template version | `GET` | `/api/templates/{templateId}/current-version` |
| Current-version placeholders | `GET` | `/api/template-versions/{templateVersionId}/placeholders` |
| Create draft | `POST` | `/api/documents` |
| Load document | `GET` | `/api/documents/{documentId}` |
| Update draft | `PATCH` | `/api/documents/{documentId}` |
| Preview document | `POST` | `/api/documents/{documentId}/preview` |
| Finalize document | `POST` | `/api/documents/{documentId}/finalize` |
| Download document | `GET` | `/api/documents/{documentId}/download` |
| Document history | `GET` | `/api/documents` |
| Admin version list | `GET` | `/api/admin/templates/{templateId}/versions` |
| Admin create Draft version | `POST` | `/api/admin/templates/{templateId}/versions` |
| Admin version detail | `GET` | `/api/admin/template-versions/{versionId}` |
| Admin update Draft version | `PATCH` | `/api/admin/template-versions/{versionId}` |
| Admin publish version | `POST` | `/api/admin/template-versions/{versionId}/publish` |
| Admin set Current version | `POST` | `/api/admin/template-versions/{versionId}/set-current` |
| Admin placeholder list | `GET` | `/api/admin/template-versions/{versionId}/placeholders` |
| Admin create placeholder | `POST` | `/api/admin/template-versions/{versionId}/placeholders` |
| Admin update placeholder | `PATCH` | `/api/admin/template-versions/{versionId}/placeholders/{placeholderId}` |
| Admin remove placeholder | `DELETE` | `/api/admin/template-versions/{versionId}/placeholders/{placeholderId}` |
| Admin list users | `GET` | `/api/admin/users` |
| Admin create user | `POST` | `/api/admin/users` |
| Admin user detail | `GET` | `/api/admin/users/{userId}` |
| Admin edit user | `PATCH` | `/api/admin/users/{userId}` |
| Admin activate user | `POST` | `/api/admin/users/{userId}/activate` |
| Admin deactivate user | `POST` | `/api/admin/users/{userId}/deactivate` |
| Admin update user role | `PATCH` | `/api/admin/users/{userId}/role` |
| Admin audit log | `GET` | `/api/admin/audit-logs` |

## 5. Authentication endpoints

### 5.1 Sign in

**Method and path:** `POST /api/auth/login`

**Authorization:** Anonymous. Authenticated callers may also call it, but no
existing session is required.

**Route/query parameters:** None.

**Request DTO — LoginRequestDto**

```json
{
  "username": "maya.chen",
  "password": "example-password"
}
```

Both properties are strings.

**Response DTO — LoginResponseDto**

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.example.signature",
  "tokenType": "Bearer",
  "expiresAt": "2026-09-29T11:30:00Z",
  "user": {
    "id": "8d1471f4-e867-44f0-876f-4008626fc51e",
    "username": "maya.chen",
    "fullName": "Maya Chen",
    "email": "maya@example.test",
    "role": "User",
    "permissions": ["Templates.View", "Documents.Create", "Documents.ViewOwn", "Documents.EditOwn"]
  }
}
```

**Validation rules**

- `username` is required after trimming.
- `password` is required and is never echoed, logged, or stored as plaintext.
- Invalid credentials and inactive users receive the same generic response so
  account state is not disclosed.

**HTTP status codes**

- `200 OK` — credentials accepted.
- `400 Bad Request` — malformed body or required field missing.
- `401 Unauthorized` — credentials invalid or user inactive.
- `429 Too Many Requests` — the public-auth rate limit was exceeded.
- `500 Internal Server Error` — unexpected server failure.

**Example request**

```http
POST /api/auth/login HTTP/1.1
Content-Type: application/json

{"username":"maya.chen","password":"example-password"}
```

**Example response:** The `LoginResponseDto` shown above.

**Error cases**

- `VALIDATION_FAILED` (`400`)
- `INVALID_CREDENTIALS` (`401`)
- `RATE_LIMIT_EXCEEDED` (`429`)

### 5.2 Register

**Method and path:** `POST /api/auth/register`

**Authorization:** Anonymous.

**Route/query parameters:** None.

**Request DTO — RegisterRequestDto**

```json
{
  "username": "maya.chen",
  "fullName": "Maya Chen",
  "email": "maya@example.test",
  "password": "example-password"
}
```

**Response DTO:** `UserDto`. Registration creates an active user with role
`User` and its fixed permissions. It does not issue a JWT; the client redirects
to sign-in after success.

**Validation rules**

- `username`, `fullName`, `email`, and `password` are required after trimming
  where appropriate.
- `email` must be a syntactically valid email address.
- `username` and `email` must each be unique.
- The password is securely hashed before persistence and is never returned,
  logged, or stored as plaintext.
- Password confirmation is a client validation field and is not transmitted.

**HTTP status codes:** `201 Created`, `400 Bad Request`, `409 Conflict`,
`429 Too Many Requests`, `500 Internal Server Error`.

**Example request**

```http
POST /api/auth/register HTTP/1.1
Content-Type: application/json

{"username":"maya.chen","fullName":"Maya Chen","email":"maya@example.test","password":"example-password"}
```

**Example response:** the `UserDto` in section 3.3.

**Error cases:** `VALIDATION_FAILED` (`400`), `USERNAME_ALREADY_EXISTS` (`409`),
`EMAIL_ALREADY_EXISTS` (`409`), `RATE_LIMIT_EXCEEDED` (`429`).

### 5.3 Get current identity

**Method and path:** `GET /api/auth/me`

**Authorization:** Valid Bearer token.

**Route/query parameters:** None.

**Request DTO:** None.

**Response DTO:** `UserDto`.

**Validation rules:** The token must be present, valid, unexpired, and refer to
an active user.

**HTTP status codes:** `200 OK`, `401 Unauthorized`, `500 Internal Server Error`.

**Example request**

```http
GET /api/auth/me HTTP/1.1
Authorization: Bearer <access-token>
```

**Example response**

```json
{
  "id": "8d1471f4-e867-44f0-876f-4008626fc51e",
  "username": "maya.chen",
  "fullName": "Maya Chen",
  "email": "maya@example.test",
  "role": "User",
  "permissions": ["Templates.View", "Documents.Create", "Documents.ViewOwn", "Documents.EditOwn"]
}
```

**Error cases:** `AUTHENTICATION_REQUIRED` (`401`), `INVALID_TOKEN` (`401`).

## 6. Template endpoints

### 6.1 List active templates

**Method and path:** `GET /api/templates`

**Authorization:** `Templates.View`.

**Route/query parameters:** None in this slice. Search and category filtering
remain client-side.

**Request DTO:** None.

**Response DTO:** `TemplateGalleryItemDto[]`.

**Validation and business rules**

- Return only Templates whose status is `Active`.
- Each returned Template must have exactly one Current TemplateVersion, and that
  version must be `Published`.
- Draft and Inactive templates and non-current versions are not exposed by this
  user-facing endpoint.
- `placeholderCount` is calculated for the current version.

**HTTP status codes:** `200 OK`, `401 Unauthorized`, `500 Internal Server Error`.

**Example request**

```http
GET /api/templates HTTP/1.1
Authorization: Bearer <access-token>
```

**Example response**

```json
[
  {
    "id": "eb02cdda-021d-4b18-97cc-72c2a90d8618",
    "name": "Professional services agreement",
    "status": "Active",
    "category": {
      "id": "96fce3e3-cf1e-40c7-9bd0-a880d7e2cc46",
      "name": "Business"
    },
    "currentVersion": {
      "id": "f70b6a83-39a2-47ba-9963-adc13a4e1eb3",
      "versionNumber": 3,
      "status": "Published",
      "isCurrent": true,
      "contentFormat": "Html",
      "updatedAt": "2026-09-22T09:30:00Z",
      "placeholderCount": 6
    }
  }
]
```

**Error cases:** `AUTHENTICATION_REQUIRED` (`401`). Invalid catalog records are
a server data-integrity problem and must not be silently returned as usable
templates.

### 6.2 Get active template detail

**Method and path:** `GET /api/templates/{templateId}`

**Authorization:** `Templates.View`.

**Route parameters:** `templateId` — opaque Template identifier.

**Query parameters:** None.

**Request DTO:** None.

**Response DTO:** `TemplateDetailDto`.

**Validation and business rules**

- `templateId` is required and must identify an Active Template.
- The template must have a Current Published version.
- Draft and Inactive templates are treated as unavailable in this user-facing
  route.

**HTTP status codes:** `200 OK`, `400 Bad Request`, `401 Unauthorized`,
`404 Not Found`, `500 Internal Server Error`.

**Example request**

```http
GET /api/templates/eb02cdda-021d-4b18-97cc-72c2a90d8618 HTTP/1.1
Authorization: Bearer <access-token>
```

**Example response:** See `TemplateDetailDto` in section 3.7.

**Error cases**

- `VALIDATION_FAILED` (`400`) — malformed identifier.
- `TEMPLATE_NOT_FOUND` (`404`) — absent, Draft, or Inactive template.
- `CURRENT_VERSION_NOT_FOUND` (`404`) — no Current Published version.

### 6.3 Get current template version

**Method and path:** `GET /api/templates/{templateId}/current-version`

**Authorization:** `Templates.View`.

**Route parameters:** `templateId` — opaque Template identifier.

**Query parameters:** None.

**Request DTO:** None.

**Response DTO:** `TemplateVersionDetailDto`.

**Validation and business rules**

- Parent Template must be Active.
- Returned version must be both `Published` and `isCurrent: true`.
- `contentFormat` is `Html` for this slice. `Json` content is not rendered by
  this frontend.

**HTTP status codes:** `200 OK`, `400 Bad Request`, `401 Unauthorized`,
`404 Not Found`, `500 Internal Server Error`.

**Example request**

```http
GET /api/templates/eb02cdda-021d-4b18-97cc-72c2a90d8618/current-version HTTP/1.1
Authorization: Bearer <access-token>
```

**Example response:** See `TemplateVersionDetailDto` in section 3.8.

**Error cases:** `TEMPLATE_NOT_FOUND` (`404`),
`CURRENT_VERSION_NOT_FOUND` (`404`), `UNSUPPORTED_CONTENT_FORMAT` (`422`).

### 6.4 Get placeholders for the current version

**Method and path:**
`GET /api/template-versions/{templateVersionId}/placeholders`

**Authorization:** `Templates.View`.

**Route parameters:** `templateVersionId` — opaque TemplateVersion identifier.

**Query parameters:** None.

**Request DTO:** None.

**Response DTO:** `PlaceholderDto[]`.

**Validation and business rules**

- The version must be the Current Published version of an Active Template.
- Placeholder keys are unique within the version.
- Only `Text`, `Number`, `Date`, and `Email` are valid data types in this slice.
- Return an empty array when the valid current version has no placeholders.

**HTTP status codes:** `200 OK`, `400 Bad Request`, `401 Unauthorized`,
`404 Not Found`, `409 Conflict`, `500 Internal Server Error`.

**Example request**

```http
GET /api/template-versions/f70b6a83-39a2-47ba-9963-adc13a4e1eb3/placeholders HTTP/1.1
Authorization: Bearer <access-token>
```

**Example response**

```json
[
  {
    "id": "e12b0c4a-f75c-4e35-a9dd-b07db6d51cdf",
    "templateVersionId": "f70b6a83-39a2-47ba-9963-adc13a4e1eb3",
    "key": "effective_date",
    "label": "Effective date",
    "dataType": "Date",
    "isRequired": true,
    "defaultValue": null
  }
]
```

**Error cases**

- `TEMPLATE_VERSION_NOT_FOUND` (`404`)
- `TEMPLATE_VERSION_NOT_CURRENT` (`409`)
- `TEMPLATE_INACTIVE` (`409`)

## 7. Document endpoints

Document authorization in this first slice is intentionally narrow: an
authenticated caller may access documents whose `CreatedBy` is the caller's
User ID. No sharing, team access, or Admin override is defined.

### 7.1 Create draft document

**Method and path:** `POST /api/documents`

**Authorization:** `Documents.Create`.

**Route/query parameters:** None.

**Request DTO — CreateDraftDocumentRequestDto**

```json
{
  "templateVersionId": "f70b6a83-39a2-47ba-9963-adc13a4e1eb3",
  "title": "Untitled Professional services agreement"
}
```

**Response DTO:** `DocumentDetailDto`.

**Validation and business rules**

- `templateVersionId` and a non-empty trimmed `title` are required.
- The version must still be Current and Published at creation time.
- Its parent Template must still be Active at creation time.
- The Application service creates the Document through the required Prototype
  workflow. A controller must not map a TemplateVersion into a Document ad hoc.
- The clone receives a new identity, keeps `TemplateVersionId`, copies content,
  starts as `Draft`, and does not share mutable structures with the source.
- Source TemplateVersion content is never mutated.
- The initial content format is `Html`.
- Default placeholder values may be presented from `PlaceholderDto`; whether
  defaults are persisted immediately or on the first save is not approved and
  must not be guessed during implementation.

**HTTP status codes:** `201 Created`, `400 Bad Request`, `401 Unauthorized`,
`404 Not Found`, `409 Conflict`, `422 Unprocessable Entity`,
`500 Internal Server Error`.

On success, include `Location: /api/documents/{documentId}`.

**Example request**

```http
POST /api/documents HTTP/1.1
Authorization: Bearer <access-token>
Content-Type: application/json

{
  "templateVersionId": "f70b6a83-39a2-47ba-9963-adc13a4e1eb3",
  "title": "Untitled Professional services agreement"
}
```

**Example response:** See `DocumentDetailDto` in section 3.14.

**Error cases**

- `VALIDATION_FAILED` (`400`)
- `TEMPLATE_VERSION_NOT_FOUND` (`404`)
- `TEMPLATE_INACTIVE` (`409`)
- `TEMPLATE_VERSION_NOT_CURRENT` (`409`)
- `UNSUPPORTED_CONTENT_FORMAT` (`422`)

### 7.2 Get document detail

This endpoint is required to open either a saved Draft editor or a Finalized
historical document from Document History.

**Method and path:** `GET /api/documents/{documentId}`

**Authorization:** `Documents.ViewOwn`; caller must own the document.

**Route parameters:** `documentId` — opaque Document identifier.

**Query parameters:** None.

**Request DTO:** None.

**Response DTO:** `DocumentDetailDto`.

**Validation rules:** Identifier must be present and valid. The response uses
stored Document content and stored placeholder snapshots; it must not refresh
content or labels from the current TemplateVersion.

**HTTP status codes:** `200 OK`, `400 Bad Request`, `401 Unauthorized`,
`403 Forbidden`, `404 Not Found`, `500 Internal Server Error`.

**Example request**

```http
GET /api/documents/b4382058-feb4-4cd4-b8bf-627a13822161 HTTP/1.1
Authorization: Bearer <access-token>
```

**Example response:** See `DocumentDetailDto` in section 3.14.

**Error cases:** `DOCUMENT_NOT_FOUND` (`404`),
`UNAUTHORIZED_DOCUMENT_ACCESS` (`403`).

### 7.3 Update draft document

**Method and path:** `PATCH /api/documents/{documentId}`

**Authorization:** `Documents.EditOwn`; caller must own the document.

**Route parameters:** `documentId` — opaque Document identifier.

**Query parameters:** None.

**Request DTO — UpdateDraftDocumentRequestDto**

```json
{
  "title": "Acme consulting agreement",
  "content": "<h1>Professional services agreement</h1><p>This agreement is made on {{effective_date}} between {{provider_name}} and {{client_name}}.</p>",
  "placeholderValues": [
    {
      "placeholderId": "e12b0c4a-f75c-4e35-a9dd-b07db6d51cdf",
      "value": "2026-09-30"
    }
  ]
}
```

**Response DTO:** `DocumentDetailDto` containing the saved state.

**Validation and business rules**

- Document must exist, belong to the caller, and have status `Draft`.
- At least one of `title`, `content`, or `placeholderValues` must be present.
- Omitted properties retain their currently persisted values.
- When supplied, `title` must be non-empty after trimming.
- When supplied, `content` represents the independent Document copy; updating
  it never changes TemplateVersion content.
- When supplied, `placeholderValues` is the complete set of values to retain;
  an empty array clears the Draft's stored placeholder values.
- Placeholder IDs must belong to the retained source TemplateVersion.
- Duplicate or unknown Placeholder IDs are rejected.
- Required placeholder values may be absent or empty while the document remains
  Draft.
- Any non-empty value must match its Placeholder DataType. Validation is
  performed through the required Strategy abstraction.
- Snapshot key, label, and data type are populated by the server from the source
  Placeholder and are never accepted from the request.
- Saving does not change `Draft` to `Finalized`.

**HTTP status codes:** `200 OK`, `400 Bad Request`, `401 Unauthorized`,
`403 Forbidden`, `404 Not Found`, `409 Conflict`,
`422 Unprocessable Entity`, `500 Internal Server Error`.

**Example request:** The request DTO above is the example JSON body for:

```http
PATCH /api/documents/b4382058-feb4-4cd4-b8bf-627a13822161 HTTP/1.1
Authorization: Bearer <access-token>
Content-Type: application/json
```

**Example response:** `DocumentDetailDto` with the updated title, content,
snapshot values, and `updatedAt`.

**Error cases**

- `DOCUMENT_NOT_FOUND` (`404`)
- `UNAUTHORIZED_DOCUMENT_ACCESS` (`403`)
- `DOCUMENT_FINALIZED` (`409`)
- `INVALID_PLACEHOLDER_VALUE` (`422`)
- `PLACEHOLDER_NOT_IN_SOURCE_VERSION` (`422`)

### 7.4 Preview document

**Method and path:** `POST /api/documents/{documentId}/preview`

**Authorization:** `Documents.EditOwn`; caller must own the document.

**Route parameters:** `documentId` — opaque Document identifier.

**Query parameters:** None.

**Request DTO — PreviewDocumentRequestDto**

For a Draft, the request carries the current unsaved editor state so Preview
does not need to save or mutate it. For a Finalized document, the server renders
the stored content and stored placeholder snapshots and ignores attempts to
substitute changed request state.

```json
{
  "content": "<h1>Professional services agreement</h1><p>This agreement is made on {{effective_date}} between {{provider_name}} and {{client_name}}.</p>",
  "placeholderValues": [
    {
      "placeholderId": "e12b0c4a-f75c-4e35-a9dd-b07db6d51cdf",
      "value": "2026-09-30"
    },
    {
      "placeholderId": "aaf482d9-16ab-4ae5-af2d-46ca54f0486c",
      "value": "Northstar Studio"
    },
    {
      "placeholderId": "5e24ae89-c232-48cb-a24d-9ef9188798ad",
      "value": "Acme Industries"
    }
  ]
}
```

**Response DTO — PreviewDocumentResponseDto**

```json
{
  "documentId": "b4382058-feb4-4cd4-b8bf-627a13822161",
  "renderedContent": "<h1>Professional services agreement</h1><p>This agreement is made on 2026-09-30 between Northstar Studio and Acme Industries.</p>",
  "contentFormat": "Html"
}
```

**Validation and business rules**

- Document must be owned by the caller. Both Draft and Finalized documents may
  be previewed.
- Draft request values are transient and are not persisted by Preview.
- Finalized preview always uses stored immutable content and values.
- Source TemplateVersion is never mutated.
- Placeholder IDs must belong to the retained source version and be unique.
- Required values must be present and all values must pass their Strategy
  validator before rendering.
- Rendering uses the approved Composite abstraction and returns HTML in this
  initial slice.
- Returning to edit leaves the Draft unchanged.

**HTTP status codes:** `200 OK`, `400 Bad Request`, `401 Unauthorized`,
`403 Forbidden`, `404 Not Found`,
`422 Unprocessable Entity`, `500 Internal Server Error`.

**Example request:** The request DTO above is the example JSON body for:

```http
POST /api/documents/b4382058-feb4-4cd4-b8bf-627a13822161/preview HTTP/1.1
Authorization: Bearer <access-token>
Content-Type: application/json
```

**Example response:** `PreviewDocumentResponseDto` above.

**Error cases**

- `DOCUMENT_NOT_FOUND` (`404`)
- `UNAUTHORIZED_DOCUMENT_ACCESS` (`403`)
- `MISSING_REQUIRED_PLACEHOLDER` (`422`)
- `INVALID_PLACEHOLDER_VALUE` (`422`)
- `PLACEHOLDER_NOT_IN_SOURCE_VERSION` (`422`)

### 7.5 Finalize document

**Method and path:** `POST /api/documents/{documentId}/finalize`

**Authorization:** `Documents.EditOwn`; caller must own the document.

**Route parameters:** `documentId` — opaque Document identifier.

**Query parameters:** None.

**Request DTO — FinalizeDocumentRequestDto**

The full current state is sent so the user does not have to perform a separate
save immediately before finalizing. Persistence and the lifecycle change occur
atomically.

```json
{
  "title": "Acme consulting agreement",
  "content": "<h1>Professional services agreement</h1><p>This agreement is made on {{effective_date}} between {{provider_name}} and {{client_name}}.</p>",
  "placeholderValues": [
    {
      "placeholderId": "e12b0c4a-f75c-4e35-a9dd-b07db6d51cdf",
      "value": "2026-09-30"
    },
    {
      "placeholderId": "aaf482d9-16ab-4ae5-af2d-46ca54f0486c",
      "value": "Northstar Studio"
    },
    {
      "placeholderId": "5e24ae89-c232-48cb-a24d-9ef9188798ad",
      "value": "Acme Industries"
    }
  ]
}
```

**Response DTO:** `DocumentDetailDto` with `status: "Finalized"` and a non-null
`finalizedAt`.

**Validation and business rules**

- Document must exist, belong to the caller, and currently be `Draft`.
- Title and content rules are the same as Update Draft.
- Every required Placeholder must have a non-empty value.
- Every value must pass the correct Strategy validator.
- Placeholder snapshots and independent Document content are persisted before
  the lifecycle state changes.
- The operation is atomic: failure leaves the Document as Draft.
- After success, the Document is read-only and later Update or Finalize calls
  return `DOCUMENT_FINALIZED`.
- Finalization never mutates the source Template or TemplateVersion.

**HTTP status codes:** `200 OK`, `400 Bad Request`, `401 Unauthorized`,
`403 Forbidden`, `404 Not Found`, `409 Conflict`,
`422 Unprocessable Entity`, `500 Internal Server Error`.

**Example request:** The request DTO above is the example JSON body for:

```http
POST /api/documents/b4382058-feb4-4cd4-b8bf-627a13822161/finalize HTTP/1.1
Authorization: Bearer <access-token>
Content-Type: application/json
```

**Example response**

```json
{
  "id": "b4382058-feb4-4cd4-b8bf-627a13822161",
  "title": "Acme consulting agreement",
  "status": "Finalized",
  "source": {
    "templateId": "eb02cdda-021d-4b18-97cc-72c2a90d8618",
    "templateName": "Professional services agreement",
    "templateVersionId": "f70b6a83-39a2-47ba-9963-adc13a4e1eb3",
    "versionNumber": 3
  },
  "content": "<h1>Professional services agreement</h1><p>This agreement is made on {{effective_date}} between {{provider_name}} and {{client_name}}.</p>",
  "contentFormat": "Html",
  "placeholderValues": [
    {
      "id": "89d4191d-8344-4367-8dd7-9dcd75cc16cc",
      "placeholderId": "e12b0c4a-f75c-4e35-a9dd-b07db6d51cdf",
      "placeholderKeySnapshot": "effective_date",
      "labelSnapshot": "Effective date",
      "dataTypeSnapshot": "Date",
      "value": "2026-09-30"
    }
  ],
  "createdAt": "2026-09-28T08:00:00Z",
  "updatedAt": "2026-09-28T09:12:00Z",
  "finalizedAt": "2026-09-28T09:12:00Z"
}
```

**Error cases**

- `DOCUMENT_NOT_FOUND` (`404`)
- `UNAUTHORIZED_DOCUMENT_ACCESS` (`403`)
- `DOCUMENT_FINALIZED` (`409`)
- `MISSING_REQUIRED_PLACEHOLDER` (`422`)
- `INVALID_PLACEHOLDER_VALUE` (`422`)

### 7.6 Download document

**Method and path:** `GET /api/documents/{documentId}/download`

**Authorization:** `Documents.ViewOwn`; caller must own the document.

**Route parameters:** `documentId` — opaque Document identifier.

**Query parameters:** None. A `format` query parameter is intentionally not
invented before supported formats are approved.

**Request DTO:** None.

**Response DTO:** No JSON DTO on success. The success response is an HTML file
with:

```http
Content-Type: text/html; charset=utf-8
Content-Disposition: attachment; filename*=UTF-8''<safe-title>.html
```

JSON failures still use `ErrorResponseDto`.

**Validation and business rules**

- Document must exist and be authorized for the caller.
- Export must use the Document's stored independent content and stored
  placeholder snapshots, never the latest TemplateVersion.
- Both Draft and Finalized documents may be downloaded.
- Placeholder tokens are rendered from the Document's stored snapshot values.
- HTML is the only approved MVP export format. PDF and DOCX are not supported.
- The filename is derived from the title, made safe for a download filename,
  and uses the `.html` extension.

**HTTP status codes:** `200 OK`,
`400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found`,
`422 Unprocessable Entity`, `500 Internal Server Error`.

**Example request**

```http
GET /api/documents/b4382058-feb4-4cd4-b8bf-627a13822161/download HTTP/1.1
Authorization: Bearer <access-token>
```

**Example response**

```http
HTTP/1.1 200 OK
Content-Type: text/html; charset=utf-8
Content-Disposition: attachment; filename*=UTF-8''Acme-consulting-agreement.html

<h1>Professional services agreement</h1><p>This agreement is made on 2026-09-30 between Northstar Studio and Acme Industries.</p>
```

**Error cases:** `DOCUMENT_NOT_FOUND` (`404`),
`UNAUTHORIZED_DOCUMENT_ACCESS` (`403`), `EXPORT_FAILED` (`422`).

### 7.7 List document history

**Method and path:** `GET /api/documents`

**Authorization:** `Documents.ViewOwn`.

**Route/query parameters:** None in this slice. The current frontend performs
search and Draft/Finalized filtering locally.

**Request DTO:** None.

**Response DTO:** `DocumentSummaryDto[]`.

**Validation and business rules**

- Return documents whose `CreatedBy` is the authenticated caller.
- Include both Draft and Finalized documents.
- Order newest `updatedAt` first to match the current history presentation.
- Source template/version information is a read projection and must continue to
  identify the retained historical TemplateVersion.
- Template updates, publication, or deactivation must not alter returned
  historical document content or placeholder snapshots.

**HTTP status codes:** `200 OK`, `401 Unauthorized`, `500 Internal Server Error`.

**Example request**

```http
GET /api/documents HTTP/1.1
Authorization: Bearer <access-token>
```

**Example response**

```json
[
  {
    "id": "b4382058-feb4-4cd4-b8bf-627a13822161",
    "title": "Acme consulting agreement",
    "status": "Draft",
    "source": {
      "templateId": "eb02cdda-021d-4b18-97cc-72c2a90d8618",
      "templateName": "Professional services agreement",
      "templateVersionId": "f70b6a83-39a2-47ba-9963-adc13a4e1eb3",
      "versionNumber": 3
    },
    "createdAt": "2026-09-28T08:00:00Z",
    "updatedAt": "2026-09-28T08:42:00Z",
    "finalizedAt": null
  }
]
```

**Error cases:** `AUTHENTICATION_REQUIRED` (`401`), `INVALID_TOKEN` (`401`).

## 8. Frontend integration mapping

The implemented frontend API layer maps DTOs rather than changing domain or
transport contracts to mirror the retired mock shape:

| Original mock field/behavior | Implemented API source/change |
|---|---|
| `TemplateSummary.id` slug | Map from opaque `TemplateGalleryItemDto.id` |
| `category` string | Display `category.name`; filter by `category.id` locally |
| `description` | Remove/hide until an approved source exists |
| `versionNumber` | `currentVersion.versionNumber` |
| `updatedAt` | `currentVersion.updatedAt` |
| `placeholderCount` | `currentVersion.placeholderCount` |
| `TemplateDetail.content` | Load `TemplateVersionDetailDto.content` |
| `PlaceholderDefinition.example` | Remove; use only approved `defaultValue` |
| `Record<string,string>` values | Adapt from/to placeholder-value arrays by ID |
| Local Draft creation | Call `POST /api/documents` before editing the clone |
| Local Save Draft | Call `PATCH /api/documents/{documentId}` |
| Local Preview substitution | Call the non-mutating preview endpoint |
| Local Finalize state change | Call the atomic finalize endpoint |
| Hard-coded shell user | Load `GET /api/auth/me` |
| Date display strings | Format ISO values in the presentation layer |

## 9. Explicit exclusions

This contract does not define:

- a server logout endpoint, refresh tokens, avatar/profile preferences, or MFA;
- inactive/draft template browsing for authors;
- historical template-version browsing outside retained Document history;
- Admin password assignment/reset or Audit Log mutation;
- sharing, comments, teams, collaboration, approval chains, or cross-user access;
- autosave guarantees, bulk operations, server sorting, pagination, or search;
- template mutation or editing Published versions;
- a JSON content editor;
- custom roles, custom permissions, or runtime permission management;
- PDF or DOCX export.

Those capabilities require separate approval and contract work.

## 10. Phase 6A admin DTOs

Admin DTOs remain separate from domain entities. `Template` creation produces
the approved empty initial Draft `TemplateVersion` in `Html` format; Phase 6A
does not expose content or version mutation.

```text
AdminCategoryDto
  id: string
  name: string
  isActive: boolean
  createdAt: string (ISO 8601 UTC)

CreateCategoryRequestDto
  name: string

UpdateCategoryRequestDto
  name: string

AdminTemplateVersionSummaryDto
  id: string
  versionNumber: integer
  status: "Draft" | "Published"
  isCurrent: boolean
  contentFormat: "Html" | "Json"
  placeholderCount: integer
  createdAt: string (ISO 8601 UTC)
  updatedAt: string (ISO 8601 UTC)

AdminTemplateSummaryDto
  id: string
  name: string
  status: "Draft" | "Active" | "Inactive"
  category: CategoryReferenceDto
  createdAt: string (ISO 8601 UTC)
  versionCount: integer
  currentVersionNumber: integer | null

AdminTemplateDetailDto
  id: string
  name: string
  status: "Draft" | "Active" | "Inactive"
  category: CategoryReferenceDto
  createdAt: string (ISO 8601 UTC)
  versions: AdminTemplateVersionSummaryDto[]

CreateTemplateRequestDto
  name: string
  categoryId: string

UpdateDraftTemplateRequestDto
  name?: string
  categoryId?: string
```

## 11. Category administration

All endpoints in this section require `Templates.Manage`.
Category state changes are soft changes; there is no delete endpoint.

### 11.1 List categories

**Method and path:** `GET /api/admin/categories`

**Route/query parameters:** None. MVP search/filtering is client-side.

**Request DTO:** None.

**Response DTO:** `AdminCategoryDto[]`, ordered by name and including active
and inactive categories.

**Validation rules:** None beyond authentication and authorization.

**HTTP status codes:** `200 OK`, `401 Unauthorized`, `403 Forbidden`,
`500 Internal Server Error`.

**Example request**

```http
GET /api/admin/categories HTTP/1.1
Authorization: Bearer <admin-access-token>
```

**Example response**

```json
[
  {
    "id": "5c67b0b0-bf84-42ea-b567-f0872a45ea52",
    "name": "Business",
    "isActive": true,
    "createdAt": "2026-09-30T08:00:00Z"
  }
]
```

**Error cases:** `AUTHENTICATION_REQUIRED` (`401`), `FORBIDDEN` (`403`).

### 11.2 Create category

**Method and path:** `POST /api/admin/categories`

**Route/query parameters:** None.

**Request DTO:** `CreateCategoryRequestDto`.

**Response DTO:** `AdminCategoryDto`; the new category is active.

**Validation rules:** `name` is required after trimming.

**HTTP status codes:** `201 Created`, `400 Bad Request`, `401 Unauthorized`,
`403 Forbidden`, `500 Internal Server Error`.

**Example request**

```json
{ "name": "Legal" }
```

**Example response**

```json
{
  "id": "8395732d-0734-4cc0-8424-4d0b57482008",
  "name": "Legal",
  "isActive": true,
  "createdAt": "2026-09-30T09:00:00Z"
}
```

**Error cases:** `VALIDATION_ERROR` (`400`), `AUTHENTICATION_REQUIRED` (`401`),
`FORBIDDEN` (`403`).

### 11.3 Update category

**Method and path:** `PATCH /api/admin/categories/{categoryId}`

**Route parameters:** `categoryId` is the opaque Category identifier. No query
parameters.

**Request DTO:** `UpdateCategoryRequestDto`.

**Response DTO:** updated `AdminCategoryDto`.

**Validation rules:** `name` is required after trimming. Updating a category
does not change its active state.

**HTTP status codes:** `200 OK`, `400 Bad Request`, `401 Unauthorized`,
`403 Forbidden`, `404 Not Found`, `500 Internal Server Error`.

**Example request**

```json
{ "name": "Legal and compliance" }
```

**Example response**

```json
{
  "id": "8395732d-0734-4cc0-8424-4d0b57482008",
  "name": "Legal and compliance",
  "isActive": true,
  "createdAt": "2026-09-30T09:00:00Z"
}
```

**Error cases:** `CATEGORY_NOT_FOUND` (`404`), `VALIDATION_ERROR` (`400`),
`AUTHENTICATION_REQUIRED` (`401`), `FORBIDDEN` (`403`).

### 11.4 Activate category

**Method and path:** `POST /api/admin/categories/{categoryId}/activate`

**Route parameters:** `categoryId`. No query parameters or request body.

**Response DTO:** updated `AdminCategoryDto`.

**Validation rules:** The operation is idempotent and does not alter Templates.

**HTTP status codes:** `200 OK`, `401 Unauthorized`, `403 Forbidden`,
`404 Not Found`, `500 Internal Server Error`.

**Example request:** `POST /api/admin/categories/8395732d-0734-4cc0-8424-4d0b57482008/activate`

**Example response:** the `AdminCategoryDto` above with `isActive: true`.

**Error cases:** `CATEGORY_NOT_FOUND` (`404`), `AUTHENTICATION_REQUIRED`
(`401`), `FORBIDDEN` (`403`).

### 11.5 Deactivate category

**Method and path:** `POST /api/admin/categories/{categoryId}/deactivate`

**Route parameters:** `categoryId`. No query parameters or request body.

**Response DTO:** updated `AdminCategoryDto`.

**Validation rules:** The operation is idempotent. It does not delete or
deactivate related Templates and must not affect historical Documents.

**HTTP status codes:** `200 OK`, `401 Unauthorized`, `403 Forbidden`,
`404 Not Found`, `500 Internal Server Error`.

**Example request:** `POST /api/admin/categories/8395732d-0734-4cc0-8424-4d0b57482008/deactivate`

**Example response:** the `AdminCategoryDto` above with `isActive: false`.

**Error cases:** `CATEGORY_NOT_FOUND` (`404`), `AUTHENTICATION_REQUIRED`
(`401`), `FORBIDDEN` (`403`).

## 12. Template administration

All endpoints in this section require `Templates.Manage`.
Templates are never hard-deleted. Content, TemplateVersion publication, and
Placeholder administration remain outside Phase 6A.

### 12.1 List templates

**Method and path:** `GET /api/admin/templates`

**Route/query parameters:** None. MVP search/filtering is client-side.

**Request DTO:** None.

**Response DTO:** `AdminTemplateSummaryDto[]`, ordered newest first and
including Draft, Active, and Inactive Templates.

**Validation rules:** None beyond authentication and authorization.

**HTTP status codes:** `200 OK`, `401 Unauthorized`, `403 Forbidden`,
`500 Internal Server Error`.

**Example request:** `GET /api/admin/templates` with an Admin Bearer token.

**Example response**

```json
[
  {
    "id": "eb02cdda-021d-4b18-97cc-72c2a90d8618",
    "name": "Professional services agreement",
    "status": "Active",
    "category": {
      "id": "5c67b0b0-bf84-42ea-b567-f0872a45ea52",
      "name": "Business"
    },
    "createdAt": "2026-09-28T08:00:00Z",
    "versionCount": 3,
    "currentVersionNumber": 3
  }
]
```

**Error cases:** `AUTHENTICATION_REQUIRED` (`401`), `FORBIDDEN` (`403`).

### 12.2 View template detail

**Method and path:** `GET /api/admin/templates/{templateId}`

**Route parameters:** `templateId`. No query parameters.

**Request DTO:** None.

**Response DTO:** `AdminTemplateDetailDto` including read-only version summaries.

**Validation rules:** Draft and inactive Templates are visible to Admin.

**HTTP status codes:** `200 OK`, `401 Unauthorized`, `403 Forbidden`,
`404 Not Found`, `500 Internal Server Error`.

**Example request:** `GET /api/admin/templates/eb02cdda-021d-4b18-97cc-72c2a90d8618`

**Example response**

```json
{
  "id": "eb02cdda-021d-4b18-97cc-72c2a90d8618",
  "name": "Professional services agreement",
  "status": "Active",
  "category": {
    "id": "5c67b0b0-bf84-42ea-b567-f0872a45ea52",
    "name": "Business"
  },
  "createdAt": "2026-09-28T08:00:00Z",
  "versions": [
    {
      "id": "f70b6a83-39a2-47ba-9963-adc13a4e1eb3",
      "versionNumber": 3,
      "status": "Published",
      "isCurrent": true,
      "contentFormat": "Html",
      "placeholderCount": 6,
      "createdAt": "2026-09-28T08:00:00Z",
      "updatedAt": "2026-09-28T08:42:00Z"
    }
  ]
}
```

**Error cases:** `TEMPLATE_NOT_FOUND` (`404`), `AUTHENTICATION_REQUIRED`
(`401`), `FORBIDDEN` (`403`).

### 12.3 Create template

**Method and path:** `POST /api/admin/templates`

**Route/query parameters:** None.

**Request DTO:** `CreateTemplateRequestDto`.

**Response DTO:** `AdminTemplateDetailDto` with status `Draft` and one empty
Draft version numbered `1` in `Html` format.

**Validation rules:** `name` is required after trimming. `categoryId` must
identify an existing Category.
No Template content or Placeholder input is accepted in Phase 6A.

**HTTP status codes:** `201 Created`, `400 Bad Request`, `401 Unauthorized`,
`403 Forbidden`, `404 Not Found`, `500 Internal Server Error`.

**Example request**

```json
{
  "name": "Statement of work",
  "categoryId": "5c67b0b0-bf84-42ea-b567-f0872a45ea52"
}
```

**Example response:** an `AdminTemplateDetailDto` with `status: "Draft"` and
one Draft entry in `versions`.

**Error cases:** `CATEGORY_NOT_FOUND` (`404`), `VALIDATION_ERROR` (`400`),
`AUTHENTICATION_REQUIRED` (`401`), `FORBIDDEN` (`403`).

### 12.4 Update Draft template metadata

**Method and path:** `PATCH /api/admin/templates/{templateId}`

**Route parameters:** `templateId`. No query parameters.

**Request DTO:** `UpdateDraftTemplateRequestDto`.

**Response DTO:** updated `AdminTemplateDetailDto`.

**Validation rules:** At least one of `name` or `categoryId` is required.
Provided names are trimmed and required. A provided `categoryId` must exist.
Only a Template whose status is `Draft` may have its
metadata updated; versions, content, and placeholders are not changed.

**HTTP status codes:** `200 OK`, `400 Bad Request`, `401 Unauthorized`,
`403 Forbidden`, `404 Not Found`, `409 Conflict`, `500 Internal Server Error`.

**Example request**

```json
{
  "name": "Statement of work — standard",
  "categoryId": "8395732d-0734-4cc0-8424-4d0b57482008"
}
```

**Example response:** the updated `AdminTemplateDetailDto`.

**Error cases:** `TEMPLATE_NOT_FOUND` (`404`), `CATEGORY_NOT_FOUND` (`404`),
`TEMPLATE_NOT_DRAFT` (`409`), `VALIDATION_ERROR` (`400`),
`AUTHENTICATION_REQUIRED` (`401`), `FORBIDDEN` (`403`).

### 12.5 Activate template

**Method and path:** `POST /api/admin/templates/{templateId}/activate`

**Route parameters:** `templateId`. No query parameters or request body.

**Response DTO:** updated `AdminTemplateDetailDto`.

**Validation rules:** The Template must have a current Published
TemplateVersion. Activation does not modify any TemplateVersion or Document.

**HTTP status codes:** `200 OK`, `401 Unauthorized`, `403 Forbidden`,
`404 Not Found`, `409 Conflict`, `500 Internal Server Error`.

**Example request:** `POST /api/admin/templates/eb02cdda-021d-4b18-97cc-72c2a90d8618/activate`

**Example response:** the `AdminTemplateDetailDto` with `status: "Active"`.

**Error cases:** `TEMPLATE_NOT_FOUND` (`404`),
`TEMPLATE_ACTIVATION_REQUIRES_CURRENT_VERSION` (`409`),
`AUTHENTICATION_REQUIRED` (`401`), `FORBIDDEN` (`403`).

### 12.6 Deactivate template

**Method and path:** `POST /api/admin/templates/{templateId}/deactivate`

**Route parameters:** `templateId`. No query parameters or request body.

**Response DTO:** updated `AdminTemplateDetailDto`.

**Validation rules:** The operation is idempotent and changes state to
`Inactive`. It must not modify or delete TemplateVersions or historical
Documents.

**HTTP status codes:** `200 OK`, `401 Unauthorized`, `403 Forbidden`,
`404 Not Found`, `500 Internal Server Error`.

**Example request:** `POST /api/admin/templates/eb02cdda-021d-4b18-97cc-72c2a90d8618/deactivate`

**Example response:** the `AdminTemplateDetailDto` with `status: "Inactive"`.

**Error cases:** `TEMPLATE_NOT_FOUND` (`404`), `AUTHENTICATION_REQUIRED`
(`401`), `FORBIDDEN` (`403`).

## 13. Phase 6B admin DTOs

Phase 6B DTOs remain separate from domain entities. TemplateVersion content is
managed as persisted source text; this contract does not select or imply a rich
text, WYSIWYG, block, or JSON editor.

```text
AdminTemplateVersionDetailDto
  id: string
  templateId: string
  versionNumber: integer
  content: string
  contentFormat: "Html" | "Json"
  status: "Draft" | "Published"
  isCurrent: boolean
  placeholderCount: integer
  publishedAt: string | null (ISO 8601 UTC)
  createdAt: string (ISO 8601 UTC)
  updatedAt: string (ISO 8601 UTC)

UpdateDraftTemplateVersionRequestDto
  content: string

CreatePlaceholderRequestDto
  key: string
  label: string
  dataType: "Text" | "Number" | "Date" | "Email"
  isRequired: boolean
  defaultValue: string | null

UpdatePlaceholderRequestDto
  key: string
  label: string
  dataType: "Text" | "Number" | "Date" | "Email"
  isRequired: boolean
  defaultValue: string | null
```

`AdminTemplateVersionSummaryDto` and `PlaceholderDto` retain the shapes defined
in sections 10 and 3.9. A null `defaultValue` means that no default is defined.
The Placeholder update request is a complete editable definition even though
the route uses `PATCH`; this allows `defaultValue: null` to explicitly clear an
existing default without introducing an optional-value transport wrapper.

## 14. TemplateVersion and Placeholder administration

Every endpoint in this section requires `Templates.Manage`. A caller without
that permission receives `403 Forbidden`. Every successful mutation writes
an `AuditLog` in the same unit of work as the governed change.

Published TemplateVersions are retained and immutable. There is no
TemplateVersion delete endpoint. Publishing and selecting the Current version
are separate actions; publishing never changes Current state.

### 14.1 List versions for a Template

**Method and path:** `GET /api/admin/templates/{templateId}/versions`

**Route parameters:** `templateId`. No query parameters.

**Request DTO:** None.

**Response DTO:** `AdminTemplateVersionSummaryDto[]`, ordered by descending
`versionNumber` and including Draft and Published versions.

**HTTP status codes:** `200 OK`, `401 Unauthorized`, `403 Forbidden`,
`404 Not Found`, `500 Internal Server Error`.

**Error cases:** `TEMPLATE_NOT_FOUND` (`404`), `AUTHENTICATION_REQUIRED`
(`401`), `FORBIDDEN` (`403`).

### 14.2 Create a Draft TemplateVersion

**Method and path:** `POST /api/admin/templates/{templateId}/versions`

**Route parameters:** `templateId`. No query parameters or request body.

**Response DTO:** `AdminTemplateVersionDetailDto` with status `Draft` and
`isCurrent: false`.

**Validation and business rules**

- The Template must exist.
- The next version number is generated within the Template aggregate.
- When a Current version exists, the new Draft copies that version's content,
  content format, and Placeholder definitions.
- Otherwise, it copies those values from the version with the highest
  `versionNumber`.
- Copied Placeholders receive new identities and belong to the new version.
- The source version and its Placeholders are not modified.
- The new Draft is never Current.

**HTTP status codes:** `201 Created`, `401 Unauthorized`, `403 Forbidden`,
`404 Not Found`, `500 Internal Server Error`.

On success, include
`Location: /api/admin/template-versions/{versionId}`.

**Error cases:** `TEMPLATE_NOT_FOUND` (`404`), `AUTHENTICATION_REQUIRED`
(`401`), `FORBIDDEN` (`403`). A Template without any source version violates
the approved aggregate invariant and is treated as a server data-integrity
failure.

### 14.3 View TemplateVersion detail

**Method and path:** `GET /api/admin/template-versions/{versionId}`

**Route parameters:** `versionId`. No query parameters.

**Request DTO:** None.

**Response DTO:** `AdminTemplateVersionDetailDto`.

**HTTP status codes:** `200 OK`, `401 Unauthorized`, `403 Forbidden`,
`404 Not Found`, `500 Internal Server Error`.

**Error cases:** `TEMPLATE_VERSION_NOT_FOUND` (`404`),
`AUTHENTICATION_REQUIRED` (`401`), `FORBIDDEN` (`403`).

### 14.4 Update Draft TemplateVersion content

**Method and path:** `PATCH /api/admin/template-versions/{versionId}`

**Route parameters:** `versionId`. No query parameters.

**Request DTO:** `UpdateDraftTemplateVersionRequestDto`.

**Response DTO:** updated `AdminTemplateVersionDetailDto`.

**Validation and business rules**

- `content` is required and may be an empty string.
- Only a Draft version may be updated.
- `contentFormat` is retained from the existing version and is not accepted in
  this request.
- Published versions are immutable.

**HTTP status codes:** `200 OK`, `400 Bad Request`, `401 Unauthorized`,
`403 Forbidden`, `404 Not Found`, `409 Conflict`,
`500 Internal Server Error`.

**Error cases:** `VALIDATION_FAILED` (`400`),
`TEMPLATE_VERSION_NOT_FOUND` (`404`), `TEMPLATE_VERSION_PUBLISHED` (`409`),
`AUTHENTICATION_REQUIRED` (`401`), `FORBIDDEN` (`403`).

### 14.5 Publish a Draft TemplateVersion

**Method and path:**
`POST /api/admin/template-versions/{versionId}/publish`

**Route parameters:** `versionId`. No query parameters or request body.

**Response DTO:** updated `AdminTemplateVersionDetailDto` with status
`Published`, non-null `publishedAt`, and unchanged `isCurrent`.

**Validation and business rules**

- Only a Draft version may be published.
- Every non-null Placeholder default value must pass the Strategy validator for
  its configured data type before publication.
- Publishing does not make the version Current.
- After publication, content and Placeholder definitions are immutable.

**HTTP status codes:** `200 OK`, `401 Unauthorized`, `403 Forbidden`,
`404 Not Found`, `409 Conflict`, `422 Unprocessable Entity`,
`500 Internal Server Error`.

**Error cases:** `TEMPLATE_VERSION_NOT_FOUND` (`404`),
`TEMPLATE_VERSION_PUBLISHED` (`409`),
`INVALID_PLACEHOLDER_DEFAULTS` (`422`),
`AUTHENTICATION_REQUIRED` (`401`), `FORBIDDEN` (`403`).

### 14.6 Set a Published TemplateVersion as Current

**Method and path:**
`POST /api/admin/template-versions/{versionId}/set-current`

**Route parameters:** `versionId`. No query parameters or request body.

**Response DTO:** updated `AdminTemplateVersionDetailDto` with
`isCurrent: true`.

**Validation and business rules**

- Only a Published version may become Current. A Draft request returns
  `TEMPLATE_VERSION_NOT_PUBLISHED`.
- The operation clears the previous Current version before setting the selected
  Published version.
- Clearing the previous Current version, setting the new Current version, and
  writing the AuditLog occur atomically.
- Repeating the operation for the existing Current version is idempotent.
- A Template has at most one Current version.

**HTTP status codes:** `200 OK`, `401 Unauthorized`, `403 Forbidden`,
`404 Not Found`, `409 Conflict`, `500 Internal Server Error`.

**Error cases:** `TEMPLATE_VERSION_NOT_FOUND` (`404`),
`TEMPLATE_VERSION_NOT_PUBLISHED` (`409`),
`AUTHENTICATION_REQUIRED` (`401`), `FORBIDDEN` (`403`).

### 14.7 List Placeholders for a TemplateVersion

**Method and path:**
`GET /api/admin/template-versions/{versionId}/placeholders`

**Route parameters:** `versionId`. No query parameters.

**Request DTO:** None.

**Response DTO:** `PlaceholderDto[]`, ordered by `key`. Draft and Published
versions are both readable. An empty version returns `200 OK` with `[]`.

**HTTP status codes:** `200 OK`, `401 Unauthorized`, `403 Forbidden`,
`404 Not Found`, `500 Internal Server Error`.

**Error cases:** `TEMPLATE_VERSION_NOT_FOUND` (`404`),
`AUTHENTICATION_REQUIRED` (`401`), `FORBIDDEN` (`403`).

### 14.8 Create a Placeholder

**Method and path:**
`POST /api/admin/template-versions/{versionId}/placeholders`

**Route parameters:** `versionId`. No query parameters.

**Request DTO:** `CreatePlaceholderRequestDto`.

**Response DTO:** `PlaceholderDto`.

**Validation and business rules**

- Only a Draft version may receive a Placeholder.
- `key` and `label` are required after trimming.
- `dataType` must be `Text`, `Number`, `Date`, or `Email`.
- `(TemplateVersionId, Key)` must remain unique.
- `defaultValue` is optional. When non-null, including an empty string, it must
  pass the Strategy validator for `dataType`.
- `isRequired` does not require a default value.

**HTTP status codes:** `201 Created`, `400 Bad Request`, `401 Unauthorized`,
`403 Forbidden`, `404 Not Found`, `409 Conflict`,
`500 Internal Server Error`.

On success, include
`Location: /api/admin/template-versions/{versionId}/placeholders/{placeholderId}`.

**Error cases:** `VALIDATION_FAILED` (`400`, including an
`INVALID_DEFAULT_VALUE` field error), `TEMPLATE_VERSION_NOT_FOUND` (`404`),
`TEMPLATE_VERSION_PUBLISHED` (`409`), `PLACEHOLDER_KEY_CONFLICT` (`409`),
`AUTHENTICATION_REQUIRED` (`401`), `FORBIDDEN` (`403`).

### 14.9 Update a Placeholder

**Method and path:**
`PATCH /api/admin/template-versions/{versionId}/placeholders/{placeholderId}`

**Route parameters:** `versionId` and `placeholderId`. No query parameters.

**Request DTO:** `UpdatePlaceholderRequestDto`. All editable fields are
required; `defaultValue` may be null.

**Response DTO:** updated `PlaceholderDto`.

**Validation and business rules:** The create rules apply. The Placeholder must
belong to `versionId`, and only a Draft version may be changed. The uniqueness
check excludes the Placeholder being updated.

**HTTP status codes:** `200 OK`, `400 Bad Request`, `401 Unauthorized`,
`403 Forbidden`, `404 Not Found`, `409 Conflict`,
`500 Internal Server Error`.

**Error cases:** `VALIDATION_FAILED` (`400`, including an
`INVALID_DEFAULT_VALUE` field error), `TEMPLATE_VERSION_NOT_FOUND` (`404`),
`PLACEHOLDER_NOT_FOUND` (`404`), `TEMPLATE_VERSION_PUBLISHED` (`409`),
`PLACEHOLDER_KEY_CONFLICT` (`409`),
`AUTHENTICATION_REQUIRED` (`401`), `FORBIDDEN` (`403`).

### 14.10 Remove a Placeholder

**Method and path:**
`DELETE /api/admin/template-versions/{versionId}/placeholders/{placeholderId}`

**Route parameters:** `versionId` and `placeholderId`. No query parameters or
request body.

**Response DTO:** None.

**Validation and business rules:** The Placeholder must belong to `versionId`.
Only a Draft version may remove a Placeholder. Published definitions are never
deleted or changed.

**HTTP status codes:** `204 No Content`, `401 Unauthorized`, `403 Forbidden`,
`404 Not Found`, `409 Conflict`, `500 Internal Server Error`.

**Error cases:** `TEMPLATE_VERSION_NOT_FOUND` (`404`),
`PLACEHOLDER_NOT_FOUND` (`404`), `TEMPLATE_VERSION_PUBLISHED` (`409`),
`AUTHENTICATION_REQUIRED` (`401`), `FORBIDDEN` (`403`).

## 15. Phase 6D User and Audit Log DTOs

These DTOs are application-boundary projections and are not domain entities.
`PasswordHash` is never exposed.

```text
AdminUserDto
  id: string
  username: string
  fullName: string
  email: string
  role: "Admin" | "User"
  isActive: boolean
  createdAt: string (ISO 8601 UTC)

UpdateUserRoleRequestDto
  role: "Admin" | "User"

CreateAdminUserRequestDto
  username: string
  fullName: string
  email: string
  initialPassword: string
  role: "Admin" | "User"

UpdateAdminUserRequestDto
  username: string
  fullName: string
  email: string

AuditActorDto
  id: string
  username: string
  fullName: string

AdminAuditLogDto
  id: string
  performedBy: AuditActorDto
  actionType: string
  entityType: string
  entityId: string
  description: string
  createdAt: string (ISO 8601 UTC)
```

## 16. User administration

All endpoints in this section require a Bearer token and the permission stated
for the endpoint. Under the fixed matrix, only `Admin` has these permissions.
Users are never hard-deleted. Deactivation preserves all foreign-key and
historical references. Administrator-driven password assignment/reset and
runtime permission management are not defined; self-service password change
and recovery are defined separately in section 19.

Self-administration is intentionally limited: the authenticated Admin cannot
deactivate their own account or change their own role. This phase does not
introduce an unapproved last-active-Admin invariant.

### 16.1 List users

**Method and path:** `GET /api/admin/users`

**Authorization:** `Users.View`.

**Route/query parameters:** None. MVP search and role/status filtering are
client-side.

**Request DTO:** None.

**Response DTO:** `AdminUserDto[]`, ordered by `username` and including active
and inactive users.

**Validation and business rules:** `PasswordHash` and authentication secrets
must not be exposed. An empty result returns `200 OK` with `[]`.

**HTTP status codes:** `200 OK`, `401 Unauthorized`, `403 Forbidden`,
`500 Internal Server Error`.

**Example request**

```http
GET /api/admin/users HTTP/1.1
Authorization: Bearer <admin-access-token>
```

**Example response**

```json
[
  {
    "id": "c3d358ab-35bb-4018-8988-346381f6422c",
    "username": "admin",
    "fullName": "Development Admin",
    "email": "admin@example.test",
    "role": "Admin",
    "isActive": true,
    "createdAt": "2026-01-01T00:00:00Z"
  }
]
```

**Error cases:** `AUTHENTICATION_REQUIRED` (`401`), `FORBIDDEN` (`403`).

### 16.2 Create user

**Method and path:** `POST /api/admin/users`

**Authorization:** `Users.Manage`.

**Route/query parameters:** None.

**Request DTO — CreateAdminUserRequestDto**

```json
{
  "username": "alex.morgan",
  "fullName": "Alex Morgan",
  "email": "alex.morgan@example.test",
  "initialPassword": "example-password",
  "role": "User"
}
```

**Response DTO:** `AdminUserDto`. The new account is active by default.

**Validation and business rules**

- All request fields are required; `email` must be syntactically valid.
- `role` must be exactly `Admin` or `User`.
- `username` and `email` must each be unique.
- `initialPassword` is securely hashed before persistence and is never exposed,
  logged, or stored as plaintext.
- Successful creation writes a `CreateUser` AuditLog associated with the
  authenticated Admin.

**HTTP status codes:** `201 Created`, `400 Bad Request`, `401 Unauthorized`,
`403 Forbidden`, `409 Conflict`, `500 Internal Server Error`.

**Example response**

```json
{
  "id": "c6a12fa1-b189-445a-86da-19bc4a2f25ec",
  "username": "alex.morgan",
  "fullName": "Alex Morgan",
  "email": "alex.morgan@example.test",
  "role": "User",
  "isActive": true,
  "createdAt": "2026-10-02T09:00:00Z"
}
```

**Error cases:** `VALIDATION_FAILED` (`400`), `INVALID_ROLE` (`400`),
`USERNAME_ALREADY_EXISTS` (`409`), `EMAIL_ALREADY_EXISTS` (`409`),
`AUTHENTICATION_REQUIRED` (`401`), `FORBIDDEN` (`403`).

### 16.3 View user details

**Method and path:** `GET /api/admin/users/{userId}`

**Authorization:** `Users.View`.

**Route parameters:** `userId` is the opaque User identifier. No query
parameters.

**Request DTO:** None.

**Response DTO:** `AdminUserDto`.

**Validation and business rules:** Active and inactive users are readable.
No password or token data is returned.

**HTTP status codes:** `200 OK`, `400 Bad Request`, `401 Unauthorized`,
`403 Forbidden`, `404 Not Found`, `500 Internal Server Error`.

**Example request:**
`GET /api/admin/users/c3d358ab-35bb-4018-8988-346381f6422c`

**Example response:** one `AdminUserDto` as shown in section 16.1.

**Error cases:** `USER_NOT_FOUND` (`404`), `VALIDATION_FAILED` (`400`),
`AUTHENTICATION_REQUIRED` (`401`), `FORBIDDEN` (`403`).

### 16.4 Activate user

**Method and path:** `POST /api/admin/users/{userId}/activate`

**Authorization:** `Users.Manage`.

**Route parameters:** `userId`. No query parameters or request body.

**Request DTO:** None.

**Response DTO:** updated `AdminUserDto`.

**Validation and business rules:** Activation is idempotent. A transition from
inactive to active writes an `ActivateUser` AuditLog. It does not change the
user's role or historical references.

**HTTP status codes:** `200 OK`, `400 Bad Request`, `401 Unauthorized`,
`403 Forbidden`, `404 Not Found`, `500 Internal Server Error`.

**Example request:**
`POST /api/admin/users/15dbaf8b-cc8c-447c-b0a8-6dc076fb8e7c/activate`

**Example response:** the updated `AdminUserDto` with `isActive: true`.

**Error cases:** `USER_NOT_FOUND` (`404`), `VALIDATION_FAILED` (`400`),
`AUTHENTICATION_REQUIRED` (`401`), `FORBIDDEN` (`403`).

### 16.5 Deactivate user

**Method and path:** `POST /api/admin/users/{userId}/deactivate`

**Authorization:** `Users.Manage`.

**Route parameters:** `userId`. No query parameters or request body.

**Request DTO:** None.

**Response DTO:** updated `AdminUserDto`.

**Validation and business rules**

- The authenticated Admin cannot deactivate their own account.
- Deactivation is idempotent for another already-inactive user.
- A transition from active to inactive writes a `DeactivateUser` AuditLog.
- The user remains persisted and all historical references remain intact.
- An inactive user cannot complete a subsequent login.

**HTTP status codes:** `200 OK`, `400 Bad Request`, `401 Unauthorized`,
`403 Forbidden`, `404 Not Found`, `409 Conflict`,
`500 Internal Server Error`.

**Example request:**
`POST /api/admin/users/15dbaf8b-cc8c-447c-b0a8-6dc076fb8e7c/deactivate`

**Example response:** the updated `AdminUserDto` with `isActive: false`.

**Error cases:** `USER_NOT_FOUND` (`404`),
`SELF_DEACTIVATION_NOT_ALLOWED` (`409`), `VALIDATION_FAILED` (`400`),
`AUTHENTICATION_REQUIRED` (`401`), `FORBIDDEN` (`403`).

### 16.6 Update user role

**Method and path:** `PATCH /api/admin/users/{userId}/role`

**Authorization:** `Users.Manage`.

**Route parameters:** `userId`. No query parameters.

**Request DTO:** `UpdateUserRoleRequestDto`.

**Response DTO:** updated `AdminUserDto`.

**Validation and business rules**

- `role` must be exactly `Admin` or `User`.
- The authenticated Admin cannot change their own role.
- Submitting the target's existing role is idempotent and creates no AuditLog.
- A role transition writes an `UpdateUserRole` AuditLog describing the old and
  new roles.
- Role changes do not alter activity state or historical references.

**HTTP status codes:** `200 OK`, `400 Bad Request`, `401 Unauthorized`,
`403 Forbidden`, `404 Not Found`, `409 Conflict`,
`500 Internal Server Error`.

**Example request**

```json
{ "role": "Admin" }
```

**Example response:** the updated `AdminUserDto` with `role: "Admin"`.

**Error cases:** `USER_NOT_FOUND` (`404`), `INVALID_ROLE` (`400`),
`SELF_ROLE_CHANGE_NOT_ALLOWED` (`409`), `AUTHENTICATION_REQUIRED` (`401`),
`FORBIDDEN` (`403`).

### 16.7 Edit user identity

**Method and path:** `PATCH /api/admin/users/{userId}`

**Authorization:** `Users.Manage`.

**Route parameters:** `userId`. No query parameters.

**Request DTO — UpdateAdminUserRequestDto**

```json
{
  "username": "alex.morgan",
  "fullName": "Alex Morgan",
  "email": "alex.morgan@example.test"
}
```

**Response DTO:** updated `AdminUserDto`.

**Validation and business rules**

- `username`, `fullName`, and `email` are required after trimming.
- `email` must be syntactically valid.
- Username and email must remain unique, excluding the target user.
- The operation does not accept or modify role, activity state, password hash,
  permissions, or historical references.
- Submitting unchanged values is idempotent and creates no AuditLog.
- A meaningful update writes an `AdminEditUser` AuditLog without credential
  data.
- Database unique constraints remain authoritative for concurrent writes; a
  late uniqueness conflict returns `409` rather than silently overwriting data.

**HTTP status codes:** `200 OK`, `400 Bad Request`, `401 Unauthorized`,
`403 Forbidden`, `404 Not Found`, `409 Conflict`, `500 Internal Server Error`.

**Error cases:** `USER_NOT_FOUND` (`404`), `VALIDATION_FAILED` (`400`),
`USERNAME_ALREADY_EXISTS` (`409`), `EMAIL_ALREADY_EXISTS` (`409`),
`AUTHENTICATION_REQUIRED` (`401`), `FORBIDDEN` (`403`).

## 17. Audit Log administration

### 17.1 List audit logs

**Method and path:** `GET /api/admin/audit-logs`

**Authorization:** `AuditLogs.View`.

**Route/query parameters:** None. MVP search and action/entity filtering are
client-side.

**Request DTO:** None.

**Response DTO:** `AdminAuditLogDto[]`, ordered newest `createdAt` first.

**Validation and business rules**

- Each entry includes its persisted performer, action type, entity type,
  entity identifier, description, and creation time.
- Performer information is a read projection from the referenced User.
- Audit Logs are read-only. No POST, PUT, PATCH, or DELETE Audit Log endpoint
  exists.
- Deactivating or changing a User must not remove or rewrite prior Audit Logs.
- An empty result returns `200 OK` with `[]`.

**HTTP status codes:** `200 OK`, `401 Unauthorized`, `403 Forbidden`,
`500 Internal Server Error`.

**Example request**

```http
GET /api/admin/audit-logs HTTP/1.1
Authorization: Bearer <admin-access-token>
```

**Example response**

```json
[
  {
    "id": "c4ca0aa3-f073-4dfa-afef-0b51d8318f9b",
    "performedBy": {
      "id": "c3d358ab-35bb-4018-8988-346381f6422c",
      "username": "admin",
      "fullName": "Development Admin"
    },
    "actionType": "DeactivateUser",
    "entityType": "User",
    "entityId": "15dbaf8b-cc8c-447c-b0a8-6dc076fb8e7c",
    "description": "Deactivated user 'author'.",
    "createdAt": "2026-10-01T09:00:00Z"
  }
]
```

**Error cases:** `AUTHENTICATION_REQUIRED` (`401`), `FORBIDDEN` (`403`).

## 18. Fixed permission-based authorization

Permissions are code-defined constants and JWT claims. They are not additional
domain entities, database tables, or Admin-managed records.

```text
Templates.View
Templates.Manage
Documents.Create
Documents.ViewOwn
Documents.EditOwn
Users.View
Users.Manage
AuditLogs.View
```

The fixed role matrix is:

| Permission | User | Admin |
|---|:---:|:---:|
| `Templates.View` | yes | yes |
| `Templates.Manage` | no | yes |
| `Documents.Create` | yes | yes |
| `Documents.ViewOwn` | yes | yes |
| `Documents.EditOwn` | yes | yes |
| `Users.View` | no | yes |
| `Users.Manage` | no | yes |
| `AuditLogs.View` | no | yes |

Policy mapping:

- gallery, active template detail, current version, and author placeholder reads
  require `Templates.View`;
- Admin Category, Template, TemplateVersion, and Placeholder mutation/read
  endpoints require `Templates.Manage`;
- document creation requires `Documents.Create`;
- own document history, detail, and download require `Documents.ViewOwn`;
- own document update, preview, and finalize require `Documents.EditOwn`;
- Admin user list/detail require `Users.View`;
- Admin user creation, activation, deactivation, and role update require
  `Users.Manage`;
- Admin user identity editing requires `Users.Manage`;
- read-only Audit Log access requires `AuditLogs.View`.

JWTs include one claim per granted permission. Login, registration, and
`GET /api/auth/me` expose the same effective permission list through `UserDto`.
The frontend uses that list only to present permitted navigation/actions; the
API policies remain authoritative. A valid token without the policy permission
receives `403 Forbidden`.

## 19. Phase 6H account and profile management

Phase 6H keeps account DTOs separate from the `User` and
`PasswordResetToken` domain entities. Passwords, hashes, and persisted reset
records are never returned.

```text
UpdateOwnProfileRequestDto
  fullName: string
  email: string

ChangePasswordRequestDto
  currentPassword: string
  newPassword: string

ForgotPasswordRequestDto
  email: string

ForgotPasswordResponseDto
  message: string

ResetPasswordRequestDto
  token: string
  newPassword: string
```

Password confirmation is enforced by the frontend and is not transmitted.

### 19.1 Update own profile

**Method and path:** `PATCH /api/users/me`

**Authorization:** any authenticated, active user. No additional permission is
required.

**Route/query parameters:** None.

**Request DTO — UpdateOwnProfileRequestDto**

```json
{
  "fullName": "Maya Chen",
  "email": "maya.chen@example.test"
}
```

**Response DTO:** updated `UserDto`, including the unchanged role and effective
permission list.

**Validation and business rules**

- `fullName` and `email` are required after trimming.
- `email` must be syntactically valid and unique, excluding the current user.
- Username is intentionally immutable through self-service.
- Role, permissions, activity state, password, and identifiers cannot be
  supplied or changed.
- An unchanged request is idempotent and creates no AuditLog.
- A meaningful update writes a `ProfileUpdate` AuditLog.
- The frontend replaces its cached current-user identity after success.
- A late database uniqueness conflict returns `409`.

**HTTP status codes:** `200 OK`, `400 Bad Request`, `401 Unauthorized`,
`409 Conflict`, `500 Internal Server Error`.

**Error cases:** `VALIDATION_FAILED` (`400`),
`EMAIL_ALREADY_EXISTS` (`409`), `AUTHENTICATION_REQUIRED` (`401`).

### 19.2 Change own password

**Method and path:** `POST /api/users/me/change-password`

**Authorization:** any authenticated, active user. No user identifier is
accepted from the client.

**Request DTO — ChangePasswordRequestDto**

```json
{
  "currentPassword": "current-password",
  "newPassword": "new-password"
}
```

**Response DTO:** None.

**Validation and business rules**

- Both values are required. The new password follows the current project rule:
  it must be non-empty.
- `currentPassword` must verify against the authenticated user's stored hash.
- The new password is hashed with the existing password hasher.
- Success writes a `ChangePassword` AuditLog without password material.
- Success returns `204`; the frontend clears the local JWT, authenticated
  identity, and query cache, then redirects to `/login?passwordChanged=1`.
- Because JWTs are stateless and there is no revocation store, other previously
  issued tokens remain valid until their configured expiry.

**HTTP status codes:** `204 No Content`, `400 Bad Request`,
`401 Unauthorized`, `500 Internal Server Error`.

**Error cases:** `VALIDATION_FAILED` (`400`),
`CURRENT_PASSWORD_INVALID` (`400`), `AUTHENTICATION_REQUIRED` (`401`).

### 19.3 Forgot password

**Method and path:** `POST /api/auth/forgot-password`

**Authorization:** Anonymous.

**Route/query parameters:** None.

**Request DTO — ForgotPasswordRequestDto**

```json
{ "email": "maya@example.test" }
```

**Response DTO — ForgotPasswordResponseDto**

```json
{
  "message": "If an active account matches that email, password reset instructions have been sent."
}
```

**Validation and privacy rules**

- `email` is required and must be syntactically valid; malformed input receives
  ordinary field validation.
- A syntactically valid unknown, inactive, or active account receives the same
  `202` response semantics.
- Only an active matching account receives a new token and development email.
- The raw token is cryptographically random and is passed only to the email
  abstraction. Only its SHA-256 hash is persisted.
- The API response never contains the raw token, reset URL, account state, or
  persisted reset-token data.

**HTTP status codes:** `202 Accepted`, `400 Bad Request`,
`429 Too Many Requests`, `500 Internal Server Error`.

**Error cases:** `VALIDATION_FAILED` (`400`), `RATE_LIMIT_EXCEEDED` (`429`).
Account lookup never produces a `404` or authentication-specific response.

### 19.4 Reset password

**Method and path:** `POST /api/auth/reset-password`

**Authorization:** Anonymous.

**Route/query parameters:** None. The frontend reads `token` from its own URL
and sends it in the JSON body.

**Request DTO — ResetPasswordRequestDto**

```json
{
  "token": "raw-url-safe-reset-token",
  "newPassword": "new-password"
}
```

**Response DTO:** None. The user is not authenticated automatically.

**Validation and security rules**

- `token` and `newPassword` are required. The new password follows the current
  non-empty project rule.
- The raw token is SHA-256 hashed before lookup and is never persisted.
  An explicitly enabled Development-only email adapter may log the reset URL;
  non-Development environments must not expose it.
- Persisted tokens expire 30 minutes after creation and may be used once.
- Invalid, expired, already-used, unknown-user, and inactive-user cases return
  the same `INVALID_RESET_TOKEN` response.
- If an account becomes inactive after token issuance, reset is rejected. The
  token remains unusable while inactive and expires normally.
- Password update and marking `UsedAt` occur atomically. A concurrent second
  use cannot succeed.
- Success writes a `ResetPassword` AuditLog and returns `204`. The frontend
  redirects to `/login?passwordReset=1`.

**HTTP status codes:** `204 No Content`, `400 Bad Request`,
`422 Unprocessable Entity`, `429 Too Many Requests`,
`500 Internal Server Error`.

**Error cases:** `VALIDATION_FAILED` (`400`),
`INVALID_RESET_TOKEN` (`422`), `RATE_LIMIT_EXCEEDED` (`429`).

### 19.5 Password reset token persistence

`PasswordResetToken` is a domain entity and persistence record with:

```text
Id
UserId
TokenHash
ExpiresAt
UsedAt?
CreatedAt
```

`User 1 -> 0..* PasswordResetToken`. `TokenHash` is unique; the User foreign
key uses restrictive deletion. Raw tokens are never stored. The reset workflow
uses one transaction so password update and one-time token consumption commit
together.

## 20. Operational endpoints

These routes are outside the `/api` business prefix and require no
authentication. They are intended for platform probes, not application UI.

| Purpose | Method | Path | Healthy response | Unhealthy response |
|---|---|---|---|---|
| Backward-compatible liveness | `GET` | `/health` | `200` | not dependency-sensitive |
| Process liveness | `GET` | `/health/live` | `200` | not dependency-sensitive |
| PostgreSQL readiness | `GET` | `/health/ready` | `200` | `503` |

Readiness verifies that EF Core can connect to the configured PostgreSQL
database. It does not apply migrations, prove migration currency, or validate
future external services. Operational responses receive the normal API
security headers.
