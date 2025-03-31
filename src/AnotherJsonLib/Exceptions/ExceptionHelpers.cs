using System.Runtime.CompilerServices;
using AnotherJsonLib.Helper;
using Microsoft.Extensions.Logging;

namespace AnotherJsonLib.Exceptions;

public static class ExceptionHelpers
{
    /// <summary>
    /// Throws an ArgumentException if the given condition is false.
    /// </summary>
    /// <param name="condition">The condition to check. If false, an exception is thrown.</param>
    /// <param name="message">The error message to include in the exception.</param>
    /// <param name="paramName">The name of the parameter that caused the exception (optional).</param>
    /// <param name="callerMemberName">The name of the calling member (automatically populated).</param>
    /// <param name="callerFilePath">The path to the file containing the calling member (automatically populated).</param>
    /// <param name="callerLineNumber">The line number of the calling member (automatically populated).</param>
    public static void ThrowIfFalse(
        bool condition,
        string message,
        string? paramName = null,
        [CallerMemberName] string? callerMemberName = null,
        [CallerFilePath] string? callerFilePath = null,
        [CallerLineNumber] int callerLineNumber = 0)
    {
        if (!condition)
        {
            LogException(new JsonArgumentException(message, paramName), message, callerMemberName, callerFilePath, callerLineNumber);
            throw new JsonArgumentException(message, paramName);
        }
    }

    /// <summary>
    /// Throws an ArgumentNullException if the given value is null.
    /// </summary>
    /// <param name="value">The value to check. If null, an exception is thrown.</param>
    /// <param name="paramName">The name of the parameter that should not be null.</param>
    /// <param name="callerMemberName">The name of the calling member (automatically populated).</param>
    /// <param name="callerFilePath">The path to the file containing the calling member (automatically populated).</param>
    /// <param name="callerLineNumber">The line number of the calling member (automatically populated).</param>
    public static void ThrowIfNull(
        object? value,
        string paramName,
        [CallerMemberName] string? callerMemberName = null,
        [CallerFilePath] string? callerFilePath = null,
        [CallerLineNumber] int callerLineNumber = 0)
    {
        if (value == null)
        {
            LogException(new ArgumentNullException(paramName), $"Parameter '{paramName}' cannot be null.", callerMemberName, callerFilePath, callerLineNumber);
            throw new ArgumentNullException(paramName);
        }
    }
    
