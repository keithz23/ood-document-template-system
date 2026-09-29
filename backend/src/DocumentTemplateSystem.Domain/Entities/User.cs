using DocumentTemplateSystem.Domain.Enums;

namespace DocumentTemplateSystem.Domain.Entities;

public sealed class User
{
    private User()
    {
    }

    public User(
        string username,
        string passwordHash,
        string fullName,
        string email,
        UserRole role,
        DateTimeOffset? createdAt = null)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username is required.", nameof(username));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Full name is required.", nameof(fullName));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        Id = Guid.NewGuid();
        Username = username.Trim();
        PasswordHash = passwordHash;
        FullName = fullName.Trim();
        Email = email.Trim();
        Role = role;
        IsActive = true;
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Username { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public string FullName { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public UserRole Role { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
