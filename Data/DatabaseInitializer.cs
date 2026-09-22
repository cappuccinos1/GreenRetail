using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using GreenRetail.Data.Entities;
using GreenRetail.Features.Auth;
using GreenRetail.Rbac;
using GreenRetail.Core.Abstractions;

namespace GreenRetail.Data;

public interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

public sealed class DatabaseInitializer : IDatabaseInitializer
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IClock _clock;

    public DatabaseInitializer(
        IDbContextFactory<PosDbContext> dbContextFactory,
        IPasswordHasher passwordHasher,
        IClock clock)
    {
        _dbContextFactory = dbContextFactory;
        _passwordHasher = passwordHasher;
        _clock = clock;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        // Use MigrateAsync so the schema is always driven by migrations.
        // This keeps the app consistent with `dotnet ef database update`.
        await db.Database.MigrateAsync(cancellationToken);

        await SeedUsersAsync(db, cancellationToken);
        await RbacSeeder.SeedAsync(db, cancellationToken);

        // Business/sample data is opt-in. A fresh installation contains no branch,
        // terminal, catalog or test users until setup (or explicit dev seeding).
        if (IsDevelopmentSeedEnabled())
        {
            await SeedBranchAsync(db, cancellationToken);
            await SeedTerminalAsync(db, cancellationToken);
            await SeedCatalogAsync(db, cancellationToken);
            await EnsureDevCredentialsAsync(db, cancellationToken);
        }
    }

    private async Task SeedBranchAsync(PosDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Branches.AnyAsync(cancellationToken))
            return;

        db.Branches.Add(new Branch
        {
            Code = "MAIN",
            Name = "Main Branch",
            IsActive = true,
            CreatedUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedTerminalAsync(PosDbContext db, CancellationToken cancellationToken)
    {
        var activeBranchIds = await db.Branches
            .Where(x => x.IsActive)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (activeBranchIds.Count != 1)
            throw new InvalidOperationException("Terminal setup requires exactly one active branch until a terminal is explicitly assigned to a branch.");

        var existing = await db.Terminals.FirstOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            if (existing.BranchId is null)
            {
                existing.BranchId = activeBranchIds[0];
                await db.SaveChangesAsync(cancellationToken);
            }
            return;
        }

        db.Terminals.Add(new Terminal
        {
            BranchId = activeBranchIds[0],
            Name = "Main Register",
            Code = "REG-01",
            IsActive = true,
            CreatedUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedUsersAsync(PosDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Users.AnyAsync(cancellationToken)) return;

        var temporaryPassword = GenerateTemporaryPassword();
        var (salt, hash) = _passwordHasher.CreateHash(temporaryPassword);
        db.Users.Add(new AppUser
        {
            UserName = "owner", Name = "Owner", Role = "Owner",
            PasswordSalt = salt, PasswordHash = hash, IsActive = true,
            RequiresPasswordChange = true, CreatedUtc = _clock.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        WriteInitialOwnerCredentials("owner", temporaryPassword);
    }

    private static string GenerateTemporaryPassword()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%^&*";
        var chars = new char[24];
        for (var i = 0; i < chars.Length; i++) chars[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
        return new string(chars);
    }

    private void WriteInitialOwnerCredentials(string userName, string password)
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GreenRetail");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "INITIAL_OWNER_CREDENTIALS.txt");
        File.WriteAllText(path,
            $"GREEN RETAIL INITIAL OWNER CREDENTIALS{Environment.NewLine}" +
            $"Generated: {_clock.UtcNow:O}{Environment.NewLine}{Environment.NewLine}" +
            $"Username: {userName}{Environment.NewLine}" +
            $"Temporary password: {password}{Environment.NewLine}{Environment.NewLine}" +
            "Change this password immediately after first login, then delete this file.");
    }

    private static bool IsDevelopmentSeedEnabled()
        => string.Equals(Environment.GetEnvironmentVariable("GREENRETAIL_DEV_SEED"), "true", StringComparison.OrdinalIgnoreCase);

    private async Task EnsureDevCredentialsAsync(PosDbContext db, CancellationToken cancellationToken)
    {
#if DEBUG
        var devUsers = new[]
        {
            (UserName: "owner", Name: "Owner", Password: "Owner!123", RoleName: "Owner"),
            (UserName: "manager", Name: "Manager", Password: "Manager!123", RoleName: "CashierManager"),
            (UserName: "cashier", Name: "Cashier", Password: "Cashier!123", RoleName: "Cashier")
        };

        foreach (var dev in devUsers)
        {
            var user = await db.Users.FirstOrDefaultAsync(x => x.UserName == dev.UserName, cancellationToken);
            if (user is null)
            {
                var (salt, hash) = _passwordHasher.CreateHash(dev.Password);
                user = new AppUser
                {
                    UserName = dev.UserName, Name = dev.Name, Role = dev.RoleName,
                    PasswordSalt = salt, PasswordHash = hash, IsActive = true,
                    RequiresPasswordChange = false, CreatedUtc = _clock.UtcNow
                };
                db.Users.Add(user);
                await db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                var isValid = user.PasswordSalt.Length > 0 && user.PasswordHash.Length > 0 && _passwordHasher.Verify(dev.Password, user.PasswordSalt, user.PasswordHash);
                if (!isValid)
                {
                    var (salt, hash) = _passwordHasher.CreateHash(dev.Password);
                    user.PasswordSalt = salt; user.PasswordHash = hash; user.IsActive = true;
                    user.LockoutEndUtc = null; user.FailedLoginCount = 0;
                }
                user.Role = dev.RoleName;
            }

            var role = await db.Roles.FirstOrDefaultAsync(x => x.Name == dev.RoleName, cancellationToken);
            if (role is not null && !await db.UserRoles.AnyAsync(x => x.UserId == user.Id && x.RoleId == role.Id, cancellationToken))
            {
                db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, AssignedUtc = _clock.UtcNow });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
#endif
    }

    private async Task SeedCatalogAsync(PosDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Categories.AnyAsync(cancellationToken))
            return;

        var units = new[]
        {
            new Unit { Name = "Piece", Abbreviation = "PC" },
            new Unit { Name = "Pack", Abbreviation = "PK" },
            new Unit { Name = "Carton", Abbreviation = "CTN" },
            new Unit { Name = "Bottle", Abbreviation = "BTL" },
            new Unit { Name = "Bag", Abbreviation = "BAG" },
            new Unit { Name = "Kilogram", Abbreviation = "KG", IsWeighed = true },
            new Unit { Name = "Litre", Abbreviation = "L", IsWeighed = true }
        };

        db.Units.AddRange(units);

        var categories = new[]
        {
            new Category { Name = "Groceries" },
            new Category { Name = "Beverages" },
            new Category { Name = "Snacks" },
            new Category { Name = "Frozen Foods" },
            new Category { Name = "Household Cleaning" },
            new Category { Name = "Personal Care" },
            new Category { Name = "Baby Care" },
            new Category { Name = "Home & Kitchen" },
            new Category { Name = "Electronics" },
            new Category { Name = "Stationery" }
        };

        db.Categories.AddRange(categories);
        await db.SaveChangesAsync(cancellationToken);
    }
}