using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GreenRetail.Data;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PosDbContext>
{
    public PosDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PosDbContext>();

        optionsBuilder.UseSqlite(DataModule.GetConnectionString());

        return new PosDbContext(optionsBuilder.Options);
    }
}