using System.Text;
using System.Text.Json;
using AnotherJsonLib.Domain;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Helper;
using Microsoft.Extensions.Logging;

namespace AnotherJsonLib.Utility.Operations;

/// <summary>
/// Provides methods for merging JSON documents with configurable strategies.
/// 
/// JSON merging combines two or more JSON documents into a single result, applying
/// rules to resolve conflicts and handle overlapping data. This utility helps with:
/// 
/// - Combining data from multiple sources into a single document
/// - Applying patches or updates to existing documents
/// - Implementing partial updates for REST APIs
/// - Building configuration systems with layered settings
/// - Creating document version control and change management systems
/// 
/// <example>
/// <code>
/// // Basic example: Merge user profile with updates
/// string baseProfile = @"{
///   ""user"": {
///     ""name"": ""John Smith"",
///     ""email"": ""john@example.com"",
///     ""preferences"": {
///       ""theme"": ""light"",
///       ""notifications"": true
///     }
///   }
/// }";
/// 
/// string profileUpdates = @"{
///   ""user"": {
///     ""email"": ""john.smith@example.com"",
///     ""preferences"": {
///       ""theme"": ""dark""
///     },
///     ""lastLogin"": ""2023-06-15T14:30:00Z""
///   }
/// }";
/// 
/// // Merge the documents (newer values override older ones)
/// string merged = JsonMerger.Merge(baseProfile, profileUpdates);
/// 
/// // Result:
/// // {
/// //   "user": {
/// //     "name": "John Smith",
/// //     "email": "john.smith@example.com",
/// //     "preferences": {
/// //       "theme": "dark",
/// //       "notifications": true
/// //     },
/// //     "lastLogin": "2023-06-15T14:30:00Z"
/// //   }
/// // }
/// </code>
/// </example>
/// </summary>
public static class JsonMerger
{
    private static readonly ILogger Logger = JsonLoggerFactory.Instance.GetLogger(nameof(JsonMerger));

