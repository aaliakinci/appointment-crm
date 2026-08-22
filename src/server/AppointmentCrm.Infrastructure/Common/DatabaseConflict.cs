using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AppointmentCrm.Infrastructure.Common;

internal static class DatabaseConflict
{
    public static bool IsUniqueConstraint(
        DbUpdateException exception,
        params string[] constraintNames) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
        } postgresException
        && constraintNames.Contains(postgresException.ConstraintName, StringComparer.Ordinal);
}
