namespace AnotherJsonLib.Infra;

using Microsoft.Extensions.Logging;

public class LoggerFactory
{
    private readonly ILoggerFactory _loggerFactory;

    private LoggerFactory()
    {
        _loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder.AddJsonConsole(); 
        });
    }

    public static LoggerFactory Instance { get; } = new LoggerFactory();

    public ILogger GetLogger<T>()
    {
        return _loggerFactory.CreateLogger<T>();
    }
    
    public ILogger GetLogger(string categoryName)
    {
        return _loggerFactory.CreateLogger(categoryName);
    }
}
