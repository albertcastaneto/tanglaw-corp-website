using System.Text.RegularExpressions;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

namespace TanglawSite.Models;

/// <summary>Helpers for Insights articles: date, reading time and newest-first ordering.</summary>
public static partial class InsightHelper
{
    /// <summary>The "Article Date" if set, otherwise the date the page was created.</summary>
    public static DateTime DateOf(IPublishedContent item)
    {
        var d = item.HasProperty("articleDate") ? item.Value<DateTime>("articleDate") : default;
        return d == default ? item.CreateDate : d;
    }

    public static string DisplayDate(IPublishedContent item) => DateOf(item).ToString("MMMM d, yyyy");

    /// <summary>Estimated reading time at ~220 words per minute (minimum 1).</summary>
    public static int ReadingMinutes(IPublishedContent item)
    {
        var html = item.Value<IHtmlEncodedString>("bodyText")?.ToHtmlString() ?? string.Empty;
        var words = TagRegex().Replace(html, " ").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        return Math.Max(1, (int)Math.Round(words / 220.0));
    }

    /// <summary>Visible articles under an Insights page, newest first.</summary>
    public static IEnumerable<IPublishedContent> Sorted(IPublishedContent insightsPage)
        => insightsPage.Children().Where(x => x.IsVisible()).OrderByDescending(DateOf);

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagRegex();
}
