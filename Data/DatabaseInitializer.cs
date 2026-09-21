using Microsoft.EntityFrameworkCore;
using GreenRetail.Accounting;
using GreenRetail.Data.Entities;
using GreenRetail.Features.Auth;
using GreenRetail.Rbac;

namespace GreenRetail.Data;

public interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

public sealed class DatabaseInitializer : IDatabaseInitializer
{
    private readonly IDbContextFactory<PosDbContext> _dbContextFactory;
    private readonly IPasswordHasher _passwordHasher;

    public DatabaseInitializer(
        IDbContextFactory<PosDbContext> dbContextFactory,
        IPasswordHasher passwordHasher)
    {
        _dbContextFactory = dbContextFactory;
        _passwordHasher = passwordHasher;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        // Use MigrateAsync so the schema is always driven by migrations.
        // This keeps the app consistent with `dotnet ef database update`.
        await db.Database.MigrateAsync(cancellationToken);

        await SeedBranchAsync(db, cancellationToken);
        await SeedTerminalAsync(db, cancellationToken);
        await SeedUsersAsync(db, cancellationToken);
        await EnsureDevCredentialsAsync(db, cancellationToken);
        await SeedCatalogAsync(db, cancellationToken);

        await AccountingSeeder.SeedAsync(db, cancellationToken);
        await RbacSeeder.SeedAsync(db, cancellationToken);
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
        if (await db.Terminals.AnyAsync(cancellationToken))
            return;

        db.Terminals.Add(new Terminal
        {
            Name = "Main Register",
            Code = "REG-01",
            IsActive = true,
            CreatedUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedUsersAsync(PosDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Users.AnyAsync(cancellationToken))
            return;

        var users = new[]
        {
            ("owner", "Owner", "Owner", "Owner!123"),
            ("manager", "Manager", "Manager", "Manager!123"),
            ("cashier", "Cashier", "Cashier", "Cashier!123")
        };

        foreach (var (userName, displayName, role, password) in users)
        {
            var (salt, hash) = _passwordHasher.CreateHash(password);

            db.Users.Add(new AppUser
            {
                UserName = userName,
                Name = displayName,
                Role = role,
                PasswordSalt = salt,
                PasswordHash = hash,
                IsActive = true,
                RequiresPasswordChange = true,
                CreatedUtc = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureDevCredentialsAsync(PosDbContext db, CancellationToken cancellationToken)
    {
#if DEBUG
        var devUsers = new[]
        {
            ("owner", "Owner!123"),
            ("manager", "Manager!123"),
            ("cashier", "Cashier!123")
        };

        foreach (var (userName, password) in devUsers)
        {
            var user = await db.Users
                .FirstOrDefaultAsync(x => x.UserName == userName, cancellationToken);

            if (user is null)
                continue;

            var isValid =
                user.PasswordSalt.Length > 0 &&
                user.PasswordHash.Length > 0 &&
                _passwordHasher.Verify(password, user.PasswordSalt, user.PasswordHash);

            if (!isValid)
            {
                var (salt, hash) = _passwordHasher.CreateHash(password);

                user.PasswordSalt = salt;
                user.PasswordHash = hash;
                user.IsActive = true;
                user.RequiresPasswordChange = true;
                user.LockoutEndUtc = null;
                user.FailedLoginCount = 0;
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