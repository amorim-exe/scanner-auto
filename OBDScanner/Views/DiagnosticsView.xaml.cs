using OBDScanner.Models;
using OBDScanner.Services;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace OBD2Scanner;

public partial class DiagnosticsView : UserControl
{
    private readonly ObdService _obdService;

    private readonly DiagnosticCsvService _csvService;

    private readonly DispatcherTimer _uiTimer;

    private CancellationTokenSource? _diagnosticCts;

    private readonly List<DiagnosticSample> _samples = [];

    private DiagnosticStage _currentStage;

    private DateTime _sessionStart;

    private DateTime _stageStart;

    private string _sessionId = string.Empty;

    private bool _running;

    private bool _paused;

    private int _sampleCount;

    public DiagnosticsView(ObdService obdService)
    {
        InitializeComponent();

        _obdService = obdService;

        _csvService = new DiagnosticCsvService();

        _uiTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };

        _uiTimer.Tick += UiTimer_Tick;
    }

    // =========================================================
    // DTC
    // =========================================================

    private void ReadButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!_obdService.IsConnected)
        {
            StatusText.Text = "Adapter disconnected";
            return;
        }

        try
        {
            var codes =
                _obdService.ReadDiagnosticCodes();

            CodesList.Items.Clear();

            DtcCountText.Text =
                codes.Count.ToString();

            if (codes.Count == 0)
            {
                CodesList.Items.Add(
                    "No diagnostic trouble codes found.");

                StatusText.Text = "No faults";

                return;
            }

            foreach (var code in codes)
                CodesList.Items.Add(code);

            StatusText.Text =
                $"{codes.Count} fault code(s)";
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
        }
    }

    private void ClearButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!_obdService.IsConnected)
        {
            StatusText.Text =
                "Adapter disconnected";

            return;
        }

        var result = MessageBox.Show(
            "Clear all diagnostic trouble codes from the ECU?",
            "OBD2 Scanner",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            if (_obdService.ClearDiagnosticCodes())
            {
                CodesList.Items.Clear();

                CodesList.Items.Add(
                    "Diagnostic codes cleared.");

                StatusText.Text = "Cleared";

                DtcCountText.Text = "0";
            }
            else
            {
                StatusText.Text =
                    "Unable to clear codes";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
        }
    }

    // =========================================================
    // START
    // =========================================================

    private async void StartButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!_obdService.IsConnected)
        {
            MessageBox.Show(
                "Conecte o adaptador OBD2 antes de iniciar.",
                "OBD2 Scanner",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        if (_running)
            return;

        _samples.Clear();

        _sampleCount = 0;

        SamplesText.Text = "0";

        CsvPathText.Text =
            "Nenhuma sessão exportada.";

        DiagnosticStatusText.Text =
            "Preparando sessão de diagnóstico...";

        DiagnosticWarningText.Text = string.Empty;

        _sessionId =
            Guid.NewGuid()
                .ToString("N")[..12]
                .ToUpperInvariant();

        _sessionStart = DateTime.Now;

        _diagnosticCts =
            new CancellationTokenSource();

        _running = true;
        _paused = false;

        StartButton.IsEnabled = false;
        PauseButton.IsEnabled = true;
        StopButton.IsEnabled = true;

        _uiTimer.Start();

        try
        {
            await RunDiagnosticAsync(
                _diagnosticCts.Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            DiagnosticStatusText.Text =
                $"Erro: {ex.Message}";
        }
        finally
        {
            FinishSession();
        }
    }

    // =========================================================
    // PAUSE
    // =========================================================

    private void PauseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!_running)
            return;

        _paused = !_paused;

        PauseButton.Content =
            _paused
                ? "▶ Continuar"
                : "Ⅱ Pausar";

        StageDescriptionText.Text =
            _paused
                ? "Coleta pausada."
                : "Coleta retomada.";
    }

    // =========================================================
    // STOP
    // =========================================================

    private void StopButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!_running)
            return;

        var result = MessageBox.Show(
            "Finalizar o diagnóstico e exportar o CSV?",
            "OBD2 Scanner",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        _diagnosticCts?.Cancel();
    }

    // =========================================================
    // DIAGNOSTIC ENGINE
    // =========================================================

    private async Task RunDiagnosticAsync(
        CancellationToken cancellationToken)
    {
        await RunStageAsync(
            DiagnosticStage.ColdEngine,
            TimeSpan.FromSeconds(10),
            cancellationToken);

        await RunStageAsync(
            DiagnosticStage.IdleCold,
            TimeSpan.FromSeconds(60),
            cancellationToken);

        await RunWarmingStageAsync(
            cancellationToken);

        await RunStageAsync(
            DiagnosticStage.IdleWarm,
            TimeSpan.FromSeconds(120),
            cancellationToken);

        await RunStageAsync(
            DiagnosticStage.Rpm2000,
            TimeSpan.FromSeconds(60),
            cancellationToken);

        await RunStageAsync(
            DiagnosticStage.Rpm2500,
            TimeSpan.FromSeconds(60),
            cancellationToken);

        await RunStageAsync(
            DiagnosticStage.Acceleration,
            TimeSpan.FromSeconds(30),
            cancellationToken);

        await RunStageAsync(
            DiagnosticStage.Deceleration,
            TimeSpan.FromSeconds(20),
            cancellationToken);
    }

    // =========================================================
    // WARMING
    // =========================================================

    private async Task RunWarmingStageAsync(
        CancellationToken cancellationToken)
    {
        _currentStage =
            DiagnosticStage.WarmingUp;

        _stageStart = DateTime.Now;

        StageText.Text =
            "Aquecendo motor";

        StageDescriptionText.Text =
            "Deixe o motor aquecer normalmente até a temperatura de operação.";

        while (!cancellationToken.IsCancellationRequested)
        {
            await WaitIfPausedAsync(
                cancellationToken);

            var sample =
                await ReadSampleAsync(
                    _currentStage,
                    cancellationToken);

            if (sample != null)
            {
                if (sample.ECT.HasValue &&
                    sample.ECT.Value >= 85)
                {
                    break;
                }
            }

            await Task.Delay(
                350,
                cancellationToken);
        }
    }

    // =========================================================
    // GENERIC STAGE
    // =========================================================

    private async Task RunStageAsync(
        DiagnosticStage stage,
        TimeSpan duration,
        CancellationToken cancellationToken)
    {
        _currentStage = stage;

        _stageStart = DateTime.Now;

        StageText.Text =
            GetStageTitle(stage);

        StageDescriptionText.Text =
            GetStageDescription(stage);

        while (
            DateTime.Now - _stageStart < duration &&
            !cancellationToken.IsCancellationRequested)
        {
            await WaitIfPausedAsync(
                cancellationToken);

            await ReadSampleAsync(
                stage,
                cancellationToken);

            await Task.Delay(
                350,
                cancellationToken);
        }
    }

    // =========================================================
    // SAMPLE
    // =========================================================

    private async Task<DiagnosticSample?> ReadSampleAsync(
        DiagnosticStage stage,
        CancellationToken cancellationToken)
    {
        try
        {
            var sample =
                await Task.Run(
                    () =>
                        _obdService.ReadDiagnosticSampleFast(
                            _sessionId,
                            stage.ToCsvValue()),
                    cancellationToken);

            _samples.Add(sample);

            _sampleCount++;

            UpdateLiveDiagnostic(sample);

            return sample;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    // =========================================================
    // PAUSE
    // =========================================================

    private async Task WaitIfPausedAsync(
        CancellationToken cancellationToken)
    {
        while (_paused &&
               !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(
                200,
                cancellationToken);
        }
    }

    // =========================================================
    // LIVE UI
    // =========================================================

    private void UpdateLiveDiagnostic(
        DiagnosticSample sample)
    {
        Dispatcher.Invoke(() =>
        {
            if (sample.RPM.HasValue)
                RpmText.Text =
                    $"{sample.RPM:0}";

            if (sample.ECT.HasValue)
                EctText.Text =
                    $"{sample.ECT:0} °C";

            if (sample.MAP.HasValue)
                MapText.Text =
                    $"{sample.MAP:0.0} kPa";

            if (sample.TPS.HasValue)
                TpsText.Text =
                    $"{sample.TPS:0.0} %";

            if (sample.STFT.HasValue)
            {
                StftText.Text =
                    $"{sample.STFT:+0.0;-0.0;0.0} %";

                StftBar.Value =
                    Math.Clamp(
                        sample.STFT.Value,
                        -30,
                        30);

                StftStatus.Text =
                    GetFuelTrimStatus(
                        sample.STFT.Value);
            }

            if (sample.LTFT.HasValue)
            {
                LtftText.Text =
                    $"{sample.LTFT:+0.0;-0.0;0.0} %";

                LtftBar.Value =
                    Math.Clamp(
                        sample.LTFT.Value,
                        -30,
                        30);

                LtftStatus.Text =
                    GetFuelTrimStatus(
                        sample.LTFT.Value);
            }

            if (sample.O2B1S1.HasValue)
            {
                O2Text.Text =
                    $"{sample.O2B1S1:0.000} V";
            }

            if (sample.EngineLoad.HasValue)
            {
                LoadText.Text =
                    $"{sample.EngineLoad:0.0} %";
            }

            SamplesText.Text =
                _sampleCount.ToString();

            DiagnosticStatusText.Text =
                $"Sistema: {sample.FuelSystemStatus}";

            UpdateWarnings(sample);
        });
    }

    // =========================================================
    // WARNINGS
    // =========================================================

    private void UpdateWarnings(
        DiagnosticSample sample)
    {
        var warnings =
            new List<string>();

        if (sample.LTFT.HasValue &&
            sample.LTFT.Value <= -10)
        {
            warnings.Add(
                $"LTFT negativo significativo: {sample.LTFT.Value:0.0}%");
        }

        if (sample.LTFT.HasValue &&
            sample.LTFT.Value <= -15)
        {
            warnings.Add(
                "A ECU está realizando uma correção negativa forte de combustível.");
        }

        if (sample.STFT.HasValue &&
            sample.STFT.Value <= -20)
        {
            warnings.Add(
                $"STFT fortemente negativo: {sample.STFT.Value:0.0}%");
        }

        if (sample.STFT.HasValue &&
            sample.STFT.Value >= 20)
        {
            warnings.Add(
                $"STFT fortemente positivo: +{sample.STFT.Value:0.0}%");
        }

        if (sample.ECT.HasValue &&
            sample.ECT.Value >= 85 &&
            sample.FuelSystemStatus.Contains(
                "CLOSED",
                StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add(
                "Motor aquecido e operando em closed loop.");
        }

        DiagnosticWarningText.Text =
            warnings.Count == 0
                ? "Nenhuma observação relevante nesta amostra."
                : string.Join(
                    Environment.NewLine,
                    warnings);
    }

    // =========================================================
    // FUEL TRIM
    // =========================================================

    private static string GetFuelTrimStatus(
        double value)
    {
        if (value <= -15)
            return "Correção negativa significativa";

        if (value <= -10)
            return "Correção negativa";

        if (value >= 15)
            return "Correção positiva significativa";

        if (value >= 10)
            return "Correção positiva";

        return "Dentro de faixa moderada";
    }

    // =========================================================
    // STAGE TEXT
    // =========================================================

    private static string GetStageTitle(
        DiagnosticStage stage)
    {
        return stage switch
        {
            DiagnosticStage.ColdEngine =>
                "Motor frio",

            DiagnosticStage.IdleCold =>
                "Marcha lenta - motor frio",

            DiagnosticStage.WarmingUp =>
                "Aquecendo motor",

            DiagnosticStage.IdleWarm =>
                "Marcha lenta - motor aquecido",

            DiagnosticStage.Rpm2000 =>
                "Teste a 2.000 RPM",

            DiagnosticStage.Rpm2500 =>
                "Teste a 2.500 RPM",

            DiagnosticStage.Acceleration =>
                "Aceleração progressiva",

            DiagnosticStage.Deceleration =>
                "Desaceleração",

            _ => "Diagnóstico"
        };
    }

    private static string GetStageDescription(
        DiagnosticStage stage)
    {
        return stage switch
        {
            DiagnosticStage.ColdEngine =>
                "Motor desligado/frio. Registre as condições iniciais.",

            DiagnosticStage.IdleCold =>
                "Deixe o motor funcionando em marcha lenta.",

            DiagnosticStage.WarmingUp =>
                "Deixe o motor aquecer normalmente.",

            DiagnosticStage.IdleWarm =>
                "Não acelere. Deixe o motor estabilizar em marcha lenta.",

            DiagnosticStage.Rpm2000 =>
                "Mantenha aproximadamente 2.000 RPM.",

            DiagnosticStage.Rpm2500 =>
                "Mantenha aproximadamente 2.500 RPM.",

            DiagnosticStage.Acceleration =>
                "Com segurança, acelere progressivamente.",

            DiagnosticStage.Deceleration =>
                "Solte o acelerador e deixe o motor desacelerar normalmente.",

            _ => string.Empty
        };
    }

    // =========================================================
    // TIMER
    // =========================================================

    private void UiTimer_Tick(
        object? sender,
        EventArgs e)
    {
        if (!_running)
            return;

        var sessionElapsed =
            DateTime.Now - _sessionStart;

        var stageElapsed =
            DateTime.Now - _stageStart;

        TimerText.Text =
            sessionElapsed.ToString(@"mm\:ss");

        SessionTimeText.Text =
            sessionElapsed.ToString(@"mm\:ss");

        StageProgress.Value =
            Math.Min(
                60,
                stageElapsed.TotalSeconds);

        ConnectionText.Text =
            _obdService.IsConnected
                ? "Conectado"
                : "Desconectado";
    }

    // =========================================================
    // FINISH
    // =========================================================

    private void FinishSession()
    {
        _running = false;
        _paused = false;

        _uiTimer.Stop();

        StartButton.IsEnabled = true;
        PauseButton.IsEnabled = false;
        StopButton.IsEnabled = false;

        PauseButton.Content =
            "Ⅱ Pausar";

        _diagnosticCts?.Dispose();
        _diagnosticCts = null;

        if (_samples.Count == 0)
        {
            DiagnosticStatusText.Text =
                "Nenhuma amostra foi coletada.";

            return;
        }

        try
        {
            var path =
                _csvService.Export(_samples);

            CsvPathText.Text = path;

            DiagnosticStatusText.Text =
                $"Diagnóstico concluído. {_samples.Count} amostras coletadas.";

            StageText.Text =
                "Diagnóstico finalizado";

            StageDescriptionText.Text =
                "Os dados foram exportados para CSV.";

            MessageBox.Show(
                $"Diagnóstico concluído.\n\n" +
                $"Amostras: {_samples.Count}\n\n" +
                $"CSV:\n{path}",
                "OBD2 Scanner",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            DiagnosticStatusText.Text =
                $"Erro ao exportar CSV: {ex.Message}";
        }
    }
}