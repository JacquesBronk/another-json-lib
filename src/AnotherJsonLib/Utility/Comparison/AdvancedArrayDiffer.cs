using System.Text.Json;
using AnotherJsonLib.Domain;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Infra;
using Microsoft.Extensions.Logging;

namespace AnotherJsonLib.Utility.Comparison;

/// <summary>
/// Provides advanced diffing capabilities for JSON arrays.
/// </summary>
public static class AdvancedArrayDiffer
{
    private static readonly ILogger Logger = ExceptionHelpers.CreateLogger(nameof(AdvancedArrayDiffer));

    /// <summary>
    /// Generates a list of JSON Patch operations (add, remove, move) for differences between two JSON arrays.
    /// The diffing algorithm used depends on the provided ArrayDiffMode.
    /// 
    /// Use this method when you need to create a minimal set of operations to transform one 
    /// JSON array into another, such as when implementing PATCH endpoints in REST APIs or 
    /// when synchronizing data between client and server.
    /// 
    /// <example>
    /// <code>
    /// // Create an array differ instance
    /// var differ = new AdvancedArrayDiffer();
    /// 
    /// // Original array: [1, 2, 3]
    /// var originalJson = JsonDocument.Parse("[1, 2, 3]").RootElement;
    /// 
    /// // Updated array: [1, 4, 3, 5]
    /// var updatedJson = JsonDocument.Parse("[1, 4, 3, 5]").RootElement;
    /// 
    /// // Generate patch operations
    /// var patchOperations = differ.GenerateArrayPatch("/items", originalJson, updatedJson);
    /// 
    /// // Result will be a list containing:
    /// // - Replace operation: { "op": "replace", "path": "/items/1", "value": 4 }
    /// // - Add operation: { "op": "add", "path": "/items/3", "value": 5 }
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="basePath">The JSON Pointer path representing the array.</param>
    /// <param name="originalArray">The original JSON array element.</param>
    /// <param name="updatedArray">The updated JSON array element.</param>
    /// <param name="mode">The array diff mode: Full or Fast.</param>
    /// <returns>A list of JSON Patch operations representing the diff.</returns>
    /// <exception cref="JsonArgumentException">Thrown when inputs are null or not arrays</exception>
    /// <exception cref="JsonOperationException">Thrown when array comparison fails</exception>
    
    public static List<JsonPatchOperation>? GenerateArrayPatch(
        string basePath,
        JsonElement originalArray,
        JsonElement updatedArray,
        ArrayDiffMode mode = ArrayDiffMode.Full)
    {
        Logger.LogDebug("Generating array patch with mode {Mode} for path {Path}", mode, basePath);
        
        // Validate parameters
        ExceptionHelpers.ThrowIfNull(basePath, nameof(basePath));
        ExceptionHelpers.ThrowIfFalse(
            originalArray.ValueKind == JsonValueKind.Array,
            "Original element must be an array");
        ExceptionHelpers.ThrowIfFalse(
            updatedArray.ValueKind == JsonValueKind.Array,
            "Updated element must be an array");

        using var performance = new PerformanceTracker(Logger, nameof(GenerateArrayPatch));
        return ExceptionHelpers.SafeExecute(() =>
            {
                var ops = new List<JsonPatchOperation>();

                // Convert arrays to lists for easier handling.
                var origList = originalArray.EnumerateArray().ToList();
                var updList = updatedArray.EnumerateArray().ToList();

                Logger.LogTrace("Original array length: {OriginalLength}, Updated array length: {UpdatedLength}",
                    origList.Count, updList.Count);


                if (mode == ArrayDiffMode.Full)
                {
                    Logger.LogDebug("Using full diff algorithm");

                    // Full diff: compute LCS.
                    var lcs = ComputeLcs(origList, updList);
                    var origIndicesInLcs = new HashSet<int>(lcs.Select(pair => pair.OrigIndex));
                    var updIndicesInLcs = new HashSet<int>(lcs.Select(pair => pair.UpdIndex));

                    // Generate remove operations (reverse order).
                    for (int i = origList.Count - 1; i >= 0; i--)
                    {
                        if (!origIndicesInLcs.Contains(i))
                        {
                            ops.Add(new JsonPatchOperation
                            {
                                Op = "remove",
                                Path = $"{basePath}/{i}"
                            });
                        }
                    }

                    // Generate add operations (forward order).
                    for (int j = 0; j < updList.Count; j++)
                    {
                        if (!updIndicesInLcs.Contains(j))
                        {
                            ops.Add(new JsonPatchOperation
                            {
                                Op = "add",
                                Path = $"{basePath}/{j}",
                                Value = (JsonElement?)updList[j].CloneValue()
                            });
                        }
                    }

                    // Optimize for moves.
                    OptimizePatchOperations(ops, basePath, origList);
                }
                else // Fast diff
                {
                    Logger.LogDebug("Using fast diff algorithm");

                    int minCount = Math.Min(origList.Count, updList.Count);
                    for (int i = 0; i < minCount; i++)
                    {
                        if (!JsonElementsAreEqual(origList[i], updList[i]))
                        {
                            ops.Add(new JsonPatchOperation
                            {
                                Op = "replace",
                                Path = $"{basePath}/{i}",
                                Value = (JsonElement?)updList[i].CloneValue()
                            });
                        }
                    }

                    // If original array is longer, remove the extra elements.
                    for (int i = origList.Count - 1; i >= minCount; i--)
                    {
                        ops.Add(new JsonPatchOperation
                        {
                            Op = "remove",
                            Path = $"{basePath}/{i}"
                        });
                    }

                    // If updated array is longer, add the extra elements.
                    for (int i = minCount; i < updList.Count; i++)
                    {
                        ops.Add(new JsonPatchOperation
                        {
                            Op = "add",
                            Path = $"{basePath}/{i}",
                            Value = (JsonElement?)updList[i].CloneValue()
                        });
                    }
                }

                Logger.LogInformation("Generated {Count} patch operations", ops.Count);
                return ops;
            },
            (ex, msg) => new JsonOperationException(msg, ex),
            $"Failed to generate array patch with mode {mode}"
        );
    }

