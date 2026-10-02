---
name: Document Template System
description: A restrained operational workspace for creating and governing versioned documents.
colors:
  canvas: "oklch(0.985 0 0)"
  surface: "oklch(1 0 0)"
  surface-subtle: "oklch(0.97 0 0)"
  foreground: "oklch(0.145 0 0)"
  foreground-muted: "oklch(0.556 0 0)"
  primary: "oklch(0.205 0 0)"
  primary-foreground: "oklch(0.985 0 0)"
  border: "oklch(0.922 0 0)"
  focus: "oklch(0.488 0.243 264.376)"
  information: "oklch(0.488 0.243 264.376)"
  information-subtle: "oklch(0.97 0.014 254.604)"
  success: "oklch(0.448 0.119 151.328)"
  success-subtle: "oklch(0.982 0.018 155.826)"
  warning: "oklch(0.473 0.137 46.201)"
  warning-subtle: "oklch(0.987 0.022 95.277)"
  destructive: "oklch(0.505 0.213 27.518)"
  destructive-subtle: "oklch(0.971 0.013 17.38)"
typography:
  headline:
    fontFamily: "Arial, Helvetica, sans-serif"
    fontSize: "1.5rem"
    fontWeight: 600
    lineHeight: 1.333
    letterSpacing: "-0.015em"
  title:
    fontFamily: "Arial, Helvetica, sans-serif"
    fontSize: "1.125rem"
    fontWeight: 600
    lineHeight: 1.556
    letterSpacing: "-0.005em"
  body:
    fontFamily: "Arial, Helvetica, sans-serif"
    fontSize: "0.875rem"
    fontWeight: 400
    lineHeight: 1.429
    letterSpacing: "normal"
  label:
    fontFamily: "Arial, Helvetica, sans-serif"
    fontSize: "0.8125rem"
    fontWeight: 500
    lineHeight: 1.385
    letterSpacing: "normal"
  caption:
    fontFamily: "Arial, Helvetica, sans-serif"
    fontSize: "0.75rem"
    fontWeight: 400
    lineHeight: 1.333
    letterSpacing: "0.005em"
rounded:
  sm: "6px"
  md: "8px"
  lg: "10px"
  xl: "14px"
  full: "999px"
spacing:
  xxs: "4px"
  xs: "8px"
  sm: "12px"
  md: "16px"
  lg: "24px"
  xl: "32px"
  2xl: "48px"
  3xl: "64px"
components:
  button-primary:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.primary-foreground}"
    typography: "{typography.label}"
    rounded: "{rounded.md}"
    padding: "8px 14px"
    height: "36px"
  button-secondary:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.foreground}"
    typography: "{typography.label}"
    rounded: "{rounded.md}"
    padding: "8px 14px"
    height: "36px"
  field:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.foreground}"
    typography: "{typography.body}"
    rounded: "{rounded.md}"
    padding: "8px 12px"
    height: "40px"
  card:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.foreground}"
    rounded: "{rounded.lg}"
    padding: "24px"
  status-badge:
    backgroundColor: "{colors.surface-subtle}"
    textColor: "{colors.foreground}"
    typography: "{typography.caption}"
    rounded: "{rounded.full}"
    padding: "3px 8px"
---

# Design System: Document Template System

## Overview

**Creative North Star: "The Quiet Workbench"**

The interface is a focused place for sustained document work. It should feel
calm, exact, and dependable: closer to a well-organized desktop application than
a promotional website. Hierarchy comes from alignment, type weight, spacing,
and carefully layered neutral surfaces. Color is reserved for focus, status,
validation, and consequential actions.

The system favors operational density without becoming cramped. Primary author
workflows receive the clearest route and largest working area; administrative
screens use the same visual language at a slightly denser information level.
Copy is direct and literal. Motion is limited to brief state transitions,
progress feedback, and spatial orientation; it never decorates idle surfaces.

The current light theme is the reference baseline. Dark-theme tokens exist in
the scaffold, but dark mode is not a confirmed product requirement and must not
drive implementation decisions until explicitly scoped.

**Key Characteristics:**

- Restrained neutral palette with semantic color used sparingly.
- Clear page structure and compact, consistent controls.
- Border and tonal hierarchy before shadow.
- Document-centered workspaces with persistent context and actions.
- Accessible states communicated by text, shape, and icon as well as color.

