using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;

namespace TanglawSite.Composing;

/// <summary>
/// Registers the one-time site setup that creates document types, templates
/// and sample content the first time the site starts.
/// </summary>
public class SiteSetupComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, SiteSetupHandler>();
        // Runs after the setup: adds the editable design fields (images, home sections, CTAs...)
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, SiteContentUpgradeHandler>();
        // 2026 features: Insights section and project sectors
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, SiteFeaturesHandler>();
    }
}
