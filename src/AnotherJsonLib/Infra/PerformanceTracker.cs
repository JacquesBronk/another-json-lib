using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace AnotherJsonLib.Infra;

/// <summary>
/// A lightweight performance tracker that logs the elapsed time of an operation via ILogger.
/// Intended for targeted performance measurements using a using-block.
/// </summary>
public class PerformanceTracker : IDisposable
{
    private readonly Stopwatch _stopwatch;
    private readonly ILogger _logger;
    private readonly string _operationName;
    private readonly LogLevel _logLevel;

    /// <summary>
    /// Global flag to enable or disable performance tracking.
    /// </summary>
    public static bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Creates a PerformanceTracker instance that logs to the provided ILogger.
    /// </summary>
    /// <param name="logger">The logger to use.</param>
    /// <param name="operationName">A descriptive name for the operation.</param>
    /// <param name="logLevel">The log level to log at (default is Debug).</param>
    public PerformanceTracker(ILogger logger, string operationName, LogLevel logLevel = LogLevel.Debug)
    {
        _logger = logger;
        _operationName = operationName;
        _logLevel = logLevel;
        _stopwatch = Stopwatch.StartNew();
    }

    /// <summary>
    /// Stops tracking and logs the elapsed time.
    /// </summary>
    public void Dispose()
    {
        _stopwatch.Stop();
        if (IsEnabled && _logger.IsEnabled(_logLevel))
        {
            _logger.Log(_logLevel, "Operation {OperationName} took {ElapsedMilliseconds} ms", _operationName, _stopwatch.ElapsedMilliseconds);
        }
    }
}