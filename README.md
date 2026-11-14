# Tanglaw Infrastructure Partners: Umbraco 17 Sample Site

A sample corporate website for a fictional company focused on **Integrated Project Management and Infrastructure Delivery**, built on **Umbraco 17 LTS (.NET 10)**.

> **Not tested yet:** these files have not been compiled or run. The Umbraco API calls were checked against the Umbraco 17.0.0 source code, but expect a few small fixes on the first `dotnet run` (see *Troubleshooting*).

## What's included

| Page | Document type | Template | Editable fields |
|---|---|---|---|
| Home | `home` | `home.cshtml` | Hero title/text/button, intro, 3 stats, footer text |
| About Us | `contentPage` | `contentPage.cshtml` | Summary, rich text body |
| Services (list) | `servicesPage` | `servicesPage.cshtml` | Summary; child pages = services |
| Service (detail) | `serviceItem` | `serviceItem.cshtml` | Summary, rich text body |
| Projects (list) | `projectsPage` | `projectsPage.cshtml` | Summary; child pages = projects |
| Project (detail) | `projectItem` | `projectItem.cshtml` | Location, client, year, scope, summary, body |
| Contact | `contactPage` | `contactPage.cshtml` | Address, phone, email, office hours + contact form |

Every page also has a **Meta Description (SEO)** field.

Other features:
- Responsive layout with a mobile menu (CSS only, no JavaScript)
- Navigation built automatically from the pages under Home
- Contact form with validation, anti-forgery token and honeypot spam protection; sends email via SMTP
- First-run setup that creates all document types, templates and demo content automatically

## Requirements

- .NET 10 SDK: https://dotnet.microsoft.com/download/dotnet/10.0
- Visual Studio 2026 / VS Code / Rider (optional)
- No SQL Server needed for local development (uses SQLite)

## Getting started

```powershell
.\setup.ps1          # installs Umbraco templates, creates the project, copies the sample files
cd TanglawSite
dotnet run
```

Open the URL shown in the console (e.g. `https://localhost:44xxx`).

- Website: `/`
- Backoffice: `/umbraco`
- Login: `admin@example.com` / `Tanglaw@2026!` (**change this right away**)

### Manual alternative to setup.ps1

```powershell
dotnet new install Umbraco.Templates::17.0.0 --force
dotnet new umbraco -n TanglawSite --friendly-name "Administrator" --email "admin@example.com" --password "Tanglaw@2026!" --development-database-type SQLite
Copy-Item .\overlay\* .\TanglawSite -Recurse -Force
```

## Project structure (files added on top of the Umbraco template)

```
TanglawSite/
├── Composing/
│   ├── SiteSetupComposer.cs     # registers the first-run setup
│   ├── SiteSetupHandler.cs      # creates doc types, templates, demo content (runs once)
│   └── SiteContentUpgradeHandler.cs # adds the editable image/section fields (idempotent)
├── Controllers/
│   └── ContactFormController.cs # SurfaceController for the contact form
├── Models/
│   ├── ContactFormModel.cs      # form fields + validation
│   ├── SiteImages.cs            # picked image or temporary placeholder photo
│   ├── TextLines.cs             # parses "one item per line" fields
│   └── Icons.cs                 # inline SVG icons
├── Views/
│   ├── master.cshtml            # shared layout: header, nav, footer
│   ├── home.cshtml
│   ├── contentPage.cshtml
│   ├── servicesPage.cshtml / serviceItem.cshtml
│   ├── projectsPage.cshtml / projectItem.cshtml
│   └── contactPage.cshtml
└── wwwroot/css/site.css         # theme (colors are CSS variables at the top)
```

## How it works

1. **On first start**, `SiteSetupHandler` checks whether a `home` document type exists. If not, it:
   - registers each `.cshtml` in `/Views` as an Umbraco template (it passes the file content, so your views are not overwritten),
   - creates the document types and their fields, and sets which child pages are allowed,
   - creates and publishes the demo pages.
2. **After that**, it does nothing. All content is edited in the backoffice under **Content**.
3. Views read fields by alias (`Model.Value<string>("heroTitle")`), so they work with any ModelsBuilder mode. You can switch to strongly typed models later.

## Editing content (for the client)

- **Content** section → expand **Home** → click any page → edit → **Save and publish**
- To add a service: right-click **Services** → **Create** → **Service**
- To add a project: right-click **Projects** → **Create** → **Project**
- New top-level pages (e.g. Careers): right-click **Home** → **Create** → **Content Page**. They appear in the menu automatically.

