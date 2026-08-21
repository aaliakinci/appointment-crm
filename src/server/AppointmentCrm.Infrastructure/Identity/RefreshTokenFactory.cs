using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;

namespace AppointmentCrm.Infrastructure.Identity;

internal static class RefreshTokenFactory
{
    public static string Create() =>
        WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
}
