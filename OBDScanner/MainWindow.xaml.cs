using OBDScanner.Services;
using System;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace OBD2Scanner;

public partial class MainWindow : Window
{
    private readonly ObdService _obdService;
    private readonly DispatcherTimer _liveDataTimer;

    private DashboardView? _dashboardView;
    private SensorsView? _sensorsView;
    private DiagnosticsView? _diagnosticsView;
    private SettingsView? _settingsView;

    private string _activeView = "Dashboard";

    private bool _readingLiveData;

    public MainWindow()
    {
        InitializeComponent();

        DataContext =
            LocalizationService.Instance;

        _obdService =
            new ObdService();

        _liveDataTimer =
            new DispatcherTimer
            {
                Interval =
                    TimeSpan.FromMilliseconds(700)
            };

        _liveDataTimer.Tick +=
            LiveDataTimer_Tick;

        LoadPorts();

        ShowDashboard();

        UpdateConnectionStatus();
    }

    private void LoadPorts()
    {
        var selected =
            PortComboBox.SelectedItem as string;

        PortComboBox.Items.Clear();

        var ports =
            SerialPort.GetPortNames();

        Array.Sort(ports);

        foreach (var port in ports)
        {
            PortComboBox.Items.Add(port);
        }

        if (!string.IsNullOrWhiteSpace(selected) &&
            PortComboBox.Items.Contains(selected))
        {
            PortComboBox.SelectedItem =
                selected;
        }
        else if (PortComboBox.Items.Count > 0)
        {
            PortComboBox.SelectedIndex = 0;
        }
    }

