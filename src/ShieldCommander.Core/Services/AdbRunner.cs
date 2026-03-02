using System.Diagnostics;
using ShieldCommander.Core.Models;
using ShieldCommander.Core.Services.Commands;
using ShieldCommander.Core.Services.Queries;

namespace ShieldCommander.Core.Services;

public sealed class AdbRunner(AdbPathProvider pathProvider)
{
    private AdbShellSession? _session;

    public event Action? SessionLost;

    public async Task OpenSessionAsync()
    {
        CloseSession();
        var session = new AdbShellSession(pathProvider.CurrentPath);
        session.SessionLost += () => SessionLost?.Invoke();
        await session.OpenAsync();
        _session = session;
    }

    public void CloseSession()
    {
        _session?.Dispose();
        _session = null;
    }

    public async Task<string> RunShellAsync(string command, CancellationToken ct = default)
    {
        return _session is not null
            ? await _session.RunAsync(command, ct) ?? string.Empty
            : string.Empty;
    }

    internal Task<AdbResult> ExecuteAsync(IAdbCommand command) => command.ExecuteAsync(this);

    internal Task<T> ExecuteAsync<T>(IAdbQuery<T> query) => query.ExecuteAsync(this);

    public Task<AdbResult> RunAdbAsync(string arguments) => RunAdbAsync(arguments, strictCheck: true);

    public async Task<AdbResult> RunAdbAsync(string arguments, bool strictCheck)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = pathProvider.CurrentPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            process.Start();

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            var output = await stdoutTask;
            var error = await stderrTask;

            var success = process.ExitCode == 0;
            if (strictCheck)
            {
                success = success && !output.Contains("error", StringComparison.OrdinalIgnoreCase) && !output.Contains("failed", StringComparison.OrdinalIgnoreCase);
            }

            return new AdbResult(success, output.Trim(), error.Trim());
        }
        catch (Exception ex)
        {
            return new AdbResult(false, "", ex.Message);
        }
    }
}
