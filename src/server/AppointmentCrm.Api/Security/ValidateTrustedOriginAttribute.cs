using AppointmentCrm.Api.Errors;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.WebUtilities;

namespace AppointmentCrm.Api.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public sealed class ValidateTrustedOriginAttribute : TypeFilterAttribute
{
    public ValidateTrustedOriginAttribute()
        : base(typeof(ValidateTrustedOriginFilter))
    {
    }
}

internal sealed class ValidateTrustedOriginFilter(
    ICorsPolicyProvider corsPolicyProvider,
    ICorsService corsService,
    ProblemDetailsFactory problemDetailsFactory) : IAsyncResourceFilter
{
    public async Task OnResourceExecutionAsync(
        ResourceExecutingContext context,
        ResourceExecutionDelegate next)
    {
        HttpContext httpContext = context.HttpContext;
        string? origin = httpContext.Request.Headers.Origin.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(origin))
        {
            await next();
            return;
        }

        CorsPolicy? policy = await corsPolicyProvider.GetPolicyAsync(
            httpContext,
            CorsPolicyNames.Web);
        if (policy is null)
        {
            throw new InvalidOperationException(
                $"The CORS policy '{CorsPolicyNames.Web}' is not configured.");
        }

        CorsResult corsResult = corsService.EvaluatePolicy(httpContext, policy);
        if (corsResult.IsOriginAllowed)
        {
            await next();
            return;
        }

        ProblemDetails problem = problemDetailsFactory.CreateProblemDetails(
            httpContext,
            StatusCodes.Status403Forbidden,
            ReasonPhrases.GetReasonPhrase(StatusCodes.Status403Forbidden),
            detail: "Untrusted request origin.");
        ApiProblemDetailsDefaults.Apply(httpContext, problem, ApiErrorCodes.UntrustedOrigin);
        var result = new ObjectResult(problem)
        {
            StatusCode = StatusCodes.Status403Forbidden,
        };
        result.ContentTypes.Add("application/problem+json");
        context.Result = result;
    }
}
