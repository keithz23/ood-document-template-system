using DocumentTemplateSystem.Domain.Entities;
using DocumentTemplateSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentTemplateSystem.Infrastructure.Persistence.Seeding;

public static class DevelopmentDataSeeder
{
    private const string AdminPasswordHash =
        "AQAAAAIAAYagAAAAEMk0+ncQn/We9Q3wXpGkvnIF2jxr9rC127s0NF30RP9TSPHpOibwkAoKdrmHT39tOA==";

    private const string UserPasswordHash =
        "AQAAAAIAAYagAAAAEHWwMFFzx+OV3jpNHT6n9tvXljGbE0d5n7geelvozhm4QsQRRfB+v3MalV249xjcbA==";

    public static async Task SeedDevelopmentDataAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await HasExistingDataAsync(context, cancellationToken))
        {
            return;
        }

        await using var transaction = await context.Database.BeginTransactionAsync(
            cancellationToken);

        var createdAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var admin = new User(
            "admin",
            AdminPasswordHash,
            "Development Admin",
            "admin@example.test",
            UserRole.Admin,
            createdAt);
        var user = new User(
            "author",
            UserPasswordHash,
            "Development Author",
            "author@example.test",
            UserRole.User,
            createdAt);

        var business = new Category("Business", admin.Id, createdAt);
        var finance = new Category("Finance", admin.Id, createdAt);
        var humanResources = new Category("Human resources", admin.Id, createdAt);
        var operations = new Category("Operations", admin.Id, createdAt);
        var marketing = new Category("Marketing", admin.Id, createdAt);

        var statementOfWork = CreatePublishedTemplate(
            "Professional services statement of work",
            business.Id,
            admin.Id,
            """
            <h1>Professional Services Statement of Work</h1>
            <p><strong>Engagement:</strong> {{engagement_name}}</p>
            <p><strong>Prepared for:</strong> {{client_name}} &nbsp; <strong>Prepared by:</strong> {{provider_name}}</p>
            <p><strong>Document date:</strong> {{document_date}} &nbsp; <strong>Target completion:</strong> {{target_completion_date}}</p>
            <hr>
            <h2>1. Engagement overview</h2>
            <p>{{engagement_overview}}</p>
            <h2>2. Scope and deliverables</h2>
            <p>{{scope_of_work}}</p>
            <table>
              <thead><tr><th>Deliverable</th><th>Acceptance criteria</th><th>Target date</th></tr></thead>
              <tbody><tr><td>{{primary_deliverable}}</td><td>{{acceptance_criteria}}</td><td>{{deliverable_due_date}}</td></tr></tbody>
            </table>
            <h2>3. Schedule and investment</h2>
            <p><strong>Estimated effort:</strong> {{estimated_effort}} days</p>
            <p><strong>Fixed fee:</strong> {{project_fee}} {{currency_code}}</p>
            <p><strong>Payment schedule:</strong> {{payment_schedule}}</p>
            <h2>4. Assumptions, dependencies, and exclusions</h2>
            <p>{{assumptions_and_dependencies}}</p>
            <p>{{out_of_scope}}</p>
            <h2>5. Governance and approval</h2>
            <p>Project contact: {{client_contact_name}} ({{client_contact_email}})</p>
            <p>Changes to scope, timing, or fees require written approval from both parties.</p>
            <p><strong>Client approver:</strong> {{client_approver_name}} &nbsp; <strong>Provider approver:</strong> {{provider_approver_name}}</p>
            <p><strong>Approval date:</strong> {{approval_date}}</p>
            """,
            createdAt,
            version => AddPlaceholders(version,
                ("engagement_name", "Engagement name", PlaceholderDataType.Text, true),
                ("client_name", "Client organization", PlaceholderDataType.Text, true),
                ("provider_name", "Provider organization", PlaceholderDataType.Text, true),
                ("document_date", "Document date", PlaceholderDataType.Date, true),
                ("target_completion_date", "Target completion date", PlaceholderDataType.Date, true),
                ("engagement_overview", "Engagement overview", PlaceholderDataType.Text, true),
                ("scope_of_work", "Scope of work", PlaceholderDataType.Text, true),
                ("primary_deliverable", "Primary deliverable", PlaceholderDataType.Text, true),
                ("acceptance_criteria", "Acceptance criteria", PlaceholderDataType.Text, true),
                ("deliverable_due_date", "Deliverable due date", PlaceholderDataType.Date, true),
                ("estimated_effort", "Estimated effort in days", PlaceholderDataType.Number, true),
                ("project_fee", "Project fee", PlaceholderDataType.Number, true),
                ("currency_code", "Currency code", PlaceholderDataType.Text, true),
                ("payment_schedule", "Payment schedule", PlaceholderDataType.Text, true),
                ("assumptions_and_dependencies", "Assumptions and dependencies", PlaceholderDataType.Text, false),
                ("out_of_scope", "Out of scope", PlaceholderDataType.Text, false),
                ("client_contact_name", "Client contact", PlaceholderDataType.Text, true),
                ("client_contact_email", "Client contact email", PlaceholderDataType.Email, true),
                ("client_approver_name", "Client approver", PlaceholderDataType.Text, true),
                ("provider_approver_name", "Provider approver", PlaceholderDataType.Text, true),
                ("approval_date", "Approval date", PlaceholderDataType.Date, false)));

        var invoice = CreatePublishedTemplate(
            "Consulting services invoice",
            finance.Id,
            admin.Id,
            """
            <h1>Consulting Services Invoice</h1>
            <p><strong>Invoice number:</strong> {{invoice_number}} &nbsp; <strong>Status:</strong> {{invoice_status}}</p>
            <p><strong>Issue date:</strong> {{issue_date}} &nbsp; <strong>Due date:</strong> {{due_date}}</p>
            <hr>
            <h2>Bill to</h2>
            <p>{{customer_name}}<br>{{billing_address}}</p>
            <p>Accounts payable: {{customer_email}}</p>
            <h2>Services</h2>
            <table>
              <thead><tr><th>Service period</th><th>Description</th><th>Hours</th><th>Rate</th><th>Line total</th></tr></thead>
              <tbody><tr><td>{{service_period}}</td><td>{{service_description}}</td><td>{{hours_worked}}</td><td>{{hourly_rate}} {{currency_code}}</td><td>{{line_total}} {{currency_code}}</td></tr></tbody>
            </table>
            <h2>Amount due</h2>
            <p>Subtotal: {{subtotal}} {{currency_code}}</p>
            <p>Tax ({{tax_rate}}%): {{tax_amount}} {{currency_code}}</p>
            <p><strong>Total due: {{total_due}} {{currency_code}}</strong></p>
            <h2>Payment instructions</h2>
            <p>{{payment_instructions}}</p>
            <p><strong>Reference:</strong> {{payment_reference}}</p>
            <p>Questions about this invoice? Contact {{billing_contact_name}} at {{billing_contact_email}}.</p>
            """,
            createdAt,
            version => AddPlaceholders(version,
                ("invoice_number", "Invoice number", PlaceholderDataType.Text, true),
                ("invoice_status", "Invoice status", PlaceholderDataType.Text, true),
                ("issue_date", "Issue date", PlaceholderDataType.Date, true),
                ("due_date", "Payment due date", PlaceholderDataType.Date, true),
                ("customer_name", "Customer name", PlaceholderDataType.Text, true),
                ("billing_address", "Billing address", PlaceholderDataType.Text, true),
                ("customer_email", "Accounts payable email", PlaceholderDataType.Email, true),
                ("service_period", "Service period", PlaceholderDataType.Text, true),
                ("service_description", "Service description", PlaceholderDataType.Text, true),
                ("hours_worked", "Hours worked", PlaceholderDataType.Number, true),
                ("hourly_rate", "Hourly rate", PlaceholderDataType.Number, true),
                ("line_total", "Line total", PlaceholderDataType.Number, true),
                ("currency_code", "Currency code", PlaceholderDataType.Text, true),
                ("subtotal", "Subtotal", PlaceholderDataType.Number, true),
                ("tax_rate", "Tax rate", PlaceholderDataType.Number, true),
                ("tax_amount", "Tax amount", PlaceholderDataType.Number, true),
                ("total_due", "Total due", PlaceholderDataType.Number, true),
                ("payment_instructions", "Payment instructions", PlaceholderDataType.Text, true),
                ("payment_reference", "Payment reference", PlaceholderDataType.Text, true),
                ("billing_contact_name", "Billing contact", PlaceholderDataType.Text, true),
                ("billing_contact_email", "Billing contact email", PlaceholderDataType.Email, true)));

        var curriculumVitae = CreatePublishedTemplate(
            "Executive profile and curriculum vitae",
            humanResources.Id,
            admin.Id,
            """
            <h1>{{full_name}}</h1>
            <h2>{{professional_title}}</h2>
            <p>{{email}} &nbsp; | &nbsp; {{phone_number}} &nbsp; | &nbsp; {{location}}</p>
            <hr>
            <h2>Executive profile</h2>
            <p>{{professional_summary}}</p>
            <h2>Leadership experience</h2>
            <p><strong>{{current_role}}</strong> — {{current_employer}} ({{current_role_start}} to {{current_role_end}})</p>
            <p>{{leadership_achievement}}</p>
            <p><strong>{{previous_role}}</strong> — {{previous_employer}} ({{previous_role_start}} to {{previous_role_end}})</p>
            <p>{{previous_achievement}}</p>
            <h2>Education and credentials</h2>
            <p>{{education_credential}} — {{education_institution}}, {{education_year}}</p>
            <p>{{professional_certification}}</p>
            <h2>Areas of expertise</h2>
            <ul>
              <li>{{expertise_area_one}}</li>
              <li>{{expertise_area_two}}</li>
              <li>{{expertise_area_three}}</li>
            </ul>
            <h2>Selected impact</h2>
            <p>{{career_impact}}</p>
            """,
            createdAt,
            version => AddPlaceholders(version,
                ("full_name", "Full name", PlaceholderDataType.Text, true),
                ("professional_title", "Professional title", PlaceholderDataType.Text, true),
                ("email", "Email address", PlaceholderDataType.Email, true),
                ("phone_number", "Phone number", PlaceholderDataType.Text, false),
                ("location", "Location", PlaceholderDataType.Text, true),
                ("professional_summary", "Executive profile", PlaceholderDataType.Text, true),
                ("current_role", "Current role", PlaceholderDataType.Text, true),
                ("current_employer", "Current employer", PlaceholderDataType.Text, true),
                ("current_role_start", "Current role start date", PlaceholderDataType.Date, true),
                ("current_role_end", "Current role end date or Present", PlaceholderDataType.Text, true),
                ("leadership_achievement", "Current role achievement", PlaceholderDataType.Text, true),
                ("previous_role", "Previous role", PlaceholderDataType.Text, true),
                ("previous_employer", "Previous employer", PlaceholderDataType.Text, true),
                ("previous_role_start", "Previous role start date", PlaceholderDataType.Date, true),
                ("previous_role_end", "Previous role end date", PlaceholderDataType.Date, true),
                ("previous_achievement", "Previous role achievement", PlaceholderDataType.Text, true),
                ("education_credential", "Education credential", PlaceholderDataType.Text, true),
                ("education_institution", "Education institution", PlaceholderDataType.Text, true),
                ("education_year", "Graduation year", PlaceholderDataType.Number, true),
                ("professional_certification", "Professional certification", PlaceholderDataType.Text, false),
                ("expertise_area_one", "Expertise area one", PlaceholderDataType.Text, true),
                ("expertise_area_two", "Expertise area two", PlaceholderDataType.Text, true),
                ("expertise_area_three", "Expertise area three", PlaceholderDataType.Text, true),
                ("career_impact", "Selected career impact", PlaceholderDataType.Text, true)));

        var projectReport = CreatePublishedTemplate(
            "Executive project health report",
            operations.Id,
            admin.Id,
            """
            <h1>Executive Project Health Report</h1>
            <p><strong>Project:</strong> {{project_name}} &nbsp; <strong>Executive sponsor:</strong> {{executive_sponsor}}</p>
            <p><strong>Project manager:</strong> {{project_manager}} &nbsp; <strong>Reporting date:</strong> {{reporting_date}}</p>
            <p><strong>Overall health:</strong> {{overall_health}} &nbsp; <strong>Phase:</strong> {{project_phase}}</p>
            <hr>
            <h2>Executive summary</h2>
            <p>{{executive_summary}}</p>
            <h2>Delivery metrics</h2>
            <table>
              <thead><tr><th>Measure</th><th>Baseline</th><th>Current forecast</th><th>Variance</th></tr></thead>
              <tbody>
                <tr><td>Completion</td><td>{{baseline_completion}}%</td><td>{{forecast_completion}}%</td><td>{{completion_variance}}%</td></tr>
                <tr><td>Budget</td><td>{{approved_budget}} {{currency_code}}</td><td>{{forecast_cost}} {{currency_code}}</td><td>{{budget_variance}} {{currency_code}}</td></tr>
              </tbody>
            </table>
            <h2>Milestones and decisions</h2>
            <p><strong>Next milestone:</strong> {{next_milestone}} by {{next_milestone_date}}</p>
            <p><strong>Decision required:</strong> {{decision_required}}</p>
            <h2>Top risks and mitigations</h2>
            <ol>
              <li><strong>{{risk_one_title}}</strong> — {{risk_one_mitigation}}</li>
              <li><strong>{{risk_two_title}}</strong> — {{risk_two_mitigation}}</li>
            </ol>
            <h2>Priorities for the next reporting period</h2>
            <p>{{next_period_priorities}}</p>
            """,
            createdAt,
            version => AddPlaceholders(version,
                ("project_name", "Project name", PlaceholderDataType.Text, true),
                ("executive_sponsor", "Executive sponsor", PlaceholderDataType.Text, true),
                ("project_manager", "Project manager", PlaceholderDataType.Text, true),
                ("reporting_date", "Reporting date", PlaceholderDataType.Date, true),
                ("overall_health", "Overall health", PlaceholderDataType.Text, true),
                ("project_phase", "Project phase", PlaceholderDataType.Text, true),
                ("executive_summary", "Executive summary", PlaceholderDataType.Text, true),
                ("baseline_completion", "Baseline completion percent", PlaceholderDataType.Number, true),
                ("forecast_completion", "Forecast completion percent", PlaceholderDataType.Number, true),
                ("completion_variance", "Completion variance percent", PlaceholderDataType.Number, true),
                ("approved_budget", "Approved budget", PlaceholderDataType.Number, true),
                ("forecast_cost", "Forecast cost", PlaceholderDataType.Number, true),
                ("budget_variance", "Budget variance", PlaceholderDataType.Number, true),
                ("currency_code", "Currency code", PlaceholderDataType.Text, true),
                ("next_milestone", "Next milestone", PlaceholderDataType.Text, true),
                ("next_milestone_date", "Next milestone date", PlaceholderDataType.Date, true),
                ("decision_required", "Decision required", PlaceholderDataType.Text, false),
                ("risk_one_title", "Risk one", PlaceholderDataType.Text, true),
                ("risk_one_mitigation", "Risk one mitigation", PlaceholderDataType.Text, true),
                ("risk_two_title", "Risk two", PlaceholderDataType.Text, true),
                ("risk_two_mitigation", "Risk two mitigation", PlaceholderDataType.Text, true),
                ("next_period_priorities", "Next-period priorities", PlaceholderDataType.Text, true)));

        var onboardingPlan = CreatePublishedTemplate(
            "30-60-90 day employee onboarding plan",
            humanResources.Id,
            admin.Id,
            """
            <h1>30-60-90 Day Onboarding Plan</h1>
            <p><strong>New team member:</strong> {{employee_name}} ({{employee_email}})</p>
            <p><strong>Role:</strong> {{job_title}} &nbsp; <strong>Department:</strong> {{department}}</p>
            <p><strong>Manager:</strong> {{manager_name}} &nbsp; <strong>Start date:</strong> {{start_date}}</p>
            <hr>
            <h2>Before the first day</h2>
            <ul>
              <li>{{equipment_and_access}}</li>
              <li>{{workspace_arrangements}}</li>
              <li>{{welcome_contact}}</li>
            </ul>
            <h2>Days 1–30 · Learn</h2>
            <p><strong>Outcomes:</strong> {{first_month_outcomes}}</p>
            <p><strong>Key introductions:</strong> {{key_introductions}}</p>
            <p><strong>Checkpoint:</strong> {{first_checkpoint_date}}</p>
            <h2>Days 31–60 · Contribute</h2>
            <p><strong>Outcomes:</strong> {{second_month_outcomes}}</p>
            <p><strong>First owned deliverable:</strong> {{first_owned_deliverable}}</p>
            <p><strong>Checkpoint:</strong> {{second_checkpoint_date}}</p>
            <h2>Days 61–90 · Own</h2>
            <p><strong>Outcomes:</strong> {{third_month_outcomes}}</p>
            <p><strong>Success measures:</strong> {{success_measures}}</p>
            <p><strong>Checkpoint:</strong> {{third_checkpoint_date}}</p>
            <h2>Support and acknowledgement</h2>
            <p><strong>Buddy:</strong> {{onboarding_buddy}}</p>
            <p>{{support_needs}}</p>
            <p><strong>Manager acknowledgement:</strong> {{manager_name}} · {{plan_date}}</p>
            """,
            createdAt,
            version => AddPlaceholders(version,
                ("employee_name", "Employee name", PlaceholderDataType.Text, true),
                ("employee_email", "Employee email", PlaceholderDataType.Email, true),
                ("job_title", "Job title", PlaceholderDataType.Text, true),
                ("department", "Department", PlaceholderDataType.Text, true),
                ("manager_name", "Manager name", PlaceholderDataType.Text, true),
                ("start_date", "Start date", PlaceholderDataType.Date, true),
                ("equipment_and_access", "Equipment and access setup", PlaceholderDataType.Text, true),
                ("workspace_arrangements", "Workspace arrangements", PlaceholderDataType.Text, true),
                ("welcome_contact", "First-day contact", PlaceholderDataType.Text, true),
                ("first_month_outcomes", "Days 1–30 outcomes", PlaceholderDataType.Text, true),
                ("key_introductions", "Key introductions", PlaceholderDataType.Text, true),
                ("first_checkpoint_date", "First checkpoint date", PlaceholderDataType.Date, true),
                ("second_month_outcomes", "Days 31–60 outcomes", PlaceholderDataType.Text, true),
                ("first_owned_deliverable", "First owned deliverable", PlaceholderDataType.Text, true),
                ("second_checkpoint_date", "Second checkpoint date", PlaceholderDataType.Date, true),
                ("third_month_outcomes", "Days 61–90 outcomes", PlaceholderDataType.Text, true),
                ("success_measures", "Success measures", PlaceholderDataType.Text, true),
                ("third_checkpoint_date", "Third checkpoint date", PlaceholderDataType.Date, true),
                ("onboarding_buddy", "Onboarding buddy", PlaceholderDataType.Text, true),
                ("support_needs", "Additional support needs", PlaceholderDataType.Text, false),
                ("plan_date", "Plan date", PlaceholderDataType.Date, true)));

        var incidentReview = CreatePublishedTemplate(
            "Production incident postmortem",
            operations.Id,
            admin.Id,
            """
            <h1>Production Incident Postmortem</h1>
            <p><strong>Incident:</strong> {{incident_id}} · {{incident_title}}</p>
            <p><strong>Service:</strong> {{affected_service}} &nbsp; <strong>Severity:</strong> {{severity}}</p>
            <p><strong>Incident lead:</strong> {{incident_lead}} &nbsp; <strong>Review date:</strong> {{review_date}}</p>
            <hr>
            <h2>Impact</h2>
            <p><strong>Started:</strong> {{incident_start}} &nbsp; <strong>Resolved:</strong> {{incident_resolved}}</p>
            <p><strong>Customer impact:</strong> {{customer_impact}}</p>
            <p><strong>Detection source:</strong> {{detection_source}}</p>
            <h2>Executive summary</h2>
            <p>{{incident_summary}}</p>
            <h2>Event timeline</h2>
            <table>
              <thead><tr><th>Time</th><th>Event</th><th>Owner</th></tr></thead>
              <tbody><tr><td>{{key_event_time}}</td><td>{{key_event_description}}</td><td>{{key_event_owner}}</td></tr></tbody>
            </table>
            <h2>Root cause and contributing factors</h2>
            <p>{{root_cause}}</p>
            <p>{{contributing_factors}}</p>
            <h2>Response and recovery</h2>
            <p>{{mitigation_steps}}</p>
            <p><strong>What went well:</strong> {{what_went_well}}</p>
            <p><strong>What slowed recovery:</strong> {{recovery_gaps}}</p>
            <h2>Corrective actions</h2>
            <table>
              <thead><tr><th>Action</th><th>Owner</th><th>Due date</th><th>Priority</th></tr></thead>
              <tbody><tr><td>{{corrective_action}}</td><td>{{action_owner}}</td><td>{{action_due_date}}</td><td>{{action_priority}}</td></tr></tbody>
            </table>
            <h2>Communications and follow-up</h2>
            <p>{{customer_communication}}</p>
            <p><strong>Follow-up owner:</strong> {{follow_up_owner}} ({{follow_up_email}})</p>
            """,
            createdAt,
            version => AddPlaceholders(version,
                ("incident_id", "Incident ID", PlaceholderDataType.Text, true),
                ("incident_title", "Incident title", PlaceholderDataType.Text, true),
                ("affected_service", "Affected service", PlaceholderDataType.Text, true),
                ("severity", "Severity", PlaceholderDataType.Text, true),
                ("incident_lead", "Incident lead", PlaceholderDataType.Text, true),
                ("review_date", "Review date", PlaceholderDataType.Date, true),
                ("incident_start", "Incident start date and time", PlaceholderDataType.Text, true),
                ("incident_resolved", "Resolution date and time", PlaceholderDataType.Text, true),
                ("customer_impact", "Customer impact", PlaceholderDataType.Text, true),
                ("detection_source", "Detection source", PlaceholderDataType.Text, true),
                ("incident_summary", "Incident summary", PlaceholderDataType.Text, true),
                ("key_event_time", "Key event time", PlaceholderDataType.Text, true),
                ("key_event_description", "Key event", PlaceholderDataType.Text, true),
                ("key_event_owner", "Key event owner", PlaceholderDataType.Text, true),
                ("root_cause", "Root cause", PlaceholderDataType.Text, true),
                ("contributing_factors", "Contributing factors", PlaceholderDataType.Text, false),
                ("mitigation_steps", "Mitigation and recovery steps", PlaceholderDataType.Text, true),
                ("what_went_well", "What went well", PlaceholderDataType.Text, true),
                ("recovery_gaps", "Recovery gaps", PlaceholderDataType.Text, true),
                ("corrective_action", "Corrective action", PlaceholderDataType.Text, true),
                ("action_owner", "Action owner", PlaceholderDataType.Text, true),
                ("action_due_date", "Action due date", PlaceholderDataType.Date, true),
                ("action_priority", "Action priority", PlaceholderDataType.Text, true),
                ("customer_communication", "Customer communication", PlaceholderDataType.Text, false),
                ("follow_up_owner", "Follow-up owner", PlaceholderDataType.Text, true),
                ("follow_up_email", "Follow-up email", PlaceholderDataType.Email, true)));

        var travelExpense = CreatePublishedTemplate(
            "Business travel expense report",
            finance.Id,
            admin.Id,
            """
            <h1>Business Travel Expense Report</h1>
            <p><strong>Employee:</strong> {{employee_name}} ({{employee_email}})</p>
            <p><strong>Department:</strong> {{department}} &nbsp; <strong>Cost center:</strong> {{cost_center}}</p>
            <p><strong>Trip:</strong> {{trip_purpose}}</p>
            <p><strong>Travel dates:</strong> {{departure_date}} – {{return_date}}</p>
            <p><strong>Destination:</strong> {{destination}}</p>
            <hr>
            <h2>Expense summary</h2>
            <table>
              <thead><tr><th>Category</th><th>Description</th><th>Date</th><th>Amount</th></tr></thead>
              <tbody>
                <tr><td>{{expense_category_one}}</td><td>{{expense_description_one}}</td><td>{{expense_date_one}}</td><td>{{expense_amount_one}} {{currency_code}}</td></tr>
                <tr><td>{{expense_category_two}}</td><td>{{expense_description_two}}</td><td>{{expense_date_two}}</td><td>{{expense_amount_two}} {{currency_code}}</td></tr>
              </tbody>
            </table>
            <p><strong>Total requested:</strong> {{total_requested}} {{currency_code}}</p>
            <h2>Business justification and exceptions</h2>
            <p>{{business_outcome}}</p>
            <p><strong>Policy exceptions:</strong> {{policy_exceptions}}</p>
            <h2>Approvals</h2>
            <p><strong>Manager:</strong> {{manager_name}} · {{manager_email}}</p>
            <p><strong>Approval date:</strong> {{approval_date}}</p>
            <p><strong>Finance review:</strong> {{finance_reviewer}} · {{finance_review_date}}</p>
            """,
            createdAt,
            version => AddPlaceholders(version,
                ("employee_name", "Employee name", PlaceholderDataType.Text, true),
                ("employee_email", "Employee email", PlaceholderDataType.Email, true),
                ("department", "Department", PlaceholderDataType.Text, true),
                ("cost_center", "Cost center", PlaceholderDataType.Text, true),
                ("trip_purpose", "Business purpose", PlaceholderDataType.Text, true),
                ("departure_date", "Departure date", PlaceholderDataType.Date, true),
                ("return_date", "Return date", PlaceholderDataType.Date, true),
                ("destination", "Destination", PlaceholderDataType.Text, true),
                ("expense_category_one", "Expense category one", PlaceholderDataType.Text, true),
                ("expense_description_one", "Expense description one", PlaceholderDataType.Text, true),
                ("expense_date_one", "Expense date one", PlaceholderDataType.Date, true),
                ("expense_amount_one", "Expense amount one", PlaceholderDataType.Number, true),
                ("expense_category_two", "Expense category two", PlaceholderDataType.Text, true),
                ("expense_description_two", "Expense description two", PlaceholderDataType.Text, true),
                ("expense_date_two", "Expense date two", PlaceholderDataType.Date, true),
                ("expense_amount_two", "Expense amount two", PlaceholderDataType.Number, true),
                ("currency_code", "Currency code", PlaceholderDataType.Text, true),
                ("total_requested", "Total requested", PlaceholderDataType.Number, true),
                ("business_outcome", "Business outcome", PlaceholderDataType.Text, true),
                ("policy_exceptions", "Policy exceptions", PlaceholderDataType.Text, false),
                ("manager_name", "Manager name", PlaceholderDataType.Text, true),
                ("manager_email", "Manager email", PlaceholderDataType.Email, true),
                ("approval_date", "Manager approval date", PlaceholderDataType.Date, true),
                ("finance_reviewer", "Finance reviewer", PlaceholderDataType.Text, true),
                ("finance_review_date", "Finance review date", PlaceholderDataType.Date, true)));

        var campaignBrief = CreatePublishedTemplate(
            "Integrated marketing campaign brief",
            marketing.Id,
            admin.Id,
            """
            <h1>Integrated Marketing Campaign Brief</h1>
            <p><strong>Campaign:</strong> {{campaign_name}}</p>
            <p><strong>Campaign owner:</strong> {{campaign_owner}} ({{campaign_owner_email}})</p>
            <p><strong>Planning date:</strong> {{planning_date}} &nbsp; <strong>Launch date:</strong> {{launch_date}}</p>
            <p><strong>Campaign window:</strong> {{campaign_start_date}} – {{campaign_end_date}}</p>
            <hr>
            <h2>Opportunity and audience</h2>
            <p><strong>Business objective:</strong> {{business_objective}}</p>
            <p><strong>Primary audience:</strong> {{primary_audience}}</p>
            <p><strong>Customer insight:</strong> {{customer_insight}}</p>
            <h2>Positioning and message</h2>
            <p><strong>Value proposition:</strong> {{value_proposition}}</p>
            <p><strong>Core message:</strong> {{core_message}}</p>
            <p><strong>Call to action:</strong> {{call_to_action}}</p>
            <h2>Channel plan</h2>
            <table>
              <thead><tr><th>Channel</th><th>Role in journey</th><th>Owner</th><th>Publish date</th></tr></thead>
              <tbody><tr><td>{{primary_channel}}</td><td>{{channel_role}}</td><td>{{channel_owner}}</td><td>{{channel_publish_date}}</td></tr></tbody>
            </table>
            <h2>Budget and measurement</h2>
            <p><strong>Budget:</strong> {{campaign_budget}} {{currency_code}}</p>
            <p><strong>Primary KPI:</strong> {{primary_kpi}} &nbsp; <strong>Target:</strong> {{kpi_target}}</p>
            <p><strong>Measurement approach:</strong> {{measurement_approach}}</p>
            <h2>Risks and approvals</h2>
            <p>{{campaign_risks}}</p>
            <p><strong>Approver:</strong> {{approver_name}} ({{approver_email}})</p>
            """,
            createdAt,
            version => AddPlaceholders(version,
                ("campaign_name", "Campaign name", PlaceholderDataType.Text, true),
                ("campaign_owner", "Campaign owner", PlaceholderDataType.Text, true),
                ("campaign_owner_email", "Campaign owner email", PlaceholderDataType.Email, true),
                ("planning_date", "Planning date", PlaceholderDataType.Date, true),
                ("launch_date", "Launch date", PlaceholderDataType.Date, true),
                ("campaign_start_date", "Campaign start date", PlaceholderDataType.Date, true),
                ("campaign_end_date", "Campaign end date", PlaceholderDataType.Date, true),
                ("business_objective", "Business objective", PlaceholderDataType.Text, true),
                ("primary_audience", "Primary audience", PlaceholderDataType.Text, true),
                ("customer_insight", "Customer insight", PlaceholderDataType.Text, true),
                ("value_proposition", "Value proposition", PlaceholderDataType.Text, true),
                ("core_message", "Core message", PlaceholderDataType.Text, true),
                ("call_to_action", "Call to action", PlaceholderDataType.Text, true),
                ("primary_channel", "Primary channel", PlaceholderDataType.Text, true),
                ("channel_role", "Channel role", PlaceholderDataType.Text, true),
                ("channel_owner", "Channel owner", PlaceholderDataType.Text, true),
                ("channel_publish_date", "Channel publish date", PlaceholderDataType.Date, true),
                ("campaign_budget", "Campaign budget", PlaceholderDataType.Number, true),
                ("currency_code", "Currency code", PlaceholderDataType.Text, true),
                ("primary_kpi", "Primary KPI", PlaceholderDataType.Text, true),
                ("kpi_target", "KPI target", PlaceholderDataType.Number, true),
                ("measurement_approach", "Measurement approach", PlaceholderDataType.Text, true),
                ("campaign_risks", "Campaign risks", PlaceholderDataType.Text, false),
                ("approver_name", "Approver name", PlaceholderDataType.Text, true),
                ("approver_email", "Approver email", PlaceholderDataType.Email, true)));

        context.Users.AddRange(admin, user);
        context.Categories.AddRange(business, finance, humanResources, operations, marketing);
        context.Templates.AddRange(
            statementOfWork,
            invoice,
            curriculumVitae,
            projectReport,
            onboardingPlan,
            incidentReview,
            travelExpense,
            campaignBrief);

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task<bool> HasExistingDataAsync(
        AppDbContext context,
        CancellationToken cancellationToken)
    {
        return await context.Users.AnyAsync(cancellationToken)
            || await context.Categories.AnyAsync(cancellationToken)
            || await context.Templates.AnyAsync(cancellationToken);
    }

    private static Template CreatePublishedTemplate(
        string name,
        Guid categoryId,
        Guid createdBy,
        string content,
        DateTimeOffset createdAt,
        Action<TemplateVersion> configurePlaceholders)
    {
        var template = new Template(
            name,
            categoryId,
            createdBy,
            content,
            ContentFormat.Html,
            createdAt);
        var version = template.Versions.Single();

        configurePlaceholders(version);
        version.Publish(createdBy, createdAt);
        template.SetCurrentVersion(version.Id);
        template.Activate();

        return template;
    }

    private static void AddPlaceholders(
        TemplateVersion version,
        params (string Key, string Label, PlaceholderDataType DataType, bool Required)[] placeholders)
    {
        foreach (var placeholder in placeholders)
        {
            version.AddPlaceholder(
                placeholder.Key,
                placeholder.Label,
                placeholder.DataType,
                placeholder.Required);
        }
    }
}
