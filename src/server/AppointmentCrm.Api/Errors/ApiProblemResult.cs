using AppointmentCrm.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace AppointmentCrm.Api.Errors;

internal static class ApiProblemResult
{
    public static ObjectResult Create(
        ProblemDetailsFactory problemDetailsFactory,
        HttpContext httpContext,
        int statusCode,
        string code,
        string detail)
    {
        ProblemDetails problemDetails = problemDetailsFactory.CreateProblemDetails(
            httpContext,
            statusCode,
            detail: detail);
        ApiProblemDetailsDefaults.Apply(httpContext, problemDetails, code);

        var result = new ObjectResult(problemDetails) { StatusCode = statusCode };
        result.ContentTypes.Add("application/problem+json");
        return result;
    }

    public static ObjectResult CreateValidation(
        HttpContext httpContext,
        IReadOnlyDictionary<string, string[]> errors)
    {
        var problemDetails = new ValidationProblemDetails(
            errors.ToDictionary(pair => pair.Key, pair => pair.Value))
        {
            Status = StatusCodes.Status400BadRequest,
            Detail = "One or more validation errors occurred.",
        };
        ApiProblemDetailsDefaults.Apply(
            httpContext,
            problemDetails,
            CommonErrorCodes.ValidationFailed);

        var result = new ObjectResult(problemDetails)
        {
            StatusCode = StatusCodes.Status400BadRequest,
        };
        result.ContentTypes.Add("application/problem+json");
        return result;
    }
}
