using System.Text.Json;
using System.Text.RegularExpressions;
using AnotherJsonLib.Domain;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Helper;
using Microsoft.Extensions.Logging;

namespace AnotherJsonLib.Utility.Operations;

/// <summary>
/// Generates JSON Patch documents according to RFC 6902 standard by comparing two JSON objects.
/// </summary>
public class JsonPatchGenerator
{
    private static readonly ILogger Logger = JsonLoggerFactory.Instance.GetLogger(nameof(JsonPatchGenerator));
    private static readonly Regex ArrayIndexRegex = new Regex(@"\/\d+(?:\/|$)", RegexOptions.Compiled);
    private readonly PatchGeneratorOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonPatchGenerator"/> class with default options.
    /// </summary>
    public JsonPatchGenerator() : this(new PatchGeneratorOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonPatchGenerator"/> class with specified options.
    /// </summary>
    /// <param name="options">Options to customize the patch generation behavior.</param>
    /// <exception cref="JsonArgumentException">Thrown when options is null.</exception>
    public JsonPatchGenerator(PatchGeneratorOptions options)
    {
        ExceptionHelpers.ThrowIfNull(options, nameof(options));
        _options = options;
    }

    /// <summary>
    /// Generates a JSON Patch document by comparing original and updated JSON.
    /// </summary>
    /// <param name="originalJson">The original JSON string.</param>
    /// <param name="updatedJson">The updated JSON string.</param>
    /// <returns>A list of JSON Patch operations.</returns>
    /// <exception cref="JsonArgumentException">Thrown when either JSON string is null or empty.</exception>
    /// <exception cref="JsonParsingException">Thrown when either input is not valid JSON.</exception>
    /// <exception cref="JsonOperationException">Thrown when the patch generation fails.</exception>
    public List<JsonPatchOperation> GeneratePatch(string originalJson, string updatedJson)
    {
        using var performance = new PerformanceTracker(Logger, nameof(GeneratePatch));
        
        // Validate inputs
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(originalJson, nameof(originalJson));
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(updatedJson, nameof(updatedJson));
        
        return ExceptionHelpers.SafeExecute(() => 
        {
            Logger.LogDebug("Generating patch between {OriginalLength} char original and {UpdatedLength} char updated JSON", 
                originalJson.Length, updatedJson.Length);
            
            var originalDoc = JsonDocument.Parse(originalJson);
            var updatedDoc = JsonDocument.Parse(updatedJson);
            
            var patchOperations = new List<JsonPatchOperation>();
            GeneratePatchOperations("", originalDoc.RootElement, updatedDoc.RootElement, patchOperations);
            
            if (_options.OptimizePatch)
            {
                OptimizePatchOperations(patchOperations);
            }
            
            Logger.LogDebug("Generated {OperationCount} patch operations", patchOperations.Count);
            return patchOperations;
        }, (ex, msg) => 
        {
            if (ex is JsonException jsonEx)
                return new JsonParsingException("Failed to generate patch: Invalid JSON format", jsonEx);
                
            return new JsonOperationException("Failed to generate JSON patch: " + msg, ex);
        }, "Failed to generate JSON patch document") ?? new List<JsonPatchOperation>();
    }
    
    /// <summary>
    /// Attempts to generate a JSON Patch document without throwing exceptions.
    /// </summary>
    /// <param name="originalJson">The original JSON string.</param>
    /// <param name="updatedJson">The updated JSON string.</param>
    /// <param name="patchOperations">When successful, contains the list of patch operations; otherwise, an empty list.</param>
    /// <returns>True if the patch was successfully generated; otherwise, false.</returns>
    public bool TryGeneratePatch(string originalJson, string updatedJson, out List<JsonPatchOperation> patchOperations)
    {
        patchOperations = new List<JsonPatchOperation>();
        
        if (string.IsNullOrWhiteSpace(originalJson) || string.IsNullOrWhiteSpace(updatedJson))
        {
            return false;
        }
        
        try
        {
            patchOperations = GeneratePatch(originalJson, updatedJson);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to generate JSON patch");
            return false;
        }
    }
    
    /// <summary>
    /// Generates a JSON Patch document as a string in RFC 6902 format.
    /// </summary>
    /// <param name="originalJson">The original JSON string.</param>
    /// <param name="updatedJson">The updated JSON string.</param>
    /// <returns>A JSON string containing the patch operations.</returns>
    /// <exception cref="JsonArgumentException">Thrown when either JSON string is null or empty.</exception>
    /// <exception cref="JsonParsingException">Thrown when either input is not valid JSON.</exception>
    /// <exception cref="JsonOperationException">Thrown when the patch generation fails.</exception>
    public string GeneratePatchAsJson(string originalJson, string updatedJson)
    {
        using var performance = new PerformanceTracker(Logger, nameof(GeneratePatchAsJson));
        
        var operations = GeneratePatch(originalJson, updatedJson);
        
        return ExceptionHelpers.SafeExecute(() => JsonSerializer.Serialize(operations, new JsonSerializerOptions
        {
            WriteIndented = _options.FormatOutput,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }), (ex, msg) => new JsonOperationException("Failed to serialize JSON patch: " + msg, ex), "Failed to serialize JSON patch to string") ?? string.Empty;
    }

    /// <summary>
    /// Recursively generates patch operations by comparing two JSON elements.
    /// </summary>
    private void GeneratePatchOperations(string path, JsonElement original, JsonElement updated, 
        List<JsonPatchOperation> ops)
    {
        // Handle different or null values
        if (original.ValueKind != updated.ValueKind)
        {
            // Different types - replace entire value
            ops.Add(new JsonPatchOperation
            {
                Op = "replace",
                Path = path,
                Value = updated
            });
            return;
        }

        switch (original.ValueKind)
        {
            case JsonValueKind.Object:
                CompareObjects(path, original, updated, ops);
                break;

            case JsonValueKind.Array:
                if (_options.UseArrayDiffAlgorithm)
                {
                    GenerateArrayDiff(path, original, updated, ops);
                }
                else
                {
                    // Simple replace for arrays when diffing is disabled
                    if (!JsonElementsAreEqual(original, updated))
                    {
                        ops.Add(new JsonPatchOperation
                        {
                            Op = "replace",
                            Path = path,
                            Value = updated
                        });
                    }
                }
                break;

            default:
                // Handle primitive values
                if (!JsonElementsAreEqual(original, updated))
                {
                    ops.Add(new JsonPatchOperation
                    {
                        Op = "replace",
                        Path = path,
                        Value = updated
                    });
                }
                break;
        }
    }
    
    /// <summary>
    /// Compares two JSON objects and generates appropriate patch operations.
    /// </summary>
    private void CompareObjects(string path, JsonElement original, JsonElement updated, List<JsonPatchOperation> ops)
    {
        // Track properties in both objects for comparison
        var originalProperties = new HashSet<string>();
        
        // Process original properties
        foreach (var property in original.EnumerateObject())
        {
            originalProperties.Add(property.Name);
            
            string propertyPath = CombinePath(path, property.Name);
            
            if (updated.TryGetProperty(property.Name, out JsonElement updatedValue))
            {
                // Property exists in both - recursively compare
                GeneratePatchOperations(propertyPath, property.Value, updatedValue, ops);
            }
            else
            {
                // Property was removed
                if (!_options.IgnoreRemovals)
                {
                    ops.Add(new JsonPatchOperation
                    {
                        Op = "remove",
                        Path = propertyPath
                    });
                }
            }
        }
        
        // Find added properties
        foreach (var property in updated.EnumerateObject())
        {
            if (!originalProperties.Contains(property.Name))
            {
                string propertyPath = CombinePath(path, property.Name);
                ops.Add(new JsonPatchOperation
                {
                    Op = "add",
                    Path = propertyPath,
                    Value = property.Value
                });
            }
        }
    }

    /// <summary>
    /// Generates patch operations for array differences using Longest Common Subsequence algorithm.
    /// </summary>
    private void GenerateArrayDiff(string path, JsonElement original, JsonElement updated, 
        List<JsonPatchOperation> ops)
    {
        // Convert arrays to lists for processing
        var originalList = original.EnumerateArray().ToList();
        var updatedList = updated.EnumerateArray().ToList();
        
        if (originalList.Count == 0 && updatedList.Count > 0)
        {
            // Simple case: empty original array, just replace with updated array
            ops.Add(new JsonPatchOperation
            {
                Op = "replace",
                Path = path,
                Value = updated
            });
            return;
        }
        
        if (updatedList.Count == 0 && originalList.Count > 0)
        {
            // Simple case: empty updated array, just replace with empty array
            ops.Add(new JsonPatchOperation
            {
                Op = "replace",
                Path = path,
                Value = updated
            });
            return;
        }
        
        // If arrays are small enough, use the LCS algorithm for optimal diffing
        if (originalList.Count <= _options.MaxArraySizeForLcs && updatedList.Count <= _options.MaxArraySizeForLcs)
        {
            DiffArraysWithLcs(path, originalList, updatedList, ops);
        }
        else
        {
            // For larger arrays, use a simpler but less optimal algorithm
            DiffArraysSimple(path, originalList, updatedList, ops);
        }
    }
    
    /// <summary>
    /// Diffs two arrays using the Longest Common Subsequence algorithm.
    /// </summary>
    private void DiffArraysWithLcs(string path, List<JsonElement> originalList, List<JsonElement> updatedList, 
        List<JsonPatchOperation> ops)
    {
        // Calculate LCS to find matching elements
        List<IndexPair> lcs = ComputeLcs(originalList, updatedList);
        
        // Sort by original index to process removals from end to beginning
        var removals = new List<int>();
        var currentLcsIndex = 0;
        
        // Identify elements to remove (those not in LCS)
        for (int i = 0; i < originalList.Count; i++)
        {
            if (currentLcsIndex < lcs.Count && lcs[currentLcsIndex].OrigIndex == i)
            {
                currentLcsIndex++;
            }
            else
            {
                removals.Add(i);
            }
        }
        
        // Process removals from end to beginning to maintain indexes
        for (int i = removals.Count - 1; i >= 0; i--)
        {
            ops.Add(new JsonPatchOperation
            {
                Op = "remove",
                Path = $"{path}/{removals[i]}"
            });
        }
        
        // Process additions
        currentLcsIndex = 0;
        for (int i = 0; i < updatedList.Count; i++)
        {
            if (currentLcsIndex < lcs.Count && lcs[currentLcsIndex].UpdIndex == i)
            {
                int origIndex = lcs[currentLcsIndex].OrigIndex;
                
                // Check if the elements need updating despite being in the LCS
                if (!JsonElementsAreEqual(originalList[origIndex], updatedList[i]))
                {
                    ops.Add(new JsonPatchOperation
                    {
                        Op = "replace",
                        Path = $"{path}/{i}",
                        Value = updatedList[i]
                    });
                }
                
                currentLcsIndex++;
            }
            else
            {
                // This element needs to be added
                ops.Add(new JsonPatchOperation
                {
                    Op = "add",
                    Path = $"{path}/{i}",
                    Value = updatedList[i]
                });
            }
        }
    }
    
    /// <summary>
    /// Diffs two arrays using a simpler algorithm for larger arrays.
    /// </summary>
    private void DiffArraysSimple(string path, List<JsonElement> originalList, List<JsonElement> updatedList, 
        List<JsonPatchOperation> ops)
    {
        // For very large arrays, we take a simpler approach:
        // 1. If lengths are the same, check elements one by one for replacements
        if (originalList.Count == updatedList.Count && _options.UsePositionalArrayPatching)
        {
            for (int i = 0; i < originalList.Count; i++)
            {
                if (!JsonElementsAreEqual(originalList[i], updatedList[i]))
                {
                    ops.Add(new JsonPatchOperation
                    {
                        Op = "replace",
                        Path = $"{path}/{i}",
                        Value = updatedList[i]
                    });
                }
            }
        }
        else
        {
            // 2. If lengths are different or positional patching is disabled, replace the entire array
            ops.Add(new JsonPatchOperation
            {
                Op = "replace",
                Path = path,
                Value = JsonSerializer.Deserialize<JsonElement>(
                    JsonSerializer.Serialize(updatedList.ToArray()))
            });
        }
    }

    /// <summary>
    /// Computes the Longest Common Subsequence between two arrays.
    /// </summary>
    /// <returns>A list of matching index pairs between the original and updated arrays.</returns>
    private List<IndexPair> ComputeLcs(List<JsonElement> orig, List<JsonElement> upd)
    {
        int origLen = orig.Count;
        int updLen = upd.Count;
        
        // Build matrix for dynamic programming approach
        int[,] lcsMatrix = new int[origLen + 1, updLen + 1];
        
        for (int i = 0; i <= origLen; i++)
        {
            for (int j = 0; j <= updLen; j++)
            {
                if (i == 0 || j == 0)
                    lcsMatrix[i, j] = 0;
                else if (JsonElementsAreEqual(orig[i - 1], upd[j - 1]))
                    lcsMatrix[i, j] = lcsMatrix[i - 1, j - 1] + 1;
                else
                    lcsMatrix[i, j] = Math.Max(lcsMatrix[i - 1, j], lcsMatrix[i, j - 1]);
            }
        }
        
        // Reconstruct LCS from the matrix
        var result = new List<IndexPair>();
        int x = origLen;
        int y = updLen;
        
        while (x > 0 && y > 0)
        {
            if (JsonElementsAreEqual(orig[x - 1], upd[y - 1]))
            {
                result.Add(new IndexPair { OrigIndex = x - 1, UpdIndex = y - 1 });
                x--; y--;
            }
            else if (lcsMatrix[x - 1, y] > lcsMatrix[x, y - 1])
            {
                x--;
            }
            else
            {
                y--;
            }
        }
        
        // Reverse to get indexes in ascending order
        result.Reverse();
        return result;
    }

    /// <summary>
    /// Optimizes the list of patch operations by combining related operations when possible.
    /// </summary>
    private void OptimizePatchOperations(List<JsonPatchOperation> ops)
    {
        if (ops.Count < 2)
            return;
        
        Logger.LogDebug("Optimizing patch with {OperationCount} operations", ops.Count);
        
        for (int i = ops.Count - 2; i >= 0; i--)
        {
            // Look for "remove" followed by "add" at similar paths
            if (ops[i].Op == "remove" && i < ops.Count - 1 && ops[i + 1].Op == "add" && 
                IsMatchingArrayAdd(ops[i].Path, ops[i + 1].Path))
            {
                // Convert to "move" operation
                var moveOp = new JsonPatchOperation
                {
                    Op = "move",
                    From = ops[i].Path,
                    Path = ops[i + 1].Path,
                    Value = ops[i + 1].Value
                };
                
                ops[i] = moveOp;
                ops.RemoveAt(i + 1);
            }
            
            // Look for duplicate changes to the same path
            if (i < ops.Count - 1 && ops[i].Path == ops[i + 1].Path)
            {
                // Keep only the last operation
                ops.RemoveAt(i);
            }
            
            // Look for no-op replacements where values are equal
            if (ops[i].Op == "replace" && ops[i].Value.HasValue)
            {
                var path = ops[i].Path;
                for (int j = i + 1; j < ops.Count; j++)
                {
                    if (ops[j].Path == path && ValuesEqual(ops[i], ops[j]))
                    {
                        ops.RemoveAt(i);
                        break;
                    }
                }
            }
        }
        
        Logger.LogDebug("Optimized to {OperationCount} operations", ops.Count);
    }

    /// <summary>
    /// Determines if a path refers to an array index.
    /// </summary>
    private bool IsArrayIndexPath(string path)
    {
        return ArrayIndexRegex.IsMatch(path);
    }

    /// <summary>
    /// Determines if a remove path and add path form a matching pair for a move operation.
    /// </summary>
    private bool IsMatchingArrayAdd(string removePath, string addPath)
    {
        // Check if both paths are in the same array with different indices
        if (!IsArrayIndexPath(removePath) || !IsArrayIndexPath(addPath))
            return false;
        
        // Extract array base paths
        string removeBase = removePath.Substring(0, removePath.LastIndexOf('/'));
        string addBase = addPath.Substring(0, addPath.LastIndexOf('/'));
        
        // Check if they're in the same array
        return removeBase == addBase;
    }

    /// <summary>
    /// Determines if two operations have equal values.
    /// </summary>
    private bool ValuesEqual(JsonPatchOperation op1, JsonPatchOperation op2)
    {
        if (!op1.Value.HasValue || !op2.Value.HasValue)
            return false;
            
        return JsonElementsAreEqual(op1.Value.Value, op2.Value.Value);
    }

    /// <summary>
    /// Combines a base path with a property name, handling JSON Pointer escaping.
    /// </summary>
    private string CombinePath(string basePath, string property)
    {
        // Escape / and ~ according to JSON Pointer spec (RFC 6901)
        property = property.Replace("~", "~0").Replace("/", "~1");
        
        return string.IsNullOrEmpty(basePath) ? "/" + property : basePath + "/" + property;
    }

    /// <summary>
    /// Determines if two JsonElements are equal in value.
    /// </summary>
    private bool JsonElementsAreEqual(JsonElement a, JsonElement b)
    {
        if (a.ValueKind != b.ValueKind)
            return false;
            
        switch (a.ValueKind)
        {
            case JsonValueKind.String:
                return a.GetString() == b.GetString();
                
            case JsonValueKind.Number:
                // Compare as decimals for most accurate numeric comparison
                return a.GetDecimal() == b.GetDecimal();
                
            case JsonValueKind.True:
            case JsonValueKind.False:
                return a.GetBoolean() == b.GetBoolean();
                
            case JsonValueKind.Null:
                return true;  // Both are null
                
            case JsonValueKind.Object:
                // Compare objects by serializing to strings if deep comparison is enabled
                if (_options.DeepCompareObjects)
                {
                    string aStr = JsonSerializer.Serialize(a);
                    string bStr = JsonSerializer.Serialize(b);
                    return aStr == bStr;
                }
                
                // Otherwise, check if they have the same properties with same values
                foreach (var property in a.EnumerateObject())
                {
                    if (!b.TryGetProperty(property.Name, out JsonElement bValue) || 
                        !JsonElementsAreEqual(property.Value, bValue))
                        return false;
                }
                
                // Check b doesn't have properties that a doesn't
                int aCount = 0, bCount = 0;
                foreach (var _ in a.EnumerateObject()) aCount++;
                foreach (var _ in b.EnumerateObject()) bCount++;
                
                return aCount == bCount;
                
            case JsonValueKind.Array:
                // Compare arrays by serializing to strings if deep comparison is enabled
                if (_options.DeepCompareArrays)
                {
                    string aStr = JsonSerializer.Serialize(a);
                    string bStr = JsonSerializer.Serialize(b);
                    return aStr == bStr;
                }
                
                // Otherwise, check if they have the same elements in the same order
                int aLength = 0, bLength = 0;
                foreach (var _ in a.EnumerateArray()) aLength++;
                foreach (var _ in b.EnumerateArray()) bLength++;
                
                if (aLength != bLength)
                    return false;

                using (var aEnum = a.EnumerateArray().GetEnumerator())
                {
                    using var bEnum = b.EnumerateArray().GetEnumerator();

                    while (aEnum.MoveNext() && bEnum.MoveNext())
                    {
                        if (!JsonElementsAreEqual(aEnum.Current, bEnum.Current))
                            return false;
                    }

                    return true;
                }

            default:
                return false;
        }
    }
    
    /// <summary>
    /// Represents a pair of matching indices between two arrays in the LCS algorithm.
    /// </summary>
    private class IndexPair
    {
        /// <summary>
        /// The index in the original array.
        /// </summary>
        public int OrigIndex { get; set; }
        
        /// <summary>
        /// The index in the updated array.
        /// </summary>
        public int UpdIndex { get; set; }
    }
}