    /// <summary>
    /// Merges two JSON strings with the new JSON overriding values in the original where they overlap.
    /// For overlapping keys, patch values override source values according to the provided MergeOptions.
    /// 
    /// <para>
    /// Available array merge strategies:
    /// - Concat (default): Combines arrays from both documents
    /// - Replace: Uses arrays from the patch document where they exist
    /// - Merge: Combines array elements by position
    /// </para>
    /// 
    /// <example>
    /// <code>
    /// // Merge with custom array handling
    /// string originalJson = @"{
    ///   ""items"": [1, 2, 3],
    ///   ""config"": {
    ///     ""debug"": false,
    ///     ""cache"": true
    ///   }
    /// }";
    /// 
    /// string patchJson = @"{
    ///   ""items"": [4, 5],
    ///   ""config"": {
    ///     ""debug"": true
    ///   }
    /// }";
    /// 
    /// // Using the default 'Concat' strategy for arrays
    /// var options = new MergeOptions { ArrayMergeStrategy = ArrayMergeStrategy.Concat };
    /// string merged1 = JsonMerger.Merge(originalJson, patchJson, options);
    /// // items will be [1, 2, 3, 4, 5]
    /// 
    /// // Using the 'Replace' strategy for arrays
    /// options.ArrayMergeStrategy = ArrayMergeStrategy.Replace;
    /// string merged2 = JsonMerger.Merge(originalJson, patchJson, options);
    /// // items will be [4, 5]
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="originalJson">The original JSON string.</param>
    /// <param name="patchJson">The JSON string containing the patch.</param>
    /// <param name="options">Optional merge options; if null, defaults are used.</param>
    /// <returns>The merged JSON string.</returns>
    /// <exception cref="JsonArgumentException">Thrown when either JSON string is null or empty.</exception>
    /// <exception cref="JsonParsingException">Thrown when either input is not valid JSON.</exception>
    /// <exception cref="JsonOperationException">Thrown when the merge operation fails.</exception>
    public static string Merge(this string originalJson, string patchJson, MergeOptions? options = null)
    {
        using var performance = new PerformanceTracker(Logger, nameof(Merge));

        return ExceptionHelpers.SafeExecute(() =>
            {
                // Validation moved inside SafeExecute
                ExceptionHelpers.ThrowIfNullOrWhiteSpace(originalJson, nameof(originalJson));
                ExceptionHelpers.ThrowIfNullOrWhiteSpace(patchJson, nameof(patchJson));

                options ??= new MergeOptions();

                Logger.LogDebug(
                    "Merging original JSON ({OriginalLength} chars) with patch JSON ({PatchLength} chars) using strategy: {Strategy}",
                    originalJson.Length, patchJson.Length, options.ArrayMergeStrategy);

                using var originalDoc = JsonDocument.Parse(originalJson);
                using var patchDoc = JsonDocument.Parse(patchJson);

                using var memStream = new MemoryStream();
                using var writer = new Utf8JsonWriter(memStream, new JsonWriterOptions
                {
                    Indented = options.PreserveFormatting && (IsIndented(originalJson) || IsIndented(patchJson)),
                    SkipValidation = false,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                MergeElements(originalDoc.RootElement, patchDoc.RootElement, writer, options);
                writer.Flush();

                string result = Encoding.UTF8.GetString(memStream.ToArray());

                Logger.LogDebug("Successfully merged JSON documents into {ResultLength} chars",
                    result.Length);

                return result;
            },
            (ex, msg) =>
            {
                if (ex is ArgumentException argEx)
                    return new JsonArgumentException($"Invalid argument in JSON merge: {argEx.Message}", argEx);

                if (ex is JsonException jsonEx)
                    return new JsonParsingException("Failed to merge JSON: Invalid JSON format", jsonEx);

                return new JsonOperationException("Failed to merge JSON: " + msg, ex);
            },
            "Failed to merge JSON documents") ?? string.Empty;
    }

    /// <summary>
    /// Merges multiple JSON strings in sequence, with later documents overriding earlier ones.
    /// This is useful for layered configurations or applying multiple patches in order.
    /// 
    /// <example>
    /// <code>
    /// // Multi-document merge example: Layer configurations
    /// string defaultConfig = @"{""logging"":false,""caching"":false,""debug"":false}";
    /// string envConfig = @"{""logging"":true,""caching"":true}";
    /// string userConfig = @"{""debug"":true}";
    /// 
    /// // Apply configurations in order of precedence (user overrides env overrides default)
    /// string finalConfig = JsonMerger.MergeMultiple(defaultConfig, new[] { envConfig, userConfig });
    /// 
    /// // Result: {"logging":true,"caching":true,"debug":true}
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="baseJson">The base JSON string.</param>
    /// <param name="jsonPatches">An array of JSON strings to merge with the base.</param>
    /// <param name="options">Optional merge options; if null, defaults are used.</param>
    /// <returns>The merged JSON string.</returns>
    /// <exception cref="JsonArgumentException">Thrown when the base JSON is null or empty.</exception>
    /// <exception cref="JsonOperationException">Thrown when the merge operation fails.</exception>
    public static string MergeMultiple(this string baseJson, string[] jsonPatches, MergeOptions? options = null)
    {
        using var performance = new PerformanceTracker(Logger, nameof(MergeMultiple));

        // Validate inputs
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(baseJson, nameof(baseJson));
        ExceptionHelpers.ThrowIfNull(jsonPatches, nameof(jsonPatches));

        if (jsonPatches.Length == 0)
        {
            Logger.LogDebug("No patches to merge, returning original JSON");
            return baseJson;
        }

        options ??= new MergeOptions();

        return ExceptionHelpers.SafeExecute(() =>
            {
                Logger.LogDebug("Starting multiple JSON merge with {BaseLength} base chars and {PatchCount} patches",
                    baseJson.Length, jsonPatches.Length);

                string current = baseJson;

                foreach (string patch in jsonPatches)
                {
                    if (string.IsNullOrWhiteSpace(patch))
                    {
                        Logger.LogWarning("Empty patch detected during multiple merge operation - skipping");
                        continue;
                    }

                    current = Merge(current, patch, options);
                }

                Logger.LogDebug("Successfully merged {PatchCount} JSON documents into {ResultLength} chars",
                    jsonPatches.Length, current.Length);

                return current;
            },
            (ex, msg) =>
            {
                if (ex is JsonParsingException jsonParsingEx)
                    return jsonParsingEx; // Let specific parsing exceptions bubble up unchanged

                return new JsonOperationException("Failed to merge multiple JSON documents: " + msg, ex);
            },
            "Failed to merge multiple JSON documents") ?? baseJson;
    }

    /// <summary>
    /// Merges a JSON object with multiple patches in parallel and combines the results.
    /// Each patch is applied to the original independently, then the results are merged.
    /// This is useful for applying changes from different sources without one overriding another.
    /// 
    /// <example>
    /// <code>
    /// // Start with a user record
    /// string user = @"{""name"":""John"",""email"":""john@example.com"",""role"":""user""}";
    /// 
    /// // System 1 wants to update the role
    /// string patch1 = @"{""role"":""admin""}";
    /// 
    /// // System 2 wants to update the email
    /// string patch2 = @"{""email"":""john.smith@example.com""}";
    /// 
    /// // Apply both patches in parallel (neither overrides the other)
    /// string result = JsonMerger.MergeParallel(user, new[] { patch1, patch2 });
    /// 
    /// // Result: {"name":"John","email":"john.smith@example.com","role":"admin"}
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="baseJson">The base JSON string.</param>
    /// <param name="jsonPatches">An array of JSON strings to merge in parallel.</param>
    /// <param name="options">Optional merge options; if null, defaults are used.</param>
    /// <returns>The merged JSON string.</returns>
    /// <exception cref="JsonArgumentException">Thrown when inputs are invalid.</exception>
    /// <exception cref="JsonOperationException">Thrown when the merge operation fails.</exception>
    public static string MergeParallel(this string baseJson, string[] jsonPatches, MergeOptions? options = null)
    {
        using var performance = new PerformanceTracker(Logger, nameof(MergeParallel));

        // Validate inputs
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(baseJson, nameof(baseJson));
        ExceptionHelpers.ThrowIfNull(jsonPatches, nameof(jsonPatches));

        if (jsonPatches.Length == 0)
        {
            Logger.LogDebug("No patches to merge, returning original JSON");
            return baseJson;
        }

        options ??= new MergeOptions();

        return ExceptionHelpers.SafeExecute(() =>
            {
                Logger.LogDebug("Starting parallel JSON merge with {BaseLength} base chars and {PatchCount} patches",
                    baseJson.Length, jsonPatches.Length);

                // Apply each patch to the base independently
                var mergedPatches = new List<string>();
                foreach (string patch in jsonPatches)
                {
                    if (string.IsNullOrWhiteSpace(patch))
                    {
                        Logger.LogWarning("Empty patch detected during parallel merge operation - skipping");
                        continue;
                    }

                    string mergedPatch = Merge(baseJson, patch, options);
                    mergedPatches.Add(mergedPatch);
                }

                if (mergedPatches.Count == 0)
                {
                    Logger.LogDebug("No valid patches to merge, returning original JSON");
                    return baseJson;
                }

                if (mergedPatches.Count == 1)
                {
                    Logger.LogDebug("Only one valid patch, returning directly merged result");
                    return mergedPatches[0];
                }

                // Now merge all the patches together
                string result = mergedPatches[0];
                for (int i = 1; i < mergedPatches.Count; i++)
                {
                    result = Merge(result, mergedPatches[i], options);
                }

                Logger.LogDebug("Successfully merged {PatchCount} JSON documents in parallel into {ResultLength} chars",
                    jsonPatches.Length, result.Length);

                return result;
            },
            (ex, msg) =>
            {
                if (ex is JsonParsingException jsonParsingEx)
                    return jsonParsingEx;

                return new JsonOperationException("Failed to merge JSON documents in parallel: " + msg, ex);
            },
            "Failed to merge JSON documents in parallel") ?? baseJson;
    }

    /// <summary>
    /// Attempts to merge two JSON strings without throwing exceptions.
    /// 
    /// <para>
    /// This method provides a safe way to merge JSON documents when error handling is
    /// preferred over exceptions. If the merge fails for any reason, the original JSON
    /// is returned and success is set to false.
    /// </para>
    /// 
    /// <example>
    /// <code>
    /// // Safe merging that won't throw exceptions
    /// string original = @"{""name"":""John""}";
    /// string patch = @"{""age"":30}";
    /// 
    /// if (JsonMerger.TryMerge(original, patch, out string result))
    /// {
    ///     Console.WriteLine($"Successfully merged: {result}");
    ///     // Output: {"name":"John","age":30}
    /// }
    /// else
    /// {
    ///     Console.WriteLine("Failed to merge JSON");
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="originalJson">The original JSON string.</param>
    /// <param name="patchJson">The JSON string containing the patch.</param>
    /// <param name="result">When successful, contains the merged JSON; otherwise, the original JSON.</param>
    /// <param name="options">Optional merge options; if null, defaults are used.</param>
    /// <returns>True if the merge was successful; otherwise, false.</returns>
    public static bool TryMerge(this string originalJson, string patchJson, out string result, MergeOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(originalJson) || string.IsNullOrWhiteSpace(patchJson))
        {
            Logger.LogDebug("Cannot merge with null or empty JSON strings");
            result = originalJson ?? string.Empty;
            return false;
        }
    
        try
        {
            result = Merge(originalJson, patchJson, options);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Error merging JSON documents");
            result = originalJson;
            return false;
        }
    }

    /// <summary>
    /// Attempts to merge multiple JSON strings without throwing exceptions.
    /// </summary>
    /// <param name="baseJson">The base JSON string.</param>
    /// <param name="jsonPatches">An array of JSON strings to merge with the base.</param>
    /// <param name="result">When successful, contains the merged JSON; otherwise, the base JSON.</param>
    /// <param name="options">Optional merge options; if null, defaults are used.</param>
    /// <returns>True if the merge was successful; otherwise, false.</returns>
    public static bool TryMergeMultiple(this string baseJson, string[] jsonPatches, out string result,
        MergeOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(baseJson) || jsonPatches.Any() == false || jsonPatches.Length == 0)
        {
            result = baseJson;
            return false;
        }

        try
        {
            result = MergeMultiple(baseJson, jsonPatches, options);
            return result != baseJson;
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Error merging multiple JSON documents");
            result = baseJson;
            return false;
        }
    }