    private void RefreshButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        LoadPorts();
    }

    private void ConnectButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_obdService.IsConnected)
        {
            DisconnectObd();
            return;
        }

        if (PortComboBox.SelectedItem
            is not string portName)
        {
            MessageBox.Show(
                "Selecione uma porta COM disponível.",
                "OBD2 Scanner",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        try
        {
            ConnectButton.IsEnabled =
                false;

            _obdService.Connect(
                portName);

            UpdateConnectionStatus();
        }
        catch (Exception ex)
        {
            _obdService.Disconnect();

            UpdateConnectionStatus();

            MessageBox.Show(
                ex.Message,
                "OBD2 Scanner",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            ConnectButton.IsEnabled =
                true;
        }
    }

    private void DisconnectObd()
    {
        _liveDataTimer.Stop();

        _obdService.Disconnect();

        _readingLiveData =
            false;

        UpdateConnectionStatus();
    }

    private async void LiveDataTimer_Tick(
        object? sender,
        EventArgs e)
    {
        if (!_obdService.IsConnected)
            return;

        /*
         * Impede que duas leituras OBD ocorram
         * simultaneamente.
         */
        if (_readingLiveData)
            return;

        _readingLiveData = true;

        try
        {
            var data =
                await _obdService.ReadLiveDataAsync();

            /*
             * A leitura terminou em background.
             *
             * Como estamos de volta na thread da WPF,
             * podemos atualizar os controles diretamente.
             */

            _dashboardView?.UpdateData(data);

            _sensorsView?.UpdateData(data);
        }
        catch
        {
            /*
             * Não apagamos imediatamente os dados.
             *
             * Um PID individual pode simplesmente não responder.
             * O próximo ciclo tentará novamente.
             */
        }
        finally
        {
            _readingLiveData =
                false;
        }
    }

    private void UpdateConnectionStatus()
    {
        if (_obdService.IsConnected)
        {
            StatusIndicator.Foreground =
                new SolidColorBrush(
                    Color.FromRgb(
                        0,
                        204,
                        102));

            StatusText.Text =
                LocalizationService.Instance[
                    "status_connected"];

            ConnectButton.Content =
                LocalizationService.Instance[
                    "disconnect"];

            PortComboBox.IsEnabled =
                false;

            _liveDataTimer.Start();
        }
        else
        {
            StatusIndicator.Foreground =
                new SolidColorBrush(
                    Color.FromRgb(
                        125,
                        133,
                        141));

            StatusText.Text =
                LocalizationService.Instance[
                    "status_disconnected"];

            ConnectButton.Content =
                LocalizationService.Instance[
                    "connect"];

            PortComboBox.IsEnabled =
                true;

            _liveDataTimer.Stop();

            _dashboardView?.ClearData();

            _sensorsView?.ClearData();
        }
    }

    private void ShowDashboard()
    {
        _dashboardView ??=
            new DashboardView();

        MainContent.Content =
            _dashboardView;

        _activeView =
            "Dashboard";

        DashboardButton.Background =
            (Brush)FindResource(
                "SurfaceHoverBrush");

        DiagnosticsButton.Background =
            Brushes.Transparent;

        SensorsButton.Background =
            Brushes.Transparent;
    }

    private void ShowDiagnostics()
    {
        _diagnosticsView ??=
            new DiagnosticsView(
                _obdService);

        MainContent.Content =
            _diagnosticsView;

        _activeView =
            "Diagnostics";

        DashboardButton.Background =
            Brushes.Transparent;

        DiagnosticsButton.Background =
            (Brush)FindResource(
                "SurfaceHoverBrush");

        SensorsButton.Background =
            Brushes.Transparent;

    }

    private void ShowSensors()
    {
        _sensorsView ??=
            new SensorsView();

        MainContent.Content =
            _sensorsView;

        _activeView =
            "Sensors";

        DashboardButton.Background =
            Brushes.Transparent;

        DiagnosticsButton.Background =
            Brushes.Transparent;

        SensorsButton.Background =
            (Brush)FindResource(
                "SurfaceHoverBrush");

    }

    private void DashboardButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ShowDashboard();
    }

    private void DiagnosticsButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ShowDiagnostics();
    }

    private void SensorsButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ShowSensors();
    }

    private void SettingsButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        _settingsView ??=
            new SettingsView();

        MainContent.Content =
            _settingsView;

        _activeView =
            "Settings";

        DashboardButton.Background =
            Brushes.Transparent;

        DiagnosticsButton.Background =
            Brushes.Transparent;

        SensorsButton.Background =
            Brushes.Transparent;

    }

    private void Theme_Click(
    object sender,
    RoutedEventArgs e)
    {
        LocalizationService.Instance.ToggleTheme();

        var isDark =
            LocalizationService.Instance.IsDarkTheme;

        if (isDark)
        {
            // ==============================
            // DARK THEME
            // ==============================

            Background =
                new SolidColorBrush(
                    Color.FromRgb(
                        11,
                        17,
                        23));

            Foreground =
                new SolidColorBrush(
                    Color.FromRgb(
                        235,
                        241,
                        245));

            //ThemeButton.Content = "☀";

            // Background
            Resources["BackgroundBrush"] =
                new SolidColorBrush(
                    Color.FromRgb(
                        11,
                        17,
                        23));

            // Cards / Surfaces
            Resources["SurfaceBrush"] =
                new SolidColorBrush(
                    Color.FromRgb(
                        17,
                        26,
                        35));

            // Hover
            Resources["SurfaceHoverBrush"] =
                new SolidColorBrush(
                    Color.FromRgb(
                        24,
                        37,
                        49));

            // Borders
            Resources["BorderBrush"] =
                new SolidColorBrush(
                    Color.FromRgb(
                        38,
                        52,
                        65));

            // Main text
            Resources["TextBrush"] =
                new SolidColorBrush(
                    Color.FromRgb(
                        235,
                        241,
                        245));

            // Secondary text
            Resources["SecondaryTextBrush"] =
                new SolidColorBrush(
                    Color.FromRgb(
                        139,
                        153,
                        165));

            // Vermelho principal
            Resources["AccentBrush"] =
                new SolidColorBrush(
                    Color.FromRgb(
                        255,
                        68,
                        68));

            // Verde de status
            Resources["SuccessBrush"] =
                new SolidColorBrush(
                    Color.FromRgb(
                        0,
                        204,
                        102));
        }
        else
        {
            // ==============================
            // LIGHT THEME
            // ==============================

            Background =
                new SolidColorBrush(
                    Color.FromRgb(
                        244,
                        247,
                        250));

            Foreground =
                new SolidColorBrush(
                    Color.FromRgb(
                        20,
                        30,
                        40));

            //ThemeButton.Content = "☾";

            // Background
            Resources["BackgroundBrush"] =
                new SolidColorBrush(
                    Color.FromRgb(
                        244,
                        247,
                        250));

            // Cards / Surfaces
            Resources["SurfaceBrush"] =
                new SolidColorBrush(
                    Color.FromRgb(
                        255,
                        255,
                        255));

            // Hover
            Resources["SurfaceHoverBrush"] =
                new SolidColorBrush(
                    Color.FromRgb(
                        232,
                        239,
                        246));

            // Borders
            Resources["BorderBrush"] =
                new SolidColorBrush(
                    Color.FromRgb(
                        211,
                        220,
                        228));

            // Main text
            Resources["TextBrush"] =
                new SolidColorBrush(
                    Color.FromRgb(
                        20,
                        30,
                        40));

            // Secondary text
            Resources["SecondaryTextBrush"] =
                new SolidColorBrush(
                    Color.FromRgb(
                        91,
                        105,
                        118));

            // Azul principal
            Resources["AccentBrush"] =
                new SolidColorBrush(
                    Color.FromRgb(
                        37,
                        99,
                        235));

            // Verde de status
            Resources["SuccessBrush"] =
                new SolidColorBrush(
                    Color.FromRgb(
                        22,
                        163,
                        74));
        }
    }

    private void WindowHeader_MouseLeftButtonDown(
    object sender,
    MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            try
            {
                DragMove();
            }
            catch
            {
                // Ignora se a janela estiver em uma operação de resize.
            }
        }
    }

    private void MinimizeButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            MaximizeButton.Content = "□";
        }
        else
        {
            WindowState = WindowState.Maximized;
            MaximizeButton.Content = "❐";
        }
    }

    private void CloseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }

    private void Portuguese_Click(
        object sender,
        RoutedEventArgs e)
    {
        LocalizationService.Instance
            .SetLanguage("pt-BR");
    }

    private void English_Click(
        object sender,
        RoutedEventArgs e)
    {
        LocalizationService.Instance
            .SetLanguage("en-US");
    }

    private void Spanish_Click(
        object sender,
        RoutedEventArgs e)
    {
        LocalizationService.Instance
            .SetLanguage("es-ES");
    }

    protected override void OnClosed(
        EventArgs e)
    {
        _liveDataTimer.Stop();

        _obdService.Dispose();

        base.OnClosed(e);
    }
}