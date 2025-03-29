using System.Diagnostics;

namespace AnotherJsonLib.Infra;

public class DiagnosticPerformanceTracker: IDisposable
{
    private readonly Stopwatch _stopwatch;
    private readonly string _operationName;
    private readonly DiagnosticListener _diagnosticListener;

    /// <summary>
    /// Global flag to enable or disable diagnostic performance tracking.
    /// </summary>
    public static bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Creates a DiagnosticPerformanceTracker that writes start/stop events via DiagnosticListener.
    /// </summary>
    /// <param name="operationName">A descriptive name for the operation.</param>
    public DiagnosticPerformanceTracker(string operationName)
    {
        _operationName = operationName;
        _diagnosticListener = new DiagnosticListener("AnotherJsonLib.Performance");
        _stopwatch = Stopwatch.StartNew();

        if (IsEnabled && _diagnosticListener.IsEnabled("Operation.Start"))
        {
            _diagnosticListener.Write("Operation.Start", new
            {
                Operation = _operationName,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Stops tracking and writes a diagnostic event with the elapsed time.
    /// </summary>
    public void Dispose()
    {
        _stopwatch.Stop();
        if (IsEnabled && _diagnosticListener.IsEnabled("Operation.Stop"))
        {
            _diagnosticListener.Write("Operation.Stop", new
            {
                Operation = _operationName,
                ElapsedMilliseconds = _stopwatch.ElapsedMilliseconds
            });
        }
    }
}