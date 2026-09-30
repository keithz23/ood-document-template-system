using DocumentTemplateSystem.Application.Interfaces;
using DocumentTemplateSystem.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace DocumentTemplateSystem.Infrastructure.Authentication;

public sealed class AspNetPasswordHashService : IPasswordHashService
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public string HashPassword(User user, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        return _passwordHasher.HashPassword(user, password);
    }

    public bool VerifyPassword(User user, string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return false;
        }

        return _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            password) is not PasswordVerificationResult.Failed;
    }
}
