using System.Diagnostics;
using System.Text.Json;
using AnotherJsonLib.Domain;
using AnotherJsonLib.Exceptions;
using Microsoft.Extensions.Logging;
using AnotherJsonLib.Infra;

namespace AnotherJsonLib.Utility;

/// <summary>
/// Provides functionality to compute differences between JSON strings,
/// identifying added, modified, and removed properties.
/// </summary>
public static class JsonDiffer
{
    private static readonly ILogger Logger = JsonLoggerFactory.Instance.GetLogger(nameof(JsonDiffer));
    
    /// <summary>
    /// Computes a bidirectional diff between two JSON strings.
    /// Returns keys added, removed, or modified (with old and new values).
    /// </summary>
    /// <param name="originalJson">The original JSON string.</param>
    /// <param name="newJson">The new JSON string to compare against the original.</param>
    /// <returns>A JsonDiffResult containing the differences between the two JSON strings.</returns>
    /// <exception cref="JsonArgumentException">Thrown when the input JSON is null, empty, or not valid JSON.</exception>
    /// <exception cref="JsonOperationException">Thrown when an operation fails during the diff process.</exception>
    /// <exception cref="JsonParsingException">Thrown when the JSON cannot be parsed.</exception>
    /// <code>
    /// string originalJson = "{\"name\":\"John\"}";
    /// string newJson = "{\"name\":\"Jane\", \"age\":30}";
    /// JsonDiffResult diff = JsonDiffer.ComputeDiff(originalJson, newJson);
    /// // diff.Added will contain {"age": 30}
    /// // diff.Removed will be empty
    /// // diff.Modified will contain {"name": { OldValue = "John", NewValue = "Jane" }}
    /// </code>
    public static JsonDiffResult ComputeDiff(this string originalJson, string newJson)
    {
        using var performance = new PerformanceTracker(Logger, "ComputeDiff");
        
        // Validate inputs
        ExceptionHelpers.ThrowIfNull(originalJson, nameof(originalJson));
        ExceptionHelpers.ThrowIfNull(newJson, nameof(newJson));
        
        ExceptionHelpers.ThrowIfFalse(!string.IsNullOrEmpty(originalJson), "The original JSON string cannot be empty", nameof(originalJson));
        ExceptionHelpers.ThrowIfFalse(!string.IsNullOrEmpty(newJson), "The new JSON string cannot be empty", nameof(newJson));

        return ExceptionHelpers.SafeExecute(() => 
        {
            var result = new JsonDiffResult();
            var comparer = new JsonElementComparer();

            var originalDict = originalJson.FromJson<Dictionary<string, JsonElement>>(options: new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var newDict = newJson.FromJson<Dictionary<string, JsonElement>>(options: new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            ExceptionHelpers.ThrowIfNull(originalDict, nameof(originalDict));
            ExceptionHelpers.ThrowIfNull(newDict, nameof(newDict));

            // Added or modified keys.
            Debug.Assert(newDict != null, nameof(newDict) + " != null");
            Debug.Assert(originalDict != null, nameof(originalDict) + " != null");
            foreach (var kvp in newDict)
            {
                if (!originalDict.ContainsKey(kvp.Key))
                {
                    var addedValue = comparer.ConvertToValueType(kvp.Value) ?? new JsonElement();
                    result.Added[kvp.Key] = addedValue;
                }
                else if (!comparer.Equals(originalDict[kvp.Key], kvp.Value))
                {
                    var oldValue = comparer.ConvertToValueType(originalDict[kvp.Key]) ?? new JsonElement();
                    var newValue = comparer.ConvertToValueType(kvp.Value) ?? new JsonElement();
                    result.Modified[kvp.Key] = new DiffEntry
                    {
                        OldValue = oldValue,
                        NewValue = newValue
                    };
                }
            }

            // Removed keys.
            foreach (var kvp in originalDict)
            {
                if (!newDict.ContainsKey(kvp.Key))
                {
                    var removedValue = comparer.ConvertToValueType(kvp.Value) ?? new JsonElement();
                    result.Removed[kvp.Key] = removedValue;
                }
            }

            Logger.LogDebug("Computed JSON diff: {AddedCount} keys added, {RemovedCount} keys removed, {ModifiedCount} keys modified",
                result.Added.Count, result.Removed.Count, result.Modified.Count);

            return result;
        }, (ex, msg) => 
        {
            // Handle different exception types
            if (ex is JsonException)
                return new JsonParsingException("Invalid JSON format during diff computation.", ex);
            
            return new JsonOperationException("Failed to compute JSON diff: " + msg, ex);
        }, "Failed to compute JSON diff between the provided JSON strings") ?? new JsonDiffResult();
    }

    /// <summary>
    /// Attempts to compute a bidirectional diff between two JSON strings.
    /// Returns false if the operation fails and doesn't throw exceptions.
    /// </summary>
    /// <param name="originalJson">The original JSON string.</param>
    /// <param name="newJson">The new JSON string to compare against the original.</param>
    /// <param name="result">When successful, contains the diff result; otherwise, null.</param>
    /// <returns>True if the diff was successfully computed; otherwise, false.</returns>
    public static bool TryComputeDiff(this string originalJson, string newJson, out JsonDiffResult? result)
    {
        result = ExceptionHelpers.SafeExecuteWithDefault(
            () => ComputeDiff(originalJson, newJson),
            null,
            "Failed to compute JSON diff"
        );

        return result != null;
    }
}