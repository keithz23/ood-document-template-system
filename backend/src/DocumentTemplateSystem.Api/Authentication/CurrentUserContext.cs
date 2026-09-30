using System.Security.Claims;
using DocumentTemplateSystem.Application.Exceptions;
using DocumentTemplateSystem.Application.Interfaces;

namespace DocumentTemplateSystem.Api.Authentication;

public sealed class CurrentUserContext(IHttpContextAccessor httpContextAccessor)
    : ICurrentUserContext
{
    public Guid UserId
    {
        get
        {
            var principal = httpContextAccessor.HttpContext?.User;
            var identifier = principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal?.FindFirstValue("sub");

            return Guid.TryParse(identifier, out var userId) && userId != Guid.Empty
                ? userId
                : throw UseCaseException.AuthenticationRequired();
        }
    }
}
