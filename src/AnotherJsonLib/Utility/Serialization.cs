using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Infra;

namespace AnotherJsonLib.Utility;

/// <summary>
/// Provides JSON serialization and deserialization functionality using System.Text.Json.
/// </summary>
public static class Serialization
{
    private static readonly JsonSerializerOptions DefaultSerializerSettings = new JsonSerializerOptions()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        IncludeFields = true,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Serializes an object to a JSON string.
    /// </summary>
    /// <typeparam name="T">The type of the object to serialize.</typeparam>
    /// <param name="data">The object to serialize.</param>
    /// <param name="options">Optional JSON serializer options to override default settings.</param>
    /// <param name="useDiagnosticTracker">If true, uses DiagnosticPerformanceTracker; otherwise uses standard PerformanceTracker.</param>
    /// <returns>A JSON string representing the object.</returns>
    public static string ToJson<T>(this T data, JsonSerializerOptions? options = null, bool useDiagnosticTracker = false)
    {
        return ToJsonInternal(data, options, useDiagnosticTracker);
    }
    
    // Internal implementation with caller info
    private static string ToJsonInternal<T>(T data, JsonSerializerOptions? options = null, bool useDiagnosticTracker = false)
    {
        options ??= DefaultSerializerSettings;
        const string operationName = "Serialize.ToJson";
        
        if (useDiagnosticTracker)
        {
            using var tracker = new DiagnosticPerformanceTracker(operationName);
            return SerializeWithExceptionHandling(data, options);
        }
        else
        {
            
            using var tracker = JsonLoggerFactory.Instance.CreatePerformanceTracker(JsonLoggerFactory.Instance.GetLogger(nameof(ToJson)), nameof(ToJson));
            return SerializeWithExceptionHandling(data, options);
        }
    }
    
    private static string SerializeWithExceptionHandling<T>(T data, JsonSerializerOptions options)
    {
        ExceptionHelpers.ThrowIfNull(data, nameof(data));
        return ExceptionHelpers.SafeExecuteWithDefault(() =>
            {
                
                return JsonSerializer.Serialize(data, options);
            },
            string.Empty,
            $"Failed to serialize object of type {typeof(T).Name} to JSON") ?? string.Empty;
    }
    
    /// <summary>
    /// Tries to serialize an object to a JSON string.
    /// </summary>
    /// <typeparam name="T">The type of the object to serialize.</typeparam>
    /// <param name="data">The object to serialize.</param>
    /// <param name="result">When this method returns, contains the serialized JSON if successful; otherwise, an empty string.</param>
    /// <param name="options">Optional JSON serializer options to override default settings.</param>
    /// <param name="useDiagnosticTracker">If true, uses DiagnosticPerformanceTracker; otherwise uses standard PerformanceTracker.</param>
    /// <returns>True if serialization was successful; otherwise, false.</returns>
    public static bool TryToJson<T>(this T data, out string result, JsonSerializerOptions? options = null, bool useDiagnosticTracker = false)
    {
        result = ToJson(data, options, useDiagnosticTracker);
        return !string.IsNullOrEmpty(result);
    }

    /// <summary>
    /// Deserializes a JSON string to an object.
    /// </summary>
    /// <typeparam name="T">The type of the object to deserialize.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="options">Optional JSON serializer options to override default settings.</param>
    /// <param name="useDiagnosticTracker">If true, uses DiagnosticPerformanceTracker; otherwise uses standard PerformanceTracker.</param>
    /// <returns>The deserialized object, or default value if deserialization fails.</returns>
    public static T? FromJson<T>(this string json, JsonSerializerOptions? options = null, bool useDiagnosticTracker = false)
    {
        return FromJsonInternal<T>(json, options, useDiagnosticTracker);
    }
    
    // Internal implementation with caller info
    private static T? FromJsonInternal<T>(string json, JsonSerializerOptions? options = null, bool useDiagnosticTracker = false)
    {
        options ??= DefaultSerializerSettings;
        const string operationName = "Serialize.FromJson";
        
        if (useDiagnosticTracker)
        {
            using var tracker = new DiagnosticPerformanceTracker(operationName);
            return DeserializeWithExceptionHandling<T>(json, options);
        }
        else
        {
            using var tracker = JsonLoggerFactory.Instance.CreatePerformanceTracker(JsonLoggerFactory.Instance.GetLogger(nameof(FromJson)), nameof(FromJson));
            return DeserializeWithExceptionHandling<T>(json, options);
        }
    }
    
    private static T? DeserializeWithExceptionHandling<T>(string json, JsonSerializerOptions options)
    {
        return ExceptionHelpers.SafeExecuteWithDefault(() =>
            {
                ExceptionHelpers.ThrowIfNull(json, nameof(json));
                return JsonSerializer.Deserialize<T>(json, options);
            },
            default,
            $"Failed to deserialize JSON to type {typeof(T).Name}");
    }
    
    /// <summary>
    /// Tries to deserialize a JSON string to an object.
    /// </summary>
    /// <typeparam name="T">The type of the object to deserialize.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="result">When this method returns, contains the deserialized object if successful; otherwise, the default value for type T.</param>
    /// <param name="options">Optional JSON serializer options to override default settings.</param>
    /// <param name="useDiagnosticTracker">If true, uses DiagnosticPerformanceTracker; otherwise uses standard PerformanceTracker.</param>
    /// <returns>True if deserialization was successful; otherwise, false.</returns>
    public static bool TryFromJson<T>(this string json, out T? result, JsonSerializerOptions? options = null, bool useDiagnosticTracker = false)
    {
        result = FromJson<T>(json, options, useDiagnosticTracker);
        return result != null;
    }
}