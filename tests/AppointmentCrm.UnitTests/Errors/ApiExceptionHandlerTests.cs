using AppointmentCrm.Api.Errors;
using AppointmentCrm.Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AppointmentCrm.UnitTests.Errors;

public sealed class ApiExceptionHandlerTests
{
    [Fact]
    public async Task UnexpectedException_ReturnsSafeGenericProblemAndKeepsDetailsOutOfResponse()
    {
        var writer = new CapturingProblemDetailsService();
        var handler = new ApiExceptionHandler(
            writer,
            NullLogger<ApiExceptionHandler>.Instance);
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "trace-500",
        };
        httpContext.Request.Path = "/api/v1/test";

        bool handled = await handler.TryHandleAsync(
            httpContext,
            new InvalidOperationException("database-secret"),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);
        ProblemDetails problem = Assert.IsType<ProblemDetails>(writer.Context?.ProblemDetails);
        Assert.Equal("An unexpected error occurred.", problem.Detail);
        Assert.DoesNotContain("database-secret", problem.Detail, StringComparison.Ordinal);
        Assert.Equal(CommonErrorCodes.UnexpectedError, problem.Extensions["code"]);
        Assert.Equal("trace-500", problem.Extensions["traceId"]);
        Assert.Equal("/api/v1/test", problem.Instance);
    }

    [Fact]
    public async Task ValidationException_ReturnsItsStableCodeAndFieldErrors()
    {
        var writer = new CapturingProblemDetailsService();
        var handler = new ApiExceptionHandler(
            writer,
            NullLogger<ApiExceptionHandler>.Instance);
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "trace-400",
        };
        var exception = new ApplicationValidationException(
            "employees.duplicate_service_assignment",
            new Dictionary<string, string[]>
            {
                ["serviceIds"] = ["Service assignments cannot contain duplicates."],
            });

        bool handled = await handler.TryHandleAsync(
            httpContext,
            exception,
            CancellationToken.None);

        Assert.True(handled);
        ValidationProblemDetails problem = Assert.IsType<ValidationProblemDetails>(
            writer.Context?.ProblemDetails);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal("employees.duplicate_service_assignment", problem.Extensions["code"]);
        Assert.Contains("serviceIds", problem.Errors.Keys);
    }

    private sealed class CapturingProblemDetailsService : IProblemDetailsService
    {
        public ProblemDetailsContext? Context { get; private set; }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            Context = context;
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            Context = context;
            return ValueTask.FromResult(true);
        }
    }
}
