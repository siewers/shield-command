using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Commands;

internal interface IAdbShellCommand
{
    string Name { get; }

    string CommandText { get; }

    void Apply(string output, DynamicSections target);
}

internal interface IAdbShellCommand<out T> : IAdbShellCommand
{
    T Parse(string output);
}
