namespace DocumentTemplateSystem.Application.Interfaces;

public sealed record GeneratedPasswordResetToken(string RawToken, string TokenHash);

public interface IPasswordResetTokenService
{
    GeneratedPasswordResetToken Generate();

    string Hash(string rawToken);
}
