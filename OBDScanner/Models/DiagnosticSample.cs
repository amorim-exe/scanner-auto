namespace OBDScanner.Models;

public sealed class DiagnosticSample
{
    public DateTime Timestamp { get; set; }

    public string SessionId { get; set; } = string.Empty;

    public string TestStage { get; set; } = string.Empty;

    public double? RPM { get; set; }

    public double? Speed { get; set; }

    public double? ECT { get; set; }

    public double? IAT { get; set; }

    public double? MAP { get; set; }

    public double? MAF { get; set; }

    public double? TPS { get; set; }

    public double? STFT { get; set; }

    public double? LTFT { get; set; }

    public double? O2B1S1 { get; set; }

    public double? O2B1S2 { get; set; }

    public double? EngineLoad { get; set; }

    public string FuelSystemStatus { get; set; } = string.Empty;

    public double? FuelPressure { get; set; }

    public double? TimingAdvance { get; set; }

    public bool IsClosedLoop =>
        FuelSystemStatus.Contains(
            "CLOSED",
            StringComparison.OrdinalIgnoreCase);
}