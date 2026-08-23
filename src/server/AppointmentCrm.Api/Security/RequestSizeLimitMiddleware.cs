using Microsoft.Extensions.Options;

namespace AppointmentCrm.Api.Security;

internal sealed class RequestSizeLimitMiddleware(
    RequestDelegate next,
    IOptions<SecurityOptions> options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        long? contentLength = context.Request.ContentLength;
        if (contentLength > options.Value.MaximumRequestBodyBytes)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            return;
        }

        await next(context);
    }
}
