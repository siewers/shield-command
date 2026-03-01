using System.Diagnostics;
using System.Text;

namespace ShieldCommander.Core.Services;

public sealed class AdbShellSession : IDisposable
{
    private const string EndMarker = "<<SHIELDCMD_END>>";
    private readonly string _adbPath;
    private readonly string? _deviceSerial;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private Process? _process;
    private StreamWriter? _stdin;
    private StreamReader? _stdout;
    private bool _disposed;

    public AdbShellSession(string adbPath, string? deviceSerial)
    {
        _adbPath = adbPath;
        _deviceSerial = deviceSerial;
    }

    public async Task OpenAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            StartProcess();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<string?> RunAsync(string command, CancellationToken ct = default)
    {
        if (_disposed)
        {
            return null;
        }

        await _semaphore.WaitAsync(ct);
        try
        {
            if (_process is null || _process.HasExited)
            {
                StartProcess();
            }

            if (_stdin is null || _stdout is null)
            {
                return null;
            }

            // Write the command followed by an echo of our end marker
            await _stdin.WriteLineAsync(command);
            await _stdin.WriteLineAsync($"echo '{EndMarker}'");
            await _stdin.FlushAsync(ct);

            var sb = new StringBuilder();
            while (true)
            {
                ct.ThrowIfCancellationRequested();

                var line = await _stdout.ReadLineAsync(ct);
                if (line is null)
                {
                    // Process died — try to restart on next call
                    KillProcess();
                    return sb.Length > 0 ? sb.ToString() : null;
                }

                if (line == EndMarker)
                {
                    break;
                }

                sb.AppendLine(line);
            }

            return sb.ToString().TrimEnd('\r', '\n');
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch
        {
            KillProcess();
            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void StartProcess()
    {
        KillProcess();

        var args = _deviceSerial != null ? $"-s {_deviceSerial} shell" : "shell";

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _adbPath,
                Arguments = args,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        process.Start();
        _process = process;
        _stdin = process.StandardInput;
        _stdout = process.StandardOutput;
        _stdin.AutoFlush = false;
    }

    private void KillProcess()
    {
        if (_process is null)
        {
            return;
        }

        try
        {
            if (!_process.HasExited)
            {
                _process.Kill();
            }
        }
        catch
        {
            // ignored
        }

        _process.Dispose();
        _process = null;
        _stdin = null;
        _stdout = null;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        KillProcess();
        _semaphore.Dispose();
    }
}
