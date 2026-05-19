namespace EnterpriseGovernance.Core.Domain;

public enum FieldSource
{
    MicrosoftDefault,
    CustomCentralHub,
    CustomLocalSite
}

public class FieldDefinition
{
    public string Id { get; set; } = string.Empty;
    public string InternalName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // bijv. Text, Number, DateTime
    public FieldSource Source { get; set; }
    public bool IsSealed { get; set; }

    // Evaluatie: Bevat de interne naam rommel (zoals spatie-coderingen)?
    public bool HasNamingDrift =>
        InternalName.Contains("_x0020_") ||
        InternalName.Contains("%20") ||
        InternalName.Contains("_x002d_"); // Koppelteken-drift
}