**The Work Before Decoration Rule.** Every visual element must improve
orientation, comprehension, input, comparison, or feedback. If it does none of
those jobs, remove it.

## Colors

The palette is predominantly achromatic so document content remains the visual
focus. The frontmatter is the normative color source; implementation should map
these roles to Tailwind and shadcn semantic variables rather than using raw
values inside feature components.

### Primary

- **Workbench Ink:** The near-black primary is used for primary buttons,
  selected navigation, strong text, and high-confidence actions. It should not
  become a large decorative field.
- **Clear Focus Blue:** The focus color is reserved for keyboard focus,
  selection, links, current-version markers, and informational emphasis.

### Secondary

- **Completion Green:** Use success roles for Active, Published, Finalized, and
  confirmed successful operations. Success backgrounds remain pale; saturated
  green should appear primarily in text, icons, or narrow indicators.
- **Working Amber:** Use warning roles for actions or states that require
  attention without being errors. Draft is normally neutral, not amber; amber is
  for actual caution such as unsaved changes or a blocking prerequisite.
- **Validation Red:** Destructive color is for validation failures, failed
  operations, and truly destructive actions. Inactive and read-only states are
  neutral rather than red.

### Neutral

- **Canvas Gray:** The application canvas separates the persistent shell from
  white working surfaces without looking tinted or branded.
- **Paper Surface:** White surfaces represent documents, forms, tables,
  popovers, and dialogs.
- **Quiet Fill:** The subtle neutral fill supports selected rows, secondary
  controls, skeletons, and grouped regions.
- **Graphite Text:** Primary copy uses the darkest neutral; muted copy is limited
  to metadata, hints, and secondary context.
- **Hairline Border:** Borders define fields, table structure, cards, and panel
  boundaries. They are structural, not ornamental.

**The Semantic Color Rule.** A colored element must communicate focus, status,
validation, or action. Do not add color merely to make a neutral screen feel
more exciting.

**The Status Is More Than Color Rule.** Every status treatment includes a text
label and, where space permits, a distinct icon or marker. Color alone never
carries lifecycle meaning.

## Typography

**Display Font:** None. This operational product does not use marketing display
type.

**Body Font:** Arial, with Helvetica and generic sans-serif fallbacks.

**Label/Mono Font:** Use the body family for labels. Use the existing system
monospace stack only for placeholder keys, technical identifiers, and code-like
values.

**Character:** Typography is compact, familiar, and deliberately quiet. One
sans-serif family carries the entire interface; hierarchy comes from weight,
size, spacing, and placement rather than decorative font pairing.

### Hierarchy

- **Page headline** (600, 24px, 32px line height): One per route. Keep it short
  and pair it with optional muted context rather than oversized introductory
  copy.
- **Section title** (600, 18px, 28px line height): Major panels, form groups,
  and dialog titles.
- **Control title** (600, 16px, 24px line height): Card titles and prominent row
  labels.
- **Body** (400, 14px, 20px line height): Default UI copy, table cells, form
  values, and descriptions. Explanatory prose should remain within 68 characters
  per line where practical.
- **Label** (500, 13px, 18px line height): Form labels, button text, navigation,
  and compact controls. Use sentence case.
- **Caption** (400, 12px, 16px line height): Timestamps, IDs, hints, and secondary
  metadata. Do not use captions for essential instructions.

Numeric table columns use tabular figures when available. Placeholder keys and
version identifiers may use monospace, but ordinary names and status labels do
not.

**The Sentence Case Rule.** Use sentence case for headings, actions, navigation,
table headers, and badges. Avoid all caps and headline-style title casing.

**The One Headline Rule.** Each screen has one page headline. Panels beneath it
step down predictably; they do not compete at the same visual size.

## Layout

The application uses a persistent shell on desktop and a compact top bar with a
drawer on small screens. The desktop shell consists of a 240px navigation rail,
a top utility region no taller than 56px when needed, and a flexible main area.
List and administration pages use a readable maximum content width of 1440px.
Editor workspaces may use the full remaining viewport width.

Pages follow a consistent vertical sequence: breadcrumb when useful, page
header, optional action row, feedback region, then primary content. Page-level
horizontal padding is 32px on large screens, 24px on tablets, and 16px on small
screens. Major sections are separated by 24-32px; related controls use 8-16px.
The spacing scale in frontmatter is normative. Arbitrary spacing values require
a concrete layout reason.

