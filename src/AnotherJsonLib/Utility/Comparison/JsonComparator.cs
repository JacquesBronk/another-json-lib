using System.Diagnostics;
using System.Text;
using System.Text.Json;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Helper;
using AnotherJsonLib.Utility.Formatting;
using Microsoft.Extensions.Logging;

namespace AnotherJsonLib.Utility.Comparison;

/// <summary>
/// Provides functionality for comparing JSON strings with various comparison options.
/// 
/// This utility helps determine if two JSON strings represent the same data structure
/// with options to ignore case sensitivity and whitespace differences.
/// 
/// <example>
/// <code>
/// string json1 = "{\"name\":\"John\", \"age\": 30}";
/// string json2 = "{\"NAME\":\"John\", \"age\": 30}";
/// 
/// // Standard comparison (case-sensitive)
/// bool strictEqual = JsonComparator.AreEqual(json1, json2); // false
/// 
/// // Case-insensitive comparison
/// bool caseInsensitiveEqual = JsonComparator.AreEqual(json1, json2, ignoreCase: true); // true
/// 
/// // Extension method syntax
/// bool areEqual = json1.AreEqual(json2, ignoreCase: true);
/// </code>
/// </example>
/// </summary>
public static class JsonComparator
{
    // Logger for this class
    private static readonly ILogger Logger = JsonLoggerFactory.Instance.GetLogger(nameof(JsonComparator));

