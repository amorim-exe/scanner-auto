using OBDScanner.Models;
using System.Globalization;
using System.IO.Ports;
using System.Text.RegularExpressions;

namespace OBDScanner.Services;

public class ObdService : IDisposable
{
    private SerialPort? _serialPort;

    private readonly object _serialLock = new();

    public bool IsConnected =>
        _serialPort?.IsOpen == true;

    public void Connect(string portName)
    {
        Disconnect();

        _serialPort = new SerialPort(portName, 38400)
        {
            ReadTimeout = 3000,
            WriteTimeout = 3000,
            NewLine = "\r",
            DtrEnable = true,
            RtsEnable = true
        };

        _serialPort.Open();

        InitializeElm327();
    }

    private void InitializeElm327()
    {
        SendCommand("ATZ");

        Thread.Sleep(1200);

        SendCommand("ATE0");
        SendCommand("ATL0");
        SendCommand("ATS0");
        SendCommand("ATH0");

        // Protocolo confirmado no veículo durante o teste real.
        SendCommand("ATSP3");

        Thread.Sleep(500);
    }

    /// <summary>
    /// Lê todos os parâmetros disponíveis do veículo.
    /// Não depende da tela atualmente aberta.
    /// </summary>
    public ObdData ReadLiveData()
    {
        var data = new ObdData();

        if (!IsConnected)
            return data;

        // Motor
        ReadRpm(data);
        ReadSpeed(data);
        ReadCoolant(data);
        ReadEngineLoad(data);

        // Admissão / acelerador
        ReadThrottle(data);
        ReadMap(data);
        ReadIat(data);

        // Combustível
        ReadStft(data);
        ReadLtft(data);
        ReadMaf(data);
        ReadFuelRate(data);

        // Lambda / O2
        ReadOxygenSensor1(data);
        ReadOxygenSensor2(data);

        CalculateFuelConsumption(data);

        return data;
    }

