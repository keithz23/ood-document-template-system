using DocumentTemplateSystem.Domain.Entities;

namespace DocumentTemplateSystem.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> FindByUsernameOrEmailAsync(
        string usernameOrEmail,
        CancellationToken cancellationToken = default);
}
