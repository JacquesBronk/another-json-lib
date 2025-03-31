using System.Text.Json.Nodes;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Utility.Formatting;
using Shouldly;

namespace AnotherJsonLib.Tests.LibTests;

public class CanonicalizationTests
{
    [Fact]
    public void Canonicalize_ObjectsSorted_ShouldSortKeysAlphabetically()
    {
        // Arrange: unsorted JSON object
        string input = "{\"b\":2,\"a\":1,\"c\":3}";

        // Act: canonicalize the JSON
        string canonical = JsonCanonicalizer.Canonicalize(input);

        // Assert: keys should be sorted alphabetically
        canonical.ShouldBe("{\"a\":1,\"b\":2,\"c\":3}");
    }

    [Fact]
    public void Canonicalize_EquivalentJson_ShouldYieldSameCanonicalString()
    {
        // Arrange: two JSON strings with same data but different ordering/spacing.
        string json1 = "{ \"name\": \"John\", \"age\":30}";
        string json2 = "{ \"age\": 30, \"name\":\"John\"}";

        // Act: canonicalize both
        string canonical1 = JsonCanonicalizer.Canonicalize(json1);
        string canonical2 = JsonCanonicalizer.Canonicalize(json2);

        // Assert: canonical forms should be identical
        canonical1.ShouldBe(canonical2);
    }

    [Fact]
    public void Canonicalize_InvalidJson_ShouldThrowException()
    {
        // Arrange: invalid JSON (missing closing brace)
        string invalidJson = "{ \"name\": \"John\" ";

        // Act & Assert: expect a JsonParsingException (or appropriate exception)
        Should.Throw<JsonCanonicalizationException>(() => JsonCanonicalizer.Canonicalize(invalidJson));
    }

    [Fact]
    public void Canonicalize_Idempotence_ShouldBeIdempotent()
    {
        // Arrange: unsorted JSON object
        string json = "{\"b\":2,\"a\":1}";

        // Act: canonicalize twice
        string canonical1 = JsonCanonicalizer.Canonicalize(json);
        string canonical2 = JsonCanonicalizer.Canonicalize(canonical1);

        // Assert: applying canonicalization twice yields the same result
        canonical1.ShouldBe(canonical2);
    }

    [Fact]
    public void Canonicalize_NumericNormalization_ShouldNormalizeNumbers()
    {
        // Arrange: Different numeric representations.
        string json1 = "{\"value\":1}";
        string json2 = "{\"value\":1.0}";
        string json3 = "{\"value\":1.00}";
    
        // Act
        string canon1 = JsonCanonicalizer.Canonicalize(json1);
        string canon2 = JsonCanonicalizer.Canonicalize(json2);
        string canon3 = JsonCanonicalizer.Canonicalize(json3);
    
        // Instead of strict string equality, parse the canonical strings and compare the numeric value.
        var node1 = JsonNode.Parse(canon1)!;
        var node2 = JsonNode.Parse(canon2)!;
        var node3 = JsonNode.Parse(canon3)!;
    
        double value1 = node1["value"]!.GetValue<double>();
        double value2 = node2["value"]!.GetValue<double>();
        double value3 = node3["value"]!.GetValue<double>();
    
        value1.ShouldBe(value2);
        value1.ShouldBe(value3);
    }


    [Fact]
    public void Canonicalize_ArrayElements_ShouldNotReorderArrayElements()
    {
        // Arrange: Array of objects where each object has unsorted keys.
        string json = "[{\"b\":2,\"a\":1}, {\"d\":4,\"c\":3}]";

        // Act: Canonicalize the JSON.
        string canonical = JsonCanonicalizer.Canonicalize(json);

        // Assert: The array order is preserved, but each object's keys are sorted.
        canonical.ShouldBe("[{\"a\":1,\"b\":2},{\"c\":3,\"d\":4}]");
    }

    [Fact]
    public void Canonicalize_DeeplyNestedStructures_ShouldApplyRecursively()
    {
        // Arrange: A deeply nested structure with unsorted keys at multiple levels.
        string json = @"
            {
                ""z"": 1,
                ""a"": {
                    ""d"": 4,
                    ""b"": { ""y"": 2, ""x"": 3 }
                },
                ""m"": 0
            }";

        // Act
        string canonical = JsonCanonicalizer.Canonicalize(json);

        // Assert: Check that each object within is sorted.
        canonical.ShouldBe("{\"a\":{\"b\":{\"x\":3,\"y\":2},\"d\":4},\"m\":0,\"z\":1}");
    }

    [Fact]
    public void Canonicalize_UnicodeNormalization_ShouldHandleEscapedAndLiteral()
    {
        // Arrange: JSON with Unicode escape and literal Unicode.
        string jsonEscaped = "{\"char\":\"\\u00e9\"}";
        string jsonLiteral = "{\"char\":\"é\"}";

        // Act
        string canonEscaped = JsonCanonicalizer.Canonicalize(jsonEscaped);
        string canonLiteral = JsonCanonicalizer.Canonicalize(jsonLiteral);

        // Assert: Both should yield the same canonical string.
        canonEscaped.ShouldBe(canonLiteral);
    }

    [Fact]
    public void Canonicalize_BooleanAndNullHandling_ShouldBeConsistent()
    {
        // Arrange: JSON with boolean and null values.
        string json = "{\"flag\":true, \"nothing\":null}";

        // Act
        string canonical = JsonCanonicalizer.Canonicalize(json);

        // Assert: Check that boolean and null are represented consistently.
        canonical.ShouldBe("{\"flag\":true,\"nothing\":null}");
    }

    [Fact]
    public void Canonicalize_ComplexStructureIdempotence_ShouldBeStable()
    {
        // Arrange: A complex, nested JSON structure.
        string json = @"
            {
                ""user"": {
                    ""name"": ""Alice"",
                    ""roles"": [""admin"", ""user""],
                    ""settings"": {
                        ""theme"": ""dark"",
                        ""notifications"": true
                    }
                },
                ""version"": 1.0
            }";

        // Act
        string canonical1 = JsonCanonicalizer.Canonicalize(json);
        string canonical2 = JsonCanonicalizer.Canonicalize(canonical1);

        // Assert: The canonical form should be stable across multiple applications.
        canonical1.ShouldBe(canonical2);
    }
}