namespace AppointmentCrm.Application.Common;

public abstract class KnownApplicationException : Exception
{
    protected KnownApplicationException(string code, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        Code = code;
    }

    public string Code { get; }
}

public sealed class ApplicationConflictException : KnownApplicationException
{
    public ApplicationConflictException(
        string code,
        string message,
        Exception? innerException = null)
        : base(code, message, innerException)
    {
    }
}

public sealed class ApplicationNotFoundException : KnownApplicationException
{
    public ApplicationNotFoundException(
        string code,
        string message,
        Exception? innerException = null)
        : base(code, message, innerException)
    {
    }
}

public sealed class ApplicationValidationException : KnownApplicationException
{
    public ApplicationValidationException(
        string code,
        IReadOnlyDictionary<string, string[]> errors,
        string message = "One or more validation errors occurred.",
        Exception? innerException = null)
        : base(code, message, innerException)
    {
        ArgumentNullException.ThrowIfNull(errors);
        Errors = errors.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.ToArray(),
            StringComparer.Ordinal);
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public static ApplicationValidationException FromArgument(
        ArgumentException exception,
        string? field = null)
    {
        ArgumentNullException.ThrowIfNull(exception);
        string errorField = string.IsNullOrWhiteSpace(field)
            ? exception.ParamName ?? "request"
            : field;
        return new ApplicationValidationException(
            CommonErrorCodes.ValidationFailed,
            new Dictionary<string, string[]>
            {
                [errorField] = [exception.Message],
            },
            innerException: exception);
    }
}
