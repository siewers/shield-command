namespace ShieldCommander.Core.Models;

public sealed record AdbResult(bool Success, string Output, string Error = "");
