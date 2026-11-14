using Microsoft.AspNetCore.Html;

namespace TanglawSite.Models;

/// <summary>Small inline SVG icon set (stroke icons, 24x24). Usage in views: @Icons.Get("phone")</summary>
public static class Icons
{
    private static readonly Dictionary<string, string> Paths = new(StringComparer.OrdinalIgnoreCase)
    {
        ["arrow-right"] = "<path d=\"M5 12h14M13 6l6 6-6 6\"/>",
        ["arrow-left"] = "<path d=\"M19 12H5M11 18l-6-6 6-6\"/>",
        ["arrow-up"] = "<path d=\"M12 19V5M5 12l7-7 7 7\"/>",
        ["clipboard"] = "<rect x=\"8\" y=\"2\" width=\"8\" height=\"4\" rx=\"1\"/><path d=\"M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2\"/><path d=\"m9 14 2 2 4-4\"/>",
        ["compass"] = "<circle cx=\"12\" cy=\"12\" r=\"10\"/><path d=\"m16.24 7.76-2.12 6.36-6.36 2.12 2.12-6.36z\"/>",
        ["layers"] = "<path d=\"m12 2 10 5-10 5L2 7z\"/><path d=\"m2 17 10 5 10-5\"/><path d=\"m2 12 10 5 10-5\"/>",
        ["file"] = "<path d=\"M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z\"/><path d=\"M14 2v6h6M8 13h8M8 17h8M8 9h2\"/>",
        ["hard-hat"] = "<path d=\"M2 18a1 1 0 0 0 1 1h18a1 1 0 0 0 1-1v-2a1 1 0 0 0-1-1H3a1 1 0 0 0-1 1z\"/><path d=\"M10 10V5a1 1 0 0 1 1-1h2a1 1 0 0 1 1 1v5\"/><path d=\"M4 15v-3a6 6 0 0 1 6-6M14 6a6 6 0 0 1 6 6v3\"/>",
        ["check-circle"] = "<circle cx=\"12\" cy=\"12\" r=\"10\"/><path d=\"m8.5 12 2.5 2.5 4.5-5\"/>",
        ["check"] = "<path d=\"M20 6 9 17l-5-5\"/>",
        ["map-pin"] = "<path d=\"M20 10c0 6-8 12-8 12s-8-6-8-12a8 8 0 0 1 16 0z\"/><circle cx=\"12\" cy=\"10\" r=\"3\"/>",
        ["phone"] = "<path d=\"M22 16.92v3a2 2 0 0 1-2.18 2 19.8 19.8 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6A19.8 19.8 0 0 1 2.12 4.18 2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72c.13.96.36 1.9.7 2.81a2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45c.91.34 1.85.57 2.81.7A2 2 0 0 1 22 16.92z\"/>",
        ["mail"] = "<rect x=\"2\" y=\"4\" width=\"20\" height=\"16\" rx=\"2\"/><path d=\"m22 7-10 6L2 7\"/>",
        ["clock"] = "<circle cx=\"12\" cy=\"12\" r=\"10\"/><path d=\"M12 6v6l4 2\"/>",
        ["shield"] = "<path d=\"M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z\"/><path d=\"m9 12 2 2 4-4\"/>",
        ["target"] = "<circle cx=\"12\" cy=\"12\" r=\"10\"/><circle cx=\"12\" cy=\"12\" r=\"6\"/><circle cx=\"12\" cy=\"12\" r=\"2\"/>",
        ["users"] = "<path d=\"M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2\"/><circle cx=\"9\" cy=\"7\" r=\"4\"/><path d=\"M22 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75\"/>",
        ["chart"] = "<path d=\"M3 3v18h18\"/><path d=\"m7 15 4-4 3 3 5-6\"/>",
        ["calendar"] = "<rect x=\"3\" y=\"4\" width=\"18\" height=\"18\" rx=\"2\"/><path d=\"M16 2v4M8 2v4M3 10h18\"/>",
        ["building"] = "<rect x=\"4\" y=\"2\" width=\"16\" height=\"20\" rx=\"2\"/><path d=\"M9 22v-4h6v4M8 6h.01M12 6h.01M16 6h.01M8 10h.01M12 10h.01M16 10h.01M8 14h.01M12 14h.01M16 14h.01\"/>",
        ["banknote"] = "<rect x=\"2\" y=\"6\" width=\"20\" height=\"12\" rx=\"2\"/><circle cx=\"12\" cy=\"12\" r=\"2\"/><path d=\"M6 12h.01M18 12h.01\"/>",
        ["linkedin"] = "<path d=\"M16 8a6 6 0 0 1 6 6v7h-4v-7a2 2 0 0 0-4 0v7h-4v-7a6 6 0 0 1 6-6z\"/><rect x=\"2\" y=\"9\" width=\"4\" height=\"12\"/><circle cx=\"4\" cy=\"4\" r=\"2\"/>",
        ["facebook"] = "<path d=\"M18 2h-3a5 5 0 0 0-5 5v3H7v4h3v8h4v-8h3l1-4h-4V7a1 1 0 0 1 1-1h3z\"/>",
        ["send"] = "<path d=\"m22 2-7 20-4-9-9-4z\"/><path d=\"M22 2 11 13\"/>",
    };

    private static readonly Dictionary<string, string> ServiceIcons = new(StringComparer.OrdinalIgnoreCase)
    {
        ["integrated-project-management"] = "clipboard",
        ["feasibility-planning"] = "compass",
        ["design-management"] = "layers",
        ["procurement-contract-management"] = "file",
        ["construction-supervision"] = "hard-hat",
        ["commissioning-handover"] = "check-circle",
    };

    public static IHtmlContent Get(string name, string cssClass = "icon")
        => new HtmlString(
            $"<svg class=\"{cssClass}\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\" focusable=\"false\">{(Paths.TryGetValue(name, out var p) ? p : Paths["check"])}</svg>");

    public static IHtmlContent ForService(string? urlSegment, string cssClass = "icon")
        => Get(urlSegment is not null && ServiceIcons.TryGetValue(urlSegment, out var n) ? n : "clipboard", cssClass);
}
