using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace FinTrack.Infrastructure.Security;

public static class PasswordHasher
{
    public static string Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, 100_000, 32);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string stored)
    {
        var parts = stored.Split('.', 2);
        if (parts.Length != 2) return false;

        var salt = Convert.FromBase64String(parts[0]);
        var expected = parts[1];

        var hash = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, 100_000, 32);
        return Convert.ToBase64String(hash) == expected;
    }
}
