using DocumentTemplateSystem.Domain.Entities;

namespace DocumentTemplateSystem.Application.Interfaces;

public interface IPasswordHashService
{
    string HashPassword(User user, string password);

    bool VerifyPassword(User user, string password);
}
