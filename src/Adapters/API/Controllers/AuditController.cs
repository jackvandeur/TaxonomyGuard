using EnterpriseGovernance.Adapters.M365;
using EnterpriseGovernance.Core.Domain;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseGovernance.Adapters.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditController : ControllerBase
{
    private readonly SharePointTenantScanner _scanner;
    private readonly ILogger<AuditController> _logger;

    public AuditController(SharePointTenantScanner scanner, ILogger<AuditController> logger)
    {
        _scanner = scanner;
        _logger = logger;
    }

    /// <summary>
    /// Start een auditscan op een specifieke SharePoint-locatie en berekent de hygiënescore.
    /// </summary>
    /// <param name="tenantId">De GUID of identificatie van de klantomgeving</param>
    /// <param name="siteUrl">De URL van de SharePoint site/hub die gescand moet worden</param>
    [HttpGet("scan")]
    public async Task<ActionResult<TenantAuditResult>> ExecuteScan([FromQuery] string tenantId, [FromQuery] string siteUrl)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(siteUrl))
        {
            return BadRequest("Zowel 'tenantId' als 'siteUrl' zijn verplichte parameters.");
        }

        if (!Uri.TryCreate(siteUrl, UriKind.Absolute, out var validatedUri))
        {
            return BadRequest("De opgegeven 'siteUrl' is geen geldige absolute URL.");
        }

        try
        {
            _logger.LogInformation("API verzoek ontvangen voor scan op: {Url}", siteUrl);

            // Voer de scan uit via de M365-adapter
            TenantAuditResult result = await _scanner.ScanSiteAsync(tenantId, validatedUri);

            // Retourneer het resultaat (inclusief onze berekende HygieneScore uit de Core!)
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout tijdens het uitvoeren van de API scan.");
            return StatusCode(500, $"Interne serverfout tijdens het scannen: {ex.Message}");
        }
    }
}