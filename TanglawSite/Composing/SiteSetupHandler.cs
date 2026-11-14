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
/// Runs once on startup. If the "home" document type does not exist yet, it creates:
///   1. Templates (from the .cshtml files already in /Views)
///   2. Document types (page types the editor can create)
///   3. Sample content (Home, About, Services, Projects, Contact)
/// Safe to leave in: it does nothing once "home" exists.
/// </summary>
public class SiteSetupHandler : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly IRuntimeState _runtimeState;
    private readonly IContentTypeService _contentTypeService;
    private readonly IDataTypeService _dataTypeService;
    private readonly ITemplateService _templateService;
    private readonly IContentService _contentService;
    private readonly IShortStringHelper _shortStringHelper;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<SiteSetupHandler> _logger;

    private static readonly Guid AdminKey = Constants.Security.SuperUserKey;

    public SiteSetupHandler(
        IRuntimeState runtimeState,
        IContentTypeService contentTypeService,
        IDataTypeService dataTypeService,
        ITemplateService templateService,
        IContentService contentService,
        IShortStringHelper shortStringHelper,
        IWebHostEnvironment env,
        ILogger<SiteSetupHandler> logger)
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
        if (_runtimeState.Level != RuntimeLevel.Run)
        {
            return;
        }

        if (_contentTypeService.Get("home") is not null)
        {
            return; // already set up
        }

        try
        {
            _logger.LogInformation("TanglawSite: running first-time site setup...");
            await CreateTemplatesAsync();
            await CreateDocumentTypesAsync();
            CreateSampleContent();
            _logger.LogInformation("TanglawSite: site setup complete.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TanglawSite: site setup failed. See README 'Manual setup' section.");
        }
    }

    // ------------------------------------------------------------------
    // 1. Templates
    // ------------------------------------------------------------------

    private static readonly string[] TemplateAliases =
    {
        // master must be first: the other templates use it as their layout
        "master", "home", "contentPage", "servicesPage", "serviceItem",
        "projectsPage", "projectItem", "contactPage"
    };

    private async Task CreateTemplatesAsync()
    {
        foreach (var alias in TemplateAliases)
        {
            if (await _templateService.GetAsync(alias) is not null)
            {
                continue;
            }

            // Pass the existing file content so Umbraco does not overwrite our view.
            var path = Path.Combine(_env.ContentRootPath, "Views", alias + ".cshtml");
            var content = System.IO.File.Exists(path) ? await System.IO.File.ReadAllTextAsync(path) : null;

            var name = char.ToUpperInvariant(alias[0]) + alias[1..];
            var result = await _templateService.CreateAsync(name, alias, content, AdminKey);
            if (!result.Success)
            {
                throw new InvalidOperationException($"Could not create template '{alias}': {result.Status}");
            }
        }
    }

    // ------------------------------------------------------------------
    // 2. Document types
    // ------------------------------------------------------------------

    private async Task CreateDocumentTypesAsync()
    {
        var textstring = await GetDataTypeAsync(Constants.DataTypes.Guids.TextstringGuid);
        var textarea = await GetDataTypeAsync(Constants.DataTypes.Guids.TextareaGuid);
        var richText = await GetDataTypeAsync(Constants.DataTypes.Guids.RichtextEditorGuid);

        // Child types first, because parent types reference them as allowed children.
        var serviceItem = await CreateDocTypeAsync("serviceItem", "Service", "icon-settings", false, "serviceItem",
            ("summary", "Summary", textarea, "Content"),
            ("bodyText", "Body Text", richText, "Content"));

        var projectItem = await CreateDocTypeAsync("projectItem", "Project", "icon-company", false, "projectItem",
            ("summary", "Summary", textarea, "Content"),
            ("location", "Location", textstring, "Project Details"),
            ("projectClient", "Client", textstring, "Project Details"),
            ("projectYear", "Year Completed", textstring, "Project Details"),
            ("projectScope", "Scope / Value", textstring, "Project Details"),
            ("bodyText", "Body Text", richText, "Content"));

        var contentPage = await CreateDocTypeAsync("contentPage", "Content Page", "icon-document", false, "contentPage",
            ("summary", "Summary", textarea, "Content"),
            ("bodyText", "Body Text", richText, "Content"));

        var servicesPage = await CreateDocTypeAsync("servicesPage", "Services Page", "icon-settings-alt", false, "servicesPage",
            ("summary", "Summary", textarea, "Content"));
        servicesPage.AllowedContentTypes = new[] { new ContentTypeSort(serviceItem.Key, 0, serviceItem.Alias) };
        await UpdateAsync(servicesPage);

        var projectsPage = await CreateDocTypeAsync("projectsPage", "Projects Page", "icon-folder", false, "projectsPage",
            ("summary", "Summary", textarea, "Content"));
        projectsPage.AllowedContentTypes = new[] { new ContentTypeSort(projectItem.Key, 0, projectItem.Alias) };
        await UpdateAsync(projectsPage);

        var contactPage = await CreateDocTypeAsync("contactPage", "Contact Page", "icon-message", false, "contactPage",
            ("summary", "Summary", textarea, "Content"),
            ("address", "Office Address", textarea, "Contact Details"),
            ("phone", "Phone", textstring, "Contact Details"),
            ("contactEmail", "Email (form submissions go here)", textstring, "Contact Details"),
            ("officeHours", "Office Hours", textstring, "Contact Details"));

        var home = await CreateDocTypeAsync("home", "Home", "icon-home", true, "home",
            ("heroTitle", "Hero Title", textstring, "Hero"),
            ("heroText", "Hero Text", textarea, "Hero"),
            ("heroButtonText", "Hero Button Text", textstring, "Hero"),
            ("introTitle", "Intro Title", textstring, "Intro"),
            ("introText", "Intro Text", richText, "Intro"),
            ("statsLine1", "Stat 1 (e.g. 120+|Projects Delivered)", textstring, "Stats"),
            ("statsLine2", "Stat 2", textstring, "Stats"),
            ("statsLine3", "Stat 3", textstring, "Stats"),
            ("footerText", "Footer Text", textstring, "Site Settings"));
        home.AllowedContentTypes = new[]
        {
            new ContentTypeSort(contentPage.Key, 0, contentPage.Alias),
            new ContentTypeSort(servicesPage.Key, 1, servicesPage.Alias),
            new ContentTypeSort(projectsPage.Key, 2, projectsPage.Alias),
            new ContentTypeSort(contactPage.Key, 3, contactPage.Alias),
        };
        await UpdateAsync(home);
    }

    private async Task<IDataType> GetDataTypeAsync(Guid key)
        => await _dataTypeService.GetAsync(key)
           ?? throw new InvalidOperationException($"Built-in data type {key} not found.");

    private async Task<IContentType> CreateDocTypeAsync(
        string alias, string name, string icon, bool allowAsRoot, string templateAlias,
        params (string Alias, string Name, IDataType DataType, string Group)[] properties)
    {
        var contentType = new ContentType(_shortStringHelper, -1)
        {
            Alias = alias,
            Name = name,
            Icon = icon,
            AllowedAsRoot = allowAsRoot,
        };

        // SEO field on every page
        (string Alias, string Name, IDataType DataType, string Group) seo = ("metaDescription", "Meta Description (SEO)",
            await GetDataTypeAsync(Constants.DataTypes.Guids.TextareaGuid), "SEO");
        var all = properties.Append(seo);

        foreach (var p in all)
        {
            var propertyType = new PropertyType(_shortStringHelper, p.DataType, p.Alias) { Name = p.Name };
            var groupAlias = p.Group.Replace(" ", string.Empty).ToLowerInvariant();
            contentType.AddPropertyType(propertyType, groupAlias, p.Group);
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

    // ------------------------------------------------------------------
    // 3. Sample content
    // ------------------------------------------------------------------

    private void CreateSampleContent()
    {
        if (_contentService.GetRootContent().Any())
        {
            return;
        }

        var home = SaveAndPublish("Home", Constants.System.Root, "home", new()
        {
            ["heroTitle"] = "Delivering Infrastructure That Moves Communities Forward",
            ["heroText"] = "Tanglaw Infrastructure Partners provides integrated project management and infrastructure delivery, from feasibility to handover.",
            ["heroButtonText"] = "Explore Our Services",
            ["introTitle"] = "One partner across the whole project life cycle",
            ["introText"] = Rte("<p>We bring planning, design management, procurement, construction supervision and commissioning under one accountable team. Our clients get fewer handoffs, clearer reporting and projects that finish on time and on budget.</p>"),
            ["statsLine1"] = "120+|Projects Delivered",
            ["statsLine2"] = "₱18B|Capital Value Managed",
            ["statsLine3"] = "15|Years of Experience",
            ["footerText"] = "© Tanglaw Infrastructure Partners. Sample site for demonstration purposes.",
            ["metaDescription"] = "Integrated project management and infrastructure delivery services.",
        });

        SaveAndPublish("About Us", home.Id, "contentPage", new()
        {
            ["summary"] = "A team of engineers, planners and project managers committed to public value.",
            ["bodyText"] = Rte(
                "<h2>Our Mission</h2><p>To deliver safe, sustainable and cost-effective infrastructure through disciplined project management.</p>" +
                "<h2>Our Vision</h2><p>To be the most trusted infrastructure delivery partner in the region.</p>" +
                "<h2>Our Values</h2><ul><li>Integrity in every report and decision</li><li>Safety first, on every site</li><li>Accountability for outcomes, not just tasks</li></ul>"),
            ["metaDescription"] = "About Tanglaw Infrastructure Partners: mission, vision and values.",
        });

        var services = SaveAndPublish("Services", home.Id, "servicesPage", new()
        {
            ["summary"] = "End-to-end services that cover every stage of an infrastructure project.",
            ["metaDescription"] = "Project management, design management, construction supervision and more.",
        });

        var serviceData = new[]
        {
            ("Integrated Project Management", "Single-point management of scope, schedule, cost, risk and quality across the project life cycle."),
            ("Feasibility & Planning", "Technical, financial and environmental studies that turn ideas into bankable projects."),
            ("Design Management", "Coordination of design consultants to keep designs buildable, compliant and within budget."),
            ("Procurement & Contract Management", "Tender preparation, bid evaluation and contract administration with full transparency."),
            ("Construction Supervision", "On-site quality, safety and progress monitoring with clear weekly reporting."),
            ("Commissioning & Handover", "Testing, documentation and training so assets are ready to operate from day one."),
        };
        foreach (var (name, summary) in serviceData)
        {
            SaveAndPublish(name, services.Id, "serviceItem", new()
            {
                ["summary"] = summary,
                ["bodyText"] = Rte($"<p>{summary}</p><p>Replace this text with the full service description from the client.</p>"),
            });
        }

        var projects = SaveAndPublish("Projects", home.Id, "projectsPage", new()
        {
            ["summary"] = "A selection of projects we have managed and delivered.",
            ["metaDescription"] = "Infrastructure projects delivered by Tanglaw Infrastructure Partners.",
        });

        var projectData = new[]
        {
            ("Coastal Road Widening", "Batangas", "Provincial Government (sample)", "2024", "₱2.1B", "Widening of 14 km of coastal highway including drainage and slope protection."),
            ("Municipal Water Treatment Plant", "Pampanga", "Water District (sample)", "2023", "₱850M", "Design management and supervision of a 40 MLD water treatment facility."),
            ("Regional Hospital Expansion", "Iloilo", "Department of Health (sample)", "2022", "₱1.4B", "Full project management of a 200-bed hospital wing, delivered during live operations."),
        };
        foreach (var (name, location, client, year, scope, summary) in projectData)
        {
            SaveAndPublish(name, projects.Id, "projectItem", new()
            {
                ["summary"] = summary,
                ["location"] = location,
                ["projectClient"] = client,
                ["projectYear"] = year,
                ["projectScope"] = scope,
                ["bodyText"] = Rte($"<p>{summary}</p><p>Replace with the project story: challenge, approach and results.</p>"),
            });
        }

        SaveAndPublish("Contact", home.Id, "contactPage", new()
        {
            ["summary"] = "Tell us about your project. We usually reply within one business day.",
            ["address"] = "Unit 1201, Sample Tower\nAyala Avenue, Makati City\nMetro Manila, Philippines",
            ["phone"] = "+63 2 8000 0000",
            ["contactEmail"] = "info@example.com",
            ["officeHours"] = "Monday to Friday, 8:00 AM – 5:00 PM",
            ["metaDescription"] = "Contact Tanglaw Infrastructure Partners.",
        });
    }

    private IContent SaveAndPublish(string name, int parentId, string docTypeAlias, Dictionary<string, object> values)
    {
        var content = _contentService.Create(name, parentId, docTypeAlias);
        foreach (var (alias, value) in values)
        {
            content.SetValue(alias, value);
        }

        _contentService.Save(content);
        var publish = _contentService.Publish(content, new[] { "*" });
        if (!publish.Success)
        {
            _logger.LogWarning("TanglawSite: could not publish '{Name}': {Result}", name, publish.Result);
        }

        return content;
    }

    /// <summary>Rich text values are stored as JSON with the HTML in "markup".</summary>
    private static string Rte(string html)
        => JsonSerializer.Serialize(new { markup = html, blocks = (object?)null });
}
