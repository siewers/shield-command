using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Queries;

internal sealed class ConnectedDevicesQuery : IAdbQuery<List<ShieldDevice>>
{
    public async Task<List<ShieldDevice>> ExecuteAsync(IAdbRunner runner)
    {
        var result = await runner.RunAdbAsync("devices -l");

        return result.Success ? Parse(result.Output) : [];
    }

    public List<ShieldDevice> Parse(string output)
    {
        var devices = new List<ShieldDevice>();

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
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
}
