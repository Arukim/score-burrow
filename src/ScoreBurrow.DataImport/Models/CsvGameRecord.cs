using CsvHelper.Configuration.Attributes;

namespace ScoreBurrow.DataImport.Models;

public class CsvGameRecord
{
    [Name("Player")]
    public string Player { get; set; } = string.Empty;

    [Name("City")]
    public string City { get; set; } = string.Empty;

    [Name("Start money")]
    public int StartMoney { get; set; }

    [Name("Color")]
    public string Color { get; set; } = string.Empty;

    [Name("Result")]
    public decimal Result { get; set; }

    [Name("Is Technical Loss")]
    public int IsTechnicalLoss { get; set; }

    [Name("Map name")]
    public string MapName { get; set; } = string.Empty;

    // Not from CSV - assigned during parsing
    [Ignore]
    public int RowNumber { get; set; }
}
