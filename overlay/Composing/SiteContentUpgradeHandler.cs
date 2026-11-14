using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

namespace TanglawSite.Composing;

/// <summary>
/// Adds the "design" fields (images, home page sections, values, CTAs...) to the document types
/// so editors can change them in the backoffice. Runs on every start but only adds fields that
/// are missing, and only pre-fills a field with sample text the first time it is added.
/// Clearing a field in the backoffice therefore sticks (the section is hidden).
/// </summary>
public class SiteContentUpgradeHandler : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private enum Kind { Textstring, Textarea, Image, Images }

    private sealed record Field(string DocType, string Alias, string Name, Kind Kind, string Group, string? Description = null, string? Default = null, string? OnlyForPage = null);

    private const string Lines = "One item per line.";
    private const string Pairs = "One item per line, written as: Title|Description";
    private const string ImageHint = "Optional. If empty, a temporary placeholder photo is shown.";

    private static readonly string[] PageTypes = { "contentPage", "servicesPage", "serviceItem", "projectsPage", "projectItem", "contactPage" };

    private static IEnumerable<Field> Fields()
    {
        // ---------- Home ----------
        yield return new("home", "heroImage", "Hero Background Image", Kind.Image, "Hero", ImageHint);
        yield return new("home", "heroEyebrow", "Hero Label (small text above the title)", Kind.Textstring, "Hero", null, "Integrated Project Management");
        yield return new("home", "heroPoints", "Hero Key Points", Kind.Textarea, "Hero", Lines, "On time, on budget\nSafety first\nTransparent reporting");

        yield return new("home", "introImage", "Intro Main Image", Kind.Image, "Intro", ImageHint);
        yield return new("home", "introImageSmall", "Intro Small Image", Kind.Image, "Intro", ImageHint);
        yield return new("home", "introBadge", "Intro Badge (e.g. 15+|Years of delivery)", Kind.Textstring, "Intro", "Written as: Number|Label. Leave empty to hide.", "15+|Years of delivery");
        yield return new("home", "introChecklist", "Intro Checklist", Kind.Textarea, "Intro", Lines,
            "Single point of accountability\nLicensed engineers & PMPs\nPublic & private sector clients\nWeekly cost & progress reports");

        yield return new("home", "sectorsTitle", "Sectors Title", Kind.Textstring, "Sectors", null, "Sectors we serve");
        yield return new("home", "sectors", "Sectors (scrolling ticker)", Kind.Textarea, "Sectors", Lines + " Leave empty to hide the ticker.",
            "Roads & Bridges\nWater & Sanitation\nHealthcare Facilities\nEnergy & Utilities\nSchools & Public Buildings\nPorts & Transport\nFlood Control\nUrban Development");

        yield return new("home", "servicesHeading", "Services Section Heading", Kind.Textstring, "Section Headings", null, "Services across the full project life cycle");
        yield return new("home", "projectsHeading", "Projects Section Heading", Kind.Textstring, "Section Headings", null, "Featured projects");

        yield return new("home", "processTitle", "Process Title", Kind.Textstring, "Delivery Process", null, "A proven four-stage delivery approach");
        yield return new("home", "processText", "Process Intro Text", Kind.Textarea, "Delivery Process", null,
            "Every project follows the same disciplined framework, so clients always know where things stand.");
        yield return new("home", "processSteps", "Process Steps", Kind.Textarea, "Delivery Process", Pairs + " Leave empty to hide the section.",
            "Initiate|Define needs, test feasibility and build a bankable business case.\n" +
            "Plan|Integrated schedule, budget, risk register and procurement strategy.\n" +
            "Deliver|Supervise construction with weekly reporting on cost, time, quality and safety.\n" +
            "Handover|Testing, commissioning, documentation and training for day-one operations.");

        yield return new("home", "testimonialQuote", "Testimonial Quote", Kind.Textarea, "Testimonial", "Leave empty to hide the section.",
            "Tanglaw brought order to a complex, multi-contractor program. Their weekly reports gave us full visibility, and the project was handed over ahead of schedule.");
        yield return new("home", "testimonialName", "Testimonial Name / Title", Kind.Textstring, "Testimonial", null, "Provincial Engineer");
        yield return new("home", "testimonialRole", "Testimonial Organization", Kind.Textstring, "Testimonial", null, "Provincial Government (sample testimonial)");
        yield return new("home", "testimonialImage", "Testimonial Image", Kind.Image, "Testimonial", ImageHint);

        yield return new("home", "ctaTitle", "Call to Action Title", Kind.Textstring, "Call to Action", null, "Have an infrastructure project in mind?");
        yield return new("home", "ctaText", "Call to Action Text", Kind.Textarea, "Call to Action", null,
            "Tell us about your goals. We'll help you plan, procure and deliver it with confidence.");
        yield return new("home", "ctaImage", "Call to Action Background (used on all pages)", Kind.Image, "Call to Action", ImageHint);

        yield return new("home", "linkedinUrl", "LinkedIn URL", Kind.Textstring, "Site Settings", "Leave empty to hide the icon.");
        yield return new("home", "facebookUrl", "Facebook URL", Kind.Textstring, "Site Settings", "Leave empty to hide the icon.");

        // ---------- Banner image on every inner page ----------
        foreach (var t in PageTypes)
        {
            yield return new(t, "pageImage", "Banner / Main Image", Kind.Image, "Images", ImageHint);
        }

        // ---------- Content page (About Us) ----------
        yield return new("contentPage", "sideImage", "Side Image", Kind.Image, "Images", ImageHint);
        yield return new("contentPage", "valuesTitle", "Values Title", Kind.Textstring, "Values", null, "Principles that guide every project", "About Us");
        yield return new("contentPage", "values", "Values", Kind.Textarea, "Values", Pairs + " Leave empty to hide the section.",
            "Outcome-focused|We measure success by assets delivered and value created, not by reports filed.\n" +
            "Safety & integrity|Zero-harm sites and honest reporting, even when the news is difficult.\n" +
            "Cost discipline|Tight control of budgets, variations and claims across every package.\n" +
            "One integrated team|Engineers, planners and contract specialists working as a single unit.", "About Us");

        yield return new("contentPage", "teamTitle", "Team Title", Kind.Textstring, "Team", null, "Leadership team", "About Us");
        yield return new("contentPage", "team", "Team Members", Kind.Textarea, "Team", "One person per line, written as: Name|Role. Leave empty to hide the section.",
            "Maria Santos|Managing Director\nJose Ramirez|Director, Project Delivery\nAna Villanueva|Head of Commercial & Procurement\nCarlo Mendoza|Principal Engineer, Construction Supervision", "About Us");

        // ---------- Call to action on listing / content pages ----------
        foreach (var (t, title, text) in new[]
        {
            ("contentPage", "Let's talk about your next project", "From early feasibility to final handover, we're ready to help."),
            ("servicesPage", "Not sure which service you need?", "Tell us where your project is today and we'll recommend the right support."),
            ("projectsPage", "Your project could be next", "Let's discuss how we can help deliver it safely, on time and on budget."),
        })
        {
            yield return new(t, "ctaTitle", "Call to Action Title", Kind.Textstring, "Call to Action", "Leave empty to hide the section.", title);
            yield return new(t, "ctaText", "Call to Action Text", Kind.Textarea, "Call to Action", null, text);
        }

        // ---------- Service / project details ----------
        yield return new("serviceItem", "whatYouGet", "What You Get", Kind.Textarea, "Content", Lines + " Leave empty to hide.",
            "A dedicated, accountable lead\nClear scope, schedule & budget\nRisk register & mitigation plan\nWeekly progress dashboards");

        yield return new("projectItem", "gallery", "Photo Gallery", Kind.Images, "Images", "Optional. Two or more photos. If empty, placeholder photos are shown.");
        yield return new("projectItem", "ourRole", "Our Role", Kind.Textarea, "Project Details", Lines + " Leave empty to hide.",
            "Project management\nContract administration\nConstruction supervision\nCost & schedule control");

        // ---------- Contact ----------
        yield return new("contactPage", "mapEmbedUrl", "Map Embed URL", Kind.Textstring, "Contact Details",
            "Paste the 'src' URL from Google Maps or OpenStreetMap 'Share > Embed'. Leave empty to hide the map.",
            "https://www.openstreetmap.org/export/embed.html?bbox=121.0150%2C14.5480%2C121.0350%2C14.5620&layer=mapnik&marker=14.5547%2C121.0244");
    }

    private static readonly Guid AdminKey = Constants.Security.SuperUserKey;

    private readonly IRuntimeState _runtimeState;
    private readonly IContentTypeService _contentTypeService;
    private readonly IDataTypeService _dataTypeService;
    private readonly IContentService _contentService;
    private readonly IShortStringHelper _shortStringHelper;
    private readonly ILogger<SiteContentUpgradeHandler> _logger;

    public SiteContentUpgradeHandler(
        IRuntimeState runtimeState,
        IContentTypeService contentTypeService,
        IDataTypeService dataTypeService,
        IContentService contentService,
        IShortStringHelper shortStringHelper,
        ILogger<SiteContentUpgradeHandler> logger)
    {
        _runtimeState = runtimeState;
        _contentTypeService = contentTypeService;
        _dataTypeService = dataTypeService;
        _contentService = contentService;
        _shortStringHelper = shortStringHelper;
        _logger = logger;
    }

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        if (_runtimeState.Level != RuntimeLevel.Run)
        {
            return;
        }

        try
        {
            var dataTypes = new Dictionary<Kind, IDataType>
            {
                [Kind.Textstring] = await GetDataTypeAsync(Constants.DataTypes.Guids.TextstringGuid),
                [Kind.Textarea] = await GetDataTypeAsync(Constants.DataTypes.Guids.TextareaGuid),
                [Kind.Image] = await GetDataTypeAsync(Constants.DataTypes.Guids.MediaPicker3SingleImageGuid),
                [Kind.Images] = await GetDataTypeAsync(Constants.DataTypes.Guids.MediaPicker3MultipleImagesGuid),
            };

            foreach (var group in Fields().GroupBy(f => f.DocType))
            {
                var contentType = _contentTypeService.Get(group.Key);
                if (contentType is null)
                {
                    continue;
                }

                var added = new List<Field>();
                foreach (var field in group)
                {
                    if (contentType.PropertyTypeExists(field.Alias))
                    {
                        continue;
                    }

                    var propertyType = new PropertyType(_shortStringHelper, dataTypes[field.Kind], field.Alias)
                    {
                        Name = field.Name,
                        Description = field.Description,
                    };
                    var groupAlias = field.Group.Replace(" ", string.Empty).ToLowerInvariant();
                    contentType.AddPropertyType(propertyType, groupAlias, field.Group);
                    added.Add(field);
                }

                if (added.Count == 0)
                {
                    continue;
                }

                var result = await _contentTypeService.UpdateAsync(contentType, AdminKey);
                if (!result.Success)
                {
                    _logger.LogError("TanglawSite: could not add fields to '{DocType}': {Result}", group.Key, result.Result);
                    continue;
                }

                _logger.LogInformation("TanglawSite: added {Count} editable fields to '{DocType}'.", added.Count, group.Key);
                FillDefaults(contentType.Id, added.Where(f => f.Default is not null).ToList());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TanglawSite: adding editable fields failed.");
        }
    }

    /// <summary>Pre-fill the newly added fields with the sample text so the site looks the same.</summary>
    private void FillDefaults(int contentTypeId, List<Field> fields)
    {
        if (fields.Count == 0)
        {
            return;
        }

        var items = _contentService.GetPagedOfType(contentTypeId, 0, 1000, out _, null);
        foreach (var content in items)
        {
            var changed = false;
            foreach (var f in fields)
            {
                if (f.OnlyForPage is not null && !string.Equals(content.Name, f.OnlyForPage, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(content.GetValue<string>(f.Alias)))
                {
                    content.SetValue(f.Alias, f.Default);
                    changed = true;
                }
            }

            if (!changed)
            {
                continue;
            }

            var wasPublished = content.Published;
            _contentService.Save(content);
            if (wasPublished)
            {
                var publish = _contentService.Publish(content, new[] { "*" });
                if (!publish.Success)
                {
                    _logger.LogWarning("TanglawSite: could not publish '{Name}': {Result}", content.Name, publish.Result);
                }
            }
        }
    }

    private async Task<IDataType> GetDataTypeAsync(Guid key)
        => await _dataTypeService.GetAsync(key)
           ?? throw new InvalidOperationException($"Built-in data type {key} not found.");
}
