using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace ServicoPix.Tests;

public sealed class TestLogger<T> : ILogger<T>
{
    public sealed record Entry(LogLevel Level, EventId EventId, string Message, Exception? Exception, IReadOnlyList<KeyValuePair<string, object?>> State);

    private readonly ConcurrentQueue<Entry> _entries = new();

    public IReadOnlyCollection<Entry> Entries => _entries.ToArray();

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);

        var statePairs = state as IEnumerable<KeyValuePair<string, object?>>;
        var captured = statePairs?.ToList() ?? new List<KeyValuePair<string, object?>>();

        _entries.Enqueue(new Entry(logLevel, eventId, message, exception, captured));
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
