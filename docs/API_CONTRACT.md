# API Contract — First Authoring Vertical Slice

Status: **design contract only; endpoints are not implemented**.

This document derives the minimum REST contract for the current author-facing
frontend and the approved rules in `AGENTS.md` and `PRODUCT.md`. It deliberately
excludes administration, sharing, collaboration, registration, password reset,
token refresh, template mutation, and every other later-phase capability.

`AGENTS.md` section 7 is the only approved domain-model source currently present
in the repository. No separate ERD or Class Diagram file was found. If one is
added later and conflicts with this document, the decision priority in
`AGENTS.md` applies and this contract must be revised before implementation.

## 1. Contract boundaries

- Base path: `/api`
- Media type for JSON: `application/json`
- Authentication: JWT bearer token in `Authorization: Bearer <token>`
- Protected roles in this slice: `User` and `Admin`
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

## 2. Conflicts and unresolved decisions

These points must not be silently encoded into entities or persistence:

1. **Template description:** `TemplateSummary` and `TemplateDetail` in the mock
   frontend require `description`, but the approved `Template` and
   `TemplateVersion` models contain no such field. The normative DTOs below do
   not expose a description. The frontend must omit that copy during integration
   unless the domain model is explicitly amended or an approved projection
   source is identified.
2. **Template updated date:** the mock exposes `TemplateSummary.updatedAt`, but
   `Template` has no `UpdatedAt`. The contract exposes the current version's
   `updatedAt` under `currentVersion`; it must not be mislabeled as a template
   modification timestamp.
3. **Fixed categories:** the mock hard-codes `Business`, `Finance`, `Human
   resources`, and `Operations` as a TypeScript union. The approved model treats
   Category as managed data. DTOs therefore return `{ id, name }`; the client
   must not assume a closed category enum.
4. **Placeholder examples:** the mock has `PlaceholderDefinition.example`, but
   the approved Placeholder has only `DefaultValue?`. The API does not expose
   `example`. A default value may be shown as a default, not relabeled as an
   example.
5. **Identifier shape:** mock template/document IDs are readable slugs. The
   approved model specifies only `Id`. API IDs remain opaque.
6. **Placeholder-value shape:** the mock uses `Record<placeholderKey, value>`.
   The approved model requires `PlaceholderId?`, key/label/data-type snapshots,
   and value. Document responses therefore return an array of snapshot DTOs.
   A frontend adapter may build a key/value map for form state but must retain
   the full DTO for history.
7. **Invalid finalized examples:** some mocked Finalized documents have empty
   placeholder maps even though their source templates contain required fields.
   Such records violate the approved finalization rules and are not valid API
   examples.
8. **Preview behavior:** the mock locally substitutes values and leaves tokens
   visible when required values are absent. The approved rules require
   placeholder validation before rendering. The API preview operation therefore
   returns `422` rather than rendering invalid required or typed values.
9. **Content representation:** mock content is plain text containing
   `{{placeholder_key}}` tokens while the approved initial persisted format is
   `Html`. The API contract uses `contentFormat: "Html"`. The exact editor and
   token-to-Composite parsing mechanism remains unresolved and is not defined
   here.
10. **Download format:** Download is present in the mock, but `DESIGN.md`
    explicitly leaves PDF, DOCX, HTML, and other formats unresolved. The route
    is reserved below, but its success media type, filename extension, and
    renderer are **not implementable until a format is approved**.
11. **Download lifecycle:** the mock enables Download only for Finalized
    documents. The approved business rules do not explicitly say drafts cannot
    be downloaded. This contract does not add that backend restriction. The
    question must be decided with the export format.
12. **User identity:** the shell hard-codes a user name and does not implement
    authentication UI. `/api/auth/me` supplies the real shell identity.

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
  "role": "User"
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
| Current identity | `GET` | `/api/auth/me` |
| Active template gallery | `GET` | `/api/templates` |
| Template detail | `GET` | `/api/templates/{templateId}` |
| Current template version | `GET` | `/api/templates/{templateId}/current-version` |
| Current-version placeholders | `GET` | `/api/template-versions/{templateVersionId}/placeholders` |
| Create draft | `POST` | `/api/documents` |
| Load document | `GET` | `/api/documents/{documentId}` |
| Update draft | `PUT` | `/api/documents/{documentId}` |
| Preview draft | `POST` | `/api/documents/{documentId}/preview` |
| Finalize document | `POST` | `/api/documents/{documentId}/finalize` |
| Download document | `GET` | `/api/documents/{documentId}/download` |
| Document history | `GET` | `/api/documents` |

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
    "role": "User"
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

### 5.2 Get current identity

**Method and path:** `GET /api/auth/me`