Use CSS grid for page and editor regions, flex layouts for toolbars and compact
control groups, and normal document flow for forms. Keep labels, fields, and
actions aligned to a stable column grid. Avoid grids of interchangeable cards
when a list or table better supports scanning and comparison.

### Navigation structure

- The primary user group contains **Templates** and **My documents**.
- Admin-only navigation is visually separated and contains **Categories**,
  **Templates**, **Users**, and **Audit logs**. Template versions and
  placeholders live inside template detail rather than becoming permanent
  top-level destinations.
- The active destination uses a quiet filled background, strong text, and a
  narrow leading indicator. Hover alone must not resemble the active state.
- Breadcrumbs communicate resource hierarchy on detail and editor screens; they
  do not repeat a single top-level page name.
- The shell may display the signed-in user's identity and role. Additional
  account-management features are not implied by this design document.

### Responsive behavior

- **Large screens (1280px and above):** Persistent navigation and full-width
  working layouts. The editor uses simultaneous input and document panes.
- **Medium screens (768-1279px):** Navigation collapses to an icon rail or
  drawer. Secondary editor panels become collapsible so the document area stays
  usable.
- **Small screens (below 768px):** Navigation moves into a labeled drawer;
  toolbars wrap or move secondary actions into an overflow menu. Forms become
  single-column. Tables expose priority fields first and allow deliberate
  horizontal scrolling only when column comparison is essential.
- The document workflow becomes one active pane at a time—**Fields**,
  **Document**, or **Preview**—with an explicit labeled switcher. State must
  survive pane changes.
- Primary actions may use a bottom sticky action region on small screens, but it
  must not obscure fields, validation messages, or browser controls.

**The Stable Workspace Rule.** Loading, validation, and mode changes must not
reflow the entire page. Reserve space for feedback and preserve the user's
scroll position whenever possible.

## Elevation & Depth

The system is flat by default. Hierarchy comes from canvas-to-surface contrast,
hairline borders, and grouped neutral fills. Shadows are reserved for overlays
that physically sit above the workspace: dialogs, popovers, dropdown menus, and
temporary floating controls. Resting cards and table containers use borders,
not dramatic shadows.

### Shadow vocabulary

- **Surface trace** (`0 1px 2px rgb(0 0 0 / 0.04)`): Optional on a document
  sheet or elevated working surface when the surrounding canvas is visible.
- **Overlay** (`0 12px 32px rgb(0 0 0 / 0.12)`): Dialogs, popovers, and menus.
  Never apply it to every card in a collection.

Transitions use a standard 150ms ease-out for hover, focus, disclosure, and
selection. Dialog and drawer entry may use up to 200ms. Respect
`prefers-reduced-motion`; no workflow depends on animation to explain state.

**The Flat-by-Default Rule.** If a border or tonal change can communicate the
layer, do not add a shadow.

## Shapes

Corners are gently rounded and consistent rather than soft or playful. Fields
and buttons use the medium radius; cards and table containers use the large
radius; dialogs may use the extra-large radius. Small tags and status badges may
be pill-shaped because their compact silhouette communicates metadata.

Borders are one pixel at default density. Dividers should be used only where
spacing cannot communicate grouping, especially inside tables, side panels, and
toolbars. Document preview pages may use square paper corners on small screens
when edge-to-edge, but retain a slight radius when floating on a desktop canvas.

**The Pills Are Metadata Rule.** Reserve fully rounded shapes for status,
filters, and compact metadata. Buttons, cards, fields, and navigation items do
not become pills.

## Components

Use shadcn/ui primitives as accessible foundations, then apply these semantic
conventions through shared variants. Feature components compose primitives but
must not redefine foundational colors, radii, control heights, or focus styles.

### Buttons and actions

- Primary buttons represent the single strongest action in a region, such as
  **Use template**, **Save draft**, or a confirmed **Finalize** action.
- Secondary or outline buttons represent reversible alternatives such as
  **Preview**, **Back to edit**, or **Cancel**.
- Ghost buttons are for toolbar and row actions. Destructive actions use the
  destructive variant and require clear target naming; do not style ordinary
  deactivation as destructive when history is preserved.
- Default desktop controls are 36px high; form controls are 40px high. Touch
  layouts provide at least a 44px target through height or surrounding hit area.
