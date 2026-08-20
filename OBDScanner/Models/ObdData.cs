namespace OBDScanner.Models;

public class ObdData
{
    public int Rpm { get; set; }
    public int Speed { get; set; }
    public double CoolantTemperature { get; set; }
    public double EngineLoad { get; set; }
    public double ThrottlePosition { get; set; }
    public double MapPressure { get; set; }
    public double IntakeAirTemperature { get; set; }
    public double StftBank1 { get; set; }
    public double LtftBank1 { get; set; }
    public double O2Sensor1Voltage { get; set; }
    public double O2Sensor1Trim { get; set; }
    public double O2Sensor2Voltage { get; set; }
    public double O2Sensor2Trim { get; set; }
    public double Maf { get; set; }
    public double FuelRate { get; set; }
    public double FuelConsumption { get; set; }
    public double FuelEconomy { get; set; }
    public bool RpmAvailable { get; set; }
    public bool SpeedAvailable { get; set; }
    public bool CoolantAvailable { get; set; }
    public bool LoadAvailable { get; set; }
    public bool ThrottleAvailable { get; set; }
    public bool MapAvailable { get; set; }
    public bool IatAvailable { get; set; }
    public bool StftAvailable { get; set; }
    public bool LtftAvailable { get; set; }
    public bool O2Sensor1Available { get; set; }
    public bool O2Sensor2Available { get; set; }
    public bool MafAvailable { get; set; }
    public bool FuelRateAvailable { get; set; }
    public bool FuelConsumptionAvailable { get; set; }
    public bool FuelEconomyAvailable { get; set; }
}