    /// <summary>
    /// Recursively merges two JsonElements, writing the result to a Utf8JsonWriter.
    /// </summary>
    /// <param name="source">The source JsonElement.</param>
    /// <param name="patch">The patch JsonElement.</param>
    /// <param name="writer">The writer to output the merged result.</param>
    /// <param name="options">The merge options to use.</param>
    /// <exception cref="JsonOperationException">Thrown when the merge operation fails.</exception>
    private static void MergeElements(JsonElement source, JsonElement patch, Utf8JsonWriter writer,
        MergeOptions options)
    {
        ExceptionHelpers.SafeExecute(() =>
            {
                try
                {
                    // If patch is null or undefined and source isn't, just use source
                    if ((patch.ValueKind == JsonValueKind.Null || patch.ValueKind == JsonValueKind.Undefined) &&
                        source.ValueKind != JsonValueKind.Null && source.ValueKind != JsonValueKind.Undefined &&
                        !options.NullOverridesValue)
                    {
                        source.WriteTo(writer);
                        return;
                    }

                    // If value kinds don't match, and we're not merging an object with anything
                    if (source.ValueKind != patch.ValueKind &&
                        !(source.ValueKind == JsonValueKind.Object && patch.ValueKind != JsonValueKind.Null))
                    {
                        // For mismatched types, use the patch value
                        patch.WriteTo(writer);
                        return;
                    }

                    switch (source.ValueKind)
                    {
                        case JsonValueKind.Object:
                            writer.WriteStartObject();

                            // Write properties from source; override if present in patch.
                            foreach (var prop in source.EnumerateObject())
                            {
                                if (patch.TryGetProperty(prop.Name, out JsonElement patchValue))
                                {
                                    writer.WritePropertyName(prop.Name);

                                    // Special handling for null values based on options
                                    if (patchValue.ValueKind == JsonValueKind.Null && !options.NullOverridesValue)
                                    {
                                        prop.Value.WriteTo(writer);
                                    }
                                    else
                                    {
                                        MergeElements(prop.Value, patchValue, writer, options);
                                    }
                                }
                                else if (!options.RemoveUnmatchedProperties)
                                {
                                    prop.WriteTo(writer);
                                }
                            }

                            // Write properties only present in patch.
                            foreach (var prop in patch.EnumerateObject())
                            {
                                if (!source.TryGetProperty(prop.Name, out _))
                                {
                                    prop.WriteTo(writer);
                                }
                            }

                            writer.WriteEndObject();
                            break;

                        case JsonValueKind.Array:
                            if (patch.ValueKind == JsonValueKind.Array)
                            {
                                HandleArrayMerge(source, patch, writer, options);
                            }
                            else
                            {
                                // Unexpected type in patch for an array - use patch
                                patch.WriteTo(writer);
                            }

                            break;

                        default:
                            // For primitives, prefer the patch value.
                            patch.WriteTo(writer);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error merging JSON elements of types {SourceType} and {PatchType}",
                        source.ValueKind, patch.ValueKind);
                    throw;
                }
            },
            (ex, msg) => new JsonOperationException($"Failed to merge elements: {msg}", ex),
            $"Error merging elements of types {source.ValueKind} and {patch.ValueKind}");
    }

