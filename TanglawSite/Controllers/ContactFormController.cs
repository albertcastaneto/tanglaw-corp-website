using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TanglawSite.Models;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Mail;
using Umbraco.Cms.Core.Models.Email;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Website.Controllers;
using Umbraco.Extensions;

namespace TanglawSite.Controllers;

/// <summary>
/// Handles the contact form on the Contact page. Sends the message by email to the
/// address in the page's "contactEmail" field (requires SMTP settings in appsettings.json).
/// If SMTP is not configured, the submission is written to the log instead.
/// </summary>
public class ContactFormController : SurfaceController
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ContactFormController> _logger;

    public ContactFormController(
        IUmbracoContextAccessor umbracoContextAccessor,
        IUmbracoDatabaseFactory databaseFactory,
        ServiceContext services,
        AppCaches appCaches,
        IProfilingLogger profilingLogger,
        IPublishedUrlProvider publishedUrlProvider,
        IEmailSender emailSender,
        ILogger<ContactFormController> logger)
        : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
    {
        _emailSender = emailSender;
        _logger = logger;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(ContactFormModel model)
    {
        // Honeypot: pretend success for bots
        if (!string.IsNullOrWhiteSpace(model.Website))
        {
            TempData["ContactSuccess"] = true;
            return RedirectToCurrentUmbracoPage();
        }

        if (!ModelState.IsValid)
        {
            return CurrentUmbracoPage();
        }

        var recipient = CurrentPage?.Value<string>("contactEmail");
        var body =
            $"<p><strong>Name:</strong> {WebUtility.HtmlEncode(model.Name)}</p>" +
            $"<p><strong>Email:</strong> {WebUtility.HtmlEncode(model.Email)}</p>" +
            $"<p><strong>Company:</strong> {WebUtility.HtmlEncode(model.Company ?? "-")}</p>" +
            $"<p><strong>Message:</strong><br>{WebUtility.HtmlEncode(model.Message).Replace("\n", "<br>")}</p>";

        if (!string.IsNullOrWhiteSpace(recipient) && _emailSender.CanSendRequiredEmail())
        {
            try
            {
                var message = new EmailMessage(null, recipient, $"Website inquiry from {model.Name}", body, true);
                await _emailSender.SendAsync(message, "ContactForm");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Contact form email failed");
                ModelState.AddModelError(string.Empty, "Sorry, we could not send your message. Please email or call us instead.");
                return CurrentUmbracoPage();
            }
        }
        else
        {
            _logger.LogWarning(
                "Contact form received but SMTP is not configured. From {Name} <{Email}>: {Message}",
                model.Name, model.Email, model.Message);
        }

        TempData["ContactSuccess"] = true;
        return RedirectToCurrentUmbracoPage();
    }
}