    /// <summary>
    /// Executa a leitura em uma thread de background.
    /// A interface WPF não fica bloqueada enquanto o ELM327 responde.
    /// </summary>
    public Task<ObdData> ReadLiveDataAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.Run(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                return ReadLiveData();
            },
            cancellationToken);
    }

    private void ReadRpm(ObdData data)
    {
        var bytes = ReadPid("010C");

        if (bytes.Length >= 2)
        {
            data.Rpm =
                ((bytes[0] * 256) + bytes[1]) / 4;

            data.RpmAvailable = true;
        }
    }

    private void ReadSpeed(ObdData data)
    {
        var bytes = ReadPid("010D");

        if (bytes.Length >= 1)
        {
            data.Speed = bytes[0];

            data.SpeedAvailable = true;
        }
    }

    private void ReadCoolant(ObdData data)
    {
        var bytes = ReadPid("0105");

        if (bytes.Length >= 1)
        {
            data.CoolantTemperature =
                bytes[0] - 40;

            data.CoolantAvailable = true;
        }
    }

    private void ReadEngineLoad(ObdData data)
    {
        var bytes = ReadPid("0104");

        if (bytes.Length >= 1)
        {
            data.EngineLoad =
                bytes[0] * 100.0 / 255.0;

            data.LoadAvailable = true;
        }
    }

    private void ReadThrottle(ObdData data)
    {
        var bytes = ReadPid("0111");

        if (bytes.Length >= 1)
        {
            data.ThrottlePosition =
                bytes[0] * 100.0 / 255.0;

            data.ThrottleAvailable = true;
        }
    }

    private void ReadMap(ObdData data)
    {
        var bytes = ReadPid("010B");

        if (bytes.Length >= 1)
        {
            data.MapPressure = bytes[0];

            data.MapAvailable = true;
        }
    }

    private void ReadIat(ObdData data)
    {
        var bytes = ReadPid("010F");

        if (bytes.Length >= 1)
        {
            data.IntakeAirTemperature =
                bytes[0] - 40;

            data.IatAvailable = true;
        }
    }

    private void ReadStft(ObdData data)
    {
        var bytes = ReadPid("0106");

        if (bytes.Length >= 1)
        {
            data.StftBank1 =
                (bytes[0] - 128) * 100.0 / 128.0;

            data.StftAvailable = true;
        }
    }

    private void ReadLtft(ObdData data)
    {
        var bytes = ReadPid("0107");

        if (bytes.Length >= 1)
        {
            data.LtftBank1 =
                (bytes[0] - 128) * 100.0 / 128.0;

            data.LtftAvailable = true;
        }
    }

    private void ReadMaf(ObdData data)
    {
        var bytes = ReadPid("0110");

        if (bytes.Length >= 2)
        {
            data.Maf =
                ((bytes[0] * 256) + bytes[1]) / 100.0;

            data.MafAvailable = true;
        }
    }

    private void ReadOxygenSensor1(ObdData data)
    {
        var bytes = ReadPid("0114");

        if (bytes.Length >= 2)
        {
            data.O2Sensor1Voltage =
                bytes[0] / 200.0;

            data.O2Sensor1Trim =
                (bytes[1] - 128) * 100.0 / 128.0;

            data.O2Sensor1Available = true;
        }
    }

    private void ReadOxygenSensor2(ObdData data)
    {
        var bytes = ReadPid("0115");

        if (bytes.Length >= 2)
        {
            data.O2Sensor2Voltage =
                bytes[0] / 200.0;

            data.O2Sensor2Trim =
                (bytes[1] - 128) * 100.0 / 128.0;

            data.O2Sensor2Available = true;
        }
    }

    private void ReadFuelRate(ObdData data)
    {
        var bytes = ReadPid("015E");

        if (bytes.Length >= 2)
        {
            data.FuelRate =
                ((bytes[0] * 256) + bytes[1]) / 20.0;

            data.FuelRateAvailable = true;
        }
    }

    private void CalculateFuelConsumption(ObdData data)
    {
        if (data.FuelRateAvailable &&
            data.FuelRate > 0)
        {
            data.FuelConsumption =
                data.FuelRate;

            data.FuelConsumptionAvailable = true;

            if (data.SpeedAvailable &&
                data.Speed > 0)
            {
                data.FuelEconomy =
                    data.Speed / data.FuelRate;

                data.FuelEconomyAvailable = true;
            }

            return;
        }

        if (data.MafAvailable &&
            data.Maf > 0)
        {
            data.FuelConsumption =
                data.Maf * 0.068;

            data.FuelConsumptionAvailable = true;

            if (data.SpeedAvailable &&
                data.Speed > 0 &&
                data.FuelConsumption > 0)
            {
                data.FuelEconomy =
                    data.Speed / data.FuelConsumption;

                data.FuelEconomyAvailable = true;
            }
        }
    }

    public DiagnosticSample ReadDiagnosticSampleFast(
    string sessionId,
    string stage)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException(
                "OBD adapter is not connected.");
        }

        var sample = new DiagnosticSample
        {
            Timestamp = DateTime.Now,
            SessionId = sessionId,
            TestStage = stage
        };

        var rpm = ReadPid("010C");

        if (rpm.Length >= 2)
            sample.RPM =
                ((rpm[0] * 256) + rpm[1]) / 4.0;

        var ect = ReadPid("0105");

        if (ect.Length >= 1)
            sample.ECT =
                ect[0] - 40;

        var map = ReadPid("010B");

        if (map.Length >= 1)
            sample.MAP =
                map[0];

        var tps = ReadPid("0111");

        if (tps.Length >= 1)
            sample.TPS =
                tps[0] * 100.0 / 255.0;

        var stft = ReadPid("0106");

        if (stft.Length >= 1)
            sample.STFT =
                (stft[0] - 128) * 100.0 / 128.0;

        var ltft = ReadPid("0107");

        if (ltft.Length >= 1)
            sample.LTFT =
                (ltft[0] - 128) * 100.0 / 128.0;

        var o2 = ReadPid("0114");

        if (o2.Length >= 1)
            sample.O2B1S1 =
                o2[0] / 200.0;

        var load = ReadPid("0104");

        if (load.Length >= 1)
            sample.EngineLoad =
                load[0] * 100.0 / 255.0;

        sample.FuelSystemStatus =
            ReadFuelSystemStatus();

        return sample;
    }

    public DiagnosticSample ReadDiagnosticSample(
    string sessionId,
    string stage)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException(
                "OBD adapter is not connected.");
        }

        var data = ReadLiveData();

        var sample = new DiagnosticSample
        {
            Timestamp = DateTime.Now,
            SessionId = sessionId,
            TestStage = stage,

            RPM = data.RpmAvailable
                ? data.Rpm
                : null,

            Speed = data.SpeedAvailable
                ? data.Speed
                : null,

            ECT = data.CoolantAvailable
                ? data.CoolantTemperature
                : null,

            IAT = data.IatAvailable
                ? data.IntakeAirTemperature
                : null,

            MAP = data.MapAvailable
                ? data.MapPressure
                : null,

            MAF = data.MafAvailable
                ? data.Maf
                : null,

            TPS = data.ThrottleAvailable
                ? data.ThrottlePosition
                : null,

            STFT = data.StftAvailable
                ? data.StftBank1
                : null,

            LTFT = data.LtftAvailable
                ? data.LtftBank1
                : null,

            O2B1S1 = data.O2Sensor1Available
                ? data.O2Sensor1Voltage
                : null,

            O2B1S2 = data.O2Sensor2Available
                ? data.O2Sensor2Voltage
                : null,

            EngineLoad = data.LoadAvailable
                ? data.EngineLoad
                : null,

            FuelSystemStatus = ReadFuelSystemStatus(),

            FuelPressure = ReadFuelPressure(),

            TimingAdvance = ReadTimingAdvance()
        };

        return sample;
    }

    public string ReadFuelSystemStatus()
    {
        var bytes = ReadPid("0103");

        if (bytes.Length < 1)
            return string.Empty;

        return bytes[0] switch
        {
            0x01 => "OPEN_LOOP_COLD",
            0x02 => "CLOSED_LOOP",
            0x04 => "OPEN_LOOP_LOAD",
            0x08 => "OPEN_LOOP_DECELERATION",
            0x10 => "OPEN_LOOP_SYSTEM_FAILURE",
            _ => $"UNKNOWN_0x{bytes[0]:X2}"
        };
    }

    public double? ReadFuelPressure()
    {
        var bytes = ReadPid("010A");

        if (bytes.Length < 1)
            return null;

        // Fuel pressure PID 0x0A = 3 * A kPa
        return bytes[0] * 3.0;
    }

    public double? ReadTimingAdvance()
    {
        var bytes = ReadPid("010E");

        if (bytes.Length < 1)
            return null;

        // Timing advance = A / 2 - 64
        return bytes[0] / 2.0 - 64.0;
    }

    private byte[] ReadPid(string command)
    {
        try
        {
            if (!IsConnected)
                return [];

            var response = SendCommand(command);

            return ExtractPidBytes(
                response,
                command);
        }
        catch
        {
            return [];
        }
    }

    private byte[] ExtractPidBytes(
        string response,
        string command)
    {
        if (string.IsNullOrWhiteSpace(response))
            return [];

        var pid =
            command
                .Substring(2, 2)
                .ToUpperInvariant();

        /*
         * O ELM327 está configurado com ATS0.
         *
         * Portanto respostas como:
         *
         * 410C0DE8
         * 1057F
         * 10D00
         *
         * podem chegar sem espaços.
         *
         * Procuramos diretamente pelo:
         *
         * 41 + PID
         */

        var compact =
            Regex.Replace(
                response.ToUpperInvariant(),
                @"[^0-9A-F]",
                "");

        var marker = "41" + pid;

        var index =
            compact.IndexOf(
                marker,
                StringComparison.Ordinal);

        if (index < 0)
            return [];

        var dataStart =
            index + marker.Length;

        if (dataStart >= compact.Length)
            return [];

        var dataHex =
            compact.Substring(dataStart);

        var bytes =
            new List<byte>();

        for (
            var i = 0;
            i + 1 < dataHex.Length;
            i += 2)
        {
            var pair =
                dataHex.Substring(i, 2);

            if (!byte.TryParse(
                    pair,
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture,
                    out var value))
            {
                break;
            }

            bytes.Add(value);
        }

        return bytes.ToArray();
    }

    public List<string> ReadDiagnosticCodes()
    {
        if (!IsConnected)
            return [];

        try
        {
            var response =
                SendCommand("03");

            return ParseDiagnosticCodes(response);
        }
        catch
        {
            return [];
        }
    }

    public bool ClearDiagnosticCodes()
    {
        if (!IsConnected)
            return false;

        try
        {
            SendCommand("04");

            return true;
        }
        catch
        {
            return false;
        }
    }

    public string SendCommand(string command)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException(
                "OBD adapter is not connected.");
        }

        lock (_serialLock)
        {
            _serialPort!.DiscardInBuffer();

            _serialPort.Write(
                command + "\r");

            return ReadResponse();
        }
    }

    private string ReadResponse()
    {
        if (!IsConnected)
            return string.Empty;

        var start =
            DateTime.UtcNow;

        while (
            (DateTime.UtcNow - start).TotalMilliseconds < 3000)
        {
            var response =
                _serialPort!.ReadExisting();

            if (!string.IsNullOrWhiteSpace(response) &&
                response.Contains('>'))
            {
                return response;
            }

            Thread.Sleep(20);
        }

        return _serialPort!.ReadExisting();
    }

    private List<string> ParseDiagnosticCodes(
        string response)
    {
        var compact =
            Regex.Replace(
                response.ToUpperInvariant(),
                @"[^0-9A-F]",
                "");

        var result =
            new List<string>();

        var index =
            compact.IndexOf(
                "43",
                StringComparison.Ordinal);

        if (index < 0)
            return result;

        var start =
            index + 2;

        for (
            var i = start;
            i + 3 < compact.Length;
            i += 4)
        {
            var hex =
                compact.Substring(i, 4);

            if (!ushort.TryParse(
                    hex,
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture,
                    out var raw))
            {
                break;
            }

            var a =
                (byte)(raw >> 8);

            var b =
                (byte)(raw & 0xFF);

            if (a == 0 && b == 0)
                continue;

            result.Add(
                ConvertToDtc(a, b));
        }

        return result;
    }

    private string ConvertToDtc(
        byte a,
        byte b)
    {
        var first =
            (a >> 6) & 0x03;

        var prefix =
            first switch
            {
                0 => "P",
                1 => "C",
                2 => "B",
                3 => "U",
                _ => "P"
            };

        var digit =
            (a >> 4) & 0x03;

        return
            prefix +
            digit.ToString(
                CultureInfo.InvariantCulture) +
            (a & 0x0F).ToString("X1") +
            b.ToString("X2");
    }

    public void Disconnect()
    {
        lock (_serialLock)
        {
            try
            {
                if (_serialPort?.IsOpen == true)
                    _serialPort.Close();
            }
            catch
            {
            }

            try
            {
                _serialPort?.Dispose();
            }
            catch
            {
            }

            _serialPort = null;
        }
    }

    public void Dispose()
    {
        Disconnect();
    }
}