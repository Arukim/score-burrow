namespace ScoreBurrow.DataImport.Models;

public class ImportOptions
{
    public string CsvPath { get; set; } = string.Empty;
    public Guid? LeagueId { get; set; }
    public string? LeagueName { get; set; }
    public Guid? OwnerId { get; set; }
    public bool DryRun { get; set; }
}
