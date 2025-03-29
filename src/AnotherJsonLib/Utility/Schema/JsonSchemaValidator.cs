using System.Text.Json;
using System.Text.RegularExpressions;
using AnotherJsonLib.Domain;

namespace AnotherJsonLib.Utility.Schema;

/// <summary>
/// A JSON Schema validator that supports a subset of JSON Schema keywords, including:
/// - type, enum, properties, required, additionalProperties
/// - items, minItems, maxItems, uniqueItems
/// - minLength, maxLength, pattern, format
/// - minimum, maximum, multipleOf
/// - Composite keywords: allOf, anyOf, oneOf, not
/// 
/// This implementation is extensible and intended as a production-ready starting point.
/// </summary>
public static class JsonSchemaValidator
{
    /// <summary>
    /// Validates a JSON instance (as a JsonElement) against a JSON Schema (as a JsonElement).
    /// </summary>
    /// <param name="schema">The JSON Schema.</param>
    /// <param name="instance">The JSON instance to validate.</param>
    /// <returns>A validation result with a validity flag and error messages.</returns>
    public static JsonSchemaValidationResult Validate(JsonElement schema, JsonElement instance)
    {
        var result = new JsonSchemaValidationResult();
        ValidateInternal(schema, instance, "", result);
        return result;
    }

    private static void ValidateInternal(JsonElement schema, JsonElement instance, string path,
        JsonSchemaValidationResult result)
    {
        // Composite Keywords

        // allOf: Instance must be valid against all subschemas.
        if (schema.TryGetProperty("allOf", out JsonElement allOfElement) &&
            allOfElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var subSchema in allOfElement.EnumerateArray())
            {
                var subResult = new JsonSchemaValidationResult();
                ValidateInternal(subSchema, instance, path, subResult);
                if (!subResult.IsValid)
                {
                    result.IsValid = false;
                    result.Errors.Add($"allOf failure at '{path}': {string.Join("; ", subResult.Errors)}");
                }
            }
        }

        // anyOf: Instance must be valid against at least one subschema.
        if (schema.TryGetProperty("anyOf", out JsonElement anyOfElement) &&
            anyOfElement.ValueKind == JsonValueKind.Array)
        {
            int validCount = 0;
            var anyOfErrors = new List<string>();
            foreach (var subSchema in anyOfElement.EnumerateArray())
            {
                var subResult = new JsonSchemaValidationResult();
                ValidateInternal(subSchema, instance, path, subResult);
                if (subResult.IsValid)
                    validCount++;
                else
                    anyOfErrors.Add(string.Join(", ", subResult.Errors));
            }

            if (validCount < 1)
            {
                result.IsValid = false;
                result.Errors.Add($"anyOf failure at '{path}': {string.Join("; ", anyOfErrors)}");
            }
        }

        // oneOf: Instance must be valid against exactly one subschema.
        if (schema.TryGetProperty("oneOf", out JsonElement oneOfElement) &&
            oneOfElement.ValueKind == JsonValueKind.Array)
        {
            int validCount = 0;
            var oneOfErrors = new List<string>();
            foreach (var subSchema in oneOfElement.EnumerateArray())
            {
                var subResult = new JsonSchemaValidationResult();
                ValidateInternal(subSchema, instance, path, subResult);
                if (subResult.IsValid)
                    validCount++;
                else
                    oneOfErrors.Add(string.Join(", ", subResult.Errors));
            }

            if (validCount != 1)
            {
                result.IsValid = false;
                result.Errors.Add(
                    $"oneOf failure at '{path}': Expected exactly one valid subschema, found {validCount}. Details: {string.Join("; ", oneOfErrors)}");
            }
        }

        // not: Instance must not be valid against the subschema.
        if (schema.TryGetProperty("not", out JsonElement notElement))
        {
            var subResult = new JsonSchemaValidationResult();
            ValidateInternal(notElement, instance, path, subResult);
            if (subResult.IsValid)
            {
                result.IsValid = false;
                result.Errors.Add($"not failure at '{path}': Instance should not match the subschema.");
            }
        }

        // Basic Keywords

        // "type" validation.
        if (schema.TryGetProperty("type", out JsonElement typeElement))
        {
            string expectedType = typeElement.GetString()!;
            if (!IsTypeValid(expectedType, instance))
            {
                result.IsValid = false;
                result.Errors.Add(
                    $"Type mismatch at '{path}': Expected '{expectedType}', got '{GetInstanceType(instance)}'.");
                return; // Stop further validation if type mismatches.
            }
        }

