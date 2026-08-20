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