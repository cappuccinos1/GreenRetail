using Microsoft.EntityFrameworkCore;
using GreenRetail.Core.Abstractions;
using GreenRetail.Core.Results;
using GreenRetail.Data;
using GreenRetail.Data.Entities;
using GreenRetail.Shared.State;

namespace GreenRetail.Features.Auth;

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword);

public interface IChangePasswordUseCase : IUseCase<ChangePasswordCommand, Result<bool>> { }

public sealed class ChangePasswordUseCase : IChangePasswordUseCase
{
    private readonly IDbContextFactory<PosDbContext> _dbFactory;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUser;
    private readonly IClock _clock;

    public ChangePasswordUseCase(
        IDbContextFactory<PosDbContext> dbFactory,
        IPasswordHasher passwordHasher,
        ICurrentUserService currentUser,
        IClock clock)
    {
        _dbFactory = dbFactory;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<bool>> ExecuteAsync(
        ChangePasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            return Result<bool>.Fail("You must be signed in.");

        if (!_passwordHasher.IsValidPasswordPolicy(command.NewPassword))
            return Result<bool>.Fail("Use at least 12 characters with upper/lowercase letters, a number, and a symbol.");

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var user = await db.Users.FirstOrDefaultAsync(
            x => x.Id == _currentUser.UserId.Value && x.IsActive,
            cancellationToken);

        if (user is null)
            return Result<bool>.Fail("Your user account is no longer active.");

        if (!_passwordHasher.Verify(command.CurrentPassword, user.PasswordSalt, user.PasswordHash))
            return Result<bool>.Fail("Current password is incorrect.");

        if (_passwordHasher.Verify(command.NewPassword, user.PasswordSalt, user.PasswordHash))
            return Result<bool>.Fail("Choose a different password.");

        var (salt, hash) = _passwordHasher.CreateHash(command.NewPassword);
        user.PasswordSalt = salt;
        user.PasswordHash = hash;
        user.RequiresPasswordChange = false;
        user.FailedLoginCount = 0;
        user.LockoutEndUtc = null;

        db.AuditLog.Add(new AuditLogEntry
        {
            CreatedUtc = _clock.UtcNow,
            UserId = user.Id,
            Action = "PasswordChanged",
            Details = "User changed password successfully."
        });

        await db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Ok(true);
    }
}
