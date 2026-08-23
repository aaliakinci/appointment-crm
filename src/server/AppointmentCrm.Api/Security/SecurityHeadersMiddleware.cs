namespace AppointmentCrm.Api.Security;

internal sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(static state =>
        {
            var response = (HttpResponse)state;
            response.Headers.XContentTypeOptions = "nosniff";
            response.Headers.XFrameOptions = "DENY";
            response.Headers["Referrer-Policy"] = "no-referrer";
            response.Headers.ContentSecurityPolicy =
                "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";
            response.Headers["Permissions-Policy"] =
                "camera=(), microphone=(), geolocation=(), payment=()";
            return Task.CompletedTask;
        }, context.Response);
        return next(context);
    }
}
