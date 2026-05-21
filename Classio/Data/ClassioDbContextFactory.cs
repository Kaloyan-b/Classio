using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Classio.Data
{
    /// <summary>
    /// Used only by EF Core CLI/PMC tools (Add-Migration, Remove-Migration, etc.).
    /// Prevents the tools from running the full app startup (and hitting the DB).
    /// </summary>
    public class ClassioDbContextFactory : IDesignTimeDbContextFactory<ClassioDbContext>
    {
        public ClassioDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ClassioDbContext>();
            optionsBuilder.UseNpgsql("Host=localhost;Database=classio;Username=postgres;Password=postgres");
            return new ClassioDbContext(optionsBuilder.Options);
        }
    }
}
