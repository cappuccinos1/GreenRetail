using Microsoft.EntityFrameworkCore;
using GreenRetail.Core.Abstractions;
using GreenRetail.Core.Results;
using GreenRetail.Data;
using GreenRetail.Data.Entities;

namespace GreenRetail.Features.Auth;

public sealed record AuthenticateUserCommand(string UserName, string Password);

public sealed record AuthenticatedUser(
    Guid Id,
    string UserName,
    string DisplayName,
    string Role,
    bool RequiresPasswordChange);

public interface IAuthenticateUserUseCase
    : IUseCase<AuthenticateUserCommand, Result<AuthenticatedUser>>
{
}

public sealed class AuthenticateUserUseCase : IAuthenticateUserUseCase
{
    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 5;

    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IClock _clock;

    public AuthenticateUserUseCase(
        IDbContextFactory<PosDbContext> dbContextFactory,
        IPasswordHasher passwordHasher,
        IClock clock)
    {
        _dbContextFactory = dbContextFactory;
        _passwordHasher = passwordHasher;
        _clock = clock;
    }

    public async Task<Result<AuthenticatedUser>> ExecuteAsync(
        AuthenticateUserCommand request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            return Result<AuthenticatedUser>.Fail("Username is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<AuthenticatedUser>.Fail("Password is required.");
        }

        var userName = request.UserName.Trim().ToLowerInvariant();

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var user = await db.Users
            .FirstOrDefaultAsync(x => x.UserName == userName && x.IsActive, cancellationToken);

        if (user is null)
        {
            // Dummy verification to reduce username-enumeration timing differences.
            _passwordHasher.Verify(request.Password, new byte[16], new byte[32]);

            return Result<AuthenticatedUser>.Fail("Invalid username or password.");
        }

        if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > _clock.UtcNow)
        {
            return Result<AuthenticatedUser>.Fail("Account is temporarily locked. Try again later.");
        }

        var isValidPassword = _passwordHasher.Verify(request.Password, user.PasswordSalt, user.PasswordHash);

        if (!isValidPassword)
        {
            user.FailedLoginCount++;

            if (user.FailedLoginCount >= MaxFailedAttempts)
            {
                user.LockoutEndUtc = _clock.UtcNow.AddMinutes(LockoutMinutes);
                user.FailedLoginCount = 0;
            }

            db.AuditLog.Add(new AuditLogEntry
            {
                CreatedUtc = _clock.UtcNow,
                UserId = user.Id,
                Action = "LoginFailed",
                Details = "Invalid password."
            });

            await db.SaveChangesAsync(cancellationToken);

            return Result<AuthenticatedUser>.Fail("Invalid username or password.");
        }

        user.FailedLoginCount = 0;
        user.LockoutEndUtc = null;

        db.AuditLog.Add(new AuditLogEntry
        {
            CreatedUtc = _clock.UtcNow,
            UserId = user.Id,
            Action = "LoginSuccess",
            Details = $"User '{user.UserName}' logged in."
        });

        await db.SaveChangesAsync(cancellationToken);

        return Result<AuthenticatedUser>.Ok(new AuthenticatedUser(
            user.Id,
            user.UserName,
            user.Name,
            user.Role,
            user.RequiresPasswordChange));
    }
}