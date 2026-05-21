using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
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
        _logger.LogInformation("Start SharePoint governance scan voor site: {SiteUrl}", siteUrl);

        try
        {
            // 1. Configuratiewaarden ophalen
            var spConfig = _configuration.GetSection("SharePoint");
            var azureTenantId = spConfig["TenantId"] ?? throw new InvalidOperationException("TenantId ontbreekt in configuratie.");
            var clientId = spConfig["ClientId"] ?? throw new InvalidOperationException("ClientId ontbreekt in configuratie.");
            var vaultUrl = spConfig["KeyVaultUrl"] ?? throw new InvalidOperationException("KeyVaultUrl ontbreekt in configuratie.");
            var certificateName = spConfig["CertificateName"] ?? throw new InvalidOperationException("CertificateName ontbreekt in configuratie.");

            _logger.LogInformation("In-memory certificaat {CertName} ophalen uit {VaultUrl} via DefaultAzureCredential...", certificateName, vaultUrl);

            // 2. Initialiseer Azure credentials met uitsluiting van corrupte lokale VS-sessies
            var credentialOptions = new DefaultAzureCredentialOptions
            {
                ExcludeVisualStudioCredential = true,
                ExcludeVisualStudioCodeCredential = true
            };
            var azureCredential = new DefaultAzureCredential(credentialOptions);
            var secretClient = new SecretClient(new Uri(vaultUrl), azureCredential);

            // 3. Haal het certificaat op als Secret (bevat de Private Key)
            KeyVaultSecret secret = await secretClient.GetSecretAsync(certificateName);

            // Key Vault slaat PFX certificaten op als Base64 gecodeerde strings
            byte[] privateKeyBytes = Convert.FromBase64String(secret.Value);

            // Moderne .NET 9 methode voor in-memory PKCS12 (PFX) laadacties
            using var certificate = X509CertificateLoader.LoadPkcs12(
                privateKeyBytes,
                string.Empty,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet);

            _logger.LogInformation("Certificaat succesvol geladen in geheugen. Token aanvragen bij Entra ID...");

            // 4. Genereer het SharePoint specifieke App-Only token via het certificaat
            var spCredential = new ClientCertificateCredential(azureTenantId, clientId, certificate);
            var tokenContext = new TokenRequestContext(new[] { $"{siteUrl.Scheme}://{siteUrl.Host}/.default" });
            var tokenResult = await spCredential.GetTokenAsync(tokenContext);

            // 5. Injecteer het token in de PnP Core Context
            var authProvider = new SimpleTokenProvider(tokenResult.Token);
            using var context = await _pnpContextFactory.CreateAsync(siteUrl, authProvider);

            _logger.LogInformation("Verbinding met SharePoint geslaagd. Metadata-query opstarten...");

            // 6. Voer de diepe metadata-query uit via de formele PnP Core expressie-syntax
            await context.Web.LoadAsync(
                w => w.ContentTypes.QueryProperties(
                    c => c.Id,
                    c => c.Name,
                    c => c.Group,
                    c => c.Fields.QueryProperties(
                        f => f.Id,
                        f => f.InternalName,
                        f => f.Title,
                        f => f.TypeAsString,
                        f => f.Sealed,
                        f => f.Group
                    )
                ),
                w => w.Fields.QueryProperties(
                    f => f.Id,
                    f => f.InternalName,
                    f => f.Title,
                    f => f.TypeAsString,
                    f => f.Sealed,
                    f => f.Group
                )
            );

            var web = context.Web;

            _logger.LogInformation("Metadata succesvol ontvangen. Mapping naar domeinmodellen starten...");

            // 7. Map de data naar de zuivere Core domeinmodellen
            var result = _mappingService.MapToDomain(tenantId, web);

            _logger.LogInformation("Scan succesvol afgerond voor site {SiteUrl}. HygieneScore: {Score}%", siteUrl, result.HygieneScore);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout opgetreden tijdens de in-memory certificaat-opvraging of de SharePoint-scan.");
            throw;
        }
    }
}