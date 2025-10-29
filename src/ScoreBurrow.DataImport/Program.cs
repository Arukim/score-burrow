using System.CommandLine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ScoreBurrow.Data;
using ScoreBurrow.DataImport.Models;
using ScoreBurrow.DataImport.Services;

namespace ScoreBurrow.DataImport;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Import historical game data from CSV into ScoreBurrow database");

        var csvOption = new Option<string>(
            name: "--csv",
            description: "Path to CSV file containing historical game data")
        {
            IsRequired = true
        };

        var leagueIdOption = new Option<Guid?>(
            name: "--league-id",
            description: "GUID of existing league to import into (mutually exclusive with --league-name)");

        var leagueNameOption = new Option<string?>(
            name: "--league-name",
            description: "Name for new league to create (mutually exclusive with --league-id)");

        var ownerIdOption = new Option<Guid?>(
            name: "--owner-id",
            description: "GUID of user who will own the new league (required with --league-name)");

        var dryRunOption = new Option<bool>(
            name: "--dry-run",
            description: "Preview import without making database changes",
            getDefaultValue: () => false);

        rootCommand.AddOption(csvOption);
        rootCommand.AddOption(leagueIdOption);
        rootCommand.AddOption(leagueNameOption);
        rootCommand.AddOption(ownerIdOption);
        rootCommand.AddOption(dryRunOption);

        rootCommand.SetHandler(async (string csvPath, Guid? leagueId, string? leagueName, Guid? ownerId, bool dryRun) =>
        {
            try
            {
                // Validate inputs
                if (!leagueId.HasValue && string.IsNullOrWhiteSpace(leagueName))
                {
                    Console.Error.WriteLine("Error: Either --league-id or --league-name must be provided");
                    Environment.Exit(1);
                }

                if (leagueId.HasValue && !string.IsNullOrWhiteSpace(leagueName))
                {
                    Console.Error.WriteLine("Error: Cannot specify both --league-id and --league-name");
                    Environment.Exit(1);
                }

                if (!string.IsNullOrWhiteSpace(leagueName) && !ownerId.HasValue)
                {
                    Console.Error.WriteLine("Error: --owner-id is required when creating a new league with --league-name");
                    Environment.Exit(1);
                }

                if (!File.Exists(csvPath))
                {
                    Console.Error.WriteLine($"Error: CSV file not found: {csvPath}");
                    Environment.Exit(1);
                }

                // Load configuration
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false)
                    .AddJsonFile("appsettings.Development.json", optional: true)
                    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                    .AddEnvironmentVariables()
                    .Build();

                var connectionString = configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    Console.Error.WriteLine("Error: Connection string 'DefaultConnection' not found in appsettings.json");
                    Environment.Exit(1);
                }

                // Create DbContext
                var optionsBuilder = new DbContextOptionsBuilder<ScoreBurrowDbContext>();
                optionsBuilder.UseSqlServer(connectionString);
                
                using var dbContext = new ScoreBurrowDbContext(optionsBuilder.Options, "System");

                // Create import options
                var options = new ImportOptions
                {
                    CsvPath = csvPath,
                    LeagueId = leagueId,
                    LeagueName = leagueName,
                    OwnerId = ownerId,
                    DryRun = dryRun
                };

                // Run import
                var importer = new GameImporter(dbContext);
                var result = await importer.ImportAsync(options);

                Console.WriteLine("\n=== Import Complete ===");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"\nError: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.Error.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                Environment.Exit(1);
            }
        }, csvOption, leagueIdOption, leagueNameOption, ownerIdOption, dryRunOption);

        return await rootCommand.InvokeAsync(args);
    }
}