**Authorization:** Bearer token; role `User` or `Admin`.

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
  "role": "User"
}
```

**Error cases:** `AUTHENTICATION_REQUIRED` (`401`), `INVALID_TOKEN` (`401`).

## 6. Template endpoints

### 6.1 List active templates

**Method and path:** `GET /api/templates`

**Authorization:** Bearer token; role `User` or `Admin`.

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

**Authorization:** Bearer token; role `User` or `Admin`.

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

**Authorization:** Bearer token; role `User` or `Admin`.

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

**Authorization:** Bearer token; role `User` or `Admin`.

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

**Authorization:** Bearer token; role `User` or `Admin`.

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

**Authorization:** Bearer token; caller must own the document in this slice.

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

**Method and path:** `PUT /api/documents/{documentId}`

**Authorization:** Bearer token; caller must own the document.

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
- `title` must be non-empty after trimming.
- `content` is required and represents the independent Document copy; updating
  it never changes TemplateVersion content.
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
PUT /api/documents/b4382058-feb4-4cd4-b8bf-627a13822161 HTTP/1.1
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

### 7.4 Preview draft document

**Method and path:** `POST /api/documents/{documentId}/preview`

**Authorization:** Bearer token; caller must own the document.

**Route parameters:** `documentId` — opaque Document identifier.

**Query parameters:** None.

**Request DTO — PreviewDocumentRequestDto**

The request carries the current unsaved editor state so Preview does not need to
save or mutate the Draft.

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

- Document must be an owned Draft.
- Request values are transient and are not persisted by Preview.
- Source TemplateVersion is never mutated.
- Placeholder IDs must belong to the retained source version and be unique.
- Required values must be present and all values must pass their Strategy
  validator before rendering.
- Rendering uses the approved Composite abstraction and returns HTML in this
  initial slice.
- Returning to edit leaves the Draft unchanged.

**HTTP status codes:** `200 OK`, `400 Bad Request`, `401 Unauthorized`,
`403 Forbidden`, `404 Not Found`, `409 Conflict`,
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
- `DOCUMENT_FINALIZED` (`409`)
- `MISSING_REQUIRED_PLACEHOLDER` (`422`)
- `INVALID_PLACEHOLDER_VALUE` (`422`)
- `PLACEHOLDER_NOT_IN_SOURCE_VERSION` (`422`)

### 7.5 Finalize document

**Method and path:** `POST /api/documents/{documentId}/finalize`

**Authorization:** Bearer token; caller must own the document.

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

### 7.6 Download document — reserved contract

**Method and path:** `GET /api/documents/{documentId}/download`

**Implementation status:** **Blocked by the unresolved export-format decision.**
Do not implement this endpoint until PDF, DOCX, HTML, or another exact format is
approved. Implementing `application/octet-stream` as a hidden default would
silently choose export semantics and is not approved.

**Authorization:** Bearer token; caller must own the document.

**Route parameters:** `documentId` — opaque Document identifier.

**Query parameters:** None. A `format` query parameter is intentionally not
invented before supported formats are approved.

**Request DTO:** None.

**Response DTO:** No JSON DTO on success. The eventual success response is a
binary file result with:

```http
Content-Type: <pending approved export media type>
Content-Disposition: attachment; filename*=UTF-8''<safe-title>.<pending-extension>
```

JSON failures still use `ErrorResponseDto`.

**Validation and business rules currently known**

- Document must exist and be authorized for the caller.
- Export must use the Document's stored independent content and stored
  placeholder snapshots, never the latest TemplateVersion.
- The permitted lifecycle state is unresolved; the mock's Finalized-only rule
  is not promoted to a backend rule here.

**HTTP status codes after the format is approved:** `200 OK`,
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
Content-Type: <pending approved export media type>
Content-Disposition: attachment; filename*=UTF-8''Acme-consulting-agreement.<pending-extension>

<binary body in the approved format>
```

**Error cases currently known:** `DOCUMENT_NOT_FOUND` (`404`),
`UNAUTHORIZED_DOCUMENT_ACCESS` (`403`), `EXPORT_FAILED` (`422`). Exact
format-specific validation remains pending.

### 7.7 List document history

**Method and path:** `GET /api/documents`

**Authorization:** Bearer token; role `User` or `Admin`.

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

The future frontend API layer should map DTOs rather than changing domain or
transport contracts to mirror the existing mocks:

| Current mock field/behavior | API source or required change |
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
| Local Save Draft | Call `PUT /api/documents/{documentId}` |
| Local Preview substitution | Call the non-mutating preview endpoint |
| Local Finalize state change | Call the atomic finalize endpoint |
| Hard-coded shell user | Load `GET /api/auth/me` |
| Date display strings | Format ISO values in the presentation layer |

## 9. Explicit exclusions

This contract does not define:

- registration, logout, refresh tokens, password reset, or profile editing;
- inactive/draft template browsing for authors;
- historical template-version browsing outside retained Document history;
- category management or a standalone category endpoint;
- admin template, version, placeholder, user, or audit-log endpoints;
- sharing, comments, teams, collaboration, approval chains, or cross-user access;
- autosave guarantees, bulk operations, server sorting, pagination, or search;
- template mutation or editing Published versions;
- an editor technology or a JSON content editor;
- an export/download format.

Those capabilities require separate approval and contract work.
