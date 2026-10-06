namespace DocumentTemplateSystem.Infrastructure.Authentication;

public sealed class EmailOptions
{
    public const string SectionName = "Email";
    public const string DevelopmentProvider = "Development";
    public const string SmtpProvider = "Smtp";

    public string Provider { get; set; } = DevelopmentProvider;
    public string SmtpHost { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public bool UseStartTls { get; set; } = true;
    public string SmtpUser { get; set; } = string.Empty;
    public string SmtpAppPassword { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Document Template System";
    public string FrontendBaseUrl { get; set; } = "http://localhost:3000";
}
