using System.Globalization;
using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services;

internal static class DeviceInfoParser
{
    public static MemoryInfo ParseMemoryInfo(string section)
    {
        long total = 0;
        long available = 0;
        long free = 0;
        long buffers = 0;
        long cached = 0;
        long swapTotal = 0;
        long swapFree = 0;

        foreach (var line in section.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("MemTotal:"))
            {
                total = KbToBytes(trimmed);
            }
            else if (trimmed.StartsWith("MemFree:"))
            {
                free = KbToBytes(trimmed);
            }
            else if (trimmed.StartsWith("MemAvailable:"))
            {
                available = KbToBytes(trimmed);
            }
            else if (trimmed.StartsWith("Buffers:"))
            {
                buffers = KbToBytes(trimmed);
            }
            else if (trimmed.StartsWith("Cached:"))
            {
                cached = KbToBytes(trimmed);
            }
            else if (trimmed.StartsWith("SwapTotal:"))
            {
                swapTotal = KbToBytes(trimmed);
            }
            else if (trimmed.StartsWith("SwapFree:"))
            {
                swapFree = KbToBytes(trimmed);
            }
        }

        var snapshot = new MemorySnapshot(total, available, free, buffers, cached, swapTotal, swapFree);
        return new MemoryInfo(snapshot, total);
    }

    public static DiskFreeInfo? ParseDiskFree(string section)
    {
        var dfLines = section.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (dfLines.Length < 2)
        {
            return null;
        }

        var cols = dfLines[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (cols.Length < 4)
        {
            return null;
        }

        var totalBytes = ParseSizeWithUnit(cols[1]);
        if (totalBytes > 0)
        {
            return new DiskFreeInfo(totalBytes);
        }

        return null;
    }

    public static string? ParseUptime(string section)
    {
        var uptimeOutput = section.Trim();
        var upIdx = uptimeOutput.IndexOf("up ", StringComparison.Ordinal);
        if (upIdx < 0)
        {
            return null;
        }

        var rest = uptimeOutput[(upIdx + 3)..];
        var commaIdx = rest.IndexOf(',');
        if (commaIdx > 0)
        {
            var afterFirst = rest[(commaIdx + 1)..];
            var secondComma = afterFirst.IndexOf(',');
            if (secondComma > 0 && afterFirst[..secondComma].Trim().Contains(':'))
            {
                return rest[..commaIdx].Trim();
            }

            if (secondComma > 0)
            {
                return rest[..(commaIdx + 1 + secondComma)].Trim().TrimEnd(',');
            }

            return rest[..commaIdx].Trim();
        }

        return rest.Trim();
    }

    public static ThermalSnapshot ParseThermal(string section)
    {
        float maxTemp = 0;
        var temps = new List<(string Name, float Value)>();
        var phase = 0; // 0=seeking temps, 1=in temps, 2=seeking cooling, 3=in cooling
        string? fanState = null;

        foreach (var line in section.Split('\n'))
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

                    var (tName, tValue) = ExtractMValueEntry(trimmed);
                    if (tValue is not null && float.TryParse(tValue,
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

                    var (_, cValue) = ExtractMValueEntry(trimmed);
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

    public static CpuSnapshot ParseCpuStat(string section)
    {
        long user = 0;
        long nice = 0;
        long system = 0;
        long idle = 0;
        long ioWait = 0;
        long irq = 0;
        long softIrq = 0;
        long steal = 0;
        var cores = new List<(string Name, long Active, long Total)>();

        foreach (var statLine in section.Split('\n'))
        {
            if (!statLine.StartsWith("cpu"))
            {
                continue;
            }

            var vals = statLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (vals.Length < 8)
            {
                continue;
            }

            long.TryParse(vals[1], out var u);
            long.TryParse(vals[2], out var n);
            long.TryParse(vals[3], out var s);
            long.TryParse(vals[4], out var id);
            long.TryParse(vals[5], out var w);
            long.TryParse(vals[6], out var q);
            long.TryParse(vals[7], out var sq);
            long st = 0;
            if (vals.Length >= 9)
            {
                long.TryParse(vals[8], out st);
            }

            var active = u + n + s + w + q + sq + st;
            var total = active + id;

            if (vals[0] == "cpu")
            {
                user = u;
                nice = n;
                system = s;
                idle = id;
                ioWait = w;
                irq = q;
                softIrq = sq;
                steal = st;
            }
            else
            {
                cores.Add((vals[0].ToUpperInvariant(), active, total));
            }
        }

        return new CpuSnapshot(user, nice, system, idle, ioWait, irq, softIrq, steal, cores);
    }

    public static int ParseLoadAverage(string section)
    {
        var fields = section.Trim().Split(' ');
        if (fields.Length < 4)
        {
            return 0;
        }

        var parts = fields[3].Split('/');
        if (parts.Length == 2 && int.TryParse(parts[1], out var threads))
        {
            return threads;
        }

        return 0;
    }

    public static int ParseProcessCount(string section)
    {
        var count = 0;
        foreach (var entry in section.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = entry.AsSpan().Trim();
            if (trimmed.Length > 0 && IsAllDigits(trimmed))
            {
                count++;
            }
        }

        return count;
    }

    public static NetworkSnapshot ParseNetDev(string section)
    {
        long bytesIn = 0, bytesOut = 0, packetsIn = 0, packetsOut = 0;

        foreach (var line in section.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var colonIdx = line.IndexOf(':');
            if (colonIdx < 0)
            {
                continue;
            }

            var iface = line[..colonIdx].Trim();
            if (iface == "lo" || iface.StartsWith("Inter") || iface.StartsWith("face"))
            {
                continue;
            }

            var vals = line[(colonIdx + 1)..].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (vals.Length < 10)
            {
                continue;
            }

            if (long.TryParse(vals[0], out var rxBytes))
            {
                bytesIn += rxBytes;
            }

            if (long.TryParse(vals[1], out var rxPackets))
            {
                packetsIn += rxPackets;
            }

            if (long.TryParse(vals[8], out var txBytes))
            {
                bytesOut += txBytes;
            }

            if (long.TryParse(vals[9], out var txPackets))
            {
                packetsOut += txPackets;
            }
        }

        return new NetworkSnapshot(bytesIn, bytesOut, packetsIn, packetsOut);
    }

    public static (long BytesRead, long BytesWritten) ParseVmstat(string section)
    {
        long bytesRead = 0, bytesWritten = 0;

        foreach (var line in section.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Trim().Split(' ');
            if (parts.Length < 2)
            {
                continue;
            }

            // pgpgin/pgpgout are in KB
            if (parts[0] == "pgpgin" && long.TryParse(parts[1], out var pgIn))
            {
                bytesRead = pgIn * 1024;
            }
            else if (parts[0] == "pgpgout" && long.TryParse(parts[1], out var pgOut))
            {
                bytesWritten = pgOut * 1024;
            }
        }

        return (bytesRead, bytesWritten);
    }

    public static DiskSnapshot ParseDiskStats(string section, long bytesRead, long bytesWritten)
    {
        var writeLatencyMs = 0;
        long writeSpeed = 0;

        foreach (var line in section.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("Latency:"))
            {
                var msIdx = trimmed.IndexOf("ms", StringComparison.Ordinal);
                if (msIdx > 0)
                {
                    var numStr = trimmed["Latency:".Length..msIdx].Trim();
                    if (int.TryParse(numStr, out var ms))
                    {
                        writeLatencyMs = ms;
                    }
                }
            }
            else if (trimmed.StartsWith("Recent Disk Write Speed"))
            {
                var eqIdx = trimmed.IndexOf('=');
                if (eqIdx <= 0)
                {
                    continue;
                }

                var numStr = trimmed[(eqIdx + 1)..].Trim();
                if (long.TryParse(numStr, out var speed))
                {
                    writeSpeed = speed * 1024; // KB/s → bytes/s
                }
            }
        }

        return new DiskSnapshot(bytesRead, bytesWritten, writeLatencyMs, writeSpeed);
    }

    private static long ParseSizeWithUnit(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        // Plain number = KB (df default)
        if (long.TryParse(value, out var plain))
        {
            return plain * 1024;
        }

        // Suffixed: e.g. "12G", "8.3M", "512K", "1T"
        var suffix = char.ToUpperInvariant(value[^1]);
        var numPart = value[..^1];
        if (!double.TryParse(numPart, CultureInfo.InvariantCulture, out var num))
        {
            return 0;
        }

        return suffix switch
        {
            'K' => (long)(num * 1024),
            'M' => (long)(num * 1024 * 1024),
            'G' => (long)(num * 1024 * 1024 * 1024),
            'T' => (long)(num * 1024L * 1024 * 1024 * 1024),
            _ => 0
        };
    }

    private static long KbToBytes(string memLine)
    {
        var parts = memLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 && long.TryParse(parts[1], out var kb) ? kb * 1024 : 0;
    }

    private static (string Name, string? Value) ExtractMValueEntry(string trimmed)
    {
        var mValueIdx = trimmed.IndexOf("mValue=", StringComparison.Ordinal);
        var valueStr = trimmed[(mValueIdx + "mValue=".Length)..];
        var endIdx = valueStr.IndexOfAny([',', ' ', '}']);
        if (endIdx > 0)
        {
            valueStr = valueStr[..endIdx];
        }

        var nameStr = "Unknown";
        var mNameIdx = trimmed.IndexOf("mName=", StringComparison.Ordinal);
        if (mNameIdx >= 0)
        {
            var nameVal = trimmed[(mNameIdx + "mName=".Length)..];
            var nameEnd = nameVal.IndexOfAny([',', ' ', '}']);
            if (nameEnd > 0)
            {
                nameStr = nameVal[..nameEnd];
            }
        }

        return (nameStr, valueStr);
    }

    private static bool IsAllDigits(ReadOnlySpan<char> span)
    {
        foreach (var c in span)
        {
            if (!char.IsDigit(c))
            {
                return false;
            }
        }

        return true;
    }
}
