using System.Globalization;
using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Queries;

internal sealed class ThermalQuery : IAdbShellQuery<ThermalSnapshot>
{
    public string Name => nameof(DynamicSections.Thermal);

    public string CommandText => "dumpsys thermalservice";

    public ThermalSnapshot Parse(ReadOnlySpan<char> output)
    {
        float maxTemp = 0;
        var temps = new List<(string Name, float Value)>();
        var inTemps = false;

        foreach (var line in output.EnumerateLines())
        {
            var trimmed = line.Trim();

            if (!inTemps)
            {
                if (trimmed.StartsWith("Current temperatures from HAL"))
                {
                    inTemps = true;
                }

                continue;
            }

            if (trimmed.IndexOf("mValue=") < 0)
            {
                break;
            }

            trimmed.ExtractDumpsysFields(out var tName, out var tValue);
            if (float.TryParse(tValue, CultureInfo.InvariantCulture, out var temp))
            {
                temps.Add((tName.ToString(), temp));
                if (temp > maxTemp)
                {
                    maxTemp = temp;
                }
            }
        }

        string? summary = null;
        List<(string Name, double Value)> zones = [];

        if (temps.Count > 0)
        {
            summary = string.Join(", ", temps.Select(t => $"{t.Name}: {t.Value:F1}°C"));
            zones = temps.Select(t => (t.Name, (double)t.Value)).ToList();
        }

        return new ThermalSnapshot(summary, zones);
    }

    public void Apply(ReadOnlySpan<char> output, DynamicSections target)
        => target.Thermal = Parse(output);
}
