using DocumentTemplateSystem.Application.Interfaces;
using DocumentTemplateSystem.Domain.Entities;
using DocumentTemplateSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DocumentTemplateSystem.Infrastructure.Repositories;

public sealed class UserRepository(AppDbContext context) : IUserRepository
{
    public Task<User?> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);
    }

    public Task<User?> FindByUsernameOrEmailAsync(
        string usernameOrEmail,
        CancellationToken cancellationToken = default)
    {
        return FindSingleMatchAsync(context, usernameOrEmail, cancellationToken);
    }

    private static async Task<User?> FindSingleMatchAsync(
        AppDbContext context,
        string usernameOrEmail,
        CancellationToken cancellationToken)
    {
        var matches = await context.Users
            .AsNoTracking()
            .Where(user => user.Username == usernameOrEmail
                || user.Email == usernameOrEmail)
            .Take(2)
            .ToListAsync(cancellationToken);

        return matches.Count == 1 ? matches[0] : null;
    }
}
