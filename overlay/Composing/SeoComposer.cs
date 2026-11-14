using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.ApplicationBuilder;
using Umbraco.Extensions;

namespace TanglawSite.Composing;

/// <summary>
/// Serves /robots.txt and /sitemap.xml (all published, visible pages).
/// Registered through Umbraco's pipeline so Program.cs stays the stock template.
/// </summary>
public class SeoComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.Configure<UmbracoPipelineOptions>(options =>
            options.AddFilter(new UmbracoPipelineFilter("TanglawSeo")
            {
                Endpoints = app => app.UseEndpoints(endpoints => MapSeo(endpoints)),
            }));
    }

    private static void MapSeo(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/robots.txt", (HttpContext ctx) =>
        {
            var baseUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
            var body = $"User-agent: *\nDisallow: /umbraco/\nAllow: /\n\nSitemap: {baseUrl}/sitemap.xml\n";
            return Results.Text(body, "text/plain", Encoding.UTF8);
        });

        endpoints.MapGet("/sitemap.xml", (HttpContext ctx, IUmbracoContextFactory contextFactory) =>
        {
            var baseUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
            using var reference = contextFactory.EnsureUmbracoContext();
            var query = ctx.RequestServices.GetRequiredService<Umbraco.Cms.Core.IPublishedContentQuery>();
            var pages = query.ContentAtRoot()
                .SelectMany(root => root.DescendantsOrSelf())
                .Where(p => p.IsVisible() && p.TemplateId > 0);

            var sb = new StringBuilder();
            using (var xml = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 }))
            {
                xml.WriteStartDocument();
                xml.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");
                foreach (var page in pages)
                {
                    xml.WriteStartElement("url");
                    xml.WriteElementString("loc", baseUrl + page.Url());
                    xml.WriteElementString("lastmod", page.UpdateDate.ToString("yyyy-MM-dd"));
                    xml.WriteElementString("priority", page.Level == 1 ? "1.0" : page.Level == 2 ? "0.8" : "0.6");
                    xml.WriteEndElement();
                }
                xml.WriteEndElement();
                xml.WriteEndDocument();
            }

            // XmlWriter over a StringBuilder declares utf-16; the response is utf-8
            return Results.Text(sb.ToString().Replace("encoding=\"utf-16\"", "encoding=\"utf-8\""), "application/xml", Encoding.UTF8);
        });
    }
}
