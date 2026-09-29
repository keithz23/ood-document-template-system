# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

The primary users are document authors who need to create documents from
reusable templates without managing template internals. Their core workflow is:

1. Browse active templates.
2. Select a template and its current published version.
3. Fill and validate placeholder values.
4. Edit and preview the independent document copy.
5. Save a draft or finalize and download the document.

Administrators are the secondary audience. They need a clear, efficient way to
manage categories, templates, template versions, placeholders, users, and audit
logs while preserving historical records.

## Product Purpose

Document Template System is a generic academic and demonstration product for an
OOD and Design Patterns project. It shows how users can create, edit, preview,
save, finalize, and eventually download documents from governed, versioned
templates while keeping each created document independent from later template
changes.

Success means the primary authoring flow is straightforward, saved documents
remain historically accurate, administrative changes are safe, and the required
design patterns remain visible, testable, and easy to explain during a project
presentation.

## Positioning

The product is an explicit, defensible reference implementation of document
templating through three required patterns: Prototype creates independent
documents from template versions, Composite represents renderable document
content, and Strategy validates typed placeholder values. Version retention and
placeholder snapshots make the system's historical behavior demonstrable rather
than implicit.

## Operating Context

The system is domain-neutral and should support reusable templates for
contracts, CVs, invoices, reports, and other document types. Authors work through
a guided create-and-preview flow. Administrators maintain the reusable source
material and governance records separately from authored documents.

The project is also evaluated as an academic software-design artifact. Class
boundaries, dependency direction, business rules, tests, and named pattern
abstractions must remain understandable and presentation-ready.

## Capabilities and Constraints

- Users authenticate before protected actions; the initial roles are `Admin`
  and `User`.
- Only active templates with a published current version can create documents.
- Documents retain their source template-version identity but become independent
  copies after creation.
- Draft documents are editable; finalized documents are read-only.
- Template changes and deactivation never mutate existing documents.
- Placeholder values support Text, Number, Date, and Email types and must use
  the Strategy validation workflow.
- Saved placeholder values retain key, label, and data-type snapshots for
  historical integrity.
- Published template versions are immutable; edits create a new version.
- Prototype, Composite, and Strategy are mandatory architectural requirements
  and must not be bypassed.
- HTML is the initial persisted content format; JSON remains extensible but is
  outside the initial implementation.
- The product remains a single-repository web application with a Next.js
  frontend and ASP.NET Core REST API backed by PostgreSQL.
- The implementation should remain simple, explicit, testable, and suitable for
  an MVP rather than introducing unnecessary architectural complexity.

## Brand Commitments

The product name is **Document Template System**. There are no existing brand
assets or institutional branding requirements.

The interface must behave like a professional productivity tool: clean,
neutral, readable, consistent, and task-focused. Avoid marketing-style layouts,
excessive gradients, decorative animation, and portfolio-like presentation.

## Evidence on Hand

- `AGENTS.md` is the authoritative source for the approved domain model,
  business rules, architecture, implementation order, and required patterns.
- The repository contains a Phase 1 frontend and backend scaffold, PostgreSQL
  configuration, health endpoint, Swagger/OpenAPI, and architecture and
  integration smoke tests.
- No logo, brand imagery, testimonials, customer evidence, benchmarks, pricing,
  or organization-specific content exists. Future work must not fabricate them.

## Product Principles

1. **Authoring comes first.** Optimize the browse-to-finalize journey before
   expanding secondary administration features.
2. **History is durable.** Version changes must never rewrite created documents
   or erase the context needed to understand them.
3. **Patterns stay visible.** Required OOD abstractions must be evident in the
   class design, workflows, and tests rather than hidden behind framework code.
4. **Generic by design.** Workflows and terminology should accommodate many
   document types without assuming a single industry.
5. **Clarity over cleverness.** Prefer explicit rules, predictable states, and
   presentation-ready code over unnecessary sophistication.

## Accessibility & Inclusion

Target WCAG 2.2 AA where practical. Prioritize keyboard access, readable type,
clear focus and validation states, sufficient contrast, responsive layouts, and
consistent interaction patterns across author and administrator workflows.

