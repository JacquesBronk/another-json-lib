using System.Text.Json;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Utility.Comparison;
using Shouldly;

namespace AnotherJsonLib.Tests.LibTests;

public class JsonComparatorTests
{
    [Fact]
    public void AreEqual_StrictComparison_DetectsDifferenceInCase()
    {
        // Arrange: JSON strings differing only in case.
        string json1 = "{\"name\":\"John\"}";
        string json2 = "{\"name\":\"john\"}";

        // Act: strict comparison (case-sensitive) should yield false.
        bool areEqual = JsonComparator.AreEqual(json1, json2);

        // Assert
        areEqual.ShouldBeFalse();
    }

    [Fact]
    public void AreEqual_CaseInsensitiveComparison_ShouldBeEqual()
    {
        // Arrange
        string json1 = "{\"name\":\"John\"}";
        string json2 = "{\"name\":\"john\"}";

        // Act: case-insensitive comparison.
        bool areEqual = JsonComparator.AreEqual(json1, json2, ignoreCase: true);

        // Assert
        areEqual.ShouldBeTrue();
    }

    [Fact]
    public void AreEqual_WhitespaceInsensitiveComparison_ShouldBeEqual()
    {
        // Arrange: Two JSON strings with different internal whitespace.
        // Note: This test assumes that your comparator ignores whitespace differences in string values.
        string json1 = "{\"name\":\"John Smith\"}";
        string json2 = "{\"name\":\"John  Smith\"}"; // extra space between words

        // Act
        bool areEqual = JsonComparator.AreEqual(json1, json2, ignoreWhitespace: true);

        // Assert
        areEqual.ShouldBeTrue();
    }

    [Fact]
    public void AreEqual_NumericComparison_ShouldHandleEquivalences()
    {
        // Arrange: Numeric representations that should be considered equal.
        string json1 = "{\"value\":1}";
        string json2 = "{\"value\":1.0}";

        // Act
        bool areEqual = JsonComparator.AreEqual(json1, json2);

        // Assert: Numbers should be compared based on their actual numeric value.
        areEqual.ShouldBeTrue();
    }

    [Fact]
    public void AreSemanticEqual_ShouldIgnorePropertyOrder()
    {
        // Arrange: Two objects with same properties but in different order.
        string json1 = "{\"name\":\"Alice\",\"age\":25}";
        string json2 = "{\"age\":25,\"name\":\"Alice\"}";

        // Act
        bool semanticEqual = JsonComparator.AreSemanticEqual(json1, json2);

        // Assert
        semanticEqual.ShouldBeTrue();
    }

    [Fact]
    public void AreEqual_InvalidJson_ShouldThrowException()
    {
        // Arrange: One of the JSON strings is invalid.
        string invalidJson = "{\"name\":\"John\""; // missing closing brace
        string validJson = "{\"name\":\"John\"}";

        // Act & Assert
        Should.Throw<JsonParsingException>(() => JsonComparator.AreEqual(invalidJson, validJson));
        Should.Throw<JsonParsingException>(() => JsonComparator.AreEqual(validJson, invalidJson));
    }

