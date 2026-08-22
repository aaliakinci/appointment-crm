using AppointmentCrm.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace AppointmentCrm.Api.Errors;

internal sealed class ApiProblemDetailsFactory(IOptions<ApiBehaviorOptions> options)
    : ProblemDetailsFactory
{
    private readonly ApiBehaviorOptions _options = options.Value;

    public override ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        string? detail = null,
        string? instance = null)
    {
        int resolvedStatus = statusCode ?? StatusCodes.Status500InternalServerError;
        var problemDetails = new ProblemDetails
        {
            Status = resolvedStatus,
            Title = title,
            Type = type,
            Detail = detail,
            Instance = instance,
        };
        ApplyClientErrorMapping(problemDetails, resolvedStatus);
        ApiProblemDetailsDefaults.Apply(httpContext, problemDetails);
        return problemDetails;
    }

    public override ValidationProblemDetails CreateValidationProblemDetails(
        HttpContext httpContext,
        ModelStateDictionary modelStateDictionary,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        string? detail = null,
        string? instance = null)
    {
        ArgumentNullException.ThrowIfNull(modelStateDictionary);
        int resolvedStatus = statusCode ?? StatusCodes.Status400BadRequest;
        var problemDetails = new ValidationProblemDetails(modelStateDictionary)
        {
            Status = resolvedStatus,
            Title = title,
            Type = type,
            Detail = detail,
            Instance = instance,
        };
        ApplyClientErrorMapping(problemDetails, resolvedStatus);
        ApiProblemDetailsDefaults.Apply(
            httpContext,
            problemDetails,
            CommonErrorCodes.ValidationFailed);
        return problemDetails;
    }

    private void ApplyClientErrorMapping(ProblemDetails problemDetails, int statusCode)
    {
        if (!_options.ClientErrorMapping.TryGetValue(statusCode, out ClientErrorData? clientError))
        {
            return;
        }

        problemDetails.Title ??= clientError.Title;
        problemDetails.Type ??= clientError.Link;
    }
}
