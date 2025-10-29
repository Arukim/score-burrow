using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace ScoreBurrow.Data;

public class ScoreBurrowDbContextFactory : IDesignTimeDbContextFactory<ScoreBurrowDbContext>
{
    public ScoreBurrowDbContext CreateDbContext(string[] args)
    {
        // Build configuration similar to what happens in Program.cs
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../ScoreBurrow.Web"))
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found in appsettings files.");

        var optionsBuilder = new DbContextOptionsBuilder<ScoreBurrowDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new ScoreBurrowDbContext(optionsBuilder.Options);
    }
}
