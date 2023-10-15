using Microsoft.Extensions.Logging;

namespace AJL.Tests.Utility;

public class NullLoggerFactory : ILoggerFactory
{
    public void AddProvider(ILoggerProvider provider)
    {
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new NullLogger();
    }

    public void Dispose()
    {
    }
}

public class NullLogger : ILogger
{
    IDisposable ILogger.BeginScope<TState>(TState state)
    {
        return null!;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return false;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception, string> formatter)
    {
    }
}
