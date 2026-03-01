using System.Globalization;
using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Commands;

internal sealed class ThermalCommand : IAdbShellCommand<ThermalSnapshot>
{
    public string Name => nameof(DynamicSections.Thermal);

    public string CommandText => "dumpsys thermalservice";

    public ThermalSnapshot Parse(string output)
    {
        float maxTemp = 0;
        var temps = new List<(string Name, float Value)>();
        var phase = 0; // 0=seeking temps, 1=in temps, 2=seeking cooling, 3=in cooling
        string? fanState = null;

        foreach (var line in output.Split('\n'))
        {
            var trimmed = line.Trim();

            switch (phase)
            {
                case 0:
                    if (trimmed.StartsWith("Current temperatures from HAL"))
                    {
                        phase = 1;
                    }

                    break;

                case 1:
                    if (trimmed.StartsWith("Current cooling"))
                    {
                        phase = 3; // jump directly into cooling
                        break;
                    }

                    if (!trimmed.Contains("mValue="))
                    {
                        break;
                    }

                    var (tName, tValue) = ParseHelper.ExtractMValueEntry(trimmed);
                    if (tValue is not null &&
                        float.TryParse(tValue,
                            CultureInfo.InvariantCulture, out var temp))
                    {
                        temps.Add((tName, temp));
                        if (temp > maxTemp)
                        {
                            maxTemp = temp;
                        }
                    }

                    break;

                case 2:
                {
                    if (trimmed.StartsWith("Current cooling devices from HAL"))
                    {
                        phase = 3;
                    }

                    break;
                }

                case 3:
                    if (trimmed.Length > 0 && !trimmed.Contains("mValue="))
                    {
                        phase = 4; // done
                        break;
                    }

                    if (!trimmed.Contains("mValue="))
                    {
                        break;
                    }

                    var (_, cValue) = ParseHelper.ExtractMValueEntry(trimmed);
                    if (cValue is not null && int.TryParse(cValue, out var fanLevel))
                    {
                        fanState = fanLevel > 0 ? $"Active (Level {fanLevel})" : "Off";
                        phase = 4; // Shield has one fan
                    }

                    break;
            }

            if (phase == 4)
            {
                break;
            }
        }

        string? summary = null;
        List<(string Name, double Value)> zones = [];

        if (temps.Count > 0)
        {
            summary = string.Join(", ", temps.Select(t => $"{t.Name}: {t.Value:F1}°C"));
            zones = temps.Select(t => (t.Name, (double)t.Value)).ToList();
        }

        return new ThermalSnapshot(summary, zones, fanState);
    }

    public void Apply(string output, DynamicSections target)
        => target.Thermal = Parse(output);
}
