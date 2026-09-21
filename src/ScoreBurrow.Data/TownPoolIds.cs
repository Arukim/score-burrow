namespace ScoreBurrow.Data;

public static class TownPoolIds
{
    public static string? Format(IEnumerable<int>? townIds)
    {
        if (townIds == null)
        {
            return null;
        }

        var seen = new HashSet<int>();
        var ids = new List<int>();
        foreach (var id in townIds)
        {
            if (id > 0 && seen.Add(id))
            {
                ids.Add(id);
            }
        }

        return ids.Count == 0 ? null : string.Join(',', ids);
    }

    public static IReadOnlyList<int> Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<int>();
        }

        var seen = new HashSet<int>();
        var ids = new List<int>();
        foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(part, out var id) && id > 0 && seen.Add(id))
            {
                ids.Add(id);
            }
        }

        return ids;
    }
}