    /// <summary>
    /// Handles array merging based on the specified strategy.
    /// </summary>
    /// <param name="sourceArray">The source array.</param>
    /// <param name="patchArray">The patch array.</param>
    /// <param name="writer">The JSON writer to write the result to.</param>
    /// <param name="options">Merge options containing the array merge strategy.</param>
    /// <exception cref="JsonOperationException">Thrown when array merging fails.</exception>
    private static void HandleArrayMerge(JsonElement sourceArray, JsonElement patchArray, Utf8JsonWriter writer,
        MergeOptions options)
    {
        ExceptionHelpers.SafeExecute(() =>
            {
                switch (options.ArrayMergeStrategy)
                {
                    case ArrayMergeStrategy.Replace:
                        // If replacing, simply write the patch array
                        Logger.LogTrace("Using Replace strategy for array merge");
                        patchArray.WriteTo(writer);
                        break;

                    case ArrayMergeStrategy.Merge:
                        // Merge arrays item by item if they're the same length
                        int sourceCount = GetArrayLength(sourceArray);
                        int patchCount = GetArrayLength(patchArray);

                        Logger.LogTrace("Using Merge strategy for arrays of length {SourceCount} and {PatchCount}",
                            sourceCount, patchCount);

                        writer.WriteStartArray();

                        if (sourceCount == patchCount && options.EnableDeepArrayMerge)
                        {
                            // Deep merge each item
                            Logger.LogTrace("Performing deep array merge for equal-length arrays");
                            using var sourceEnum = sourceArray.EnumerateArray().GetEnumerator();
                            using var patchEnum = patchArray.EnumerateArray().GetEnumerator();

                            while (sourceEnum.MoveNext() && patchEnum.MoveNext())
                            {
                                MergeElements(sourceEnum.Current, patchEnum.Current, writer, options);
                            }
                        }
                        else
                        {
                            // In all other cases, fall back to concatenation
                            Logger.LogTrace("Falling back to concatenation for arrays of different lengths");
                            ConcatenateArrays(sourceArray, patchArray, writer);
                        }

                        writer.WriteEndArray();
                        break;

                    case ArrayMergeStrategy.Concat:
                    default:
                        Logger.LogTrace("Using Concat strategy for array merge");
                        writer.WriteStartArray();
                        ConcatenateArrays(sourceArray, patchArray, writer);
                        writer.WriteEndArray();
                        break;
                }
            },
            (ex, msg) => new JsonOperationException($"Failed to merge arrays: {msg}", ex),
            "Error during array merge operation");
    }

