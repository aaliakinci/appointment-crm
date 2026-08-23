using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace AppointmentCrm.Api.Security;

internal static class SecurityConfiguration
{
    public static bool IsAllowedOrigin(string origin)
    {
        return Uri.TryCreate(origin, UriKind.Absolute, out Uri? uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && string.IsNullOrEmpty(uri.UserInfo)
            && string.IsNullOrEmpty(uri.Query)
            && string.IsNullOrEmpty(uri.Fragment)
            && uri.AbsolutePath == "/"
            && !origin.Contains('*', StringComparison.Ordinal);
    }

    public static void ConfigureForwardedHeaders(
        ForwardedHeadersOptions options,
        SecurityOptions security)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
            | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = security.ForwardLimit;
        foreach (string value in security.KnownProxies)
        {
            if (!IPAddress.TryParse(value, out IPAddress? address))
            {
                throw new InvalidOperationException(
                    $"Security:KnownProxies contains an invalid IP address: '{value}'.");
            }

            options.KnownProxies.Add(address);
        }
    }
}
