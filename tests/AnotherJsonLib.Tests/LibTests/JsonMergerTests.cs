using System.Text.Json;
using AnotherJsonLib.Domain;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Utility.Operations;
using Shouldly;

namespace AnotherJsonLib.Tests.LibTests;

public class JsonMergerTests
{
    [Fact]
    public void Merge_IdenticalJson_ShouldReturnSameJson()
    {
        // Arrange
        string originalJson = "{\"name\":\"John\",\"age\":30}";
        string patchJson = "{\"name\":\"John\",\"age\":30}";

        // Act
        string merged = JsonMerger.Merge(originalJson, patchJson);

        // Assert: The merged JSON should be equivalent to the original.
        merged.ShouldBe(originalJson);
    }

    [Fact]
    public void Merge_PropertyAddition_ShouldAddNewProperties()
    {
        // Arrange
        string originalJson = "{\"name\":\"John\"}";
        string patchJson = "{\"name\":\"John\",\"city\":\"Paris\"}";

        // Act
        string merged = JsonMerger.Merge(originalJson, patchJson);

        // Assert: The merged JSON should include the added property.
        merged.ShouldContain("\"city\":\"Paris\"");
        merged.ShouldContain("\"name\":\"John\"");
    }


    [Fact]
    public void Merge_NestedObjects_ShouldMergeDeeply()
    {
        // Arrange: Nested JSON objects.
        string originalJson = @"
            {
                ""user"": {
                    ""name"": ""John"",
                    ""address"": { ""city"": ""London"", ""zip"": ""E1 6AN"" }
                }
            }";
        string patchJson = @"
            {
                ""user"": {
                    ""address"": { ""city"": ""Paris"" }
                }
            }";

        // Act
        string merged = JsonMerger.Merge(originalJson, patchJson);

        // Assert: The merged result should update only the specified nested properties.
        // Expected result: "name" remains unchanged; "address" is merged so that "city" becomes "Paris" while "zip" remains.
        merged.ShouldContain("\"name\":\"John\"");
        merged.ShouldContain("\"city\":\"Paris\"");
        merged.ShouldContain("\"zip\":\"E1 6AN\"");
    }

    [Fact]
    public void Merge_MultipleChanges_ShouldApplyAllUpdates()
    {
        // Arrange: Multiple simultaneous changes.
        string originalJson = "{\"a\":1,\"b\":2,\"c\":3}";
        string patchJson = "{\"b\":20,\"d\":4,\"c\":null}";

        // Act
        string merged = JsonMerger.Merge(originalJson, patchJson);

        // Assert:
        // - Property "b" should be updated.
        // - Property "d" should be added.
        // - Property "c" should be set to null.
        merged.ShouldContain("\"a\":1");
        merged.ShouldContain("\"b\":20");
        merged.ShouldContain("\"d\":4");
        merged.ShouldContain("\"c\":null");
    }


    [Fact]
    public void Merge_InvalidJson_ShouldThrowJsonParsingException()
    {
        // Arrange
        string originalJson = "{\"a\":1}";
        string invalidPatch = "{\"b\":2,}"; // invalid due to trailing comma

        // Act & Assert
        Should.Throw<JsonParsingException>(() => JsonMerger.Merge(originalJson, invalidPatch));
        Should.Throw<JsonParsingException>(() => JsonMerger.Merge(invalidPatch, originalJson));
    }

    [Fact]
    public void TryMerge_ValidInput_ShouldReturnTrueAndMergedJson()
    {
        // Arrange
        string originalJson = "{\"x\":100}";
        string patchJson = "{\"y\":200}";

        // Act
        bool success = JsonMerger.TryMerge(originalJson, patchJson, out string result);

        // Assert
        success.ShouldBeTrue();
        result.ShouldContain("\"x\":100");
        result.ShouldContain("\"y\":200");
    }

    [Fact]
    public void TryMerge_InvalidInput_ShouldReturnFalseAndEmptyResult()
    {
        // Arrange
        string invalidJson = "{\"x\":100"; // missing closing brace
        string patchJson = "{\"y\":200}";

        // Act
        bool success = JsonMerger.TryMerge(invalidJson, patchJson, out string result);

        // Assert
        success.ShouldBeFalse();
        result.ShouldBe("{\"x\":100");
    }

    [Fact]
    public void Merge_WithArrayConcatStrategy_ShouldConcatenateArrays()
    {
        // Arrange
        var original = @"{""items"": [1, 2, 3]}";
        var patch = @"{""items"": [4, 5]}";
        var options = new MergeOptions { ArrayMergeStrategy = ArrayMergeStrategy.Concat };

        // Act
        var result = JsonMerger.Merge(original, patch, options);

        // Assert
        var expected = @"{""items"":[1,2,3,4,5]}";
        result.ShouldBe(expected);
    }

