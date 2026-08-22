using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace AppointmentCrm.Api.Errors;

internal static class ApiProblemDetailsDefaults
{
    public const string CodeExtension = "code";
    public const string TraceIdExtension = "traceId";

    public static void Apply(
        HttpContext httpContext,
        ProblemDetails problemDetails,
        string? code = null)
    {
        int statusCode = problemDetails.Status ?? httpContext.Response.StatusCode;
        problemDetails.Status = statusCode;
        problemDetails.Type ??= "about:blank";
        problemDetails.Title ??= ReasonPhrases.GetReasonPhrase(statusCode);
        problemDetails.Instance ??= httpContext.Request.Path.Value;

        if (code is not null
            || !problemDetails.Extensions.ContainsKey(CodeExtension))
        {
            problemDetails.Extensions[CodeExtension] = code ?? ApiErrorCodes.ForStatus(statusCode);
        }

        problemDetails.Extensions[TraceIdExtension] = httpContext.TraceIdentifier;
    }
}
