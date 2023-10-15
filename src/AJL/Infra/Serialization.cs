using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace AJL.Infra;

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
    /// Serializes an object to JSON string.
    /// </summary>
    /// <typeparam name="T">The type of the object to serialize.</typeparam>
    /// <param name="data">The object to serialize.</param>
    /// <returns>A JSON string representing the object.</returns>
    public static string ToJson<T>(this T data)
    {
        var logger = LoggerFactory.Instance.GetLogger(typeof(Serialization).FullName ?? string.Empty) ?? throw new NullReferenceException("LoggerFactory is null");
        try
        {
            return JsonSerializer.Serialize(data, DefaultSerializerSettings);
        }
        catch (JsonException ex)
        {
            // Log the exception here for debugging purposes
            logger.LogError(ex, "Failed to serialize JSON");
            return string.Empty;
        }
    }

    /// <summary>
    /// Deserializes a JSON string to an object.
    /// </summary>
    /// <typeparam name="T">The type of the object to deserialize.</typeparam>
    /// <param name="data">The JSON string to deserialize.</param>
    /// <returns>The deserialized object.</returns>
    public static T? FromJson<T>(this string data)
    {
        var logger = LoggerFactory.Instance.GetLogger(typeof(Serialization).FullName ?? string.Empty) ?? throw new NullReferenceException("LoggerFactory is null");
        try
        {
            return JsonSerializer.Deserialize<T>(data, DefaultSerializerSettings)!;
        }
        catch (JsonException ex)
        {
            // Log the exception here for debugging purposes
            logger.LogError(ex, "Failed to deserialize JSON");
            return default;
        }
    }
}