    /// <summary>
    /// Compares two JSON strings for equality, optionally ignoring case and/or whitespace.
    /// 
    /// This method parses both JSON strings and performs a token-by-token comparison,
    /// applying the specified comparison rules for each token type:
    /// - For strings and property names: applies case and whitespace rules as specified
    /// - For numbers: performs exact decimal comparison
    /// - For other token types: performs exact comparison
    /// 
    /// <example>
    /// <code>
    /// // Compare with default settings (case-sensitive, whitespace-sensitive)
    /// bool strictEqual = JsonComparator.AreEqual(
    ///     "{\"name\":\"John\"}", 
    ///     "{\"name\":\"john\"}"
    /// ); // false
    /// 
    /// // Compare with case insensitivity
    /// bool nameEqual = JsonComparator.AreEqual(
    ///     "{\"name\":\"John\"}", 
    ///     "{\"name\":\"john\"}", 
    ///     ignoreCase: true
    /// ); // true
    /// 
    /// // Compare with whitespace insensitivity
    /// bool whitespaceEqual = JsonComparator.AreEqual(
    ///     "{\"name\":\"John Smith\"}", 
    ///     "{\"name\":\"John  Smith\"}", 
    ///     ignoreWhitespace: true
    /// ); // true
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="json1">The first JSON string to compare.</param>
    /// <param name="json2">The second JSON string to compare.</param>
    /// <param name="ignoreCase">Whether to perform a case-insensitive comparison for string values.</param>
    /// <param name="ignoreWhitespace">Whether to ignore whitespace differences in string values.</param>
    /// <returns>True if the JSON strings are equal according to the specified options; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if either JSON string is null.</exception>
    public static bool AreEqual(string json1, string json2, bool ignoreCase = false, bool ignoreWhitespace = false)
    {
        ExceptionHelpers.ThrowIfNull(json1, nameof(json1));
        ExceptionHelpers.ThrowIfNull(json2, nameof(json2));
        Logger.LogTrace("Comparing JSON strings (ignoreCase: {IgnoreCase}, ignoreWhitespace: {IgnoreWhitespace})",
            ignoreCase, ignoreWhitespace);
        using var performance = new PerformanceTracker(Logger, nameof(AreEqual));
        return ExceptionHelpers.SafeExecute(() =>
            {
                var jsonReader1 = new Utf8JsonReader(ToUtf8Bytes(json1));
                var jsonReader2 = new Utf8JsonReader(ToUtf8Bytes(json2));

                while (jsonReader1.Read() && jsonReader2.Read())
                {
                    if (!jsonReader1.TokenType.Equals(jsonReader2.TokenType))
                    {
                        Logger.LogDebug(
                            "JSON comparison failed: Different token types at position {Position}: {Type1} vs {Type2}",
                            jsonReader1.TokenStartIndex, jsonReader1.TokenType, jsonReader2.TokenType);
                        return false;
                    }

                    if (jsonReader1.TokenType == JsonTokenType.PropertyName ||
                        jsonReader1.TokenType == JsonTokenType.String)
                    {
                        var value1 = jsonReader1.GetString();
                        var value2 = jsonReader2.GetString();

                        if (ignoreWhitespace)
                        {
                            Debug.Assert(value1 != null, nameof(value1) + " != null");
                            value1 = NormalizeWhitespace(value1);
                            Debug.Assert(value2 != null, nameof(value2) + " != null");
                            value2 = NormalizeWhitespace(value2);

                            if (string.Compare(value1?.Trim(), value2?.Trim(),
                                    ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) != 0)
                            {
                                Logger.LogDebug(
                                    "JSON comparison failed: String values differ (with whitespace ignored): '{Value1}' vs '{Value2}'",
                                    value1?.Trim(), value2?.Trim());
                                return false;
                            }
                        }
                        else if (ignoreCase)
                        {
                            if (string.Compare(value1, value2, StringComparison.OrdinalIgnoreCase) != 0)
                            {
                                Logger.LogDebug(
                                    "JSON comparison failed: String values differ (case-insensitive): '{Value1}' vs '{Value2}'",
                                    value1, value2);
                                return false;
                            }
                        }
                        else
                        {
                            if (value1 != value2)
                            {
                                Logger.LogDebug(
                                    "JSON comparison failed: String values differ (strict comparison): '{Value1}' vs '{Value2}'",
                                    value1, value2);
                                return false;
                            }
                        }
                    }
                    else if (jsonReader1.TokenType == JsonTokenType.Number)
                    {
                        decimal num1 = jsonReader1.GetDecimal();
                        decimal num2 = jsonReader2.GetDecimal();

                        if (num1 != num2)
                        {
                            Logger.LogDebug("JSON comparison failed: Number values differ: {Value1} vs {Value2}",
                                num1, num2);
                            return false;
                        }
                    }
                    else if (jsonReader1.TokenType == JsonTokenType.True ||
                             jsonReader1.TokenType == JsonTokenType.False)
                    {
                        bool bool1 = jsonReader1.GetBoolean();
                        bool bool2 = jsonReader2.GetBoolean();

                        if (bool1 != bool2)
                        {
                            Logger.LogDebug("JSON comparison failed: Boolean values differ: {Value1} vs {Value2}",
                                bool1, bool2);
                            return false;
                        }
                    }
                }

                // Check if both readers are at the end
                bool result = !jsonReader1.Read() && !jsonReader2.Read();

                if (!result)
                {
                    Logger.LogDebug("JSON comparison failed: JSON strings have different lengths");
                }
                else
                {
                    Logger.LogTrace("JSON strings are equal according to specified comparison options");
                }

                return result;
            },
            (ex, msg) =>
            {
                if (ex is JsonException)
                    return new JsonParsingException("Failed to compare JSON strings: invalid JSON format", ex);
                return new JsonOperationException($"Failed to compare JSON strings: {msg}", ex);
            },
            "Error comparing JSON strings");
    }

