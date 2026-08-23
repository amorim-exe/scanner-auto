namespace OBDScanner.Models;

public enum DiagnosticStage
{
    ColdEngine,
    IdleCold,
    WarmingUp,
    IdleWarm,
    Rpm2000,
    Rpm2500,
    Acceleration,
    Deceleration
}

public static class DiagnosticStageExtensions
{
    public static string ToCsvValue(this DiagnosticStage stage)
    {
        return stage switch
        {
            DiagnosticStage.ColdEngine => "COLD_ENGINE",
            DiagnosticStage.IdleCold => "IDLE_COLD",
            DiagnosticStage.WarmingUp => "WARMING_UP",
            DiagnosticStage.IdleWarm => "IDLE_WARM",
            DiagnosticStage.Rpm2000 => "RPM_2000",
            DiagnosticStage.Rpm2500 => "RPM_2500",
            DiagnosticStage.Acceleration => "ACCELERATION",
            DiagnosticStage.Deceleration => "DECELERATION",
            _ => "UNKNOWN"
        };
    }
}