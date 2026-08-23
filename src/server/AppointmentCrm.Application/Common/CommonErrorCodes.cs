namespace AppointmentCrm.Application.Common;

public static class CommonErrorCodes
{
    public const string ValidationFailed = "common.validation_failed";
    public const string BadRequest = "common.bad_request";
    public const string NotFound = "common.not_found";
    public const string Conflict = "common.conflict";
    public const string AuthenticationRequired = "common.authentication_required";
    public const string Forbidden = "common.forbidden";
    public const string RateLimited = "common.rate_limited";
    public const string PayloadTooLarge = "common.payload_too_large";
    public const string UnexpectedError = "common.unexpected_error";
    public const string HttpError = "common.http_error";
}
