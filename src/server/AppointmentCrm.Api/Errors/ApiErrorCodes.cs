using AppointmentCrm.Application.Common;

namespace AppointmentCrm.Api.Errors;

internal static class ApiErrorCodes
{
    public const string UntrustedOrigin = "security.untrusted_origin";

    public static string ForStatus(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => CommonErrorCodes.BadRequest,
        StatusCodes.Status401Unauthorized => CommonErrorCodes.AuthenticationRequired,
        StatusCodes.Status403Forbidden => CommonErrorCodes.Forbidden,
        StatusCodes.Status404NotFound => CommonErrorCodes.NotFound,
        StatusCodes.Status409Conflict => CommonErrorCodes.Conflict,
        StatusCodes.Status429TooManyRequests => CommonErrorCodes.RateLimited,
        StatusCodes.Status500InternalServerError => CommonErrorCodes.UnexpectedError,
        _ => CommonErrorCodes.HttpError,
    };
}