    [Fact]
    public void Merge_WithArrayReplaceStrategy_ShouldReplaceArrays()
    {
        // Arrange
        var original = @"{""items"": [1, 2, 3]}";
        var patch = @"{""items"": [4, 5]}";
        var options = new MergeOptions { ArrayMergeStrategy = ArrayMergeStrategy.Replace };

        // Act
        var result = JsonMerger.Merge(original, patch, options);

        // Assert
        var expected = @"{""items"":[4,5]}";
        result.ShouldBe(expected);
    }

    [Fact]
    public void Merge_WithArrayMergeStrategy_ShouldMergeArrays()
    {
        // Arrange
        var original = @"{""items"": [1, 2, 3]}";
        var patch = @"{""items"": [4, 5, 6]}";
        var options = new MergeOptions
        {
            ArrayMergeStrategy = ArrayMergeStrategy.Merge,
            EnableDeepArrayMerge = true
        };

        // Act
        var result = JsonMerger.Merge(original, patch, options);

        // Assert
        var expected = @"{""items"":[4,5,6]}";
        result.ShouldBe(expected);
    }

    [Fact]
    public void Merge_WithDeepArrayMerge_ShouldMergeNestedObjects()
    {
        // Arrange
        var original = @"{""users"": [{""id"": 1, ""name"": ""John""}, {""id"": 2, ""name"": ""Mary""}]}";
        var patch = @"{""users"": [{""id"": 1, ""email"": ""john@example.com""}, {""id"": 2, ""email"": ""mary@example.com""}]}";
        var options = new MergeOptions
        {
            ArrayMergeStrategy = ArrayMergeStrategy.Merge,
            EnableDeepArrayMerge = true
        };

        // Act
        var result = JsonMerger.Merge(original, patch, options);

        // Assert
        var expected = @"{""users"":[{""id"":1,""name"":""John"",""email"":""john@example.com""},{""id"":2,""name"":""Mary"",""email"":""mary@example.com""}]}";
        result.ShouldBe(expected);
    }

    [Fact]
    public void MergeMultiple_ThreeJsonDocuments_ShouldMergeAll()
    {
        // Arrange
        var base1 = @"{""name"": ""John"", ""age"": 30}";
        var patch1 = @"{""email"": ""john@example.com""}";
        var patch2 = @"{""age"": 31, ""address"": ""123 Main St""}";

        // Act
        var result = JsonMerger.MergeMultiple(base1, new[] { patch1, patch2 });

        // Assert
        var expected = @"{""name"":""John"",""age"":31,""email"":""john@example.com"",""address"":""123 Main St""}";
        result.ShouldBe(expected);
    }

    [Fact]
    public void MergeMultiple_EmptyPatchArray_ShouldReturnBaseJson()
    {
        // Arrange
        var base1 = @"{""name"": ""John"", ""age"": 30}";

        // Act
        var result = JsonMerger.MergeMultiple(base1, Array.Empty<string>());

        // Assert
        result.ShouldBe(base1);
    }

    [Fact]
    public void MergeMultiple_WithEmptyPatchInArray_ShouldIgnoreEmptyPatch()
    {
        // Arrange
        var base1 = @"{""name"": ""John"", ""age"": 30}";
        var patch1 = @"{""email"": ""john@example.com""}";
        var patch2 = "";

        // Act
        var result = JsonMerger.MergeMultiple(base1, new[] { patch1, patch2 });

        // Assert
        var expected = @"{""name"":""John"",""age"":30,""email"":""john@example.com""}";
        result.ShouldBe(expected);
    }

    [Fact]
    public void MergeParallel_TwoPatches_ShouldMergeWithoutOverriding()
    {
        // Arrange
        var baseJson = @"{""user"": {""name"": ""John"", ""email"": ""john@example.com""}}";
        var patch1 = @"{""user"": {""role"": ""admin""}}";
        var patch2 = @"{""user"": {""email"": ""john.smith@example.com""}}";

        // Act
        var result = JsonMerger.MergeParallel(baseJson, new[] { patch1, patch2 });

        // Assert
        var expected = @"{""user"":{""name"":""John"",""email"":""john.smith@example.com"",""role"":""admin""}}";
        result.ShouldBe(expected);
    }

    [Fact]
    public void MergeParallel_EmptyPatchArray_ShouldReturnBaseJson()
    {
        // Arrange
        var baseJson = @"{""name"": ""John"", ""age"": 30}";

        // Act
        var result = JsonMerger.MergeParallel(baseJson, Array.Empty<string>());

        // Assert
        result.ShouldBe(baseJson);
    }

