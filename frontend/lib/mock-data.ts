import type {
  DocumentRecord,
  TemplateDetail,
  TemplateSummary,
} from "@/types/mock-data";

export const templates: readonly TemplateDetail[] = [
  {
    id: "professional-services-agreement",
    name: "Professional services agreement",
    category: "Business",
    description:
      "A clear agreement for defining services, fees, delivery dates, and client responsibilities.",
    versionNumber: 3,
    updatedAt: "Sep 22, 2026",
    placeholderCount: 6,
    content: `PROFESSIONAL SERVICES AGREEMENT

This agreement is made on {{effective_date}} between {{provider_name}} (the “Provider”) and {{client_name}} (the “Client”).

1. Services
The Provider will deliver the following services:
{{service_description}}

2. Fees
The Client will pay the Provider {{project_fee}} according to the agreed payment schedule.

3. Completion
The expected completion date is {{completion_date}}.

Both parties confirm their acceptance of the terms in this agreement.`,
    placeholders: [
      {
        key: "effective_date",
        label: "Effective date",
        dataType: "Date",
        isRequired: true,
      },
      {
        key: "provider_name",
        label: "Provider name",
        dataType: "Text",
        isRequired: true,
        example: "Northstar Studio",
      },
      {
        key: "client_name",
        label: "Client name",
        dataType: "Text",
        isRequired: true,
        example: "Acme Industries",
      },
      {
        key: "service_description",
        label: "Service description",
        dataType: "Text",
        isRequired: true,
        example: "Research, writing, and delivery of the quarterly report.",
      },
      {
        key: "project_fee",
        label: "Project fee",
        dataType: "Number",
        isRequired: true,
        example: "4800",
      },
      {
        key: "completion_date",
        label: "Completion date",
        dataType: "Date",
        isRequired: false,
      },
    ],
  },
  {
    id: "modern-cv",
    name: "Modern CV",
    category: "Human resources",
    description:
      "A concise, readable curriculum vitae for professional and academic applications.",
    versionNumber: 2,
    updatedAt: "Sep 18, 2026",
    placeholderCount: 7,
    content: `{{full_name}}
{{email_address}}

PROFILE
{{professional_summary}}

EXPERIENCE
{{work_experience}}

EDUCATION
{{education}}

SKILLS
{{skills}}`,
    placeholders: [
      { key: "full_name", label: "Full name", dataType: "Text", isRequired: true },
      { key: "email_address", label: "Email", dataType: "Email", isRequired: true },
      { key: "professional_summary", label: "Professional summary", dataType: "Text", isRequired: true },
      { key: "work_experience", label: "Work experience", dataType: "Text", isRequired: true },
      { key: "education", label: "Education", dataType: "Text", isRequired: true },
      { key: "skills", label: "Skills", dataType: "Text", isRequired: false },
      { key: "portfolio_url", label: "Portfolio URL", dataType: "Text", isRequired: false },
    ],
  },
  {
    id: "service-invoice",
    name: "Service invoice",
    category: "Finance",
    description:
      "A straightforward invoice for itemized services, payment terms, and client billing details.",
    versionNumber: 4,
    updatedAt: "Sep 15, 2026",
    placeholderCount: 7,
    content: `INVOICE {{invoice_number}}

Bill to: {{client_name}}
Issued: {{issue_date}}
Due: {{due_date}}

Services
{{service_items}}

Total due: {{total_amount}}
Payment terms: {{payment_terms}}`,
    placeholders: [
      { key: "invoice_number", label: "Invoice number", dataType: "Text", isRequired: true },
      { key: "client_name", label: "Client name", dataType: "Text", isRequired: true },
      { key: "issue_date", label: "Issue date", dataType: "Date", isRequired: true },
      { key: "due_date", label: "Due date", dataType: "Date", isRequired: true },
      { key: "service_items", label: "Service items", dataType: "Text", isRequired: true },
      { key: "total_amount", label: "Total amount", dataType: "Number", isRequired: true },
      { key: "payment_terms", label: "Payment terms", dataType: "Text", isRequired: false, defaultValue: "Due within 14 days" },
    ],
  },
  {
    id: "quarterly-progress-report",
    name: "Quarterly progress report",
    category: "Operations",
    description:
      "A structured report for goals, outcomes, risks, and priorities for the next quarter.",
    versionNumber: 5,
    updatedAt: "Sep 10, 2026",
    placeholderCount: 6,
    content: `QUARTERLY PROGRESS REPORT

Reporting period: {{reporting_period}}
Prepared by: {{author_name}}

EXECUTIVE SUMMARY
{{executive_summary}}

KEY OUTCOMES
{{key_outcomes}}

RISKS AND ISSUES
{{risks}}

NEXT-QUARTER PRIORITIES
{{next_priorities}}`,
    placeholders: [
      { key: "reporting_period", label: "Reporting period", dataType: "Text", isRequired: true, example: "Q3 2026" },
      { key: "author_name", label: "Prepared by", dataType: "Text", isRequired: true },
      { key: "executive_summary", label: "Executive summary", dataType: "Text", isRequired: true },
      { key: "key_outcomes", label: "Key outcomes", dataType: "Text", isRequired: true },
      { key: "risks", label: "Risks and issues", dataType: "Text", isRequired: false },
      { key: "next_priorities", label: "Next-quarter priorities", dataType: "Text", isRequired: true },
    ],
  },
  {
    id: "meeting-summary",
    name: "Meeting summary",
    category: "Operations",
    description:
      "A practical record of attendees, decisions, action items, and follow-up dates.",
    versionNumber: 1,
    updatedAt: "Sep 5, 2026",
    placeholderCount: 6,
    content: `MEETING SUMMARY

Meeting: {{meeting_title}}
Date: {{meeting_date}}
Attendees: {{attendees}}

DECISIONS
{{decisions}}

ACTION ITEMS
{{action_items}}

NEXT MEETING
{{next_meeting_date}}`,
    placeholders: [
      { key: "meeting_title", label: "Meeting title", dataType: "Text", isRequired: true },
      { key: "meeting_date", label: "Meeting date", dataType: "Date", isRequired: true },
      { key: "attendees", label: "Attendees", dataType: "Text", isRequired: true },
      { key: "decisions", label: "Decisions", dataType: "Text", isRequired: false },
      { key: "action_items", label: "Action items", dataType: "Text", isRequired: true },
      { key: "next_meeting_date", label: "Next meeting date", dataType: "Date", isRequired: false },
    ],
  },
];