    /// <summary>
    /// Represents a pair of matching indices between original and updated arrays.
    /// </summary>
    private class IndexPair
    {
        public int OrigIndex { get; init; }
        public int UpdIndex { get; init; }
    }

    /// <summary>
    /// Computes the Longest Common Subsequence (LCS) of two lists of JsonElement.
    /// </summary>
    private static List<IndexPair> ComputeLcs(List<JsonElement> orig, List<JsonElement> upd)
    {
        ExceptionHelpers.ThrowIfNull(orig, nameof(orig));
        ExceptionHelpers.ThrowIfNull(upd, nameof(upd));
        return ExceptionHelpers.SafeExecute<List<IndexPair>?>(() =>
            {
                int m = orig.Count, n = upd.Count;
                var dp = new int[m + 1, n + 1];

                // Build LCS table.
                for (int i = 0; i < m; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        if (JsonElementsAreEqual(orig[i], upd[j]))
                            dp[i + 1, j + 1] = dp[i, j] + 1;
                        else
                            dp[i + 1, j + 1] = Math.Max(dp[i + 1, j], dp[i, j + 1]);
                    }
                }

                // Backtrack to recover the LCS.
                var lcs = new List<IndexPair>();
                int x = m, y = n;
                while (x > 0 && y > 0)
                {
                    if (JsonElementsAreEqual(orig[x - 1], upd[y - 1]))
                    {
                        lcs.Add(new IndexPair { OrigIndex = x - 1, UpdIndex = y - 1 });
                        x--;
                        y--;
                    }
                    else if (dp[x - 1, y] >= dp[x, y - 1])
                        x--;
                    else
                        y--;
                }

                lcs.Reverse();
                return lcs;
            },
            (ex, msg) => new JsonOperationException($"Error in LCS computation: {msg}", ex),
            "Failed to compute longest common subsequence") ?? new List<IndexPair>();
    }


    /// <summary>
    /// Optimizes patch operations by converting matching remove and add pairs into a move operation.
    /// </summary>
    private static void OptimizePatchOperations(List<JsonPatchOperation>? ops, string basePath,
        List<JsonElement> originalList)
    {
        // Check for null but don't throw - if null, nothing to optimize
        if (ops == null || ops.Count == 0) return;

        ExceptionHelpers.ThrowIfNull(basePath, nameof(basePath));
        ExceptionHelpers.ThrowIfNull(originalList, nameof(originalList));

        ExceptionHelpers.SafeExecute(() =>
            {
                var removeOps = ops.Where(o => o.Op == "remove" && IsArrayIndexPath(o.Path)).ToList();
                foreach (var removeOp in removeOps)
                {
                    if (!TryParseLastIndex(removeOp.Path, out int remIndex))
                        continue;
                    var origValue = originalList[remIndex].CloneValue();
                    var addOp = ops.FirstOrDefault(o => o.Op == "add" &&
                                                        IsSameArrayBase(o.Path, removeOp.Path, basePath) &&
                                                        ValuesEqual(o.Value, origValue));
                    if (addOp != null)
                    {
                        ops.Remove(removeOp);
                        ops.Remove(addOp);
                        ops.Add(new JsonPatchOperation
                        {
                            Op = "move",
                            From = removeOp.Path,
                            Path = addOp.Path
                        });
                    }
                }
            },
            (ex, msg) => new JsonOperationException($"Failed to optimize patch operations: {msg}", ex),
            "Error during patch optimization");
    }

    /// <summary>
    /// Checks if the path represents an array index.
    /// </summary>
    private static bool IsArrayIndexPath(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length > 0 && int.TryParse(segments.Last(), out _);
    }

    /// <summary>
    /// Checks whether two paths share the same base.
    /// </summary>
    private static bool IsSameArrayBase(string path1, string path2, string basePath)
    {
        return path1.StartsWith(basePath) && path2.StartsWith(basePath);
    }

    /// <summary>
    /// Attempts to parse the last segment of the JSON Pointer path as an integer.
    /// </summary>
    private static bool TryParseLastIndex(string path, out int index)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            index = -1;
            return false;
        }

        return int.TryParse(segments.Last(), out index);
    }

    /// <summary>
    /// Compares two objects by serializing them to JSON.
    /// </summary>
    private static bool ValuesEqual(object? value1, object? value2)
    {
        if (value1 == null || value2 == null)
            return value1 == value2;
        string s1 = JsonSerializer.Serialize(value1);
        string s2 = JsonSerializer.Serialize(value2);
        return s1 == s2;
    }

    private static bool JsonElementsAreEqual(JsonElement a, JsonElement b)
    {
        return ExceptionHelpers.SafeExecute(() =>
            {
                // Special cases for null or different kinds
                if (a.ValueKind != b.ValueKind)
                    return false;

                // Handle different JSON value types
                switch (a.ValueKind)
                {
                    case JsonValueKind.String:
                        return a.GetString() == b.GetString();
                    case JsonValueKind.Number:
                        // For numeric comparison, convert to decimal for accurate comparison
                        // This handles differences in formatting like 1.0 vs 1
                        if (a.TryGetDecimal(out decimal aDecimal) && b.TryGetDecimal(out decimal bDecimal))
                            return aDecimal == bDecimal;
                        // Fallback to raw text if decimal conversion fails
                        return a.GetRawText() == b.GetRawText();
                    case JsonValueKind.True:
                    case JsonValueKind.False:
                    case JsonValueKind.Null:
                        return true; // Both have the same ValueKind, so they're equal
                    case JsonValueKind.Object:
                        return CompareJsonObjects(a, b);
                    case JsonValueKind.Array:
                        return CompareJsonArrays(a, b);
                    default:
                        return false;
                }
            },
            (ex, msg) => new JsonOperationException($"Error comparing JSON elements: {msg}", ex),
            "Failed to compare JSON elements");
    }

    private static bool CompareJsonObjects(JsonElement a, JsonElement b)
    {
        // Check if they have the same number of properties
        int aCount = a.EnumerateObject().Count();
        int bCount = b.EnumerateObject().Count();
        if (aCount != bCount)
            return false;

        // Check if all properties in 'a' exist in 'b' with equal values
        foreach (JsonProperty property in a.EnumerateObject())
        {
            // Check if property exists in b
            if (!b.TryGetProperty(property.Name, out JsonElement bValue))
                return false;

            // Recursively compare the property values
            if (!JsonElementsAreEqual(property.Value, bValue))
                return false;
        }

        return true;
    }

    private static bool CompareJsonArrays(JsonElement a, JsonElement b)
    {
        // Check if they have the same length
        int aLength = a.GetArrayLength();
        int bLength = b.GetArrayLength();
        if (aLength != bLength)
            return false;

        // Compare each element in sequence
        for (int i = 0; i < aLength; i++)
        {
            if (!JsonElementsAreEqual(a[i], b[i]))
                return false;
        }

        return true;
    }
}