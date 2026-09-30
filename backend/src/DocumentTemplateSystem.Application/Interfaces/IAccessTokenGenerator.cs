using DocumentTemplateSystem.Domain.Entities;

namespace DocumentTemplateSystem.Application.Interfaces;

public sealed record AccessTokenResult(string AccessToken, DateTimeOffset ExpiresAt);

public interface IAccessTokenGenerator
{
    AccessTokenResult Generate(User user);
}
