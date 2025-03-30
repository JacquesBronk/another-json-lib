using System.Text.Json;
using System.Text.Json.Nodes;

namespace AnotherJsonLib.Tests.Utility;

/// <summary>
/// Generates test data for error conditions and edge cases
/// </summary>
public static class ExceptionTestCases
{
    /// <summary>
    /// Generates invalid JsonDocumentOption combinations
    /// </summary>
    public static IEnumerable<object[]> GetInvalidJsonDocumentOptions()
    {
  
        // Test negative MaxDepth
        yield return new object[] { -1, false, JsonCommentHandling.Disallow, typeof(ArgumentOutOfRangeException) };
    
        // Test invalid JsonCommentHandling enum value (if applicable)
        yield return new object[] { 64, false, (JsonCommentHandling)99, typeof(ArgumentOutOfRangeException) };
    
        // Test zero MaxDepth
        yield return new object[] { 0, false, JsonCommentHandling.Allow, typeof(ArgumentOutOfRangeException) };
    
    }

    /// <summary>
    /// Generates various JSON strings that should fail to parse
    /// </summary>
    public static IEnumerable<object[]> GetInvalidJsonStrings()
    {
        // Missing closing brace
        yield return new object[]
        {
            "{ \"name\": \"Test\"",
            typeof(JsonException)
        };

        // Unterminated string
        yield return new object[]
        {
            "{ \"name\": \"Test",
            typeof(JsonException)
        };

        // Invalid escape sequence
        yield return new object[]
        {
            "{ \"name\": \"Test\\x\" }",
            typeof(JsonException)
        };

        // Trailing comma
        yield return new object[]
        {
            "{ \"name\": \"Test\", }",
            typeof(JsonException)
        };

        // No value after property name
        yield return new object[]
        {
            "{ \"name\": }",
            typeof(JsonException)
        };

        // Invalid value type (function)
        yield return new object[]
        {
            "{ \"name\": function() {} }",
            typeof(JsonException)
        };

        // Comments (which are not allowed in JSON)
        yield return new object[]
        {
            "{ \"name\": \"Test\" } // comment",
            typeof(JsonException)
        };

        // Multiple root elements
        yield return new object[]
        {
            "{ \"name\": \"Test\" } { \"age\": 30 }",
            typeof(JsonException)
        };

        // Invalid JSON value (NaN)
        yield return new object[]
        {
            "{ \"value\": NaN }",
            typeof(JsonException)
        };

        // Invalid JSON value (Infinity)
        yield return new object[]
        {
            "{ \"value\": Infinity }",
            typeof(JsonException)
        };
    }

    /// <summary>
    /// Generates test cases for max depth limit exceptions
    /// </summary>
    public static IEnumerable<object[]> GetMaxDepthExceededTestCases()
    {
        // Generate JSON with depth 10
        JsonNode json = CreateNestedObject(10);

        // Test with different MaxDepth settings
        yield return new object[]
        {
            json.ToJsonString(),
            new JsonDocumentOptions { MaxDepth = 5 },
            typeof(JsonException)
        };

        yield return new object[]
        {
            json.ToJsonString(),
            new JsonDocumentOptions { MaxDepth = 8 },
            typeof(JsonException)
        };

        yield return new object[]
        {
            json.ToJsonString(),
            new JsonDocumentOptions { MaxDepth = 20 }, // Should not throw
            null
        };
    }

    private static JsonNode CreateNestedObject(int depth)
    {
        if (depth <= 0)
            return JsonValue.Create("leaf");

        var obj = new JsonObject();
        obj.Add("nested", CreateNestedObject(depth - 1));
        return obj;
    }

    /// <summary>
    /// Creates test cases for unicode edge cases in JSON strings
    /// </summary>
    public static IEnumerable<object[]> GetUnicodeEdgeCases()
    {
        // Valid Unicode escapes
        yield return new object[]
        {
            "{ \"value\": \"\\u0041\\u0042\\u0043\" }",
            "ABC",
            null // no exception expected
        };

        // Invalid Unicode escape (not 4 hex digits)
        yield return new object[]
        {
            "{ \"value\": \"\\u00\" }",
            null,
            typeof(JsonException)
        };

        // Invalid hex in Unicode escape
        yield return new object[]
        {
            "{ \"value\": \"\\u004X\" }",
            null,
            typeof(JsonException)
        };

        // Surrogate pair handling
        yield return new object[]
        {
            "{ \"value\": \"\\uD834\\uDD1E\" }",
            "𝄞", // Musical G-clef
            null // no exception expected
        };

        // Incomplete surrogate pair (high surrogate without low surrogate)
        yield return new object[]
        {
            "{ \"value\": \"\\uD834\" }",
            null,
            typeof(JsonException)
        };
    }

    /// <summary>
    /// Creates test cases for number parsing edge cases
    /// </summary>
    public static IEnumerable<object[]> GetNumberParsingEdgeCases()
    {
        // Very large integers
        yield return new object[]
        {
            "{ \"value\": 9223372036854775807 }", // Max long
            9223372036854775807L,
            null // no exception expected
        };

        yield return new object[]
        {
            "{ \"value\": 9223372036854775808 }", // Overflow long
            9223372036854775808.0, // Should be parsed as double
            null // no exception expected
        };

        // Very small numbers
        yield return new object[]
        {
            "{ \"value\": 1e-323 }",
            1e-323,
            null // no exception expected
        };

        yield return new object[]
        {
            "{ \"value\": 1e-324 }", // Smallest positive subnormal double
            0.0, // Should be treated as zero
            null // no exception expected
        };

        // Leading zeros (disallowed in JSON)
        yield return new object[]
        {
            "{ \"value\": 01 }",
            null,
            typeof(JsonException)
        };

        // Multiple decimal points
        yield return new object[]
        {
            "{ \"value\": 1.2.3 }",
            null,
            typeof(JsonException)
        };

        // Number with invalid exponent
        yield return new object[]
        {
            "{ \"value\": 1e }",
            null,
            typeof(JsonException)
        };
    }
}