using OBDScanner.Services;
using System.Windows;
using System.Windows.Controls;

namespace OBD2Scanner;

public partial class DiagnosticsView : UserControl
{
    private readonly ObdService _obdService;

    public DiagnosticsView(ObdService obdService)
    {
        InitializeComponent();

        _obdService = obdService;
    }

    private void ReadButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_obdService.IsConnected)
        {
            StatusText.Text = "Adapter disconnected";
            return;
        }

        try
        {
            var codes = _obdService.ReadDiagnosticCodes();

            CodesList.Items.Clear();

            if (codes.Count == 0)
            {
                CodesList.Items.Add("No diagnostic trouble codes found.");
                StatusText.Text = "No faults";
                return;
            }

            foreach (var code in codes)
                CodesList.Items.Add(code);

            StatusText.Text = $"{codes.Count} fault code(s)";
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
        }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_obdService.IsConnected)
        {
            StatusText.Text = "Adapter disconnected";
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
                CodesList.Items.Add("Diagnostic codes cleared.");
                StatusText.Text = "Cleared";
            }
            else
            {
                StatusText.Text = "Unable to clear codes";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
        }
    }
}