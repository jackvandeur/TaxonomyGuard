namespace EnterpriseGovernance.Core.Domain;

public class TenantAuditResult
{
    public string TenantId { get; set; } = string.Empty;
    public DateTime ScanDateTime { get; set; } = DateTime.UtcNow;

    public List<ContentTypeDefinition> DetectedContentTypes { get; set; } = new();
    public List<FieldDefinition> DetectedGlobalFields { get; set; } = new();

    // Berekeningen op basis van de geïndexeerde data
    public int TotalCustomContentTypes => DetectedContentTypes.Count(ct => ct.IsCustom);

    public int FieldsWithNamingDrift =>
        DetectedGlobalFields.Count(f => f.HasNamingDrift) +
        DetectedContentTypes.SelectMany(ct => ct.Fields).Count(f => f.HasNamingDrift);

    public double HygieneScore
    {
        get
        {
            int totalFields = DetectedGlobalFields.Count + DetectedContentTypes.SelectMany(ct => ct.Fields).Count();
            if (totalFields == 0) return 100.0;

            // Simpele weging: aftrek op basis van het percentage velden met naamgevingsfouten
            double driftPercentage = (double)FieldsWithNamingDrift / totalFields * 100;
            return Math.Max(0, 100 - driftPercentage);
        }
    }
}