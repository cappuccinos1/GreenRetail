using System.Security.Cryptography;

namespace GreenRetail.Features.Auth;

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    public (byte[] Salt, byte[] Hash) CreateHash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize);

        return (salt, hash);
    }

    public bool Verify(string password, byte[] salt, byte[] hash)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        if (salt.Length == 0 || hash.Length == 0)
            return false;

        var candidate = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize);

        return CryptographicOperations.FixedTimeEquals(candidate, hash);
    }
}