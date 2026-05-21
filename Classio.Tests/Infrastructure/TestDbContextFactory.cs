using Classio.Data;
using Microsoft.EntityFrameworkCore;

namespace Classio.Tests.Infrastructure;

/// <summary>
/// Builds an isolated <see cref="ClassioDbContext"/> backed by the EF Core
/// InMemory provider. Each call returns a fresh context with a unique
/// database name so tests cannot share state.
/// </summary>
public static class TestDbContextFactory
{
    public static ClassioDbContext Create(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<ClassioDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            // InMemory has no transactions; silencing the warning keeps
            // SaveChanges quiet under EF Identity initialisation.
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new ClassioDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
