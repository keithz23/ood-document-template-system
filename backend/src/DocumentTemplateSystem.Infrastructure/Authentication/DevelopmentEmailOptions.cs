namespace DocumentTemplateSystem.Infrastructure.Authentication;

public sealed class DevelopmentEmailOptions
{
    public const string SectionName = "DevelopmentEmail";

    public string FrontendBaseUrl { get; init; } = "http://localhost:3000";

    public bool ExposePasswordResetUrlInLogs { get; init; }
}
