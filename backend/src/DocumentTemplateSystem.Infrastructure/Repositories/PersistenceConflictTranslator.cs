using DocumentTemplateSystem.Application.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DocumentTemplateSystem.Infrastructure.Repositories;

internal static class PersistenceConflictTranslator
{
    public static Exception Translate(DbUpdateException exception)
    {
        if (exception.InnerException is not PostgresException postgres
            || postgres.SqlState != PostgresErrorCodes.UniqueViolation)
        {
            return exception;
        }

        if (postgres.ConstraintName?.Contains("Username", StringComparison.OrdinalIgnoreCase) == true)
        {
            return UseCaseException.Conflict(
                "USERNAME_ALREADY_EXISTS",
                "That username is already in use.");
        }

        if (postgres.ConstraintName?.Contains("Email", StringComparison.OrdinalIgnoreCase) == true)
        {
            return UseCaseException.Conflict(
                "EMAIL_ALREADY_EXISTS",
                "That email address is already in use.");
        }

        return exception;
    }
}