        // "enum" validation.
        if (schema.TryGetProperty("enum", out JsonElement enumElement) && enumElement.ValueKind == JsonValueKind.Array)
        {
            bool found = false;
            foreach (var enumVal in enumElement.EnumerateArray())
            {
                if (JsonElementsAreEqual(enumVal, instance))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                result.IsValid = false;
                result.Errors.Add($"Value at '{path}' is not in the allowed enum list.");
            }
        }

        // Instance-specific validations.
        switch (instance.ValueKind)
        {
            case JsonValueKind.Object:
                // "required" properties.
                if (schema.TryGetProperty("required", out JsonElement requiredElement) &&
                    requiredElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var req in requiredElement.EnumerateArray())
                    {
                        string propName = req.GetString()!;
                        if (!instance.TryGetProperty(propName, out _))
                        {
                            result.IsValid = false;
                            result.Errors.Add($"Missing required property '{propName}' at '{path}'.");
                        }
                    }
                }

                // "properties" validation.
                if (schema.TryGetProperty("properties", out JsonElement propertiesElement) &&
                    propertiesElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in propertiesElement.EnumerateObject())
                    {
                        string propPath = CombinePath(path, prop.Name);
                        if (instance.TryGetProperty(prop.Name, out JsonElement propValue))
                        {
                            ValidateInternal(prop.Value, propValue, propPath, result);
                        }
                    }
                }

                // "additionalProperties" validation.
                if (schema.TryGetProperty("properties", out JsonElement definedProps) &&
                    definedProps.ValueKind == JsonValueKind.Object &&
                    schema.TryGetProperty("additionalProperties", out JsonElement additionalProps))
                {
                    var definedNames = definedProps.EnumerateObject().Select(p => p.Name).ToHashSet();
                    foreach (var prop in instance.EnumerateObject())
                    {
                        if (!definedNames.Contains(prop.Name))
                        {
                            if (additionalProps.ValueKind == JsonValueKind.False)
                            {
                                result.IsValid = false;
                                result.Errors.Add($"Additional property '{prop.Name}' not allowed at '{path}'.");
                            }
                            else if (additionalProps.ValueKind == JsonValueKind.Object)
                            {
                                string propPath = CombinePath(path, prop.Name);
                                ValidateInternal(additionalProps, prop.Value, propPath, result);
                            }
                        }
                    }
                }

                break;

            case JsonValueKind.Array:
                int count = instance.GetArrayLength();
                // "minItems" and "maxItems"
                if (schema.TryGetProperty("minItems", out JsonElement minItemsElement) &&
                    minItemsElement.ValueKind == JsonValueKind.Number)
                {
                    int minItems = minItemsElement.GetInt32();
                    if (count < minItems)
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Array at '{path}' has {count} items; minimum is {minItems}.");
                    }
                }

                if (schema.TryGetProperty("maxItems", out JsonElement maxItemsElement) &&
                    maxItemsElement.ValueKind == JsonValueKind.Number)
                {
                    int maxItems = maxItemsElement.GetInt32();
                    if (count > maxItems)
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Array at '{path}' has {count} items; maximum is {maxItems}.");
                    }
                }

                // "uniqueItems"
                if (schema.TryGetProperty("uniqueItems", out JsonElement uniqueItemsElement) &&
                    uniqueItemsElement.ValueKind == JsonValueKind.True)
                {
                    var seen = new List<string>();
                    int index = 0;
                    foreach (var item in instance.EnumerateArray())
                    {
                        string serialized = item.ToString();
                        if (seen.Contains(serialized))
                        {
                            result.IsValid = false;
                            result.Errors.Add($"Array at '{path}' contains duplicate items (at index {index}).");
                            break;
                        }

                        seen.Add(serialized);
                        index++;
                    }
                }

                // "items" validation.
                if (schema.TryGetProperty("items", out JsonElement itemsSchema))
                {
                    int idx = 0;
                    foreach (var item in instance.EnumerateArray())
                    {
                        string itemPath = CombinePath(path, idx.ToString());
                        ValidateInternal(itemsSchema, item, itemPath, result);
                        idx++;
                    }
                }

                break;

            case JsonValueKind.String:
                string str = instance.GetString() ?? "";
                if (schema.TryGetProperty("minLength", out JsonElement minLengthElement) &&
                    minLengthElement.ValueKind == JsonValueKind.Number)
                {
                    int minLength = minLengthElement.GetInt32();
                    if (str.Length < minLength)
                    {
                        result.IsValid = false;
                        result.Errors.Add($"String at '{path}' length {str.Length} is less than minimum {minLength}.");
                    }
                }

                if (schema.TryGetProperty("maxLength", out JsonElement maxLengthElement) &&
                    maxLengthElement.ValueKind == JsonValueKind.Number)
                {
                    int maxLength = maxLengthElement.GetInt32();
                    if (str.Length > maxLength)
                    {
                        result.IsValid = false;
                        result.Errors.Add($"String at '{path}' length {str.Length} exceeds maximum {maxLength}.");
                    }
                }

                if (schema.TryGetProperty("pattern", out JsonElement patternElement) &&
                    patternElement.ValueKind == JsonValueKind.String)
                {
                    string pattern = patternElement.GetString()!;
                    if (!Regex.IsMatch(str, pattern))
                    {
                        result.IsValid = false;
                        result.Errors.Add($"String at '{path}' does not match pattern '{pattern}'.");
                    }
                }

                if (schema.TryGetProperty("format", out JsonElement formatElement) &&
                    formatElement.ValueKind == JsonValueKind.String)
                {
                    string format = formatElement.GetString()!;
                    switch (format)
                    {
                        case "date-time":
                            if (!DateTime.TryParse(str, out _))
                            {
                                result.IsValid = false;
                                result.Errors.Add($"String at '{path}' is not a valid date-time.");
                            }

                            break;
                        case "email":
                            if (!Regex.IsMatch(str, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                            {
                                result.IsValid = false;
                                result.Errors.Add($"String at '{path}' is not a valid email address.");
                            }

                            break;
                        case "uri":
                            if (!Uri.IsWellFormedUriString(str, UriKind.Absolute))
                            {
                                result.IsValid = false;
                                result.Errors.Add($"String at '{path}' is not a well-formed URI.");
                            }

                            break;
                        // Additional formats can be added here.
                    }
                }

                break;

            case JsonValueKind.Number:
                if (schema.TryGetProperty("minimum", out JsonElement minimumElement) &&
                    minimumElement.ValueKind == JsonValueKind.Number)
                {
                    decimal minimum = minimumElement.GetDecimal();
                    if (instance.GetDecimal() < minimum)
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Number at '{path}' is less than minimum {minimum}.");
                    }
                }

                if (schema.TryGetProperty("maximum", out JsonElement maximumElement) &&
                    maximumElement.ValueKind == JsonValueKind.Number)
                {
                    decimal maximum = maximumElement.GetDecimal();
                    if (instance.GetDecimal() > maximum)
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Number at '{path}' exceeds maximum {maximum}.");
                    }
                }

                if (schema.TryGetProperty("multipleOf", out JsonElement multipleOfElement) &&
                    multipleOfElement.ValueKind == JsonValueKind.Number)
                {
                    decimal multiple = multipleOfElement.GetDecimal();
                    decimal value = instance.GetDecimal();
                    if (multiple == 0)
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Schema at '{path}' has multipleOf 0, which is invalid.");
                    }
                    else if (value / multiple % 1 != 0)
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Number at '{path}' is not a multiple of {multiple}.");
                    }
                }

                break;
            // Booleans and null need no additional constraints.
        }
    }

    private static bool IsTypeValid(string expectedType, JsonElement instance)
    {
        return expectedType switch
        {
            "object" => instance.ValueKind == JsonValueKind.Object,
            "array" => instance.ValueKind == JsonValueKind.Array,
            "string" => instance.ValueKind == JsonValueKind.String,
            "number" => instance.ValueKind == JsonValueKind.Number,
            "integer" => instance.ValueKind == JsonValueKind.Number && IsInteger(instance),
            "boolean" => instance.ValueKind == JsonValueKind.True || instance.ValueKind == JsonValueKind.False,
            "null" => instance.ValueKind == JsonValueKind.Null,
            _ => false,
        };
    }

    private static bool IsInteger(JsonElement instance)
    {
        try
        {
            decimal value = instance.GetDecimal();
            return value == Math.Truncate(value);
        }
        catch
        {
            return false;
        }
    }

    private static string GetInstanceType(JsonElement instance)
    {
        return instance.ValueKind.ToString().ToLowerInvariant();
    }

    private static bool JsonElementsAreEqual(JsonElement a, JsonElement b)
    {
        // For simplicity, compare their serialized strings.
        return a.ToString() == b.ToString();
    }

    private static string CombinePath(string basePath, string property)
    {
        return string.IsNullOrEmpty(basePath) ? "/" + property : basePath + "/" + property;
    }
}