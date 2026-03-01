using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Commands;

internal interface IAdbShellCommand
{
    string Name { get; }

    string CommandText { get; }

    void Apply(ReadOnlySpan<char> output, DynamicSections target);
}

internal interface IAdbShellCommand<out T> : IAdbShellCommand
{
    T Parse(ReadOnlySpan<char> output);
}
