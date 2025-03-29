using System.Text;
using System.Text.Json;
using AnotherJsonLib.Domain;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Infra;
using Microsoft.Extensions.Logging;

namespace AnotherJsonLib.Utility;

/// <summary>
/// Provides methods for merging JSON documents.
/// </summary>
public static class JsonMerger
{
    private static readonly ILogger Logger = JsonLoggerFactory.Instance.GetLogger(nameof(JsonMerger));

    /// <summary>
    /// Merges two JSON strings. For overlapping keys, patch values override source values.
    /// Arrays are merged according to the provided MergeOptions (concatenated by default).
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
        
        // Validate inputs
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(originalJson, nameof(originalJson));
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(patchJson, nameof(patchJson));
        
        options ??= new MergeOptions();
        
        return ExceptionHelpers.SafeExecute(() => 
        {
            Logger.LogDebug("Starting JSON merge with {OriginalLength} original chars and {PatchLength} patch chars, using strategy: {Strategy}",
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
        }, (ex, msg) => 
        {
            if (ex is JsonException jsonEx)
                return new JsonParsingException("Failed to merge JSON: Invalid JSON format", jsonEx);
                
            return new JsonOperationException("Failed to merge JSON: " + msg, ex);
        }, "Failed to merge JSON documents") ?? originalJson;
    }
    
    /// <summary>
    /// Attempts to merge two JSON strings without throwing exceptions.
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
            result = originalJson;
            return false;
        }
        
        result = ExceptionHelpers.SafeExecuteWithDefault(
            () => Merge(originalJson, patchJson, options),
            originalJson,
            "Failed to merge JSON documents"
        ) ?? string.Empty;
        
        return result != originalJson;
    }
    
    /// <summary>
    /// Merges multiple JSON strings into one. For overlapping keys, later documents override earlier ones.
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
        }, (ex, msg) => 
        {
            if (ex is JsonParsingException jsonParsingEx)
                return jsonParsingEx;  // Let specific parsing exceptions bubble up unchanged

            return new JsonOperationException("Failed to merge multiple JSON documents: " + msg, ex);
        }, "Failed to merge multiple JSON documents") ?? baseJson;
    }

    /// <summary>
    /// Merges two JsonElements, writing the result to a Utf8JsonWriter.
    /// </summary>
    /// <param name="source">The source JsonElement.</param>
    /// <param name="patch">The patch JsonElement.</param>
    /// <param name="writer">The writer to output the merged result.</param>
    /// <param name="options">The merge options to use.</param>
    private static void MergeElements(JsonElement source, JsonElement patch, Utf8JsonWriter writer,
        MergeOptions options)
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
    }
    
    /// <summary>
    /// Handles array merging based on the specified strategy.
    /// </summary>
    private static void HandleArrayMerge(JsonElement sourceArray, JsonElement patchArray, Utf8JsonWriter writer, MergeOptions options)
    {
        switch (options.ArrayMergeStrategy)
        {
            case ArrayMergeStrategy.Replace:
                // If replacing, simply write the patch array
                patchArray.WriteTo(writer);
                break;
                
            case ArrayMergeStrategy.Merge:
                // Merge arrays item by item if they're the same length
                int sourceCount = GetArrayLength(sourceArray);
                int patchCount = GetArrayLength(patchArray);
                
                writer.WriteStartArray();
                
                if (sourceCount == patchCount && options.EnableDeepArrayMerge)
                {
                    // Deep merge each item
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
                    ConcatenateArrays(sourceArray, patchArray, writer);
                }
                
                writer.WriteEndArray();
                break;
                
            case ArrayMergeStrategy.Concat:
            default:
                writer.WriteStartArray();
                ConcatenateArrays(sourceArray, patchArray, writer);
                writer.WriteEndArray();
                break;
        }
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
}