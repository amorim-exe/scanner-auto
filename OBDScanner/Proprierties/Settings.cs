using System;
using System.IO;
using System.Text.Json;

namespace OBD2Scanner.Views;

public sealed class SettingsStore
{
    private static readonly Lazy<SettingsStore> _instance =
        new(() => new SettingsStore());

    private readonly string _filePath;

    public static SettingsStore Instance =>
        _instance.Value;

    public string BaudRate { get; set; } = "38400";

    public string Protocol { get; set; } = "Automatic";

    public string RefreshRate { get; set; } = "500 ms";

    public bool AutoConnect { get; set; }

    public bool StartLiveData { get; set; } = true;

    public bool KeepScreenAwake { get; set; } = true;

    public bool ShowRawData { get; set; }

    public bool SaveDiagnosticData { get; set; }

    private SettingsStore()
    {
        var directory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "OBD2Scanner");

        Directory.CreateDirectory(directory);

        _filePath =
            Path.Combine(
                directory,
                "settings.json");

        Load();
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(
                this,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            File.WriteAllText(
                _filePath,
                json);
        }
        catch
        {
        }
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return;

            var json =
                File.ReadAllText(_filePath);

            var settings =
                JsonSerializer.Deserialize<SettingsStoreData>(
                    json);

            if (settings == null)
                return;

            BaudRate =
                string.IsNullOrWhiteSpace(settings.BaudRate)
                    ? "38400"
                    : settings.BaudRate;

            Protocol =
                string.IsNullOrWhiteSpace(settings.Protocol)
                    ? "Automatic"
                    : settings.Protocol;

            RefreshRate =
                string.IsNullOrWhiteSpace(settings.RefreshRate)
                    ? "500 ms"
                    : settings.RefreshRate;

            AutoConnect =
                settings.AutoConnect;

            StartLiveData =
                settings.StartLiveData;

            KeepScreenAwake =
                settings.KeepScreenAwake;

            ShowRawData =
                settings.ShowRawData;

            SaveDiagnosticData =
                settings.SaveDiagnosticData;
        }
        catch
        {
            Reset();
        }
    }

    public void Reset()
    {
        BaudRate = "38400";
        Protocol = "Automatic";
        RefreshRate = "500 ms";
        AutoConnect = false;
        StartLiveData = true;
        KeepScreenAwake = true;
        ShowRawData = false;
        SaveDiagnosticData = false;

        Save();
    }

    private sealed class SettingsStoreData
    {
        public string BaudRate { get; set; } = "38400";

        public string Protocol { get; set; } = "Automatic";

        public string RefreshRate { get; set; } = "500 ms";

        public bool AutoConnect { get; set; }

        public bool StartLiveData { get; set; } = true;

        public bool KeepScreenAwake { get; set; } = true;

        public bool ShowRawData { get; set; }

        public bool SaveDiagnosticData { get; set; }
    }
}