- Buttons show a focused ring with sufficient contrast. A loading button keeps
  its width, replaces or precedes its label with a spinner, and prevents repeat
  submission without making the whole page inert.
- Icon-only actions require accessible names and tooltips. Prefer labeled
  actions when space allows.

### Forms

- Use React Hook Form for interaction and Zod for client-side schemas, while
  treating backend validation as authoritative.
- Labels are persistent and placed above fields. Placeholder text is an example,
  never the only label or instruction.
- Required fields use a text indicator explained once per form. Optional fields
  may be marked "Optional" when that reduces uncertainty.
- Put short helper text below the label or field. Put validation immediately
  below the affected control and connect it with `aria-describedby`.
- Validate obvious formatting on blur and all rules on submit. Do not announce
  errors on every keystroke. Before finalization, show a concise error summary
  that links to invalid placeholder fields.
- Match controls to data types: text input, number input with appropriate input
  mode, date control with an accessible text fallback, and email input. Preserve
  the user's entered value when server validation fails.
- Group placeholders under a clear section heading. The placeholder label is
  primary; its key and data type are secondary metadata.
- Finalized documents use read-only presentation, not a forest of disabled
  inputs. Explain the finalized state once in a persistent banner.

### Tables

- Use tables for repeatable records that benefit from cross-row comparison:
  templates, versions, documents, users, categories, and audit logs.
- Headers are left aligned except numeric or tightly scoped action columns.
  Default body rows are at least 44px high with 12px horizontal cell padding.
- The first meaningful column is the visual anchor and usually links to detail.
  Status and date columns remain compact. Row actions occupy the final column
  and use a labeled menu when more than two actions exist.
- Use a subtle selected or hover fill without removing row dividers. Keyboard
  focus must be visible on interactive cells and row actions.
- Sorting, filtering, pagination, bulk selection, and sticky headers appear only
  when the corresponding behavior exists; the design must not imply unsupported
  server features.
- On narrow screens, preserve the most important identity, status, and primary
  action. Move secondary metadata into an expandable detail region or allow
  horizontal scrolling when comparison would be lost by stacking.

### Status badges

- **Draft:** Neutral quiet fill and foreground-muted text.
- **Active / Published / Finalized:** Success-subtle fill and success text.
- **Current:** Information-subtle fill and information text; use alongside
  Published when both meanings matter rather than collapsing them.
- **Inactive:** Neutral quiet fill with muted text and an inactive marker. It is
  not an error state.
- **Validation or operation failure:** Destructive-subtle fill and destructive
  text, with an error icon when space permits.
- Use one or two words. Badges report state; they are not action buttons.

### Cards, dialogs, and feedback

- Cards group a coherent object or task, not every paragraph. Use 16px internal
  padding for compact cards and 24px for primary work panels.
- Dialogs are reserved for short, interruptive decisions such as confirming
  finalization. Multi-step creation and editing remain full-page workflows.
- Toasts confirm transient success or failure; they never contain the only copy
  of a validation error. Persistent problems stay near the affected region.
- Destructive and irreversible confirmations name the affected record and state
  the consequence in plain language.

### Document editor

The editor is an operational workspace, not a decorative document mockup.

- A compact page header contains breadcrumb, editable document title when
  permitted, document status, source template/version context, and the allowed
  actions for the current lifecycle state.
- On large screens, use a two-pane layout: a 320-360px placeholder/input panel
  on the left and a flexible document area on the right. The document content
  column targets a readable 720-820px paper width and remains centered within
  its pane.
- **Edit** and **Preview** are explicit modes or clearly separated regions. The
  preview renders current placeholder values and document content without
  mutating the source template version.
- Save status appears near the document title or action group using literal
  language such as "Saving…", "Saved", or "Save failed". Do not rely on a
  disappearing toast for persistence confidence.
- Draft actions prioritize **Save draft**. **Finalize** is visually distinct and
  requires a confirmation that explains the document becomes read-only.
  **Download** exports the current Draft or Finalized Document as HTML. The UI
  must not imply that PDF or DOCX is available.
- Finalized documents replace editing tools with a read-only banner, historical
  metadata, and permitted viewing or export actions.

