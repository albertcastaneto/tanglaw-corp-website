using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

namespace TanglawSite.Composing;

/// <summary>
/// 2026 features that need new document types or per-page values:
///   - Insights section (list page + articles), with templates and sample articles
///   - "Sector" field on projects (used by the project filter), pre-filled for the sample projects
/// Idempotent: only creates what is missing, so it is safe on every start and on fresh installs.
/// </summary>
public class SiteFeaturesHandler : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private static readonly Guid AdminKey = Constants.Security.SuperUserKey;

    private readonly IRuntimeState _runtimeState;
    private readonly IContentTypeService _contentTypeService;
    private readonly IDataTypeService _dataTypeService;
    private readonly ITemplateService _templateService;
    private readonly IContentService _contentService;
    private readonly IShortStringHelper _shortStringHelper;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<SiteFeaturesHandler> _logger;

    public SiteFeaturesHandler(
        IRuntimeState runtimeState,
        IContentTypeService contentTypeService,
        IDataTypeService dataTypeService,
        ITemplateService templateService,
        IContentService contentService,
        IShortStringHelper shortStringHelper,
        IWebHostEnvironment env,
        ILogger<SiteFeaturesHandler> logger)
    {
        _runtimeState = runtimeState;
        _contentTypeService = contentTypeService;
        _dataTypeService = dataTypeService;
        _templateService = templateService;
        _contentService = contentService;
        _shortStringHelper = shortStringHelper;
        _env = env;
        _logger = logger;
    }

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        if (_runtimeState.Level != RuntimeLevel.Run || _contentTypeService.Get("home") is null)
        {
            return;
        }

        try
        {
            await EnsureInsightsAsync();
            await EnsureProjectSectorsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TanglawSite: adding 2026 features failed.");
        }
    }

    // ------------------------------------------------------------------
    // Insights
    // ------------------------------------------------------------------

    private async Task EnsureInsightsAsync()
    {
        // Each step checks its own state, so an interrupted run completes on the next start.
        await EnsureTemplateAsync("insightsPage", "Insights Page");
        await EnsureTemplateAsync("insightItem", "Insight Item");
        _logger.LogInformation("TanglawSite: insights step 1/4 templates ok.");

        if (_contentTypeService.Get("insightsPage") is null || _contentTypeService.Get("insightItem") is null)
        {
            await CreateInsightTypesAsync();
        }
        _logger.LogInformation("TanglawSite: insights step 2/4 document types ok.");

        await EnsureHomeAllowsInsightsAsync();
        _logger.LogInformation("TanglawSite: insights step 3/4 home settings ok.");

        var homeNode = _contentService.GetRootContent().FirstOrDefault(c => c.ContentType.Alias == "home");
        var listType = _contentTypeService.Get("insightsPage");
        if (homeNode is not null && listType is not null && !_contentService.GetPagedOfType(listType.Id, 0, 1, out _, null).Any())
        {
            CreateSampleInsights();
            _logger.LogInformation("TanglawSite: added the Insights section.");
        }
        _logger.LogInformation("TanglawSite: insights step 4/4 content ok.");
    }

    private async Task CreateInsightTypesAsync()
    {
        var textstring = await DataTypeAsync(Constants.DataTypes.Guids.TextstringGuid);
        var textarea = await DataTypeAsync(Constants.DataTypes.Guids.TextareaGuid);
        var richText = await DataTypeAsync(Constants.DataTypes.Guids.RichtextEditorGuid);
        var image = await DataTypeAsync(Constants.DataTypes.Guids.MediaPicker3SingleImageGuid);
        var date = await DataTypeAsync(Constants.DataTypes.Guids.DatePickerGuid);

        var item = _contentTypeService.Get("insightItem") ?? await CreateDocTypeAsync("insightItem", "Insight", "icon-newspaper", "insightItem",
            ("summary", "Summary", textarea, "Content", null),
            ("bodyText", "Body Text", richText, "Content", null),
            ("articleDate", "Article Date", date, "Article Details", "Shown on the article and used for sorting (newest first)."),
            ("category", "Category", textstring, "Article Details", "e.g. News, Insight, Case Study"),
            ("author", "Author", textstring, "Article Details", null),
            ("pageImage", "Banner / Main Image", image, "Images", "Optional. If empty, a temporary placeholder photo is shown."),
            ("metaDescription", "Meta Description (SEO)", textarea, "SEO", null));

        if (_contentTypeService.Get("insightsPage") is not null)
        {
            return;
        }

        var list = await CreateDocTypeAsync("insightsPage", "Insights Page", "icon-folder", "insightsPage",
            ("summary", "Summary", textarea, "Content", null),
            ("pageImage", "Banner / Main Image", image, "Images", "Optional. If empty, a temporary placeholder photo is shown."),
            ("ctaTitle", "Call to Action Title", textstring, "Call to Action", "Leave empty to hide the section."),
            ("ctaText", "Call to Action Text", textarea, "Call to Action", null),
            ("metaDescription", "Meta Description (SEO)", textarea, "SEO", null));
        list.AllowedContentTypes = new[] { new ContentTypeSort(item.Key, 0, item.Alias) };
        await UpdateAsync(list);
    }

    private async Task EnsureHomeAllowsInsightsAsync()
    {
        var list = _contentTypeService.Get("insightsPage");
        if (list is null)
        {
            return;
        }

        var textstring = await DataTypeAsync(Constants.DataTypes.Guids.TextstringGuid);

        // Allow "Insights" under Home
        var home = _contentTypeService.Get("home")!;
        var allowed = (home.AllowedContentTypes ?? Enumerable.Empty<ContentTypeSort>()).ToList();
        if (allowed.All(a => a.Alias != list.Alias))
        {
            allowed.Add(new ContentTypeSort(list.Key, allowed.Count, list.Alias));
            home.AllowedContentTypes = allowed;
            await UpdateAsync(home);
        }

        // Home: heading for the "Latest insights" section
        if (!home.PropertyTypeExists("insightsHeading"))
        {
            home.AddPropertyType(new PropertyType(_shortStringHelper, textstring, "insightsHeading") { Name = "Insights Section Heading" }, "sectionheadings", "Section Headings");
            await UpdateAsync(home);
        }
    }

    private void CreateSampleInsights()
    {
        var homeNode = _contentService.GetRootContent().FirstOrDefault(c => c.ContentType.Alias == "home");
        if (homeNode is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(homeNode.GetValue<string>("insightsHeading")))
        {
            homeNode.SetValue("insightsHeading", "Latest insights");
            SaveAndPublish(homeNode);
        }

        var insights = _contentService.Create("Insights", homeNode.Id, "insightsPage");
        insights.SetValue("summary", "News, project updates and practical thinking on delivering infrastructure well.");
        insights.SetValue("ctaTitle", "Want to discuss a challenge on your project?");
        insights.SetValue("ctaText", "Our team is happy to share what has worked for us on similar programs.");
        insights.SetValue("metaDescription", "Insights and news from Tanglaw Infrastructure Partners.");
        SaveAndPublish(insights);

        var articles = new[]
        {
            ("Five lessons from delivering a hospital wing during live operations", "Case Study", new DateTime(2026, 10, 22),
             "Phasing, infection control and constant communication kept a 200-bed expansion on track without disrupting patient care.",
             "<p>Expanding a hospital while it stays fully operational leaves no margin for surprises. On the Regional Hospital Expansion we learned five lessons we now apply to every live-site project.</p>" +
             "<h2>1. Phase around the users, not the contractor</h2><p>We built the schedule from the hospital's clinical calendar first, then fitted construction around it.</p>" +
             "<h2>2. Make infection control a daily KPI</h2><p>Negative-pressure barriers and dust monitoring were inspected and reported every morning.</p>" +
             "<h2>3. Over-communicate</h2><p>A weekly one-page update for staff and patients removed most complaints before they started.</p>" +
             "<h2>4. Pre-commission early</h2><p>Testing building systems section by section meant handover was a formality.</p>" +
             "<h2>5. Keep one accountable lead</h2><p>A single project manager owned every decision, so nothing fell between contractors.</p>"),
            ("Why integrated project management lowers total project cost", "Insight", new DateTime(2026, 9, 30),
             "Fewer handoffs, earlier risk visibility and one source of truth: how a single accountable team protects public budgets.",
             "<p>Most cost overruns on public infrastructure start long before construction. They begin with gaps between feasibility, design and procurement.</p>" +
             "<h2>One team, one set of numbers</h2><p>When the same team carries a project from business case to handover, assumptions don't get lost between stages.</p>" +
             "<h2>Risk you can see early</h2><p>An integrated risk register makes it possible to act on issues months before they reach site.</p>" +
             "<ul><li>Fewer change orders</li><li>Faster approvals</li><li>Clearer accountability</li></ul>"),
            ("Tanglaw completes Coastal Road Widening ahead of schedule", "News", new DateTime(2026, 8, 18),
             "The 14 km coastal highway upgrade in Batangas, including drainage and slope protection, opened to traffic three weeks early.",
             "<p>The Coastal Road Widening project in Batangas has been formally handed over to the Provincial Government, three weeks ahead of the contract schedule.</p>" +
             "<p>The project widened 14 km of coastal highway and added drainage and slope protection designed for stronger typhoon seasons.</p>" +
             "<blockquote>Weekly reporting and early contractor coordination made the difference. (sample quote)</blockquote>"),
        };

        foreach (var (name, category, date, summary, body) in articles)
        {
            var a = _contentService.Create(name, insights.Id, "insightItem");
            a.SetValue("summary", summary);
            a.SetValue("bodyText", JsonSerializer.Serialize(new { markup = body, blocks = (object?)null }));
            a.SetValue("articleDate", date);
            a.SetValue("category", category);
            a.SetValue("author", "Tanglaw Editorial Team");
            a.SetValue("metaDescription", summary);
            SaveAndPublish(a);
        }

        // Keep "Contact" as the last top-level page so the menu reads naturally
        var children = _contentService.GetPagedChildren(homeNode.Id, 0, 100, out _).ToList();
        var ordered = children.Where(c => c.ContentType.Alias != "contactPage")
            .Concat(children.Where(c => c.ContentType.Alias == "contactPage"))
            .ToList();
        _contentService.Sort(ordered);
    }

    // ------------------------------------------------------------------
    // Project sectors (filter)
    // ------------------------------------------------------------------

    private static readonly Dictionary<string, string> SampleSectors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Coastal Road Widening"] = "Roads & Bridges",
        ["Municipal Water Treatment Plant"] = "Water & Sanitation",
        ["Regional Hospital Expansion"] = "Healthcare",
    };

    private async Task EnsureProjectSectorsAsync()
    {
        var project = _contentTypeService.Get("projectItem");
        if (project is null || project.PropertyTypeExists("sector"))
        {
            return;
        }

        var textstring = await DataTypeAsync(Constants.DataTypes.Guids.TextstringGuid);
        project.AddPropertyType(new PropertyType(_shortStringHelper, textstring, "sector")
        {
            Name = "Sector",
            Description = "Used for the filter on the Projects page, e.g. Roads & Bridges, Water & Sanitation, Healthcare.",
        }, "projectdetails", "Project Details");
        await UpdateAsync(project);

        foreach (var content in _contentService.GetPagedOfType(project.Id, 0, 1000, out _, null))
        {
            if (content.Name is not null && SampleSectors.TryGetValue(content.Name, out var sector))
            {
                content.SetValue("sector", sector);
                SaveAndPublish(content);
            }
        }

        _logger.LogInformation("TanglawSite: added project sectors.");
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private async Task EnsureTemplateAsync(string alias, string name)
    {
        if (await _templateService.GetAsync(alias) is not null)
        {
            return;
        }

        var path = Path.Combine(_env.ContentRootPath, "Views", alias + ".cshtml");
        var content = System.IO.File.Exists(path) ? await System.IO.File.ReadAllTextAsync(path) : null;
        var result = await _templateService.CreateAsync(name, alias, content, AdminKey);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Could not create template '{alias}': {result.Status}");
        }
    }

    private async Task<IContentType> CreateDocTypeAsync(string alias, string name, string icon, string templateAlias,
        params (string Alias, string Name, IDataType DataType, string Group, string? Description)[] properties)
    {
        var contentType = new ContentType(_shortStringHelper, -1) { Alias = alias, Name = name, Icon = icon };
        foreach (var p in properties)
        {
            var propertyType = new PropertyType(_shortStringHelper, p.DataType, p.Alias) { Name = p.Name, Description = p.Description };
            contentType.AddPropertyType(propertyType, p.Group.Replace(" ", string.Empty).ToLowerInvariant(), p.Group);
        }

        var template = await _templateService.GetAsync(templateAlias);
        if (template is not null)
        {
            contentType.AllowedTemplates = new[] { template };
            contentType.SetDefaultTemplate(template);
        }

        var result = await _contentTypeService.CreateAsync(contentType, AdminKey);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Could not create document type '{alias}': {result.Result}");
        }

        return contentType;
    }

    private async Task UpdateAsync(IContentType contentType)
    {
        var result = await _contentTypeService.UpdateAsync(contentType, AdminKey);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Could not update document type '{contentType.Alias}': {result.Result}");
        }
    }

    private void SaveAndPublish(IContent content)
    {
        _contentService.Save(content);
        var publish = _contentService.Publish(content, new[] { "*" });
        if (!publish.Success)
        {
            _logger.LogWarning("TanglawSite: could not publish '{Name}': {Result}", content.Name, publish.Result);
        }
    }

    private async Task<IDataType> DataTypeAsync(Guid key)
        => await _dataTypeService.GetAsync(key) ?? throw new InvalidOperationException($"Built-in data type {key} not found.");
}
