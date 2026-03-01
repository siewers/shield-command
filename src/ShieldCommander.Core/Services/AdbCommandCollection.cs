using System.Collections;
using ShieldCommander.Core.Models;
using ShieldCommander.Core.Services.Commands;

namespace ShieldCommander.Core.Services;

internal sealed class AdbCommandCollection : IEnumerable<IAdbShellCommand>
{
    private const string Prefix = "____";
    private const string Suffix = "____";

    private readonly List<IAdbShellCommand> _commands = [];
    private readonly Dictionary<string, ReadOnlyMemory<char>> _results = [];

    public IEnumerator<IAdbShellCommand> GetEnumerator() => _commands.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _commands.GetEnumerator();

    public void Add(IAdbShellCommand command)
    {
        _commands.Add(command);
        _results[command.Name] = ReadOnlyMemory<char>.Empty;
    }

    public string ToCombinedCommand()
        => string.Join("; ", _commands.Select(c => $"echo {Prefix}{c.Name}{Suffix}; {c.CommandText}"));

    public void ApplyAll(string commandResults, DynamicSections target)
    {
        UpdateResults(commandResults);
        foreach (var cmd in _commands)
        {
            cmd.Apply(_results[cmd.Name].Span, target);
        }
    }

    private void UpdateResults(string commandResults)
    {
        foreach (var key in _results.Keys)
        {
            _results[key] = ReadOnlyMemory<char>.Empty;
        }

        string? currentSectionName = null;
        var sectionStart = -1;
        var span = commandResults.AsSpan();
        var pos = 0;

        while (pos < span.Length)
        {
            var newlineIdx = span[pos..].IndexOf('\n');
            var lineEnd = newlineIdx >= 0 ? pos + newlineIdx : span.Length;
            var line = span[pos..lineEnd];

            // Strip trailing \r
            if (line.Length > 0 && line[^1] == '\r')
            {
                line = line[..^1];
            }

            if (line.StartsWith(Prefix) && line.EndsWith(Suffix) && line.Length > Prefix.Length + Suffix.Length)
            {
                if (currentSectionName is not null)
                {
                    _results[currentSectionName] = commandResults.AsMemory(sectionStart, pos - sectionStart);
                }

                currentSectionName = line[Prefix.Length..^Suffix.Length].ToString();
                sectionStart = newlineIdx >= 0 ? lineEnd + 1 : lineEnd;
            }

            pos = newlineIdx >= 0 ? lineEnd + 1 : span.Length;
        }

        if (currentSectionName is not null)
        {
            _results[currentSectionName] = commandResults.AsMemory(sectionStart, span.Length - sectionStart);
        }
    }
}
