using System.Security.Cryptography;
using System.Text;
using DocumentTemplateSystem.Application.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace DocumentTemplateSystem.Infrastructure.Authentication;

public sealed class CryptographicPasswordResetTokenService
    : IPasswordResetTokenService
{
    public GeneratedPasswordResetToken Generate()
    {
        var rawToken = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        return new GeneratedPasswordResetToken(rawToken, Hash(rawToken));
    }

    public string Hash(string rawToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
    }
}
