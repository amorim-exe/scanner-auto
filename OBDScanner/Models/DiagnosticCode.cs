namespace OBDScanner.Models;

public class DiagnosticCode
{
    public string Code { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Status { get; set; } = "ACTIVE";
}