The Admin TemplateVersion editor and author Document editor use one shared
TipTap component for the approved HTML subset: headings, emphasis, lists,
alignment, tables, images by HTTP(S) URL, placeholder tokens, and undo/redo.
Admin Draft versions and author Draft Documents are editable; Published versions
and Finalized Documents use the same component in read-only mode. The API
sanitizes persisted and rendered HTML. Neither workflow may bypass Prototype
independence, Published-version immutability, or Finalized-document read-only
rules.

### Loading, empty, error, and disabled states

- **Loading:** Preserve the destination layout with skeleton rows, fields, or
  document blocks. Use a centered spinner only for small isolated regions. After
  content has loaded once, background refreshes keep existing data visible.
- **Empty:** Distinguish "nothing created yet" from "no results match." State
  what is absent, why it matters, and offer one valid next action only when the
  user is authorized to perform it.
- **Error:** Use inline field errors for validation, a contained alert for
  section failures, and a full-page error state only when the route cannot
  render. Preserve entered data and provide a retry path where retry is safe.
- **Disabled:** Pair disabled controls with a visible reason when the cause is
  not obvious. Do not use disabled styling to conceal permission rules. Use
  read-only presentation for finalized or historical content.
- **Unavailable action:** Omit actions the role cannot perform. Show an action
  as disabled only when its presence teaches a prerequisite, such as unresolved
  validation before finalization.

### Accessibility requirements

- Target WCAG 2.2 AA contrast for text, icons, controls, focus indicators, and
  semantic status combinations.
- All workflows must be operable by keyboard in a logical order. Modals trap
  focus, drawers restore focus to their trigger, and skip navigation is
  available in the application shell.
- Every interactive element has an accessible name. Icon-only buttons have
  tooltips in addition to programmatic labels.
- Focus indicators remain visible and are never replaced by color change alone.
  Do not remove outlines without an equivalent high-contrast focus treatment.
- Form errors are associated with fields and announced. Saving, finalization,
  and asynchronous failures use appropriate live regions without repeated or
  disruptive announcements.
- Tables retain semantic headers and captions or accessible names. Responsive
  transformations must preserve record relationships.
- Minimum pointer targets are 24 by 24 CSS pixels under WCAG 2.2; prefer 44 by
  44 pixels for touch-primary actions.
- Zoom to 200% and text spacing overrides must not hide content, overlap actions,
  or require two-dimensional scrolling except for genuinely tabular data and
  the document canvas.
- Honor reduced-motion preferences and never use motion as the sole indicator of
  change.

### Explicitly unresolved decisions

- **Additional export formats:** HTML is the implemented MVP download. PDF,
  DOCX, and other formats remain unapproved.
- **Document access scope:** The rule allows documents a user is authorized to
  access, but sharing, teams, and cross-user administration are not defined. Do
  not create sharing UI.
- **Permission model expansion:** the implemented permission names and
  Admin/User mapping are fixed in code. Custom roles, persisted grants, and
  runtime permission management remain unapproved.
- **Dark mode:** Scaffold tokens exist, but product support is not confirmed.

## Do's and Don'ts

### Do

- **Do** make the author journey—templates, placeholders, document, preview,
  save—the clearest path through the shell.
- **Do** use stable alignment, concise labels, and persistent state feedback to
  make multi-step work feel trustworthy.
- **Do** use semantic shadcn variants and shared Tailwind tokens so form, table,
  navigation, and status behavior stays consistent.
- **Do** keep template-version and lifecycle context visible when it affects what
  the user can edit or finalize.
- **Do** design empty, loading, validation, permission, and read-only states at
  the same time as the default state.
- **Do** preserve document content as the dominant visual object in editor and
  preview screens.

### Don't

- **Don't** use hero sections, oversized slogans, promotional statistics,
  testimonials, or other marketing-site composition inside the application.
- **Don't** use gradients, glass effects, ornamental illustration, decorative
  animation, or large areas of semantic color.
- **Don't** turn every object into a floating card; prefer lists, tables, panels,
  and direct page structure according to the task.
- **Don't** communicate Draft, Current, Published, Active, Inactive, or Finalized
  through color alone.
- **Don't** introduce unapproved workflows such as sharing, collaboration,
  comments, approval chains, autosave guarantees, or export formats.
- **Don't** let admin density or controls leak into the primary author workflow.
- **Don't** allow UI affordances to imply that published versions, source
  templates, or finalized documents are editable in place.
