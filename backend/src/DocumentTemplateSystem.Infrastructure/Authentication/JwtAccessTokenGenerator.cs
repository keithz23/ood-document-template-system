using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DocumentTemplateSystem.Application.Authorization;
using DocumentTemplateSystem.Application.Interfaces;
using DocumentTemplateSystem.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DocumentTemplateSystem.Infrastructure.Authentication;

public sealed class JwtAccessTokenGenerator(
    IOptions<JwtOptions> options,
    TimeProvider timeProvider) : IAccessTokenGenerator
{
    public AccessTokenResult Generate(User user)
    {
        var settings = options.Value;
        ValidateSettings(settings);

        var issuedAt = timeProvider.GetUtcNow();
        var expiresAt = issuedAt.AddMinutes(settings.ExpiresMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(JwtRegisteredClaimNames.Name, user.FullName),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(Permissions.ForRole(user.Role)
            .Select(permission => new Claim(Permissions.ClaimType, permission)));
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            settings.Issuer,
            settings.Audience,
            claims,
            notBefore: issuedAt.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new AccessTokenResult(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt);
    }

    private static void ValidateSettings(JwtOptions settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Issuer)
            || string.IsNullOrWhiteSpace(settings.Audience)
            || string.IsNullOrWhiteSpace(settings.Key)
            || Encoding.UTF8.GetByteCount(settings.Key) < 32
            || settings.ExpiresMinutes <= 0)
        {
            throw new InvalidOperationException(
                "Jwt configuration requires an issuer, audience, expiry, and a key of at least 32 bytes.");
        }
    }
}
