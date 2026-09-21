namespace GreenRetail.Features.Auth;

public interface IPasswordHasher
{
    (byte[] Salt, byte[] Hash) CreateHash(string password);
    bool Verify(string password, byte[] salt, byte[] hash);
}