    [Fact]
    public void Compare_IdenticalElements_ShouldBeEqual()
    {
        // Arrange
        string json = "{\"name\":\"John\",\"age\":30}";
        using var doc1 = JsonDocument.Parse(json);
        using var doc2 = JsonDocument.Parse(json);
        var comparer = new JsonElementComparer();

        // Act
        bool result = comparer.Equals(doc1.RootElement, doc2.RootElement);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Compare_DifferentNumbersWithinEpsilon_ShouldBeEqual()
    {
        // Arrange: numbers differ by a tiny amount.
        string json1 = "{\"value\":1.0000000001}";
        string json2 = "{\"value\":1.0000000002}";
        using var doc1 = JsonDocument.Parse(json1);
        using var doc2 = JsonDocument.Parse(json2);
        var comparer = new JsonElementComparer(epsilon: 1e-9);

        // Act: compare the "value" properties.
        bool result = comparer.Equals(doc1.RootElement.GetProperty("value"), doc2.RootElement.GetProperty("value"));

        // Assert: They should be equal given the epsilon tolerance.
        result.ShouldBeTrue();
    }

    [Fact]
    public void Compare_DifferentNumbersBeyondEpsilon_ShouldNotBeEqual()
    {
        // Arrange: numbers differ by more than the epsilon.
        string json1 = "{\"value\":1.000001}";
        string json2 = "{\"value\":1.000002}";
        using var doc1 = JsonDocument.Parse(json1);
        using var doc2 = JsonDocument.Parse(json2);
        var comparer = new JsonElementComparer(epsilon: 1e-9);

        // Act
        bool result = comparer.Equals(doc1.RootElement.GetProperty("value"), doc2.RootElement.GetProperty("value"));

        // Assert: They should not be equal.
        result.ShouldBeFalse();
    }

    [Fact]
    public void Compare_ObjectsWithDifferentPropertyNameCasing_ShouldNotBeEqual_WhenCaseSensitive()
    {
        // Arrange: Two objects with same properties but different case.
        string json1 = "{\"Name\":\"John\"}";
        string json2 = "{\"name\":\"John\"}";
        using var doc1 = JsonDocument.Parse(json1);
        using var doc2 = JsonDocument.Parse(json2);
        var comparer = new JsonElementComparer(caseSensitivePropertyNames: true);

        // Act
        bool result = comparer.Equals(doc1.RootElement, doc2.RootElement);

        // Assert: In case-sensitive mode they should not be equal.
        result.ShouldBeFalse();
    }

    [Fact]
    public void Compare_ObjectsWithDifferentPropertyNameCasing_ShouldBeEqual_WhenCaseInsensitive()
    {
        // Arrange: Two objects with same properties but different case.
        string json1 = "{\"Name\":\"John\"}";
        string json2 = "{\"name\":\"John\"}";
        using var doc1 = JsonDocument.Parse(json1);
        using var doc2 = JsonDocument.Parse(json2);
        var comparer = JsonElementComparer.CaseInsensitive();

        // Act
        bool result = comparer.Equals(doc1.RootElement, doc2.RootElement);

        // Assert: In case-insensitive mode they should be equal.
        result.ShouldBeTrue();
    }

    [Fact]
    public void Compare_Arrays_ShouldRespectOrder()
    {
        // Arrange: Two arrays with same elements in different orders.
        string json1 = "[1,2,3]";
        string json2 = "[3,2,1]";
        using var doc1 = JsonDocument.Parse(json1);
        using var doc2 = JsonDocument.Parse(json2);
        var comparer = new JsonElementComparer();

        // Act
        bool result = comparer.Equals(doc1.RootElement, doc2.RootElement);

        // Assert: Arrays are order-sensitive so they should not be equal.
        result.ShouldBeFalse();
    }

    [Fact]
    public void Compare_Arrays_Equal_WhenSameOrder()
    {
        // Arrange: Two arrays with identical elements.
        string json = "[\"a\",\"b\",\"c\"]";
        using var doc1 = JsonDocument.Parse(json);
        using var doc2 = JsonDocument.Parse(json);
        var comparer = new JsonElementComparer();

        // Act
        bool result = comparer.Equals(doc1.RootElement, doc2.RootElement);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void AreEqual_NullInputs_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => JsonComparator.AreEqual(null, "{}"));
        Should.Throw<ArgumentNullException>(() => JsonComparator.AreEqual("{}", null));
    }

