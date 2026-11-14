using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace TanglawSite.Models;

/// <summary>
/// TEMPORARY placeholder photos (Unsplash) until real project photos are added.
/// Images are picked by page URL segment (e.g. "coastal-road-widening"); unknown pages
/// get a stable image from the fallback pool. To use real photos later, add a Media Picker
/// property to the document types and replace the SiteImages calls in the views.
/// </summary>
public static class SiteImages
{
    private const string Base = "https://images.unsplash.com/photo-";

    private const string Workers = "1504307651254-35680f356dfd";
    private const string SiteTeam = "1541888946425-d81bb19240f5";
    private const string Plans = "1503387762-592deb58ef4e";
    private const string Towers = "1486406146926-c627a92ad1ab";
    private const string Earthworks = "1517089596392-fb9a9033e05b";
    private const string Framing = "1508450859948-4e04fabaa4ea";
    private const string Cranes = "1429497419816-9ca5cfb4571a";
    private const string Pipes = "1513828583688-c52646db42da";
    private const string Scaffold = "1531834685032-c34bf0d84c77";
    private const string WhiteBuilding = "1523217582562-09d0def993a6";
    private const string Team = "1600880292203-757bb62b4baf";
    private const string Meeting = "1521737604893-d14cc237f11d";
    private const string Interior = "1497366216548-37526070297c";
    private const string SiteCranes = "1565008447742-97f6f38c985c";
    private const string LookingUp = "1449157291145-7efd050a4d0e";
    private const string CityAerial = "1477959858617-67f85cf4f1df";
    private const string CraneLow = "1535732759880-bbd5c7265e3f";
    private const string Welding = "1504917595217-d4dc5ebe6122";
    private const string Engineer = "1621905252507-b35492cc74b4";

    private static readonly Dictionary<string, string> ByKey = new(StringComparer.OrdinalIgnoreCase)
    {
        // home page slots
        ["hero"] = SiteCranes,
        ["intro-main"] = Engineer,
        ["intro-small"] = Plans,
        ["testimonial"] = SiteTeam,
        ["cta"] = CityAerial,
        ["about-side"] = Team,

        // pages (by URL segment)
        ["about-us"] = LookingUp,
        ["services"] = Scaffold,
        ["projects"] = CraneLow,
        ["contact"] = Towers,

        // services
        ["integrated-project-management"] = Meeting,
        ["feasibility-planning"] = Plans,
        ["design-management"] = Framing,
        ["procurement-contract-management"] = Team,
        ["construction-supervision"] = Workers,
        ["commissioning-handover"] = Interior,

        // projects
        ["coastal-road-widening"] = Earthworks,
        ["municipal-water-treatment-plant"] = Pipes,
        ["regional-hospital-expansion"] = WhiteBuilding,
    };

    private static readonly string[] Pool = { SiteCranes, Cranes, SiteTeam, Scaffold, Workers, Framing, CraneLow, Welding };

    public static string Url(string photoId, int w, int h) => $"{Base}{photoId}?auto=format&fit=crop&w={w}&h={h}&q=75";

    public static string ForKey(string key, int w, int h)
        => Url(ByKey.TryGetValue(key, out var id) ? id : Pool[StableHash(key) % Pool.Length], w, h);

    /// <summary>The page's "pageImage" (Banner / Main Image) if an editor picked one, otherwise a placeholder.</summary>
    public static string For(IPublishedContent content, int w, int h)
        => Media(content, "pageImage", w, h) ?? ForKey(content.UrlSegment ?? content.Name ?? "page", w, h);

    /// <summary>An image picked in the given property, otherwise the placeholder for <paramref name="fallbackKey"/>.</summary>
    public static string Pick(IPublishedContent content, string alias, string fallbackKey, int w, int h)
        => Media(content, alias, w, h) ?? ForKey(fallbackKey, w, h);

    /// <summary>Cropped URL of a single image picked in a media picker property, or null.</summary>
    public static string? Media(IPublishedContent content, string alias, int w, int h)
    {
        if (!content.HasProperty(alias) || content.Value<MediaWithCrops>(alias) is not { } media)
        {
            return null;
        }

        var url = media.GetCropUrl(width: w, height: h, imageCropMode: ImageCropMode.Crop);
        return string.IsNullOrEmpty(url) ? null : url;
    }

    /// <summary>Gallery photos: the editor's "gallery" images if any, otherwise placeholders.</summary>
    public static IList<string> GalleryOrPlaceholders(IPublishedContent content, int count, int w, int h)
    {
        var picked = content.HasProperty("gallery")
            ? content.Value<IEnumerable<MediaWithCrops>>("gallery")?
                .Select(m => m.GetCropUrl(width: w, height: h, imageCropMode: ImageCropMode.Crop))
                .Where(u => !string.IsNullOrEmpty(u))
                .Select(u => u!)
                .Take(count)
                .ToList()
            : null;
        return picked is { Count: > 0 } ? picked : Gallery(content, count, w, h).ToList();
    }

    /// <summary>Extra images for a project gallery (different from the main image).</summary>
    public static IEnumerable<string> Gallery(IPublishedContent content, int count, int w, int h)
    {
        var key = content.UrlSegment ?? content.Name ?? "page";
        ByKey.TryGetValue(key, out var main);
        var start = StableHash(key);
        return Enumerable.Range(0, Pool.Length)
            .Select(i => Pool[(start + i) % Pool.Length])
            .Where(id => id != main)
            .Take(count)
            .Select(id => Url(id, w, h));
    }

    private static int StableHash(string s)
    {
        unchecked
        {
            var h = 17;
            foreach (var c in s) h = h * 31 + c;
            return Math.Abs(h % 100000);
        }
    }
}
