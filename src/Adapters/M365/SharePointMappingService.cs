using EnterpriseGovernance.Core.Domain;
using PnP.Core.Model.SharePoint;
using PnP.Core.QueryModel;

namespace EnterpriseGovernance.Adapters.M365;

public class SharePointMappingService
{
    /// <summary>
    /// Vertaalt een complete SharePoint Web weergave naar een TenantAuditResult.
    /// </summary>
    public TenantAuditResult MapToDomain(string tenantId, IWeb web)
    {
        var auditResult = new TenantAuditResult
        {
            TenantId = tenantId,
            ScanDateTime = DateTime.UtcNow
        };

        // Loop door alle opgevraagde content types
        if (web.IsPropertyAvailable(w => w.ContentTypes))
        {
            foreach (var pnpContentType in web.ContentTypes.AsRequested())
            {
                auditResult.DetectedContentTypes.Add(MapToDomain(pnpContentType));
            }
        }

        // Loop door alle opgevraagde site-kolommen (fields)
        if (web.IsPropertyAvailable(w => w.Fields))
        {
            foreach (var pnpField in web.Fields.AsRequested())
            {
                auditResult.DetectedGlobalFields.Add(MapFieldToDomain(pnpField));
            }
        }

        return auditResult;
    }

    /// <summary>
    /// Vertaalt een PnP Core IContentType naar ons eigen Core Domeinmodel.
    /// </summary>
    public ContentTypeDefinition MapToDomain(IContentType pnpContentType)
    {
        var domainContentType = new ContentTypeDefinition
        {
            Id = pnpContentType.Id,
            Name = pnpContentType.Name,
            Group = pnpContentType.Group,
            IsActiveInTenant = true
        };

        // Defensieve check toegevoegd: controleer of de velden-property daadwerkelijk is geladen
        if (pnpContentType.IsPropertyAvailable(c => c.Fields))
        {
            foreach (var pnpField in pnpContentType.Fields)
            {
                domainContentType.Fields.Add(MapFieldToDomain(pnpField));
            }
        }

        return domainContentType;
    }
    /// <summary>
    /// Vertaalt een individueel PnP Core IField naar ons eigen Core Field model.
    /// </summary>
    public FieldDefinition MapFieldToDomain(IField pnpField)
    {
        return new FieldDefinition
        {
            Id = pnpField.Id.ToString(),
            InternalName = pnpField.InternalName,
            DisplayName = pnpField.Title,
            Type = pnpField.TypeAsString,
            IsSealed = pnpField.Sealed,
            Source = DetermineSource(pnpField)
        };
    }

    /// <summary>
    /// Bepaalt de herkomst van het veld op basis van Microsoft-standaarden en PnP-metadata.
    /// </summary>
    private FieldSource DetermineSource(IField field)
    {
        // 1. Is het veld verzegeld (Sealed) door Microsoft zelf?
        if (field.Sealed)
        {
            return FieldSource.MicrosoftDefault;
        }

        // 2. Behoort het tot de standaard ingebouwde groepen van SharePoint?
        string group = field.Group ?? string.Empty;
        if (group.StartsWith("Base Columns") ||
            group.StartsWith("Document Columns") ||
            group.StartsWith("List Columns"))
        {
            return FieldSource.MicrosoftDefault;
        }

        // 3. Voor de overige velden kijken we later bij de scan of ze uit de 
        // centrale Content Type Hub komen of puur lokaal zijn aangemaakt.
        return FieldSource.CustomLocalSite;
    }
}