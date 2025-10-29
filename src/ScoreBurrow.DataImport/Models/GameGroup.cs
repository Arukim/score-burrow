namespace ScoreBurrow.DataImport.Models;

public class GameGroup
{
    public string MapName { get; set; } = string.Empty;
    public DateTime GameDate { get; set; }
    public List<CsvGameRecord> Participants { get; set; } = new();
    public bool IsTechnicalLoss => Participants.Any(p => p.IsTechnicalLoss == 1);
}
