namespace ShieldCommander.Core.Services.Queries;

internal interface IAdbQuery<T>
{
    Task<T> ExecuteAsync(IAdbRunner runner);

    T Parse(string output);
}