    [Fact]
    public void AreSemanticEqual_NullInputs_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => JsonComparator.AreSemanticEqual(null, "{}"));
        Should.Throw<ArgumentNullException>(() => JsonComparator.AreSemanticEqual("{}", null));
    }

    [Fact]
    public void AreSemanticEqual_InvalidJson_ShouldThrowException()
    {
        // Act & Assert
        Should.Throw<JsonOperationException>(() => JsonComparator.AreSemanticEqual("{invalid", "{}"));
        Should.Throw<JsonOperationException>(() => JsonComparator.AreSemanticEqual("{}", "{invalid"));
    }

    [Fact]
    public void AreSemanticEqual_CaseSensitive_ShouldRespectCaseSensitivity()
    {
        // Arrange
        var json1 = @"{""name"": ""value""}";
        var json2 = @"{""NAME"": ""value""}";

        // Act & Assert
        JsonComparator.AreSemanticEqual(json1, json2, ignoreCase: false).ShouldBeFalse();
    }

    [Fact]
    public void AreSemanticEqual_CaseInsensitive_ShouldIgnoreCase()
    {
        // Arrange
        var json1 = @"{""name"": ""value""}";
        var json2 = @"{""NAME"": ""value""}";

        // Act & Assert
        JsonComparator.AreSemanticEqual(json1, json2, ignoreCase: true).ShouldBeTrue();
    }

    [Fact]
    public void AreEqual_BothWhitespaceAndCaseInsensitive_ShouldIgnoreBoth()
    {
        // Arrange
        var json1 = @"{ ""Name"" : ""value"" }";
        var json2 = @"{""name"":""value""}";

        // Act & Assert
        JsonComparator.AreEqual(json1, json2, ignoreCase: true, ignoreWhitespace: true).ShouldBeTrue();
    }

    [Fact]
    public void AreElementsEqual_DifferentTypes_ShouldNotBeEqual()
    {
        // Arrange
        var json1 = @"{""value"": 123}";
        var json2 = @"{""value"": ""123""}";

        // Using JsonDocument to get JsonElements
        using var doc1 = JsonDocument.Parse(json1);
        using var doc2 = JsonDocument.Parse(json2);

        // Act & Assert - This assumes AreElementsEqual is public, otherwise you'll need a different approach
        JsonComparator.AreElementsEqual(doc1.RootElement, doc2.RootElement, ignoreCase: false)
            .ShouldBeFalse();
    }

    [Fact]
    public void AreSemanticEqual_NestedObjects_ShouldBeEqual()
    {
        // Arrange
        var json1 = @"{""person"": {""name"": ""John"", ""age"": 30}}";
        var json2 = @"{""person"": {""age"": 30, ""name"": ""John""}}";

        // Act & Assert
        JsonComparator.AreSemanticEqual(json1, json2).ShouldBeTrue();
    }

    [Fact]
    public void AreEqual_EmptyArrays_ShouldBeEqual()
    {
        // Arrange
        var json1 = @"{""items"": []}";
        var json2 = @"{""items"": []}";

        // Act & Assert
        JsonComparator.AreEqual(json1, json2).ShouldBeTrue();
    }

    [Fact]
    public void AreEqual_ComplexNestedStructures_ShouldCompareCorrectly()
    {
        // Arrange
        var json1 = @"{
        ""people"": [
            {""name"": ""John"", ""details"": {""age"": 30, ""active"": true}},
            {""name"": ""Jane"", ""details"": {""age"": 25, ""active"": false}}
        ]
    }";

        var json2 = @"{
        ""people"": [
            {""name"": ""John"", ""details"": {""age"": 30, ""active"": true}},
            {""name"": ""Jane"", ""details"": {""age"": 25, ""active"": false}}
        ]
    }";

        // Act & Assert
        JsonComparator.AreEqual(json1, json2, ignoreWhitespace: true).ShouldBeTrue();
    }

    [Fact]
    public void AreEqual_DifferentJsonTypesCompared_ShouldReturnFalse()
    {
        // Arrange
        var json1 = @"{""key"": ""value""}";
        var json2 = @"[""value""]";

        // Act & Assert
        JsonComparator.AreEqual(json1, json2).ShouldBeFalse();
    }

    [Fact]
    public void AreEqual_DifferentArrayLengths_ShouldReturnFalse()
    {
        // Arrange
        var json1 = @"[1, 2, 3]";
        var json2 = @"[1, 2]";

        // Act & Assert
        JsonComparator.AreEqual(json1, json2).ShouldBeFalse();
    }

    [Fact]
    public void AreSemanticEqual_ArraysWithDifferentOrder_ShouldReturnFalse()
    {
        // Arrange
        var json1 = @"[1, 2, 3]";
        var json2 = @"[3, 2, 1]";

        // Act & Assert
        JsonComparator.AreSemanticEqual(json1, json2).ShouldBeFalse();
    }

    [Fact]
    public void AreEqual_EscapedCharacters_ShouldBeComparedCorrectly()
    {
        // Arrange
        var json1 = @"{""message"": ""Hello\nWorld""}";
        var json2 = @"{""message"": ""Hello\nWorld""}";

        // Act & Assert
        JsonComparator.AreEqual(json1, json2).ShouldBeTrue();
    }
}