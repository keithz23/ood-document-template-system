using DocumentTemplateSystem.Domain.Entities;

namespace DocumentTemplateSystem.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<User?> FindByUsernameOrEmailAsync(
        string usernameOrEmail,
        CancellationToken cancellationToken = default);
}