> The backoffice only works over **HTTPS** (e.g. `https://localhost:44329/umbraco`). Plain `http://` login fails with "This server only accepts HTTPS requests".

### What can be edited (fields added by `SiteContentUpgradeHandler`)

Images are optional: if an image field is empty, a temporary placeholder photo is shown. List fields use **one item per line**; fields marked *Title|Text* use `Title|Description` on each line. **Clearing a list or title field hides that section.**

| Page | Group | Fields |
|---|---|---|
| Home | Hero | Background image, label above the title, key points (list) |
| Home | Intro | Main + small image, badge (`15+\|Years of delivery`), checklist (list) |
| Home | Sectors | Title, sectors ticker (list) |
| Home | Section Headings | Services heading, projects heading |
| Home | Delivery Process | Title, intro text, steps (*Title\|Text*) |
| Home | Testimonial | Quote, name, organization, image |
| Home | Call to Action | Title, text, background image (used on every page) |
| Home | Site Settings | LinkedIn URL, Facebook URL (empty = icon hidden) |
| All inner pages | Images | Banner / main image (also used on cards) |
| Content Page (About) | Images / Values | Side image, values title, values (*Title\|Text*) |
| Content, Services, Projects pages | Call to Action | Title, text |
| Service | Content | What you get (list) |
| Project | Images / Project Details | Photo gallery (multiple images), our role (list) |
| Contact | Contact Details | Map embed URL (Google Maps / OpenStreetMap embed link or full `<iframe>` snippet) |

How it works: on every start the handler adds any of these fields that are missing and fills them **once** with the sample text, so nothing you edit later is overwritten. Icons for list items rotate through a fixed set in the views.

## Contact form email

Without SMTP settings, form submissions are written to the log (`umbraco/Logs`) and the visitor still sees the thank-you message. To send real emails, add this to `appsettings.json` under `Umbraco:CMS:Global`:

```json
"Smtp": {
  "From": "website@yourdomain.com",
  "Host": "smtp.yourprovider.com",
  "Port": 587,
  "Username": "website@yourdomain.com",
  "Password": "store-this-in-user-secrets-or-a-vault"
}
```

The recipient is the **Email** field on the Contact page. Do not commit SMTP passwords: use `dotnet user-secrets` locally and environment variables or a secrets vault on the server.

## Customizing

- **Brand colors:** edit the `:root` variables at the top of `site.css`.
- **Company name:** search for "Tanglaw" in `Views/master.cshtml`.
- **Images:** upload photos under **Media**, then pick them in each page's image fields (see the table above). Placeholder photos are listed in `Models/SiteImages.cs`.
- **Hard-coded labels:** small labels such as "Who we are", "How we deliver" and button texts are still in the views.

## Deploying to IIS (Windows Server)

1. Install the **.NET 10 Hosting Bundle** on the server.
2. Create an empty SQL Server database and a SQL login for it.
3. Set the connection string on the server (environment variable or `appsettings.Production.json`, never in source control):
   ```json
   "ConnectionStrings": {
     "umbracoDbDSN": "Server=.;Database=TanglawSite;User Id=...;Password=...;TrustServerCertificate=True",
     "umbracoDbDSN_ProviderName": "Microsoft.Data.SqlClient"
   }
   ```
4. `dotnet publish -c Release -o ./publish` and copy the output to the IIS site folder.
5. App pool: **No Managed Code**. Give the app pool identity modify rights on `umbraco/`, `wwwroot/media/` and `App_Data/` (if present).
6. Move content from local SQLite to the server by either reinstalling and re-entering content, or using a tool such as uSync / Umbraco Deploy.

## Troubleshooting

| Symptom | Fix |
|---|---|
| Build error in `SiteSetupHandler.cs` (an Umbraco API name changed) | Fix the reported line, or delete the `Composing/` folder and use *Manual setup* below. |
| Site shows the Umbraco "no published content" page | Check `umbraco/Logs` for `TanglawSite:` messages. If setup failed, use *Manual setup*. |
| Template error about `master` | Make sure `Views/master.cshtml` exists and the **Master** template appears under **Settings → Templates**. |
| Contact form shows no error but no email | SMTP not configured; check the log for "Contact form received". |

### Manual setup (fallback)

In the backoffice, **Settings → Document Types → Create → Document Type with Template** for each type in the table above. Use the exact aliases and field aliases from `SiteSetupHandler.cs` (e.g. `heroTitle`, `summary`, `bodyText`). Allow `home` at root, then set allowed child types: Home → Content Page, Services Page, Projects Page, Contact Page; Services Page → Service; Projects Page → Project. Umbraco creates a template file for each; replace its content with the matching `.cshtml` from this package.
