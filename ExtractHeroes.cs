using System;
using System.IO;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
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
        Console.WriteLine("Extraction complete.");
    }
}
