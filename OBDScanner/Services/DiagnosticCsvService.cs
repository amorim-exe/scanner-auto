using OBDScanner.Models;
using System.Globalization;
using System.IO;
using System.Text;

namespace OBDScanner.Services;

public sealed class DiagnosticCsvService
{
    public string Export(
        IEnumerable<DiagnosticSample> samples,
        string? directory = null)
    {
        directory ??= Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.MyDocuments),
            "OBD2Scanner",
            "Diagnostics");

        Directory.CreateDirectory(directory);

        var fileName =
            $"OBD_Diagnostic_{DateTime.Now:yyyy-MM-dd_HHmmss}.csv";

        var path = Path.Combine(directory, fileName);

        var sb = new StringBuilder();

        sb.AppendLine(
            "Timestamp,SessionId,TestStage,RPM,Speed,ECT,IAT,MAP,MAF,TPS,STFT,LTFT,O2_B1S1,O2_B1S2,EngineLoad,FuelSystemStatus,FuelPressure,TimingAdvance");

        foreach (var sample in samples)
        {
            sb.AppendLine(string.Join(",",
                Csv(sample.Timestamp.ToString(
                    "yyyy-MM-dd HH:mm:ss.fff")),

                Csv(sample.SessionId),

                Csv(sample.TestStage),

                Number(sample.RPM),
                Number(sample.Speed),
                Number(sample.ECT),
                Number(sample.IAT),
                Number(sample.MAP),
                Number(sample.MAF),
                Number(sample.TPS),
                Number(sample.STFT),
                Number(sample.LTFT),
                Number(sample.O2B1S1),
                Number(sample.O2B1S2),
                Number(sample.EngineLoad),

                Csv(sample.FuelSystemStatus),

                Number(sample.FuelPressure),
                Number(sample.TimingAdvance)
            ));
        }

        File.WriteAllText(
            path,
            sb.ToString(),
            new UTF8Encoding(true));

        return path;
    }

    private static string Number(double? value)
    {
        return value.HasValue
            ? value.Value.ToString(
                "0.###",
                CultureInfo.InvariantCulture)
            : string.Empty;
    }

    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Contains(',') ||
            value.Contains('"') ||
            value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}