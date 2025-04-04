﻿using System.Text.Encodings.Web;
using System.Text.Json;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Helper;
using Microsoft.Extensions.Logging;

namespace AnotherJsonLib.Utility.Operations;

/// <summary>
/// Provides methods for transforming JSON documents by mapping properties.
/// </summary>
public static class JsonMapping
{
    private static readonly ILogger Logger = JsonLoggerFactory.Instance.GetLogger(nameof(JsonMapping));

    /// <summary>
    /// Default options for JSON serialization in mapping operations.
    /// </summary>
    private static readonly JsonSerializerOptions DefaultMappingOptions = new JsonSerializerOptions
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Renames object properties in a JSON document according to a mapping dictionary.
    /// For every property in an object, if a mapping exists for the key, it is renamed accordingly.
    /// The transformation is applied recursively.
    /// </summary>
    /// <param name="json">The JSON string to transform.</param>
    /// <param name="mapping">
    /// A dictionary where keys are original property names and values are the new property names.
    /// </param>
    /// <returns>A new JSON string with the properties renamed.</returns>
    /// <exception cref="JsonArgumentException">Thrown when the input JSON or mapping is null.</exception>
    /// <exception cref="JsonParsingException">Thrown when the input is not valid JSON.</exception>
    /// <exception cref="JsonOperationException">Thrown when the mapping operation fails.</exception>
    public static string MapProperties(this string json, Dictionary<string, string> mapping)
    {
        using var performance = new PerformanceTracker(Logger, nameof(MapProperties));

        // Validate inputs
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(json, nameof(json));
        ExceptionHelpers.ThrowIfNull(mapping, nameof(mapping));

        return ExceptionHelpers.SafeExecute(() =>
        {
            Logger.LogDebug("Starting property mapping on JSON string (length: {Length}) with {MappingCount} mappings",
                json.Length, mapping.Count);

            using var document = JsonDocument.Parse(json);
            object? mappedObject = MapPropertiesInternal(document.RootElement, mapping);

            string result = JsonSerializer.Serialize(mappedObject, DefaultMappingOptions);

            Logger.LogDebug("Completed property mapping: transformed {OriginalLength} to {ResultLength} characters",
                json.Length, result.Length);

            return result;
        }, (ex, msg) =>
        {
            if (ex is JsonException jsonEx)
                return new JsonParsingException("Failed to map properties: Invalid JSON format", jsonEx);

            return new JsonOperationException("Failed to map properties: " + msg, ex);
        }, "Failed to map JSON properties") ?? json;
    }

    /// <summary>
    /// Maps properties in a JsonElement according to the provided mapping dictionary.
    /// </summary>
    /// <param name="element">The JsonElement to transform.</param>
    /// <param name="mapping">A dictionary of property name mappings.</param>
    /// <returns>A transformed object.</returns>
    private static object? MapPropertiesInternal(JsonElement element, Dictionary<string, string> mapping)
    {
        try
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var resultObj = new Dictionary<string, object?>();
                    foreach (var property in element.EnumerateObject())
                    {
                        string newKey = mapping.TryGetValue(property.Name, out var mappedName)
                            ? mappedName
                            : property.Name;
                        // Recurse for nested values
                        resultObj[newKey] = MapPropertiesInternal(property.Value, mapping);
                    }

                    return resultObj;
                case JsonValueKind.Array:
                    var resultArray = new List<object?>();
                    foreach (var item in element.EnumerateArray())
                    {
                        resultArray.Add(MapPropertiesInternal(item, mapping));
                    }

