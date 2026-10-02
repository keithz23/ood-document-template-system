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

    Task<bool> UsernameExistsAsync(
        string username,
        CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
