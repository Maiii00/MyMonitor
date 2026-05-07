using LibreHardwareMonitor.Hardware;
using Microsoft.Extensions.Logging;

namespace MyMonitor.Services;

public class HardwareInfo
{
    public string Name { get; set; } = string.Empty;
    public string SensorName { get; set; } = string.Empty;
    public float Value { get; set; }
    public string SensorType { get; set; } = string.Empty;

    public string DisplayName => SensorName.Contains("|")
        ? SensorName.Split('|').Last().Trim()
        : SensorName;

    public string Category => SensorName.Contains("|")
        ? SensorName.Split('|').First().Trim()
        : SensorType;

    public string FormattedValue => FormatBySensorType();

    private string FormatBySensorType()
    {
        if (Value < 0) return "N/A";

        return SensorType switch
        {
            "Temperature" => $"{Value:0.0} °C",
            "Load" => $"{Value:0.0} %",
            "Control" => $"{Value:0.0} %",
            "Fan" => $"{Value:0} RPM",
            "Voltage" => $"{Value:0.00} V",
            "Power" => $"{Value:0.0} W",
            "Clock" => Value >= 1000 ? $"{Value / 1000:0.00} GHz" : $"{Value:0} MHz",
            "Data" or "SmallData" => Value >= 1024 ? $"{Value / 1024:0} GB" : $"{Value:0} MB",
            _ => $"{Value:0.0}"
        };
    }
}

public class HardwareService : IDisposable
{
    private readonly Computer _computer;
    private readonly ILogger<HardwareService> _logger;

    public HardwareService(ILogger<HardwareService> logger)
    {
        _logger = logger;
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = true,
            IsControllerEnabled = true
        };

        _computer.Open();
        _logger.LogInformation("Hardware monitoring started");
    }

    public List<HardwareInfo> GetMetrics()
    {
        var metrics = new List<HardwareInfo>();

        if (_computer.Hardware.Count() == 0)
        {
            _logger.LogWarning("Nothing detected");
        }

        foreach (IHardware hardware in _computer.Hardware)
        {
            hardware.Update();
            foreach (ISensor sensor in hardware.Sensors)
            {
                if (IsGarbageSensor(sensor)) continue;

                metrics.Add(new HardwareInfo
                {
                    Name = hardware.Name,
                    SensorName = sensor.Name,
                    Value = sensor.Value ?? -1,
                    SensorType = sensor.SensorType.ToString()
                });
            }
        }

        return metrics;
    }

    private bool IsGarbageSensor(ISensor sensor)
    {
        if (sensor.Value == null) return true;

        var name = sensor.Name.ToLower();

        if (name.Contains("d3d") ||
            name.Contains("effective") ||
            name.Contains("smu") ||
            name.Contains("vid") ||
            name.Contains("factor"))
            return true;

        return false;
    }

    public void Dispose()
    {
        _logger.LogInformation("Disposed");
        _computer.Close();
    }
}