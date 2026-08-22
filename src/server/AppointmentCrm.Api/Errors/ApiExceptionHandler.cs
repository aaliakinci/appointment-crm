using AppointmentCrm.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentCrm.Api.Errors;

internal sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException
            && httpContext.RequestAborted.IsCancellationRequested)
        {
            return false;
        }

        ProblemDetails problemDetails = CreateProblemDetails(exception);
        httpContext.Response.StatusCode = problemDetails.Status
            ?? StatusCodes.Status500InternalServerError;
        ApiProblemDetailsDefaults.Apply(
            httpContext,
            problemDetails,
            (exception as KnownApplicationException)?.Code);

        if (exception is not KnownApplicationException)
        {
            logger.LogError(
                exception,
                "Unhandled API exception. ErrorCode={ErrorCode} TraceId={TraceId}",
                CommonErrorCodes.UnexpectedError,
                httpContext.TraceIdentifier);
        }

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception,
        });
        return true;
    }

    private static ProblemDetails CreateProblemDetails(Exception exception) => exception switch
    {
        ApplicationValidationException validation => new ValidationProblemDetails(
            validation.Errors.ToDictionary(pair => pair.Key, pair => pair.Value))
        {
            Status = StatusCodes.Status400BadRequest,
            Detail = validation.Message,
        },
        ApplicationConflictException conflict => new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Detail = conflict.Message,
        },
        ApplicationNotFoundException notFound => new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Detail = notFound.Message,
        },
        ApplicationUnauthorizedException unauthorized => new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Detail = unauthorized.Message,
        },
        ApplicationForbiddenException forbidden => new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Detail = forbidden.Message,
        },
        _ => new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Detail = "An unexpected error occurred.",
        },
    };
}
