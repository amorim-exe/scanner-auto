using OBDScanner.Models;
using System.Windows;
using System.Windows.Controls;

namespace OBD2Scanner;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
    }

    public void UpdateData(ObdData data)
    {
        RpmText.Text =
            data.RpmAvailable
                ? data.Rpm.ToString("N0")
                : "---";

        RpmProgress.Value =
            data.RpmAvailable
                ? Math.Clamp(data.Rpm, 0, 8000)
                : 0;

        SpeedText.Text =
            data.SpeedAvailable
                ? data.Speed.ToString("N0")
                : "---";

        CoolantText.Text =
            data.CoolantAvailable
                ? data.CoolantTemperature.ToString("N1")
                : "---";

        LoadText.Text =
            data.LoadAvailable
                ? data.EngineLoad.ToString("N1")
                : "---";

        FuelEconomyText.Text =
            data.FuelEconomyAvailable
                ? data.FuelEconomy.ToString("N1")
                : "---";
    }

    public void ClearData()
    {
        RpmText.Text = "---";
        SpeedText.Text = "---";
        CoolantText.Text = "---";
        LoadText.Text = "---";
        FuelEconomyText.Text = "---";
        RpmProgress.Value = 0;
    }
}