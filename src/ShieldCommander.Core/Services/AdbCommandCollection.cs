using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using ShieldCommander.Core.Models;
using ShieldCommander.Core.Services.Commands;

namespace ShieldCommander.Core.Services;

internal sealed partial class AdbCommandCollection : IEnumerable<IAdbShellCommand>
{
    private const string Prefix = "____";
    private const string Suffix = "____";
    private const string SectionNameGroup = "sectionName";

    private readonly List<IAdbShellCommand> _commands = [];
    private readonly Dictionary<string, string> _results = [];

    public IEnumerator<IAdbShellCommand> GetEnumerator() => _commands.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _commands.GetEnumerator();

    public void Add(IAdbShellCommand command)
    {
        _commands.Add(command);
        _results[command.Name] = string.Empty;
    }

    public string ToCombinedCommand()
        => string.Join("; ", _commands.Select(c => $"echo {Prefix}{c.Name}{Suffix}; {c.CommandText}"));

    public void ApplyAll(string commandResults, DynamicSections target)
    {
        UpdateResults(commandResults);
        foreach (var cmd in _commands)
        {
            cmd.Apply(_results[cmd.Name], target);
        }
    }

    private void UpdateResults(string commandResults)
    {
        foreach (var key in _results.Keys)
        {
            // Clear all old results on update
            _results[key] = string.Empty;
        }

        string? currentSectionName = null;
        var resultBuilder = new StringBuilder();

        using var reader = new StringReader(commandResults);
        while (reader.ReadLine() is { } line)
        {
            var sectionMatch = SectionDelimiterRegex().Match(line);

            if (sectionMatch.Success)
            {
                if (currentSectionName is not null)
                {
                    // New section - flush the results
                    _results[currentSectionName] = resultBuilder.ToString();
                }

                currentSectionName = sectionMatch.Groups[SectionNameGroup].Value;
                resultBuilder.Clear();
            }
            else if (currentSectionName is not null)
            {
                resultBuilder.AppendLine(line);
            }
        }

        if (currentSectionName is not null)
        {
            // Flush the remaining results to the last section
            _results[currentSectionName] = resultBuilder.ToString();
        }
    }

    [GeneratedRegex($"^{Prefix}(?<{SectionNameGroup}>.+?){Suffix}$")]
    private partial Regex SectionDelimiterRegex();
}
