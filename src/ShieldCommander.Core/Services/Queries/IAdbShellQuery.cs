using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Queries;

internal interface IAdbShellQuery
{
    string Name { get; }

    string CommandText { get; }

    void Apply(ReadOnlySpan<char> output, DynamicSections target);
}

internal interface IAdbShellQuery<out T> : IAdbShellQuery
{
    T Parse(ReadOnlySpan<char> output);
}
