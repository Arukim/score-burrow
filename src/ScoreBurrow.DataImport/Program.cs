using System.CommandLine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ScoreBurrow.Data;
using ScoreBurrow.DataImport.Models;
using ScoreBurrow.DataImport.Services;
using System.Text.RegularExpressions;

namespace ScoreBurrow.DataImport;

class Program
{
    static void ExtractHeroes()
    {
        string content = File.ReadAllText("heroes.txt");
        var trPattern = new Regex(@"<tr[^>]*>(.*?)</tr>", RegexOptions.Singleline);
        var trs = trPattern.Matches(content);

        using (var writer = new StreamWriter("heroes.csv"))
        {
            writer.WriteLine("Hero Name,Hero Class");
            foreach (Match tr in trs)
            {
                string row = tr.Groups[1].Value;
                // Extract name
                var nameMatch = Regex.Match(row, @"<a href=""/wiki/[^""]*"" title=""[^""]*"">\s*([^<]*)\s*</a>");
                string name = nameMatch.Success ? nameMatch.Groups[1].Value.Trim() : "";
                // Extract class
                var classMatch = Regex.Match(row, @"<td style=""padding-right:5px"">&nbsp;.*?<a href=""/wiki/[^""]*"" title=""[^""]*"">\s*([^<]*)\s*</a>", RegexOptions.Singleline);
                string className = classMatch.Success ? classMatch.Groups[1].Value.Trim() : "";

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(className))
                {
                    writer.WriteLine($"{name},{className}");
                }
            }
        }
        Console.WriteLine("Hero extraction complete. Output saved to heroes.csv");
    }
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Import historical game data from CSV into ScoreBurrow database");

        var csvOption = new Option<string>(
            name: "--csv",
            description: "Path to CSV file containing historical game data");

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

        var extractHeroesOption = new Option<bool>(
            name: "--extract-heroes",
            description: "Extract hero data from heroes.txt to heroes.csv",
            getDefaultValue: () => false);

        rootCommand.AddOption(csvOption);
        rootCommand.AddOption(leagueIdOption);
        rootCommand.AddOption(leagueNameOption);
        rootCommand.AddOption(ownerIdOption);
        rootCommand.AddOption(dryRunOption);
        rootCommand.AddOption(extractHeroesOption);

        rootCommand.SetHandler(async (string csvPath, Guid? leagueId, string? leagueName, Guid? ownerId, bool dryRun, bool extractHeroes) =>
        {
            try
            {
                if (extractHeroes)
                {
                    // Extract heroes logic
                    ExtractHeroes();
                    return;
                }

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
        }, csvOption, leagueIdOption, leagueNameOption, ownerIdOption, dryRunOption, extractHeroesOption);

        return await rootCommand.InvokeAsync(args);
    }
}
