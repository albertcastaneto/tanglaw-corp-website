namespace TanglawSite.Models;

/// <summary>Helpers for the simple "one item per line" text fields editors fill in the backoffice.</summary>
public static class TextLines
{
    /// <summary>Non-empty, trimmed lines.</summary>
    public static List<string> Split(string? value)
        => (value ?? string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(l => l.Length > 0)
            .ToList();

    /// <summary>Lines written as "Title|Description". A line without "|" becomes a title with no description.</summary>
    public static List<(string Title, string Text)> Pairs(string? value)
        => Split(value)
            .Select(l => l.Split('|', 2, StringSplitOptions.TrimEntries))
            .Select(p => (p[0], p.Length > 1 ? p[1] : string.Empty))
            .ToList();

    /// <summary>Icon to use for the n-th item, cycling through a list.</summary>
    public static string IconAt(int index, params string[] icons) => icons[index % icons.Length];
}