    [Fact]
    public void MergeParallel_SingleValidPatch_ShouldReturnDirectMerge()
    {
        // Arrange
        var baseJson = @"{""name"": ""John"", ""age"": 30}";
        var patch = @"{""email"": ""john@example.com""}";

        // Act
        var result = JsonMerger.MergeParallel(baseJson, new[] { patch });

        // Assert
        var expected = @"{""name"":""John"",""age"":30,""email"":""john@example.com""}";
        result.ShouldBe(expected);
    }

    [Fact]
    public void TryMergeMultiple_ValidInput_ShouldReturnTrueAndMergedResult()
    {
        // Arrange
        var baseJson = @"{""name"": ""John""}";
        var patches = new[] { @"{""age"": 30}", @"{""email"": ""john@example.com""}" };

        // Act
        bool success = JsonMerger.TryMergeMultiple(baseJson, patches, out string result);

        // Assert
        success.ShouldBeTrue();
        var expected = @"{""name"":""John"",""age"":30,""email"":""john@example.com""}";
        result.ShouldBe(expected);
    }

    [Fact]
    public void TryMergeMultiple_InvalidInput_ShouldReturnFalseAndBaseJson()
    {
        // Arrange
        var baseJson = @"{""name"": ""John""}";
        var patches = new[] { @"{invalid json}", @"{""email"": ""john@example.com""}" };

        // Act
        bool success = JsonMerger.TryMergeMultiple(baseJson, patches, out string result);

        // Assert
        success.ShouldBeFalse();
        result.ShouldBe(baseJson);
    }

    [Fact]
    public void Merge_NullOverridesValue_ShouldRespectOption()
    {
        // Arrange
        var original = @"{""name"": ""John"", ""email"": ""john@example.com""}";
        var patch = @"{""email"": null}";

        // Act - With NullOverridesValue = true
        var optionsTrue = new MergeOptions { NullOverridesValue = true };
        var resultTrue = JsonMerger.Merge(original, patch, optionsTrue);

        // Act - With NullOverridesValue = false
        var optionsFalse = new MergeOptions { NullOverridesValue = false };
        var resultFalse = JsonMerger.Merge(original, patch, optionsFalse);

        // Assert
        resultTrue.ShouldBe(@"{""name"":""John"",""email"":null}");
        resultFalse.ShouldBe(@"{""name"":""John"",""email"":""john@example.com""}");
    }

    [Fact]
    public void Merge_RemoveUnmatchedProperties_ShouldRespectOption()
    {
        // Arrange
        var original = @"{""name"": ""John"", ""age"": 30, ""email"": ""john@example.com""}";
        var patch = @"{""name"": ""John Smith"", ""address"": ""123 Main St""}";

        // Act - With RemoveUnmatchedProperties = true
        var optionsTrue = new MergeOptions { RemoveUnmatchedProperties = true };
        var resultTrue = JsonMerger.Merge(original, patch, optionsTrue);

        // Act - With RemoveUnmatchedProperties = false
        var optionsFalse = new MergeOptions { RemoveUnmatchedProperties = false };
        var resultFalse = JsonMerger.Merge(original, patch, optionsFalse);

        // Assert
        resultTrue.ShouldBe(@"{""name"":""John Smith"",""address"":""123 Main St""}");
        resultFalse.ShouldBe(@"{""name"":""John Smith"",""age"":30,""email"":""john@example.com"",""address"":""123 Main St""}");
    }

    [Fact]
    public void CloneWithFilter_IncludeSpecificProperties_ShouldReturnFilteredJson()
    {
        // Arrange
        var json = @"{""name"":""John"",""age"":30,""email"":""john@example.com"",""sensitive"":""data""}";
        using var doc = JsonDocument.Parse(json);

        // Act
        string[] includeProps = new[] { "name", "email" };
        string result = JsonMerger.CloneWithFilter(doc.RootElement, includeProps);

        // Assert
        var expected = @"{""name"":""John"",""email"":""john@example.com""}";
        result.ShouldBe(expected);
    }

    [Fact]
    public void CloneWithFilter_NoProperties_ShouldReturnEmptyObject()
    {
        // Arrange
        var json = @"{""name"":""John"",""age"":30}";
        using var doc = JsonDocument.Parse(json);

        // Act
        string[] includeProps = Array.Empty<string>();
        string result = JsonMerger.CloneWithFilter(doc.RootElement, includeProps);

        // Assert
        result.ShouldBe(@"{}");
    }

    [Fact]
    public void CloneWithFilter_WithIndentation_ShouldReturnIndentedJson()
    {
        // Arrange
        var json = @"{""name"":""John"",""age"":30}";
        using var doc = JsonDocument.Parse(json);

        // Act
        string result = JsonMerger.CloneWithFilter(doc.RootElement, null, true);

        // Assert
        result.ShouldContain("\n");
        result.ShouldContain("  ");
    }
}