namespace EnterpriseGovernance.Core.Domain;

public class ContentTypeDefinition
{
    public string Id { get; set; } = string.Empty; // SharePoint ID (bijv. 0x0101...)
    public string Name { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public List<FieldDefinition> Fields { get; set; } = new();
    public bool IsActiveInTenant { get; set; }

    // Evaluatie: Is dit een custom contenttype? 
    // Standaard documenten van Microsoft starten met 0x0101 en hebben geen extra custom GUID-string erachter.
    public bool IsCustom => !Id.StartsWith("0x010100") && Id.Length > 6;
}
