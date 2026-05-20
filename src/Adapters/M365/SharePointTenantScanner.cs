using Azure.Core;
using Azure.Identity;
using EnterpriseGovernance.Core.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PnP.Core.Services;
using PnP.Core.QueryModel;

namespace EnterpriseGovernance.Adapters.M365;

public class SharePointTenantScanner
{
    private readonly IPnPContextFactory _pnpContextFactory;
    private readonly SharePointMappingService _mappingService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SharePointTenantScanner> _logger;

    public SharePointTenantScanner(
        IPnPContextFactory pnpContextFactory,
        SharePointMappingService mappingService,
        IConfiguration configuration,
        ILogger<SharePointTenantScanner> logger)
    {
        _pnpContextFactory = pnpContextFactory;
        _mappingService = mappingService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<TenantAuditResult> ScanSiteAsync(string tenantId, Uri siteUrl)
    {
        var auditResult = new TenantAuditResult
        {
            TenantId = tenantId,
            ScanDateTime = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Start live token generatie via Azure.Identity voor {SiteUrl}", siteUrl);

            // 1. Haal de credentials op uit de configuratie
            var spConfig = _configuration.GetSection("SharePoint");
            var azureTenantId = spConfig["TenantId"] ?? string.Empty;
            var clientId = spConfig["ClientId"] ?? string.Empty;
            var clientSecret = spConfig["ClientSecret"] ?? string.Empty;

            // 2. Verkrijg het token via Azure.Identity
            var credential = new ClientSecretCredential(azureTenantId, clientId, clientSecret);
            var tokenContext = new TokenRequestContext(new[] { $"{siteUrl.Scheme}://{siteUrl.Host}/.default" });
            var tokenResult = await credential.GetTokenAsync(tokenContext);

            // 3. Maak onze custom provider aan met het live token
            var authProvider = new SimpleTokenProvider(tokenResult.Token);

            // 4. Open de context via de standaard factory methode die gegarandeerd bestaat
            using var context = await _pnpContextFactory.CreateAsync(siteUrl, authProvider);

            _logger.LogInformation("Verbinding met SharePoint geslaagd. Starten van metadata ophalen...");

            // 5. Haal de contenttypes en kolommen op
            await context.Web.LoadAsync(w => w.ContentTypes.QueryProperties(
                ct => ct.Id,
                ct => ct.Name,
                ct => ct.Group,
                ct => ct.Fields.QueryProperties(
                    f => f.Id,
                    f => f.InternalName,
                    f => f.Title,
                    f => f.TypeAsString,
                    f => f.Sealed,
                    f => f.Group
                )
            ));

            foreach (var pnpContentType in context.Web.ContentTypes.AsRequested())
            {
                var domainContentType = _mappingService.MapToDomain(pnpContentType);
                auditResult.DetectedContentTypes.Add(domainContentType);
            }

            await context.Web.LoadAsync(w => w.Fields.QueryProperties(
                f => f.Id,
                f => f.InternalName,
                f => f.Title,
                f => f.TypeAsString,
                f => f.Sealed,
                f => f.Group
            ));

            foreach (var pnpField in context.Web.Fields.AsRequested())
            {
                var domainField = _mappingService.MapFieldToDomain(pnpField);
                auditResult.DetectedGlobalFields.Add(domainField);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout opgetreden tijdens token-generatie of SharePoint-scan.");
            throw;
        }

        return auditResult;
    }
}