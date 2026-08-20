namespace OBDScanner.Models;

public static class ObdPid
{
    public const int MonitorStatus = 0x01;
    public const int EngineLoad = 0x04;
    public const int CoolantTemperature = 0x05;
    public const int ShortFuelTrimBank1 = 0x06;
    public const int LongFuelTrimBank1 = 0x07;
    public const int ShortFuelTrimBank2 = 0x08;
    public const int LongFuelTrimBank2 = 0x09;
    public const int FuelPressure = 0x0A;
    public const int Map = 0x0B;
    public const int Rpm = 0x0C;
    public const int Speed = 0x0D;
    public const int TimingAdvance = 0x0E;
    public const int IntakeAirTemperature = 0x0F;
    public const int Maf = 0x10;
    public const int ThrottlePosition = 0x11;

    public const int O2Sensor1 = 0x14;
    public const int O2Sensor2 = 0x15;
    public const int O2Sensor3 = 0x16;
    public const int O2Sensor4 = 0x17;
    public const int O2Sensor5 = 0x18;
    public const int O2Sensor6 = 0x19;
    public const int O2Sensor7 = 0x1A;
    public const int O2Sensor8 = 0x1B;

    public const int EngineRuntime = 0x1F;
    public const int DistanceWithMil = 0x21;

    public const int EvapPurge = 0x2E;
    public const int FuelLevel = 0x2F;
    public const int FuelRailPressure = 0x23;

    public const int BarometricPressure = 0x33;

    public const int ControlModuleVoltage = 0x42;
    public const int RelativeThrottlePosition = 0x45;
    public const int AmbientTemperature = 0x46;
    public const int RelativeAcceleratorPosition = 0x47;

    public const int AcceleratorPositionD = 0x49;
    public const int AcceleratorPositionE = 0x4A;
    public const int CommandedThrottleActuator = 0x4C;

    public const int FuelType = 0x51;
    public const int EthanolFuelPercentage = 0x52;

    public const int OilTemperature = 0x5C;
    public const int FuelRate = 0x5E;

    public const int DriverDemandTorque = 0x61;
    public const int EnginePercentTorque = 0x62;
    public const int EngineReferenceTorque = 0x63;
}