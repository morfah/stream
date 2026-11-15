namespace MorfStudios.StreamSite;

/// <summary>
/// What the build couldn't do by itself and I might want to fix in the data files.
/// Printed at the end, and shown on the run's page in GitHub Actions.
/// </summary>
public sealed class Report
{
    private readonly List<(string Title, IReadOnlyList<string> Lines)> _sections = [];

    public void Add(string title, IEnumerable<string> lines)
    {
        var list = lines.ToList();
        if (list.Count > 0)
            _sections.Add((title, list));
    }

    public void Print()
    {
        foreach (var (title, lines) in _sections)
        {
            Console.WriteLine();
            Console.WriteLine(title);
            foreach (var line in lines)
                Console.WriteLine("  " + line);
        }
    }

    /// <summary>Adds the report to the run's summary page when running in GitHub Actions.</summary>
    public void WriteGitHubSummary()
    {
        var path = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
        if (string.IsNullOrEmpty(path))
            return;

        using var summary = File.AppendText(path);
        summary.WriteLine("## Site build");
        foreach (var (title, lines) in _sections)
        {
            summary.WriteLine();
            summary.WriteLine($"### {title}");
            foreach (var line in lines)
                summary.WriteLine("- " + line);
        }
    }
}
