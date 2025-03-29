using System.Text.Json;
using AnotherJsonLib.Exceptions;
using Microsoft.Extensions.Logging;

namespace AnotherJsonLib.Helper;

/// <summary>
/// Extension methods for JsonElement to simplify working with System.Text.Json.
/// </summary>
/// <remarks>
/// These utilities help bridge the gap between JsonElement objects and native .NET types,
/// making it easier to extract and manipulate JSON data without writing repetitive code.
/// </remarks>
public static class JsonElementExtensions
{
    private static readonly ILogger Logger = JsonLoggerFactory.Instance.GetLogger(nameof(JsonElementExtensions));

    /// <summary>
    /// Clones the JsonElement's value as a native .NET object.
    /// For objects and arrays, returns nested dictionaries or lists.
    /// Numeric values are preserved in their original form:
    /// - If the raw text does not contain a decimal point or exponent, it's parsed as a long.
    /// - Otherwise, it's parsed as a decimal.
    /// </summary>
    /// <param name="element">The JsonElement to clone.</param>
    /// <returns>
    /// A native .NET object representing the JsonElement's value:
    /// - JsonValueKind.Object: Dictionary&lt;string, object?&gt;
    /// - JsonValueKind.Array: List&lt;object?&gt;
    /// - JsonValueKind.Number: long, decimal, or double depending on the value
    /// - JsonValueKind.String: string
    /// - JsonValueKind.True/False: bool
    /// - JsonValueKind.Null: null
    /// </returns>
    /// <exception cref="JsonArgumentException">Thrown if element is invalid.</exception>
    /// <exception cref="JsonOperationException">Thrown if the clone operation fails.</exception>
    /// <example>
    /// <code>
    /// using System.Text.Json;
    /// 
    /// // Parse some JSON
    /// string json = @"{""name"":""John"",""age"":30,""scores"":[95,87,92]}";
    /// using var doc = JsonDocument.Parse(json);
    /// 
    /// // Clone the "scores" array to a list
    /// var scoresElement = doc.RootElement.GetProperty("scores");
    /// var scoresList = scoresElement.CloneValue() as List&lt;object?&gt;;
    /// 
    /// // Now you can manipulate the list
    /// scoresList.Add(100);
    /// scoresList.Remove(87);
    /// </code>
    /// </example>
    public static object? CloneValue(this JsonElement element)
    {
        using var performance = new PerformanceTracker(Logger, nameof(CloneValue));
        
        return ExceptionHelpers.SafeExecute<object?>(() =>
        {
            Logger.LogTrace("Cloning JsonElement of type {ValueKind}", element.ValueKind);
            
            switch (element.ValueKind)
            {
                case JsonValueKind.Null:
                    return null;
                    
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return element.GetBoolean();
                    
                case JsonValueKind.Number:
                    string raw = element.GetRawText();
                    if (raw.Contains('.') || raw.Contains('e') || raw.Contains('E'))
                    {
                        if (decimal.TryParse(raw, out decimal decValue))
                            return decValue;
                        else if (double.TryParse(raw, out double dblValue))
                            return dblValue;
                        else
                            return raw;
                    }
                    else
                    {
                        if (long.TryParse(raw, out long longValue))
                            return longValue;
                        else if (decimal.TryParse(raw, out decimal decValue))
                            return decValue;
                        else
                            return raw;
                    }
                    
                case JsonValueKind.String:
                    return element.GetString();
                    
                case JsonValueKind.Array:
                    var list = new List<object?>();
                    foreach (var item in element.EnumerateArray())
                        list.Add(item.CloneValue());
                    return list;
                    
                case JsonValueKind.Object:
                    var dict = new Dictionary<string, object?>();
                    foreach (var prop in element.EnumerateObject())
                        dict[prop.Name] = prop.Value.CloneValue();
                    return dict;
                    
                default:
                    Logger.LogWarning("Unexpected JsonValueKind: {ValueKind}", element.ValueKind);
                    return element.ToString();
            }
        }, 
        (ex, msg) => new JsonOperationException($"Failed to clone JsonElement of type {element.ValueKind}: {msg}", ex),
        $"Error cloning JsonElement of type {element.ValueKind}");
    }
    
    /// <summary>
    /// Attempts to clone a JsonElement to a native .NET object without throwing exceptions.
    /// </summary>
    /// <param name="element">The JsonElement to clone.</param>
    /// <param name="result">When successful, contains the cloned object; otherwise, null.</param>
    /// <returns>True if cloning was successful; otherwise, false.</returns>
    /// <example>
    /// <code>
    /// // Safe cloning that won't throw exceptions
    /// if (element.TryCloneValue(out var clonedValue))
    /// {
    ///     // Use the cloned value
    /// }
    /// else
    /// {
    ///     // Handle failure case
    /// }
    /// </code>
    /// </example>
    public static bool TryCloneValue(this JsonElement element, out object? result)
    {
        result = ExceptionHelpers.SafeExecuteWithDefault(
            () => CloneValue(element),
            null,
            $"Failed to clone JsonElement of type {element.ValueKind}"
        );
        
        return result != null || element.ValueKind == JsonValueKind.Null;
    }
}