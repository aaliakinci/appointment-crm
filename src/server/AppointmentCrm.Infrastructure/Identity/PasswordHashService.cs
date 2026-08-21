using System.Globalization;
using System.Security.Cryptography;

namespace AppointmentCrm.Infrastructure.Identity;

public sealed class PasswordHashService
{
    private const int Iterations = 210_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const string Prefix = "acrm-pbkdf2-sha512";
    private static readonly byte[] DummySalt = Convert.FromHexString(
        "3F0B7A6D16A4F48B971D7BA886DC4B53");

    public string Hash(string password)
    {
        ValidatePassword(password);
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Derive(password, salt, Iterations);

        return string.Join(
            '$',
            Prefix,
            Iterations.ToString(CultureInfo.InvariantCulture),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public bool Verify(string storedHash, string password)
    {
        if (string.IsNullOrWhiteSpace(storedHash) || password is null)
        {
            PerformDummyVerification(password ?? string.Empty);
            return false;
        }

        string[] parts = storedHash.Split('$');
        if (parts.Length != 4
            || !string.Equals(parts[0], Prefix, StringComparison.Ordinal)
            || !int.TryParse(parts[1], CultureInfo.InvariantCulture, out int iterations)
            || iterations < 100_000
            || iterations > 1_000_000)
        {
            PerformDummyVerification(password);
            return false;
        }

        try
        {
            byte[] salt = Convert.FromBase64String(parts[2]);
            byte[] expected = Convert.FromBase64String(parts[3]);
            if (salt.Length != SaltSize || expected.Length != HashSize)
            {
                PerformDummyVerification(password);
                return false;
            }

            byte[] actual = Derive(password, salt, iterations);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            PerformDummyVerification(password);
            return false;
        }
    }

    public void PerformDummyVerification(string password)
    {
        _ = Derive(password, DummySalt, Iterations);
    }

    private static byte[] Derive(string password, byte[] salt, int iterations) =>
        Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA512,
            HashSize);

    private static void ValidatePassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        if (password.Length < 12)
        {
            throw new ArgumentException("Passwords must contain at least 12 characters.", nameof(password));
        }
    }
}