    /// <summary>
    /// Throws an ArgumentNullException if the given value is null or whitespace.
    /// </summary>
    /// <param name="value">The value to check. If null, an exception is thrown.</param>
    /// <param name="paramName">The name of the parameter that should not be null.</param>
    /// <param name="callerMemberName">The name of the calling member (automatically populated).</param>
    /// <param name="callerFilePath">The path to the file containing the calling member (automatically populated).</param>
    /// <param name="callerLineNumber">The line number of the calling member (automatically populated).</param>
    public static void ThrowIfNullOrWhiteSpace(
        string? value,
        string paramName,
        [CallerMemberName] string? callerMemberName = null,
        [CallerFilePath] string? callerFilePath = null,
        [CallerLineNumber] int callerLineNumber = 0)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            LogException(new JsonArgumentException($"Parameter '{paramName}' cannot be null or whitespace."), $"Parameter '{paramName}' cannot be null or whitespace.", callerMemberName, callerFilePath, callerLineNumber);
            throw new JsonArgumentException($"Parameter '{paramName}' cannot be null or whitespace.", paramName);
        }
    }

    /// <summary>
    /// Executes an operation safely, catching any exceptions and handling them using the provided exception creator.
    /// If an exception occurs, a JsonLibException is created and thrown.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="exceptionCreator">A function that takes the exception and a message and returns a JsonLibException.</param>
    /// <param name="fallbackMessage">A fallback message to use if the operation fails.</param>
    /// <param name="callerMemberName">The name of the calling member (automatically populated).</param>
    /// <param name="callerFilePath">The path to the file containing the calling member (automatically populated).</param>
    /// <param name="callerLineNumber">The line number of the calling member (automatically populated).</param>
    /// <returns>The result of the operation.</returns>
    public static TResult? SafeExecute<TResult>(
        Func<TResult?> operation,
        Func<Exception, string, JsonLibException> exceptionCreator,
        string fallbackMessage = "Operation failed",
        [CallerMemberName] string? callerMemberName = null,
        [CallerFilePath] string? callerFilePath = null,
        [CallerLineNumber] int callerLineNumber = 0)
    {
        try
        {
            return operation();
        }
        catch (Exception ex)
        {
            var exception = exceptionCreator(ex, fallbackMessage);
            LogException(exception, fallbackMessage, callerMemberName, callerFilePath, callerLineNumber);
            throw exception; // Re-throw the custom exception
        }
    }

    /// <summary>
    /// Executes an operation safely, catching any exceptions and returning a default value if an exception occurs.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="defaultValue">The default value to return if an exception occurs.</param>
    /// <param name="fallbackMessage">A fallback message to log if the operation fails.</param>
    /// <param name="callerMemberName">The name of the calling member (automatically populated).</param>
    /// <param name="callerFilePath">The path to the file containing the calling member (automatically populated).</param>
    /// <param name="callerLineNumber">The line number of the calling member (automatically populated).</param>
    /// <returns>The result of the operation, or the default value if an exception occurs.</returns>
    public static TResult? SafeExecuteWithDefault<TResult>(
        Func<TResult?> operation,
        TResult? defaultValue,
        string fallbackMessage = "Operation failed",
        [CallerMemberName] string? callerMemberName = null,
        [CallerFilePath] string? callerFilePath = null,
        [CallerLineNumber] int callerLineNumber = 0)
    {
        try
        {
            return operation();
        }
        catch (Exception ex)
        {
            LogException(ex, fallbackMessage, callerMemberName, callerFilePath, callerLineNumber);
            return defaultValue;
        }
    }

    /// <summary>
    /// Executes an action safely, catching any exceptions and handling them using the provided exception creator.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="exceptionCreator">A function that takes the exception and a message and returns a JsonLibException.</param>
    /// <param name="fallbackMessage">A fallback message to use if the action fails.</param>
    /// <param name="callerMemberName">The name of the calling member (automatically populated).</param>
    /// <param name="callerFilePath">The path to the file containing the calling member (automatically populated).</param>
    /// <param name="callerLineNumber">The line number of the calling member (automatically populated).</param>
    public static void SafeExecute(
        Action action,
        Func<Exception, string, JsonLibException> exceptionCreator,
        string fallbackMessage = "Operation failed",
        [CallerMemberName] string? callerMemberName = null,
        [CallerFilePath] string? callerFilePath = null,
        [CallerLineNumber] int callerLineNumber = 0)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            var exception = exceptionCreator(ex, fallbackMessage);
            LogException(exception, fallbackMessage, callerMemberName, callerFilePath, callerLineNumber);
            throw exception; // Re-throw the custom exception
        }
    }

    /// <summary>
    /// Logs the given exception with the specified message.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The message to log along with the exception.</param>
    /// <param name="callerMemberName">The name of the calling member (automatically populated).</param>
    /// <param name="callerFilePath">The path to the file containing the calling member (automatically populated).</param>
    /// <param name="callerLineNumber">The line number of the calling member (automatically populated).</param>
    private static void LogException(
        Exception exception,
        string message,
        [CallerMemberName] string? callerMemberName = null,
        [CallerFilePath] string? callerFilePath = null,
        [CallerLineNumber] int callerLineNumber = 0)
    {
        // Get the class name from the file path
        string className = GetClassNameFromFilePath(callerFilePath);

        // Create a logger for the class
        ILogger logger = JsonLoggerFactory.Instance.GetLogger(className);

        // Log the exception
        logger.LogError(exception, "[{ClassName}.{CallerMemberName}:{CallerLineNumber}] {Message}", className, callerMemberName, callerLineNumber, message);
    }

    /// <summary>
    /// Creates a logger for the given category name using the JsonLoggerFactory.
    /// </summary>
    /// <param name="categoryName">The name of the logger category.</param>
    /// <returns>An ILogger instance.</returns>
    public static ILogger CreateLogger(string categoryName)
    {
        return JsonLoggerFactory.Instance.GetLogger(categoryName);
    }

    /// <summary>
    /// Extracts the class name from a file path.
    /// </summary>
    /// <param name="filePath">The file path to extract the class name from.</param>
    /// <returns>The extracted class name, or null if extraction fails.</returns>
    private static string GetClassNameFromFilePath(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return "UnknownClass";

        string fileName = Path.GetFileNameWithoutExtension(filePath);
        return fileName;
    }
}