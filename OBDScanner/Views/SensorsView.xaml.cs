using OBDScanner.Models;
using System.Windows.Controls;

namespace OBD2Scanner;

public partial class SensorsView : UserControl
{
    public SensorsView()
    {
        InitializeComponent();
    }

    public void UpdateData(ObdData data)
    {
        ThrottleText.Text =
            data.ThrottleAvailable
                ? data.ThrottlePosition.ToString("N1")
                : "---";

        MapText.Text =
            data.MapAvailable
                ? data.MapPressure.ToString("N0")
                : "---";

        IatText.Text =
            data.IatAvailable
                ? data.IntakeAirTemperature.ToString("N1")
                : "---";

        StftText.Text =
            data.StftAvailable
                ? $"{data.StftBank1:+0.0;-0.0;0.0}"
                : "---";

        LtftText.Text =
            data.LtftAvailable
                ? $"{data.LtftBank1:+0.0;-0.0;0.0}"
                : "---";

        O2Text.Text =
            data.O2Sensor1Available
                ? data.O2Sensor1Voltage.ToString("N2")
                : "---";

        O2TrimText.Text =
            data.O2Sensor1Available
                ? $"Trim: {data.O2Sensor1Trim:+0.0;-0.0;0.0}%"
                : "Trim: ---";

        MafText.Text =
            data.MafAvailable
                ? data.Maf.ToString("N1")
                : "---";

        FuelRateText.Text =
            data.FuelRateAvailable
                ? data.FuelRate.ToString("N1")
                : "---";

        FuelConsumptionText.Text =
            data.FuelConsumptionAvailable
                ? data.FuelConsumption.ToString("N1")
                : "---";
    }

    public void ClearData()
    {
        ThrottleText.Text = "---";
        MapText.Text = "---";
        IatText.Text = "---";
        StftText.Text = "---";
        LtftText.Text = "---";
        O2Text.Text = "---";
        O2TrimText.Text = "Trim: ---";
        MafText.Text = "---";
        FuelRateText.Text = "---";
        FuelConsumptionText.Text = "---";
    }
}