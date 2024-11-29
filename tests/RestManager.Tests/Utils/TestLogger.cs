using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace RestManager.Tests.Utils;

internal class TestLogger<T> : ILogger<T>
{
    private readonly string _name;
    private readonly Action<string> _logAction;

    public TestLogger(string name, Action<string> logAction)
    {
        _name = name;
        _logAction = logAction;
    }

    public IDisposable BeginScope<TState>(TState state) => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (formatter != null)
        {
            _logAction($"{logLevel.ToString()}: {_name} - {formatter(state, exception)}");
        }
    }
}