    /// <summary>
    /// Concatenates two JSON arrays by writing all elements to the specified writer.
    /// </summary>
    private static void ConcatenateArrays(JsonElement sourceArray, JsonElement patchArray, Utf8JsonWriter writer)
    {
        foreach (var item in sourceArray.EnumerateArray())
        {
            item.WriteTo(writer);
        }

        foreach (var item in patchArray.EnumerateArray())
        {
            item.WriteTo(writer);
        }
    }

    /// <summary>
    /// Gets the length of a JSON array.
    /// </summary>
    private static int GetArrayLength(JsonElement array)
    {
        int count = 0;
        foreach (var _ in array.EnumerateArray())
        {
            count++;
        }

        return count;
    }

    /// <summary>
    /// Determines if a JSON string is formatted with indentation.
    /// </summary>
    private static bool IsIndented(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        // Look for a newline followed by whitespace, which indicates indentation
        return json.Contains("\n ") || json.Contains("\n\t") || json.Contains("\r\n ");
    }

    /// <summary>
    /// Creates a deep clone of a JsonElement, optionally applying property filters.
    /// This can be used to selectively clone parts of a JSON document before merging.
    /// 
    /// <example>
    /// <code>
    /// // Clone only specific properties
    /// string json = @"{""name"":""John"",""age"":30,""email"":""john@example.com"",""sensitive"":""data""}";
    /// using var doc = JsonDocument.Parse(json);
    /// 
    /// // Include only certain fields
    /// string[] includeProps = new[] { "name", "email" };
    /// string filtered = JsonMerger.CloneWithFilter(doc.RootElement, includeProps);
    /// 
    /// // Result: {"name":"John","email":"john@example.com"}
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="element">The JsonElement to clone.</param>
    /// <param name="includeProperties">
    /// Optional array of top-level property names to include. If null, all properties are included.
    /// </param>
    /// <param name="indented">Whether to format the output with indentation.</param>
    /// <returns>A JSON string containing the filtered clone.</returns>
    /// <exception cref="JsonArgumentException">Thrown when the element is null.</exception>
    /// <exception cref="JsonOperationException">Thrown when the cloning operation fails.</exception>
    public static string CloneWithFilter(JsonElement element, string[]? includeProperties = null, bool indented = false)
    {
        using var performance = new PerformanceTracker(Logger, nameof(CloneWithFilter));

        return ExceptionHelpers.SafeExecute(() =>
            {
                var options = new JsonWriterOptions
                {
                    Indented = indented,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                using var stream = new MemoryStream();
                using var writer = new Utf8JsonWriter(stream, options);

                if (element.ValueKind == JsonValueKind.Object && includeProperties != null)
                {
                    // Selective cloning of object properties
                    writer.WriteStartObject();

                    var propertySet = new HashSet<string>(includeProperties, StringComparer.Ordinal);
                    foreach (var property in element.EnumerateObject())
                    {
                        if (propertySet.Contains(property.Name))
                        {
                            property.WriteTo(writer);
                        }
                    }

                    writer.WriteEndObject();
                }
                else
                {
                    // Full clone
                    element.WriteTo(writer);
                }

                writer.Flush();

                string result = Encoding.UTF8.GetString(stream.ToArray());
                return result;
            },
            (ex, msg) => new JsonOperationException($"Failed to clone JsonElement with filter: {msg}", ex),
            "Failed to clone JsonElement with filter") ?? string.Empty;
    }
}