export const templateSummaries: readonly TemplateSummary[] = templates;

export const documents: readonly DocumentRecord[] = [
  {
    id: "q3-progress-report-draft",
    title: "Q3 product operations report",
    templateId: "quarterly-progress-report",
    templateName: "Quarterly progress report",
    templateVersion: 5,
    status: "Draft",
    createdAt: "Sep 24, 2026",
    updatedAt: "Sep 28, 2026",
    content: templates[3].content,
    placeholderValues: {
      reporting_period: "Q3 2026",
      author_name: "Maya Chen",
      executive_summary: "The team completed the document workflow prototype and improved release reliability.",
      key_outcomes: "Completed usability testing; reduced review turnaround; documented release checks.",
      risks: "Export requirements are still awaiting confirmation.",
      next_priorities: "Validate the authoring flow and prepare the first integration slice.",
    },
  },
  {
    id: "consulting-agreement-2026",
    title: "Acme consulting agreement",
    templateId: "professional-services-agreement",
    templateName: "Professional services agreement",
    templateVersion: 3,
    status: "Finalized",
    createdAt: "Sep 12, 2026",
    updatedAt: "Sep 16, 2026",
    content: templates[0].content,
    placeholderValues: {
      effective_date: "2026-09-16",
      provider_name: "Northstar Studio",
      client_name: "Acme Industries",
      service_description: "Research, content design, and delivery of a quarterly operations report.",
      project_fee: "4800",
      completion_date: "2026-11-30",
    },
  },
  {
    id: "invoice-1048",
    title: "Invoice 1048 — research services",
    templateId: "service-invoice",
    templateName: "Service invoice",
    templateVersion: 4,
    status: "Finalized",
    createdAt: "Sep 2, 2026",
    updatedAt: "Sep 2, 2026",
    content: templates[2].content,
    placeholderValues: {},
  },
  {
    id: "research-cv-draft",
    title: "Research fellowship CV",
    templateId: "modern-cv",
    templateName: "Modern CV",
    templateVersion: 2,
    status: "Draft",
    createdAt: "Aug 25, 2026",
    updatedAt: "Sep 1, 2026",
    content: templates[1].content,
    placeholderValues: {},
  },
  {
    id: "august-planning-summary",
    title: "August planning summary",
    templateId: "meeting-summary",
    templateName: "Meeting summary",
    templateVersion: 1,
    status: "Finalized",
    createdAt: "Aug 19, 2026",
    updatedAt: "Aug 19, 2026",
    content: templates[4].content,
    placeholderValues: {},
  },
];

export function getTemplate(templateId: string) {
  return templates.find((template) => template.id === templateId);
}

export function getDocument(documentId: string) {
  return documents.find((document) => document.id === documentId);
}
