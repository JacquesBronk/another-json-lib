using System.Diagnostics;
using AnotherJsonLib.Exceptions;
using Microsoft.Extensions.Logging;

namespace AnotherJsonLib.Helper;

/// <summary>
/// Provides diagnostic performance tracking for operations using DiagnosticListener.
/// This class implements IDisposable and is designed to be used in a using block
/// to automatically send diagnostic events when an operation starts and stops.
/// </summary>
/// <remarks>
/// DiagnosticPerformanceTracker integrates with .NET's DiagnosticListener system
/// to emit structured events that can be consumed by application performance
/// monitoring (APM) tools.
/// </remarks>
/// <example>
/// <code>
/// // Track performance of a database operation
/// using (var tracker = new DiagnosticPerformanceTracker("Database.Query"))
/// {
///     // Perform the database operation
///     var results = dbContext.Products.Where(p => p.Price > 100).ToList();
/// }
/// // Diagnostic events are automatically emitted when the using block exits
/// </code>
/// </example>
public class DiagnosticPerformanceTracker: IDisposable
{
    private Stopwatch _stopwatch = null!;
    private string _operationName = null!;
    private DiagnosticListener _diagnosticListener = null!;
    private static readonly ILogger Logger = JsonLoggerFactory.Instance.GetLogger(nameof(DiagnosticPerformanceTracker));

    /// <summary>
    /// Global flag to enable or disable diagnostic performance tracking.
    /// When set to false, no diagnostic events will be emitted.
    /// </summary>
    private static bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Creates a DiagnosticPerformanceTracker that writes start/stop events via DiagnosticListener.
    /// </summary>
    /// <param name="operationName">A descriptive name for the operation being tracked.</param>
    /// <exception cref="ArgumentNullException">Thrown when operationName is null or empty.</exception>
    /// <exception cref="JsonOperationException">Thrown when creating the diagnostic listener fails.</exception>
    public DiagnosticPerformanceTracker(string? operationName)
    {
        ExceptionHelpers.SafeExecute(() =>
        {
            ExceptionHelpers.ThrowIfNullOrWhiteSpace(operationName, nameof(operationName));


            Debug.Assert(operationName != null, nameof(operationName) + " != null");
            _operationName = operationName;
            _diagnosticListener = new DiagnosticListener("AnotherJsonLib.Performance");
            _stopwatch = Stopwatch.StartNew();

            if (IsEnabled && _diagnosticListener.IsEnabled("Operation.Start"))
            {
                Logger.LogTrace("Starting diagnostic tracking for operation: {OperationName}", _operationName);
                _diagnosticListener.Write("Operation.Start", new
                {
                    Operation = _operationName,
                    Timestamp = DateTime.UtcNow
                });
            }
        },
        (ex, msg) => new JsonOperationException($"Failed to initialize performance tracking: {msg}", ex),
        "Error initializing performance tracking");
    }

    /// <summary>
    /// Stops tracking and writes a diagnostic event with the elapsed time.
    /// This method is automatically called when the object is disposed.
    /// </summary>
    /// <exception cref="JsonOperationException">Thrown when writing the stop event fails.</exception>
    public void Dispose()
    {
        ExceptionHelpers.SafeExecute(() =>
        {
            _stopwatch.Stop();
            if (IsEnabled && _diagnosticListener.IsEnabled("Operation.Stop"))
            {
                var elapsedMs = _stopwatch.ElapsedMilliseconds;
                Logger.LogTrace("Stopping diagnostic tracking for operation: {OperationName} ({ElapsedMs} ms)", 
                    _operationName, elapsedMs);
                
                _diagnosticListener.Write("Operation.Stop", new
                {
                    Operation = _operationName,
                    ElapsedMilliseconds = elapsedMs
                });
            }
        },
        (ex, msg) => new JsonOperationException($"Failed to complete performance tracking: {msg}", ex),
        "Error completing performance tracking");
    }
}