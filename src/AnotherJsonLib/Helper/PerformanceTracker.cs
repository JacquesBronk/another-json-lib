using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace AnotherJsonLib.Helper;

/// <summary>
/// A lightweight performance tracker that logs the elapsed time of an operation via ILogger.
/// Intended for targeted performance measurements using a using-block.
/// </summary>
/// <remarks>
/// PerformanceTracker helps identify performance bottlenecks by measuring and logging the
/// execution time of specific operations. It integrates with the application's logging system
/// to provide consistent timing information that can be filtered by log level.
/// </remarks>
/// <example>
/// <code>
/// // Track the performance of a JSON serialization operation
/// public string SerializeObject&lt;T&gt;(T obj)
/// {
///     using var perf = new PerformanceTracker(_logger, nameof(SerializeObject));
///     // Perform the operation
///     return JsonSerializer.Serialize(obj);
/// }
/// // When the method exits, the execution time is automatically logged
/// </code>
/// </example>
public class PerformanceTracker : IDisposable
{
    private readonly Stopwatch _stopwatch;
    private readonly ILogger _logger;
    private readonly string _operationName;
    private readonly LogLevel _logLevel;

    /// <summary>
    /// Global flag to enable or disable performance tracking.
    /// </summary>
    /// <remarks>
    /// When set to false, PerformanceTracker instances will still be created but won't log anything.
    /// This allows for disabling performance tracking in production without changing code.
    /// </remarks>
    public static bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Creates a PerformanceTracker instance that logs to the provided ILogger.
    /// </summary>
    /// <param name="logger">The logger to use. Must not be null.</param>
    /// <param name="operationName">A descriptive name for the operation. Must not be null or empty.</param>
    /// <param name="logLevel">The log level to log at (default is Debug).</param>
    /// <exception cref="ArgumentNullException">Thrown when logger or operationName is null.</exception>
    /// <exception cref="ArgumentException">Thrown when operationName is empty or whitespace.</exception>
    public PerformanceTracker(ILogger logger, string operationName, LogLevel logLevel = LogLevel.Debug)
    {
        if (logger == null)
            throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(operationName))
            throw new ArgumentException("Operation name cannot be null or whitespace", nameof(operationName));

        _logger = logger;
        _operationName = operationName;
        _logLevel = logLevel;
        _stopwatch = Stopwatch.StartNew();
    }

    /// <summary>
    /// Stops tracking and logs the elapsed time.
    /// </summary>
    /// <remarks>
    /// This method is automatically called when the using block exits.
    /// The elapsed time is logged at the log level specified during construction.
    /// </remarks>
    public void Dispose()
    {
        _stopwatch.Stop();
        if (IsEnabled && _logger.IsEnabled(_logLevel))
        {
            _logger.Log(_logLevel, "Operation {OperationName} took {ElapsedMilliseconds} ms",
                _operationName, _stopwatch.ElapsedMilliseconds);
        }
    }
}