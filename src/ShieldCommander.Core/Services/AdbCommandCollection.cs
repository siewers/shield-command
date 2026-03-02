using System.Collections;
using ShieldCommander.Core.Models;
using ShieldCommander.Core.Services.Queries;

namespace ShieldCommander.Core.Services;

internal sealed class AdbCommandCollection : IEnumerable<IAdbShellQuery>
{
    private const string Prefix = "____";
    private const string Suffix = "____";

    private readonly List<IAdbShellQuery> _commands = [];
    private readonly Dictionary<string, ReadOnlyMemory<char>> _results = [];

    public IEnumerator<IAdbShellQuery> GetEnumerator() => _commands.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _commands.GetEnumerator();

    public void Add(IAdbShellQuery command)
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
        var sectionStart = 0;
        var span = commandResults.AsSpan();

        foreach (var line in span.EnumerateLines())
        {
            if (!line.StartsWith(Prefix) || !line.EndsWith(Suffix) || line.Length <= Prefix.Length + Suffix.Length)
            {
                continue;
            }

            span.Overlaps(line, out var lineStart);

            if (currentSectionName is not null)
            {
                _results[currentSectionName] = commandResults.AsMemory(sectionStart, lineStart - sectionStart);
            }

            currentSectionName = line[Prefix.Length..^Suffix.Length].ToString();
            var afterLine = lineStart + line.Length;
            if (afterLine < span.Length && span[afterLine] == '\r')
            {
                afterLine++;
            }

            if (afterLine < span.Length && span[afterLine] == '\n')
            {
                afterLine++;
            }

            sectionStart = afterLine;
        }

        if (currentSectionName is not null)
        {
            _results[currentSectionName] = commandResults.AsMemory(sectionStart, commandResults.Length - sectionStart);
        }
    }
}