                    return resultArray;
                case JsonValueKind.String:
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                case JsonValueKind.Null:
                    // Use centralized conversion for primitive values
                    return JsonElementUtils.ConvertToObject(element);
                default:
                    Logger.LogWarning("Unexpected JsonValueKind: {ValueKind} during property mapping",
                        element.ValueKind);
                    return element.ToString();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error mapping property in JsonElement of type {ValueKind}", element.ValueKind);
            throw;
        }
    }


    /// <summary>
    /// Renames object properties in a JSON document according to a mapping dictionary,
    /// preserving indentation settings from the original JSON.
    /// </summary>
    /// <param name="json">The JSON string to transform.</param>
    /// <param name="mapping">A dictionary of property name mappings.</param>
    /// <param name="preserveFormatting">Whether to preserve the indentation of the original JSON.</param>
    /// <returns>A new JSON string with the properties renamed.</returns>
    /// <exception cref="JsonArgumentException">Thrown when the input JSON or mapping is null.</exception>
    /// <exception cref="JsonParsingException">Thrown when the input is not valid JSON.</exception>
    /// <exception cref="JsonOperationException">Thrown when the mapping operation fails.</exception>
    public static string MapProperties(this string json, Dictionary<string, string> mapping, bool preserveFormatting)
    {
        using var performance = new PerformanceTracker(Logger, nameof(MapProperties) + "WithFormatting");

        // Validate inputs
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(json, nameof(json));
        ExceptionHelpers.ThrowIfNull(mapping, nameof(mapping));

        return ExceptionHelpers.SafeExecute(() =>
        {
            Logger.LogDebug(
                "Starting property mapping on JSON string (length: {Length}) with {MappingCount} mappings, preserveFormatting: {PreserveFormatting}",
                json.Length, mapping.Count, preserveFormatting);

            using var document = JsonDocument.Parse(json);
            bool isIndented = JsonDocumentHelpers.IsIndented(json);

            var options = new JsonSerializerOptions
            {
                WriteIndented = preserveFormatting && isIndented,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            object? mappedObject = MapPropertiesInternal(document.RootElement, mapping);
            string result = JsonSerializer.Serialize(mappedObject, options);

            Logger.LogDebug(
                "Completed property mapping with formatting preservation: transformed {OriginalLength} to {ResultLength} characters",
                json.Length, result.Length);

            return result;
        }, (ex, msg) =>
        {
            if (ex is JsonException jsonEx)
                return new JsonParsingException("Failed to map properties: Invalid JSON format", jsonEx);

            return new JsonOperationException("Failed to map properties: " + msg, ex);
        }, "Failed to map JSON properties with formatting preservation") ?? json;
    }

    /// <summary>
    /// Attempts to rename object properties in a JSON document according to a mapping dictionary.
    /// </summary>
    /// <param name="json">The JSON string to transform.</param>
    /// <param name="mapping">A dictionary of property name mappings.</param>
    /// <param name="result">When successful, contains the transformed JSON; otherwise, null.</param>
    /// <returns>True if the mapping operation was successful; otherwise, false.</returns>
    public static bool TryMapProperties(this string json, Dictionary<string, string> mapping, out string? result)
    {
        if (string.IsNullOrWhiteSpace(json) || mapping.Any() == false)
        {
            result = null;
            return false;
        }

        result = ExceptionHelpers.SafeExecuteWithDefault(
            () => MapProperties(json, mapping),
            null,
            "Failed to map JSON properties"
        );

        return result != null;
    }

    /// <summary>
    /// Filters object properties in a JSON document, keeping only those specified in the include list.
    /// </summary>
    /// <param name="json">The JSON string to filter.</param>
    /// <param name="propertiesToInclude">A list of property names to keep.</param>
    /// <returns>A new JSON string with only the specified properties.</returns>
    /// <exception cref="JsonArgumentException">Thrown when the input JSON or property list is null.</exception>
    /// <exception cref="JsonOperationException">Thrown when the filtering operation fails.</exception>
    public static string FilterProperties(this string json, IEnumerable<string> propertiesToInclude)
    {
        using var performance = new PerformanceTracker(Logger, nameof(FilterProperties));

        // Validate inputs
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(json, nameof(json));
        ExceptionHelpers.ThrowIfNull(propertiesToInclude, nameof(propertiesToInclude));

        var includeSet = new HashSet<string>(propertiesToInclude, StringComparer.Ordinal);

        return ExceptionHelpers.SafeExecute(() =>
        {
            Logger.LogDebug(
                "Starting property filtering on JSON string (length: {Length}) with {PropertyCount} properties to include",
                json.Length, includeSet.Count);

            using var document = JsonDocument.Parse(json);
            object? filteredObject = FilterPropertiesInternal(document.RootElement, includeSet);

            string result = JsonSerializer.Serialize(filteredObject, DefaultMappingOptions);

            Logger.LogDebug("Completed property filtering: transformed {OriginalLength} to {ResultLength} characters",
                json.Length, result.Length);

            return result;
        }, (ex, msg) =>
        {
            if (ex is JsonException jsonEx)
                return new JsonParsingException("Failed to filter properties: Invalid JSON format", jsonEx);

            return new JsonOperationException("Failed to filter properties: " + msg, ex);
        }, "Failed to filter JSON properties") ?? json;
    }

    /// <summary>
    /// Internal method to filter properties in a JsonElement.
    /// </summary>
    /// <param name="element">The JsonElement to filter.</param>
    /// <param name="propertiesToInclude">A set of property names to keep.</param>
    /// <returns>A filtered object.</returns>
    /// <exception cref="JsonSortingException">Thrown when the filtering operation fails.</exception>
    private static object? FilterPropertiesInternal(JsonElement element, HashSet<string> propertiesToInclude)
    {
        try
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var dict = new Dictionary<string, object?>();
                    foreach (var property in element.EnumerateObject())
                    {
                        bool includeProperty = propertiesToInclude.Contains(property.Name);

                        if (includeProperty)
                        {
                            // Keep this property, but still recursively filter its value if it's an object or array
                            dict[property.Name] = FilterPropertiesInternal(property.Value, propertiesToInclude);
                        }
                        else if (property.Value.ValueKind == JsonValueKind.Object || property.Value.ValueKind == JsonValueKind.Array)
                        {
                            // Even if we don't include this property directly,
                            // we need to check if any of its nested properties should be included
                            var nestedResult = FilterPropertiesInternal(property.Value, propertiesToInclude);

                            // Only include this property if there are nested properties to include
                            if (nestedResult is Dictionary<string, object?> nestedDict && nestedDict.Count > 0)
                            {
                                dict[property.Name] = nestedResult;
                            }
                            else if (nestedResult is List<object?> nestedList && nestedList.Count > 0 &&
                                     !nestedList.All(item => item is Dictionary<string, object?> dictItem &&
                                                             (dictItem == null || dictItem.Count == 0)))
                            {
                                dict[property.Name] = nestedResult;
                            }
                        }
                    }

                    return dict;

                case JsonValueKind.Array:
                    var list = new List<object?>();
                    foreach (var item in element.EnumerateArray())
                    {
                        // Recursively filter each item in the array
                        var filteredItem = FilterPropertiesInternal(item, propertiesToInclude);

                        // Only add non-empty objects to the result
                        if (!(filteredItem is Dictionary<string, object?> dictionary && dictionary.Count == 0))
                        {
                            list.Add(filteredItem);
                        }
                    }

                    return list;

                // For all other value types, just return the value as is
                default:
                    return JsonElementUtils.ConvertToObject(element);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error filtering property in JsonElement of type {ValueKind}", element.ValueKind);
            throw new JsonSortingException("Failed to filter properties: " + ex.Message, ex);
        }
    }
}