using System.Diagnostics;
using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services;

public sealed class AdbService
{
    private string _adbPath;
    private AdbShellSession? _session;

    public AdbService(string? adbPath = null)
    {
        _adbPath = adbPath
                   ?? AppSettingsAccessor.Settings.AdbPath
                   ?? FindAdb();
    }

    public string ResolvedPath => _adbPath;

    public void SetAdbPath(string? path)
    {
        _adbPath = string.IsNullOrWhiteSpace(path) ? FindAdb() : path;
    }

    public static string FindAdb()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var exe = OperatingSystem.IsWindows() ? "adb.exe" : "adb";

        var candidates = new List<string>();

        if (OperatingSystem.IsWindows())
        {
            candidates.Add(Path.Combine(localAppData, "Android", "Sdk", "platform-tools", exe));
            candidates.Add(Path.Combine(home, "AppData", "Local", "Android", "Sdk", "platform-tools", exe));
        }
        else
        {
            candidates.Add("/opt/homebrew/bin/adb");
            candidates.Add("/usr/local/bin/adb");
            candidates.Add(Path.Combine(home, "Library", "Android", "sdk", "platform-tools", exe));
            candidates.Add(Path.Combine(home, "Android", "Sdk", "platform-tools", exe));
        }

        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        return exe;
    }

    public async Task OpenSessionAsync(string? deviceSerial = null)
    {
        CloseSession();
        _session = new AdbShellSession(_adbPath, deviceSerial);
        await _session.OpenAsync();
    }

    public void CloseSession()
    {
        _session?.Dispose();
        _session = null;
    }

    private async Task<string?> RunShellAsync(string command, CancellationToken ct = default)
    {
        if (_session is not null)
        {
            return await _session.RunAsync(command, ct);
        }

        return null;
    }

    /// Runs a command via the persistent shell session if available, otherwise falls back
    /// to a one-off adb shell process. Use for polling commands that benefit from session reuse.
    private async Task<string> RunShellWithFallbackAsync(string command, string adbPrefix)
    {
        if (_session is not null)
        {
            var output = await RunShellAsync(command);
            if (output is not null)
            {
                return output;
            }
        }

        var result = await RunAdbAsync($"{adbPrefix} \"{command}\"", strictCheck: false);
        return result.Output ?? "";
    }

    public async Task<AdbResult> ConnectAsync(string ipAddress, int port = 5555)
    {
        return await RunAdbAsync($"connect {ipAddress}:{port}");
    }

    public async Task<AdbResult> DisconnectAsync(string ipAddress)
    {
        return await RunAdbAsync($"disconnect {ipAddress}");
    }

    public async Task<AdbResult> DisconnectAllAsync()
    {
        return await RunAdbAsync("disconnect");
    }

    public async Task<List<ShieldDevice>> GetConnectedDevicesAsync()
    {
        var result = await RunAdbAsync("devices -l");
        var devices = new List<ShieldDevice>();

        if (!result.Success)
        {
            return devices;
        }

        foreach (var line in result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.StartsWith("List of") || line.StartsWith("*"))
            {
                continue;
            }

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 || parts[1] != "device")
            {
                continue;
            }

            var address = parts[0];
            var model = parts
                .FirstOrDefault(p => p.StartsWith("model:"))
                ?.Replace("model:", "");

            devices.Add(new ShieldDevice(address, model, IsConnected: true));
        }

        return devices;
    }

    public async Task<List<InstalledPackage>> GetInstalledPackagesAsync(string? deviceSerial = null)
    {
        var deviceArg = deviceSerial != null ? $"-s {deviceSerial}" : "";
        var result = await RunAdbAsync($"{deviceArg} shell pm list packages -3".Trim());
        var packages = new List<InstalledPackage>();

        if (!result.Success)
        {
            return packages;
        }

        var packageNames = result.Output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Replace("package:", "").Trim())
            .Where(n => !string.IsNullOrEmpty(n))
            .ToList();

        var infoTasks = packageNames.Select(name => GetPackageInfoAsync(name, deviceSerial));
        packages.AddRange(await Task.WhenAll(infoTasks));

        return packages.OrderBy(p => p.PackageName).ToList();
    }

    public async Task<InstalledPackage> GetPackageInfoAsync(
        string packageName, string? deviceSerial = null, bool includeSize = false)
    {
        var deviceArg = deviceSerial != null ? $"-s {deviceSerial}" : "";
        var result = await RunAdbAsync($"{deviceArg} shell dumpsys package {packageName}".Trim());

        if (!result.Success)
        {
            return new InstalledPackage(packageName);
        }

        string? versionName = null;
        string? versionCode = null;
        string? installerPackageName = null;
        string? firstInstallTime = null;
        string? lastUpdateTime = null;
        string? targetSdk = null;
        string? minSdk = null;
        string? dataDir = null;
        string? uid = null;
        string? codePath = null;

        foreach (var line in result.Output.Split('\n'))
        {
            var trimmed = line.Trim();

            if (versionName == null && trimmed.StartsWith("versionName="))
            {
                versionName = trimmed["versionName=".Length..];
            }
            else if (versionCode == null && trimmed.StartsWith("versionCode="))
            {
                // Line format: "versionCode=123 minSdk=23 targetSdk=35"
                foreach (var part in trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (versionCode == null && part.StartsWith("versionCode="))
                    {
                        versionCode = part["versionCode=".Length..];
                    }
                    else if (minSdk == null && part.StartsWith("minSdk="))
                    {
                        minSdk = part["minSdk=".Length..];
                    }
                    else if (targetSdk == null && part.StartsWith("targetSdk="))
                    {
                        targetSdk = part["targetSdk=".Length..];
                    }
                }
            }
            else if (installerPackageName == null && trimmed.StartsWith("installerPackageName="))
            {
                installerPackageName = trimmed["installerPackageName=".Length..];
            }
            else if (firstInstallTime == null && trimmed.StartsWith("firstInstallTime="))
            {
                firstInstallTime = trimmed["firstInstallTime=".Length..];
            }
            else if (lastUpdateTime == null && trimmed.StartsWith("lastUpdateTime="))
            {
                lastUpdateTime = trimmed["lastUpdateTime=".Length..];
            }
            else if (dataDir == null && trimmed.StartsWith("dataDir="))
            {
                dataDir = trimmed["dataDir=".Length..];
            }
            else if (codePath == null && trimmed.StartsWith("codePath="))
            {
                codePath = trimmed["codePath=".Length..];
            }
            else if (uid == null && trimmed.StartsWith("userId="))
            {
                uid = trimmed["userId=".Length..];
            }
        }

        string? codeSize = null;
        if (includeSize)
        {
            // pm path lists all APK splits; stat each to get total installed size.
            // du fails on /data/app without root, but stat on individual APKs works.
            var pmResult = await RunAdbAsync(
                $"{deviceArg} shell pm path {packageName}".Trim());
            if (pmResult.Success)
            {
                var statArgs = string.Join(' ', pmResult.Output
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Replace("package:", "").Trim())
                    .Where(p => p.Length > 0));

                if (statArgs.Length > 0)
                {
                    var statResult = await RunAdbAsync(
                        $"{deviceArg} shell stat -c %s {statArgs}".Trim());
                    if (statResult.Success)
                    {
                        long totalBytes = 0;
                        foreach (var line in statResult.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (long.TryParse(line.Trim(), out var bytes))
                            {
                                totalBytes += bytes;
                            }
                        }

                        codeSize = totalBytes >= 1024 * 1024 * 1024L
                            ? $"{totalBytes / (1024.0 * 1024.0 * 1024.0):F1} GB"
                            : totalBytes >= 1024 * 1024
                                ? $"{totalBytes / (1024.0 * 1024.0):F1} MB"
                                : totalBytes >= 1024
                                    ? $"{totalBytes / 1024.0:F1} KB"
                                    : $"{totalBytes} B";
                    }
                }
            }
        }

        return new InstalledPackage(
            packageName, versionName, versionCode,
            installerPackageName, firstInstallTime, lastUpdateTime,
            targetSdk, minSdk, dataDir, uid, codePath, codeSize);
    }

    public async Task<ProcessDetails> GetProcessDetailsAsync(int pid, string name, string? deviceSerial = null)
    {
        var deviceArg = deviceSerial != null ? $"-s {deviceSerial}" : "";
        var result = await RunAdbAsync($"{deviceArg} shell cat /proc/{pid}/status".Trim());

        if (!result.Success)
        {
            return new ProcessDetails(pid, name);
        }

        string? state = null;
        string? uid = null;
        string? threads = null;
        string? vmRss = null;
        string? ppid = null;

        foreach (var line in result.Output.Split('\n'))
        {
            var trimmed = line.Trim();

            if (state == null && trimmed.StartsWith("State:"))
            {
                state = trimmed["State:".Length..].Trim();
            }
            else if (uid == null && trimmed.StartsWith("Uid:"))
            {
                // Uid line has real/effective/saved/fs — take the first (real)
                var parts = trimmed["Uid:".Length..].Trim().Split('\t', StringSplitOptions.RemoveEmptyEntries);
                uid = parts.Length > 0 ? parts[0] : null;
            }
            else if (threads == null && trimmed.StartsWith("Threads:"))
            {
                threads = trimmed["Threads:".Length..].Trim();
            }
            else if (vmRss == null && trimmed.StartsWith("VmRSS:"))
            {
                vmRss = trimmed["VmRSS:".Length..].Trim();
            }
            else if (ppid == null && trimmed.StartsWith("PPid:"))
            {
                ppid = trimmed["PPid:".Length..].Trim();
            }
        }

        return new ProcessDetails(pid, name, state, uid, threads, vmRss, ppid);
    }

    public async Task<AdbResult> InstallApkAsync(string apkFilePath, string? deviceSerial = null)
    {
        var deviceArg = deviceSerial != null ? $"-s {deviceSerial}" : "";
        return await RunAdbAsync($"{deviceArg} install -r \"{apkFilePath}\"".Trim());
    }

    public async Task<AdbResult> UninstallPackageAsync(string packageName, string? deviceSerial = null)
    {
        var deviceArg = deviceSerial != null ? $"-s {deviceSerial}" : "";
        return await RunAdbAsync($"{deviceArg} uninstall {packageName}".Trim());
    }

    /// Tries am force-stop first (works for app packages without root), then falls back
    /// to kill -9 (requires same UID — only works for shell-owned or same-user processes).
    public async Task<AdbResult> KillProcessAsync(int pid, string packageName, string? deviceSerial = null)
    {
        var deviceArg = deviceSerial != null ? $"-s {deviceSerial}" : "";
        var prefix = string.IsNullOrEmpty(deviceArg) ? "shell" : $"{deviceArg} shell";

        // First try am force-stop with the package name (works for app packages)
        var forceStopCmd = $"am force-stop {packageName} 2>&1; echo EXIT:$?";
        var output = (await RunShellWithFallbackAsync(forceStopCmd, prefix)).Trim();
        if (output.Contains("EXIT:0") && !output.Contains("Error"))
        {
            return new AdbResult(true, output);
        }

        // Fall back to kill -9 for non-app processes
        var killCmd = $"kill -9 {pid} 2>&1; echo EXIT:$?";
        output = (await RunShellWithFallbackAsync(killCmd, prefix)).Trim();
        var success = output.Contains("EXIT:0");
        var error = output.Replace("EXIT:0", "").Replace("EXIT:1", "").Trim();
        return new AdbResult(success, output, error);
    }

    public async Task<DeviceInfo> GetDeviceInfoAsync(string? deviceSerial = null, bool dynamicOnly = false)
    {
        var info = new DeviceInfo();
        var deviceArg = deviceSerial != null ? $"-s {deviceSerial}" : "";
        var prefix = string.IsNullOrEmpty(deviceArg) ? "shell" : $"{deviceArg} shell";

        if (!dynamicOnly)
        {
            var propTasks = new[]
            {
                RunAdbAsync($"{prefix} getprop ro.product.model"),
                RunAdbAsync($"{prefix} getprop ro.product.manufacturer"),
                RunAdbAsync($"{prefix} getprop ro.product.cpu.abi"),
                RunAdbAsync($"{prefix} getprop ro.build.version.release"),
                RunAdbAsync($"{prefix} getprop ro.build.version.sdk"),
                RunAdbAsync($"{prefix} getprop ro.build.display.id"),
            };

            var props = await Task.WhenAll(propTasks);

            info.Model = props[0].Success ? props[0].Output.Trim() : null;
            info.Manufacturer = props[1].Success ? props[1].Output.Trim() : null;
            info.Architecture = props[2].Success ? props[2].Output.Trim() : null;
            info.AndroidVersion = props[3].Success ? props[3].Output.Trim() : null;
            info.ApiLevel = props[4].Success ? props[4].Output.Trim() : null;
            info.BuildId = props[5].Success ? props[5].Output.Trim() : null;
        }

        // Dynamic info — single ADB command with delimiters
        const string sep = "____SECT____";
        var combinedCmd = string.Join($"; echo {sep}; ",
        [
            "cat /proc/meminfo",      // 0
            "df -h /data",            // 1
            "uptime",                 // 2
            "dumpsys thermalservice",  // 3
            "cat /proc/stat",         // 4
            "cat /proc/loadavg",      // 5
            "ls /proc/",              // 6
            "cat /proc/net/dev",      // 7
            "cat /proc/vmstat",       // 8
            "dumpsys diskstats",      // 9
        ]);

        // Prefer persistent shell session for polling commands
        var shellOutput = await RunShellWithFallbackAsync(combinedCmd, prefix);
        var sections = shellOutput.Split($"\n{sep}\n", StringSplitOptions.None);

        // Helper to safely get a section
        string GetSection(int index) => index < sections.Length ? sections[index] : "";

        // Parse meminfo
        {
            foreach (var line in GetSection(0).Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("MemTotal:"))
                {
                    info.TotalRam = FormatKb(trimmed);
                    info.MemTotalKb = ParseKb(trimmed);
                }
                else if (trimmed.StartsWith("MemFree:"))
                {
                    info.MemFreeKb = ParseKb(trimmed);
                }
                else if (trimmed.StartsWith("MemAvailable:"))
                {
                    info.AvailableRam = FormatKb(trimmed);
                    info.MemAvailableKb = ParseKb(trimmed);
                }
                else if (trimmed.StartsWith("Buffers:"))
                {
                    info.MemBuffersKb = ParseKb(trimmed);
                }
                else if (trimmed.StartsWith("Cached:"))
                {
                    info.MemCachedKb = ParseKb(trimmed);
                }
                else if (trimmed.StartsWith("SwapTotal:"))
                {
                    info.SwapTotalKb = ParseKb(trimmed);
                }
                else if (trimmed.StartsWith("SwapFree:"))
                {
                    info.SwapFreeKb = ParseKb(trimmed);
                }
            }
        }

        // Parse df output
        {
            var dfLines = GetSection(1).Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (dfLines.Length >= 2)
            {
                var cols = dfLines[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (cols.Length >= 4)
                {
                    info.StorageTotal = cols[1];
                    info.StorageUsed = cols[2];
                    info.StorageAvailable = cols[3];
                }
            }
        }

        // Parse uptime
        {
            var uptimeOutput = GetSection(2).Trim();
            var upIdx = uptimeOutput.IndexOf("up ");
            if (upIdx >= 0)
            {
                var rest = uptimeOutput[(upIdx + 3)..];
                var commaIdx = rest.IndexOf(',');
                // Take everything up to the second comma (users/load info)
                if (commaIdx > 0)
                {
                    var afterFirst = rest[(commaIdx + 1)..];
                    var secondComma = afterFirst.IndexOf(',');
                    if (secondComma > 0 && afterFirst[..secondComma].Trim().Contains(':'))
                    {
                        info.Uptime = rest[..commaIdx].Trim();
                    }
                    else if (secondComma > 0)
                    {
                        info.Uptime = rest[..(commaIdx + 1 + secondComma)].Trim().TrimEnd(',');
                    }
                    else
                    {
                        info.Uptime = rest[..commaIdx].Trim();
                    }
                }
                else
                {
                    info.Uptime = rest.Trim();
                }
            }
        }

        // Parse thermalservice temperature — use "Current temperatures from HAL" section
        {
            var thermalOutput = GetSection(3);
            var inCurrentSection = false;
            float maxTemp = 0;
            var temps = new List<(string Name, float Value)>();

            foreach (var line in thermalOutput.Split('\n'))
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("Current temperatures from HAL"))
                {
                    inCurrentSection = true;
                    continue;
                }

                if (inCurrentSection && trimmed.StartsWith("Current cooling"))
                {
                    break;
                }

                if (!inCurrentSection || !trimmed.Contains("mValue="))
                {
                    continue;
                }

                var mValueIdx = trimmed.IndexOf("mValue=");
                var valueStr = trimmed[(mValueIdx + "mValue=".Length)..];
                var endIdx = valueStr.IndexOfAny([',', ' ', '}']);
                if (endIdx > 0)
                {
                    valueStr = valueStr[..endIdx];
                }

                var nameStr = "Unknown";
                var mNameIdx = trimmed.IndexOf("mName=");
                if (mNameIdx >= 0)
                {
                    var nameVal = trimmed[(mNameIdx + "mName=".Length)..];
                    var nameEnd = nameVal.IndexOfAny([',', ' ', '}']);
                    if (nameEnd > 0)
                    {
                        nameStr = nameVal[..nameEnd];
                    }
                }

                if (float.TryParse(valueStr, System.Globalization.CultureInfo.InvariantCulture, out var temp))
                {
                    temps.Add((nameStr, temp));
                    if (temp > maxTemp)
                    {
                        maxTemp = temp;
                    }
                }
            }

            if (temps.Count > 0)
            {
                info.Temperature = string.Join(", ", temps.Select(t => $"{t.Name}: {t.Value:F1}°C"));
                info.TemperatureValue = maxTemp;
                info.Temperatures = temps.Select(t => (t.Name, (double)t.Value)).ToList();
            }

            // Parse cooling devices (fan state) from "Current cooling devices from HAL" section
            var inCoolingSection = false;
            foreach (var line in thermalOutput.Split('\n'))
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("Current cooling devices from HAL"))
                {
                    inCoolingSection = true;
                    continue;
                }

                if (inCoolingSection && trimmed.Length > 0 && !trimmed.Contains("mValue="))
                {
                    break;
                }

                if (!inCoolingSection || !trimmed.Contains("mValue="))
                {
                    continue;
                }

                var mValueIdx = trimmed.IndexOf("mValue=");
                var valueStr = trimmed[(mValueIdx + "mValue=".Length)..];
                var endIdx = valueStr.IndexOfAny([',', ' ', '}']);
                if (endIdx > 0)
                {
                    valueStr = valueStr[..endIdx];
                }

                if (int.TryParse(valueStr, out var fanLevel))
                {
                    info.FanState = fanLevel > 0 ? $"Active (Level {fanLevel})" : "Off";
                    break; // Shield has one fan
                }
            }
        }

        // Parse /proc/stat for CPU usage
        {
            foreach (var statLine in GetSection(4).Split('\n'))
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
                long.TryParse(vals[4], out var idle);
                long.TryParse(vals[5], out var w);
                long.TryParse(vals[6], out var q);
                long.TryParse(vals[7], out var sq);
                long st = 0;
                if (vals.Length >= 9)
                {
                    long.TryParse(vals[8], out st);
                }

                var active = u + n + s + w + q + sq + st;
                var total = active + idle;

                if (vals[0] == "cpu")
                {
                    info.CpuUser = u;
                    info.CpuNice = n;
                    info.CpuSystem = s;
                    info.CpuIdle = idle;
                    info.CpuIoWait = w;
                    info.CpuIrq = q;
                    info.CpuSoftIrq = sq;
                    info.CpuSteal = st;
                }
                else
                {
                    // cpu0, cpu1, etc.
                    info.CpuCores.Add((vals[0].ToUpperInvariant(), active, total));
                }
            }
        }

        // Parse /proc/loadavg for thread count (field 4: running/total)
        {
            var fields = GetSection(5).Trim().Split(' ');
            if (fields.Length >= 4)
            {
                var parts = fields[3].Split('/');
                if (parts.Length == 2 && int.TryParse(parts[1], out var threads))
                {
                    info.ThreadCount = threads;
                }
            }
        }

        // Parse process count from ls /proc/ (count numeric entries = PIDs)
        {
            info.ProcessCount = GetSection(6)
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Count(entry => entry.Trim().Length > 0 && entry.Trim().All(char.IsDigit));
        }

        // Parse /proc/net/dev for network I/O (section 7)
        // Format: iface: rx_bytes rx_packets ... tx_bytes tx_packets ...
        // Skip lo (loopback); sum all other interfaces
        {
            foreach (var line in GetSection(7).Split('\n', StringSplitOptions.RemoveEmptyEntries))
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

                // rx: bytes=0, packets=1; tx: bytes=8, packets=9
                if (long.TryParse(vals[0], out var rxBytes))
                {
                    info.NetBytesIn += rxBytes;
                }

                if (long.TryParse(vals[1], out var rxPackets))
                {
                    info.NetPacketsIn += rxPackets;
                }

                if (long.TryParse(vals[8], out var txBytes))
                {
                    info.NetBytesOut += txBytes;
                }

                if (long.TryParse(vals[9], out var txPackets))
                {
                    info.NetPacketsOut += txPackets;
                }
            }
        }

        // Parse /proc/vmstat for disk I/O (section 8)
        // pgpgin = KB read from disk, pgpgout = KB written to disk
        {
            foreach (var line in GetSection(8).Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Trim().Split(' ');
                if (parts.Length < 2)
                {
                    continue;
                }

                if (parts[0] == "pgpgin" && long.TryParse(parts[1], out var pgIn))
                {
                    info.DiskKbRead = pgIn;
                }
                else if (parts[0] == "pgpgout" && long.TryParse(parts[1], out var pgOut))
                {
                    info.DiskKbWritten = pgOut;
                }
            }
        }

        // Parse dumpsys diskstats for write latency and speed (section 9)
        {
            foreach (var line in GetSection(9).Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("Latency:"))
                {
                    // "Latency: 1ms [512B Data Write]"
                    var msIdx = trimmed.IndexOf("ms");
                    if (msIdx > 0)
                    {
                        var numStr = trimmed["Latency:".Length..msIdx].Trim();
                        if (int.TryParse(numStr, out var ms))
                        {
                            info.DiskWriteLatencyMs = ms;
                        }
                    }
                }
                else if (trimmed.StartsWith("Recent Disk Write Speed"))
                {
                    // "Recent Disk Write Speed (kB/s) = 20553"
                    var eqIdx = trimmed.IndexOf('=');
                    if (eqIdx > 0)
                    {
                        var numStr = trimmed[(eqIdx + 1)..].Trim();
                        if (double.TryParse(numStr, System.Globalization.CultureInfo.InvariantCulture, out var speed))
                        {
                            info.DiskWriteSpeedKbps = speed;
                        }
                    }
                }
            }
        }

        return info;
    }

    private static long ParseKb(string memLine)
    {
        var parts = memLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 && long.TryParse(parts[1], out var kb) ? kb : 0;
    }

    private static string FormatKb(string memLine)
    {
        var parts = memLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2 && long.TryParse(parts[1], out var kb))
        {
            return kb switch
            {
                >= 1_048_576 => $"{kb / 1_048_576.0:F1} GB",
                >= 1024 => $"{kb / 1024.0:F0} MB",
                _ => $"{kb} KB",
            };
        }
        return parts.Length >= 2 ? parts[1] : "Unknown";
    }

    /// <summary>
    /// Reads /proc/stat total jiffies and per-process jiffies + names from /proc/[pid]/stat.
    /// Returns (perProcessJiffies, totalCpuJiffies).
    /// </summary>
    public async Task<ProcessSnapshot> GetProcessSnapshotAsync(string? deviceSerial = null)
    {
        var deviceArg = deviceSerial != null ? $"-s {deviceSerial}" : "";
        var prefix = string.IsNullOrEmpty(deviceArg) ? "shell" : $"{deviceArg} shell";

        // Single call: read /proc/stat, per-process stat files, and directory ownership (for UID).
        // Some /proc/[pid]/stat files may vanish mid-read causing a non-zero exit,
        // but the output for surviving processes is still valid.
        const string cmd = "cat /proc/stat; echo ---; cat /proc/[0-9]*/stat; echo ---; ls -ldn /proc/[0-9]*";

        // Fire cmdline read concurrently via one-off adb call — ps is fast and avoids
        // null bytes that corrupt the persistent session.
        var cmdlineTask = RunAdbAsync($"{prefix} \"ps -A -o PID,ARGS\"", strictCheck: false);

        var output = await RunShellWithFallbackAsync(cmd, prefix);

        var procs = new Dictionary<int, RawProcessEntry>();
        var totalJiffies = 0L;
        var idleJiffies = 0L;

        if (string.IsNullOrWhiteSpace(output))
        {
            return new ProcessSnapshot(procs, totalJiffies, idleJiffies);
        }

        // Temporary storage without UID — filled during section 1, UIDs added in section 2
        var procData = new Dictionary<int, (long Jiffies, string Name, long RssPages, char State)>();
        var section = 0;
        var uidByPid = new Dictionary<int, int>();

        foreach (var line in output.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (trimmed == "---") { section++; continue; }

            if (section == 0)
            {
                if (trimmed.StartsWith("cpu "))
                {
                    var fields = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                    for (var i = 1; i < fields.Length; i++)
                    {
                        if (long.TryParse(fields[i], out var v))
                        {
                            totalJiffies += v;
                        }
                    }
                    // fields[4] is idle
                    if (fields.Length > 4)
                    {
                        long.TryParse(fields[4], out idleJiffies);
                    }
                }
            }
            else if (section == 1)
            {
                // Format: pid (comm) state ppid ... utime stime ...
                var commEnd = trimmed.LastIndexOf(')');
                if (commEnd < 0)
                {
                    continue;
                }

                var pidEnd = trimmed.IndexOf(' ');
                if (pidEnd < 0)
                {
                    continue;
                }

                if (!int.TryParse(trimmed[..pidEnd], out var pid))
                {
                    continue;
                }

                var commStart = trimmed.IndexOf('(');
                var name = commStart >= 0 && commEnd > commStart
                    ? trimmed[(commStart + 1)..commEnd]
                    : pid.ToString();

                var afterComm = trimmed[(commEnd + 1)..].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                // afterComm[0]=state [1]=ppid ... [11]=utime [12]=stime ... [21]=rss (pages)
                if (afterComm.Length < 22)
                {
                    continue;
                }

                if (!long.TryParse(afterComm[11], out var utime))
                {
                    continue;
                }

                if (!long.TryParse(afterComm[12], out var stime))
                {
                    continue;
                }

                long.TryParse(afterComm[21], out var rssPages);

                var state = afterComm[0].Length > 0 ? afterComm[0][0] : '?';
                procData[pid] = (utime + stime, name, rssPages, state);
            }
            else if (section == 2)
            {
                // Format: drwxr-xr-x NN UID GID SIZE DATE TIME /proc/PID
                var cols = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (cols.Length < 8)
                {
                    continue;
                }

                var path = cols[^1]; // last column is path
                var pidStr = path.StartsWith("/proc/") ? path["/proc/".Length..] : null;
                if (pidStr is not null && int.TryParse(pidStr, out var dirPid) && int.TryParse(cols[2], out var uid))
                {
                    uidByPid[dirPid] = uid;
                }
            }
        }

        // Parse ps output for cmdline (runs concurrently with the main snapshot)
        var cmdlineResult = await cmdlineTask;
        var cmdlineByPid = new Dictionary<int, string>();
        if (cmdlineResult.Output is { Length: > 0 })
        {
            foreach (var line in cmdlineResult.Output.Split('\n'))
            {
                var trimmed2 = line.Trim();
                if (trimmed2.Length == 0 || trimmed2.StartsWith("PID"))
                {
                    continue; // skip header
                }
                // Format: PID ARGS (e.g. "  123 com.google.android.youtube")
                var spaceIdx = trimmed2.IndexOf(' ');
                if (spaceIdx < 0)
                {
                    continue;
                }

                if (!int.TryParse(trimmed2[..spaceIdx], out var cmdPid))
                {
                    continue;
                }

                var args = trimmed2[(spaceIdx + 1)..].Trim();
                // Take only the first argument (the executable/package name)
                var firstArgEnd = args.IndexOf(' ');
                if (firstArgEnd > 0)
                {
                    args = args[..firstArgEnd];
                }

                if (args.Length > 0)
                {
                    cmdlineByPid[cmdPid] = args;
                }
            }
        }

        // Merge UID and cmdline info into process data
        foreach (var (pid, (jiffies, name, rssPages, state)) in procData)
        {
            uidByPid.TryGetValue(pid, out var uid);
            cmdlineByPid.TryGetValue(pid, out var cmdline);
            procs[pid] = new RawProcessEntry(pid, jiffies, name, rssPages, uid, cmdline ?? name, state);
        }

        return new ProcessSnapshot(procs, totalJiffies, idleJiffies);
    }

    private Task<AdbResult> RunAdbAsync(string arguments) => RunAdbAsync(arguments, strictCheck: true);

    private async Task<AdbResult> RunAdbAsync(string arguments, bool strictCheck)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = _adbPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            var success = process.ExitCode == 0;
            if (strictCheck)
            {
                success = success
                    && !output.Contains("error", StringComparison.OrdinalIgnoreCase)
                    && !output.Contains("failed", StringComparison.OrdinalIgnoreCase);
            }

            return new AdbResult(success, output.Trim(), error.Trim());
        }
        catch (Exception ex)
        {
            return new AdbResult(false, "", ex.Message);
        }
    }
}