    /// <summary>
    /// Performs a deep comparison of two JSON strings, ignoring the order of properties in objects.
    /// This is useful when comparing JSON objects where the property order doesn't matter.
    /// </summary>
    /// <param name="json1">The first JSON string to compare.</param>
    /// <param name="json2">The second JSON string to compare.</param>
    /// <param name="ignoreCase">Whether to perform a case-insensitive comparison of property names and string values.</param>
    /// <returns>True if the JSON strings are semantically equal; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if either JSON string is null.</exception>
    public static bool AreSemanticEqual(string json1, string json2, bool ignoreCase = false)
    {
        ExceptionHelpers.ThrowIfNull(json1, nameof(json1));
        ExceptionHelpers.ThrowIfNull(json2, nameof(json2));
        Logger.LogTrace("Comparing JSON strings semantically (ignoreCase: {IgnoreCase})", ignoreCase);
        using var performance = new PerformanceTracker(Logger, nameof(AreSemanticEqual));
        return ExceptionHelpers.SafeExecute(() =>
            {
                // Use the JsonCanonicalizer to create comparable versions
                string canonical1 = JsonCanonicalizer.Canonicalize(json1);
                string canonical2 = JsonCanonicalizer.Canonicalize(json2);

                // If we need case insensitivity, we'll need to parse and normalize case
                if (ignoreCase)
                {
                    using var doc1 = JsonDocument.Parse(canonical1);
                    using var doc2 = JsonDocument.Parse(canonical2);

                    return AreElementsEqual(doc1.RootElement, doc2.RootElement, ignoreCase);
                }

                // Otherwise, the canonicalized strings should be directly comparable
                return canonical1 == canonical2;
            },
            (ex, msg) =>
            {
                if (ex is JsonException)
                    return new JsonParsingException("Failed to compare JSON strings: invalid JSON format", ex);
                return new JsonOperationException($"Failed to compare JSON strings: {msg}", ex);
            },
            "Error comparing JSON strings semantically");
    }

    /// <summary>
    /// Helper method to compare two JsonElements recursively.
    /// </summary>
    public static bool AreElementsEqual(JsonElement element1, JsonElement element2, bool ignoreCase)
    {
        if (element1.ValueKind != element2.ValueKind)
        {
            return false;
        }

        switch (element1.ValueKind)
        {
            case JsonValueKind.Object:
                // Compare all properties recursively
                foreach (var property1 in element1.EnumerateObject())
                {
                    // Find matching property in element2 (with case-insensitivity if needed)
                    bool propertyFound = false;
                    foreach (var property2 in element2.EnumerateObject())
                    {
                        if (string.Equals(property1.Name, property2.Name,
                                ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                        {
                            propertyFound = true;
                            if (!AreElementsEqual(property1.Value, property2.Value, ignoreCase))
                            {
                                return false;
                            }

                            break;
                        }
                    }

                    if (!propertyFound)
                    {
                        return false;
                    }
                }

                // Check if element2 has any additional properties
                int count1 = 0, count2 = 0;
                foreach (var _ in element1.EnumerateObject()) count1++;
                foreach (var _ in element2.EnumerateObject()) count2++;

                return count1 == count2;

            case JsonValueKind.Array:
                // Compare array length
                int arrayLength = element1.GetArrayLength();
                if (arrayLength != element2.GetArrayLength())
                {
                    return false;
                }

                // Compare array elements in order
                for (int i = 0; i < arrayLength; i++)
                {
                    if (!AreElementsEqual(element1[i], element2[i], ignoreCase))
                    {
                        return false;
                    }
                }

                return true;

            case JsonValueKind.String:
                var str1 = element1.GetString();
                var str2 = element2.GetString();
                return string.Equals(str1, str2,
                    ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

            case JsonValueKind.Number:
                return element1.GetDecimal() == element2.GetDecimal();

            case JsonValueKind.True:
            case JsonValueKind.False:
                return element1.GetBoolean() == element2.GetBoolean();

            case JsonValueKind.Null:
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Converts a string to UTF-8 byte array.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>A UTF-8 encoded byte array.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the input value is null.</exception>
    private static byte[] ToUtf8Bytes(string value)
    {
        ExceptionHelpers.ThrowIfNull(value, nameof(value));
        return Encoding.UTF8.GetBytes(value);
    }
    
    private static string NormalizeWhitespace(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        
        // Replace multiple spaces with single space
        return System.Text.RegularExpressions.Regex.Replace(input, @"\s+", " ").Trim();
    }

}