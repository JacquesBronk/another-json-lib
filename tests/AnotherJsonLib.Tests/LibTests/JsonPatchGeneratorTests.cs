using System.Text.Json;
using AnotherJsonLib.Domain;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Utility;
using AnotherJsonLib.Utility.Operations;
using Shouldly;

namespace AnotherJsonLib.Tests.LibTests;

public class JsonPatchGeneratorTests
{

    [Fact]
    public void Constructor_WithDefaultOptions_ShouldCreateInstance()
    {
        // Act
        var generator = new JsonPatchGenerator();

        // Assert
        generator.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithCustomOptions_ShouldCreateInstance()
    {
        // Arrange
        var options = new PatchGeneratorOptions { OptimizePatch = false };

        // Act
        var generator = new JsonPatchGenerator(options);

        // Assert
        generator.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new JsonPatchGenerator(null));
    }

    [Fact]
    public void GeneratePatch_WithNullOrEmptyInput_ShouldThrowException()
    {
        // Arrange
        var generator = new JsonPatchGenerator();

        // Act & Assert
        Should.Throw<JsonArgumentException>(() => generator.GeneratePatch(null, "{}"));
        Should.Throw<JsonArgumentException>(() => generator.GeneratePatch("", "{}"));
        Should.Throw<JsonArgumentException>(() => generator.GeneratePatch("{}", null));
        Should.Throw<JsonArgumentException>(() => generator.GeneratePatch("{}", ""));
    }

    [Fact]
    public void GeneratePatch_WithInvalidJson_ShouldThrowException()
    {
        // Arrange
        var generator = new JsonPatchGenerator();

        // Act & Assert
        Should.Throw<JsonParsingException>(() => generator.GeneratePatch("{invalid}", "{}"));
        Should.Throw<JsonParsingException>(() => generator.GeneratePatch("{}", "{invalid}"));
    }

    [Fact]
    public void GeneratePatch_WithIdenticalJson_ShouldReturnEmptyPatch()
    {
        // Arrange
        var generator = new JsonPatchGenerator();
        var json = "{\"name\":\"test\",\"value\":42}";

        // Act
        var patch = generator.GeneratePatch(json, json);

        // Assert
        patch.ShouldNotBeNull();
        patch.Count.ShouldBe(0);
    }

    [Fact]
    public void GeneratePatch_WithSimpleChange_ShouldGeneratePatch()
    {
        // Arrange
        var generator = new JsonPatchGenerator();
        var originalJson = "{\"name\":\"test\",\"value\":42}";
        var updatedJson = "{\"name\":\"updated\",\"value\":42}";

        // Act
        var patch = generator.GeneratePatch(originalJson, updatedJson);

        // Assert
        patch.ShouldNotBeNull();
        patch.Count.ShouldBe(1);
        patch[0].Op.ShouldBe("replace");
        patch[0].Path.ShouldBe("/name");
        JsonSerializer.Serialize(patch[0].Value).ShouldContain("updated");
    }

    [Fact]
    public void GeneratePatch_WithPropertyAddition_ShouldGeneratePatch()
    {
        // Arrange
        var generator = new JsonPatchGenerator();
        var originalJson = "{\"name\":\"test\"}";
        var updatedJson = "{\"name\":\"test\",\"value\":42}";

        // Act
        var patch = generator.GeneratePatch(originalJson, updatedJson);

        // Assert
        patch.ShouldNotBeNull();
        patch.Count.ShouldBe(1);
        patch[0].Op.ShouldBe("add");
        patch[0].Path.ShouldBe("/value");
        patch[0].Value.ToJson().ShouldContain("42");
    }

    [Fact]
    public void GeneratePatch_WithPropertyRemoval_ShouldGeneratePatch()
    {
        // Arrange
        var generator = new JsonPatchGenerator();
        var originalJson = "{\"name\":\"test\",\"value\":42}";
        var updatedJson = "{\"name\":\"test\"}";

        // Act
        var patch = generator.GeneratePatch(originalJson, updatedJson);

        // Assert
        patch.ShouldNotBeNull();
        patch.Count.ShouldBe(1);
        patch[0].Op.ShouldBe("remove");
        patch[0].Path.ShouldBe("/value");
    }

    [Fact]
    public void GeneratePatch_WithPropertyRemoval_AndIgnoreRemovalsOption_ShouldNotGeneratePatch()
    {
        // Arrange
        var options = new PatchGeneratorOptions { IgnoreRemovals = true };
        var generator = new JsonPatchGenerator(options);
        var originalJson = "{\"name\":\"test\",\"value\":42}";
        var updatedJson = "{\"name\":\"test\"}";

        // Act
        var patch = generator.GeneratePatch(originalJson, updatedJson);

        // Assert
        patch.ShouldNotBeNull();
        patch.Count.ShouldBe(0);
    }

    [Fact]
    public void GeneratePatch_WithNestedObjectChanges_ShouldGeneratePatch()
    {
        // Arrange
        var generator = new JsonPatchGenerator();
        var originalJson = "{\"person\":{\"name\":\"John\",\"age\":30}}";
        var updatedJson = "{\"person\":{\"name\":\"John\",\"age\":31}}";

        // Act
        var patch = generator.GeneratePatch(originalJson, updatedJson);

        // Assert
        patch.ShouldNotBeNull();
        patch.Count.ShouldBe(1);
        patch[0].Op.ShouldBe("replace");
        patch[0].Path.ShouldBe("/person/age");
        patch[0].Value.ToJson().ShouldContain("31");
    }

    [Fact]
    public void GeneratePatch_WithSimpleArrayAddition_ShouldGeneratePatch()
    {
        // Arrange
        var generator = new JsonPatchGenerator();
        var originalJson = "{\"items\":[1,2,3]}";
        var updatedJson = "{\"items\":[1,2,3,4]}";

        // Act
        var patch = generator.GeneratePatch(originalJson, updatedJson);

        // Assert
        patch.ShouldNotBeNull();
        patch.Count.ShouldBe(1);
        patch[0].Op.ShouldBe("add");
        patch[0].Path.ShouldBe("/items/3");
        JsonSerializer.Serialize(patch[0].Value).ShouldContain("4");
    }

    [Fact]
    public void GeneratePatch_WithSimpleArrayRemoval_ShouldGeneratePatch()
    {
        // Arrange
        var generator = new JsonPatchGenerator();
        var originalJson = "{\"items\":[1,2,3,4]}";
        var updatedJson = "{\"items\":[1,2,4]}";

        // Act
        var patch = generator.GeneratePatch(originalJson, updatedJson);

        // Assert
        patch.ShouldNotBeNull();
        patch.Count.ShouldBeGreaterThanOrEqualTo(1);
        var removeOp = patch.Find(op => op.Op == "remove");
        removeOp.ShouldNotBeNull();
    }

    [Fact]
    public void GeneratePatch_WithArrayReplacement_ShouldGeneratePatch()
    {
        // Arrange
        var generator = new JsonPatchGenerator();
        var originalJson = "{\"items\":[1,2,3]}";
        var updatedJson = "{\"items\":[4,5,6]}";

        // Act
        var patch = generator.GeneratePatch(originalJson, updatedJson);

        // Assert
        patch.ShouldNotBeNull();
        // Depending on options, this might be replace operations or a combination of add/remove
        patch.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void GeneratePatch_WithEmptyToNonEmptyArray_ShouldGeneratePatch()
    {
        // Arrange
        var generator = new JsonPatchGenerator();
        var originalJson = "{\"items\":[]}";
        var updatedJson = "{\"items\":[1,2,3]}";

        // Act
        var patch = generator.GeneratePatch(originalJson, updatedJson);

        // Assert
        patch.ShouldNotBeNull();
        patch.Count.ShouldBe(1);
        patch[0].Op.ShouldBe("replace");
        patch[0].Path.ShouldBe("/items");
    }

    [Fact]
    public void GeneratePatch_WithNonEmptyToEmptyArray_ShouldGeneratePatch()
    {
        // Arrange
        var generator = new JsonPatchGenerator();
        var originalJson = "{\"items\":[1,2,3]}";
        var updatedJson = "{\"items\":[]}";

        // Act
        var patch = generator.GeneratePatch(originalJson, updatedJson);

        // Assert
        patch.ShouldNotBeNull();
        patch.Count.ShouldBe(1);
        patch[0].Op.ShouldBe("replace");
        patch[0].Path.ShouldBe("/items");
    }

    [Fact]
    public void GeneratePatch_WithLargeArrays_ShouldUseSimpleAlgorithm()
    {
        // Arrange
        var options = new PatchGeneratorOptions { MaxArraySizeForLcs = 3 };
        var generator = new JsonPatchGenerator(options);

        var originalItems = new int[5] { 1, 2, 3, 4, 5 };
        var updatedItems = new int[5] { 1, 2, 3, 5, 6 };

        var originalJson = JsonSerializer.Serialize(new { items = originalItems });
        var updatedJson = JsonSerializer.Serialize(new { items = updatedItems });

        // Act
        var patch = generator.GeneratePatch(originalJson, updatedJson);

        // Assert
        patch.ShouldNotBeNull();
        // The simple algorithm should replace the entire array or do positional replacements
        if (options.UsePositionalArrayPatching)
        {
            // Should contain replace operations for positions 3 and 4
            patch.Count.ShouldBe(2);
            patch.ShouldContain(op => op.Op == "replace" && op.Path == "/items/3");
            patch.ShouldContain(op => op.Op == "replace" && op.Path == "/items/4");
        }
        else
        {
            // Should replace the whole array
            patch.Count.ShouldBe(1);
            patch[0].Op.ShouldBe("replace");
            patch[0].Path.ShouldBe("/items");
        }
    }

    [Fact]
    public void GeneratePatch_WithDisabledArrayDiff_ShouldReplaceEntireArray()
    {
        // Arrange
        var options = new PatchGeneratorOptions { UseArrayDiffAlgorithm = false };
        var generator = new JsonPatchGenerator(options);

        var originalJson = "{\"items\":[1,2,3]}";
        var updatedJson = "{\"items\":[1,2,4]}";

        // Act
        var patch = generator.GeneratePatch(originalJson, updatedJson);

        // Assert
        patch.ShouldNotBeNull();
        patch.Count.ShouldBe(1);
        patch[0].Op.ShouldBe("replace");
        patch[0].Path.ShouldBe("/items");
    }

    [Fact]
    public void TryGeneratePatch_WithValidJson_ShouldReturnTrue()
    {
        // Arrange
        var generator = new JsonPatchGenerator();
        var originalJson = "{\"name\":\"test\"}";
        var updatedJson = "{\"name\":\"updated\"}";

        // Act
        bool result = generator.TryGeneratePatch(originalJson, updatedJson, out var patch);

        // Assert
        result.ShouldBeTrue();
        patch.ShouldNotBeNull();
        patch.Count.ShouldBe(1);
    }

    [Fact]
    public void TryGeneratePatch_WithNullInput_ShouldReturnFalse()
    {
        // Arrange
        var generator = new JsonPatchGenerator();

        // Act
        bool result = generator.TryGeneratePatch(null, "{}", out var patch);

        // Assert
        result.ShouldBeFalse();
        patch.ShouldNotBeNull();
        patch.Count.ShouldBe(0);
    }

    [Fact]
    public void TryGeneratePatch_WithInvalidJson_ShouldReturnFalse()
    {
        // Arrange
        var generator = new JsonPatchGenerator();

        // Act
        bool result = generator.TryGeneratePatch("{invalid}", "{}", out var patch);

        // Assert
        result.ShouldBeFalse();
        patch.ShouldNotBeNull();
        patch.Count.ShouldBe(0);
    }

    [Fact]
    public void GeneratePatchAsJson_WithValidInputs_ShouldReturnJsonString()
    {
        // Arrange
        var generator = new JsonPatchGenerator();
        var originalJson = "{\"name\":\"test\"}";
        var updatedJson = "{\"name\":\"updated\"}";

        // Act
        var jsonPatch = generator.GeneratePatchAsJson(originalJson, updatedJson);

        // Assert
        jsonPatch.ShouldNotBeNull();
        jsonPatch.ShouldNotBeEmpty();

        // Should be valid JSON and deserialize to a JsonPatchOperation array
        var operations = JsonSerializer.Deserialize<List<JsonPatchOperation>>(jsonPatch,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        operations.ShouldNotBeNull();
        operations.Count.ShouldBe(1);
        operations[0].Op.ShouldBe("replace");
        operations[0].Path.ShouldBe("/name");
    }

    [Fact]
    public void GeneratePatchAsJson_WithFormatOutputFalse_ShouldReturnCompactJson()
    {
        // Arrange
        var options = new PatchGeneratorOptions { FormatOutput = false };
        var generator = new JsonPatchGenerator(options);
        var originalJson = "{\"name\":\"test\"}";
        var updatedJson = "{\"name\":\"updated\"}";

        // Act
        var jsonPatch = generator.GeneratePatchAsJson(originalJson, updatedJson);

        // Assert
        jsonPatch.ShouldNotBeNull();
        jsonPatch.ShouldNotBeEmpty();
        jsonPatch.ShouldNotContain("\n"); // No newlines in compact JSON
    }

    [Fact]
    public void GeneratePatch_WithOptimizationEnabled_ShouldCombineOperations()
    {
        // Arrange
        var options = new PatchGeneratorOptions { OptimizePatch = true };
        var generator = new JsonPatchGenerator(options);

        // Create a case where one item is removed and then added at a different position
        var originalJson = "{\"items\":[\"a\",\"b\",\"c\"]}";
        var updatedJson = "{\"items\":[\"a\",\"c\",\"b\"]}";

        // Act
        var patch = generator.GeneratePatch(originalJson, updatedJson);

        // Assert
        patch.ShouldNotBeNull();
        // Due to optimization, this might be done with a "move" operation
        patch.ShouldContain(op => op.Op == "move" || (op.Op == "add" && op.Path == "/items/1"));
    }

    [Fact]
    public void GeneratePatch_WithOptimizationDisabled_ShouldNotCombineOperations()
    {
        // Arrange
        var options = new PatchGeneratorOptions { OptimizePatch = false };
        var generator = new JsonPatchGenerator(options);

        // Create a case where one item is removed and then added at a different position
        var originalJson = "{\"items\":[\"a\",\"b\",\"c\"]}";
        var updatedJson = "{\"items\":[\"a\",\"c\",\"b\"]}";

        // Act
        var patch = generator.GeneratePatch(originalJson, updatedJson);

        // Assert
        patch.ShouldNotBeNull();
        // Without optimization, expect separate remove and add operations
        patch.ShouldContain(op => op.Op == "remove");
        patch.ShouldContain(op => op.Op == "add");
        patch.ShouldNotContain(op => op.Op == "move");
    }

    [Fact]
    public void GeneratePatch_WithDifferentValueTypes_ShouldReplaceValue()
    {
        // Arrange
        var generator = new JsonPatchGenerator();
        var originalJson = "{\"value\":42}";
        var updatedJson = "{\"value\":\"42\"}"; // Number to string

        // Act
        var patch = generator.GeneratePatch(originalJson, updatedJson);

        // Assert
        patch.ShouldNotBeNull();
        patch.Count.ShouldBe(1);
        patch[0].Op.ShouldBe("replace");
        patch[0].Path.ShouldBe("/value");
    }

    [Fact]
    public void GeneratePatch_WithNestedArrayChanges_ShouldGenerateCorrectPatch()
    {
        // Arrange
        var generator = new JsonPatchGenerator();
        var originalJson = "{\"nested\":{\"array\":[1,2,3]}}";
        var updatedJson = "{\"nested\":{\"array\":[1,3,4]}}";

        // Act
        var patch = generator.GeneratePatch(originalJson, updatedJson);

        // Assert
        patch.ShouldNotBeNull();
        patch.Count.ShouldBeGreaterThan(0);
        // Should contain operations modifying the nested array
        patch.ShouldContain(op => op.Path.StartsWith("/nested/array"));
    }

    [Fact]
    public void GeneratePatch_WithSpecialCharactersInPropertyNames_ShouldEscapeCorrectly()
    {
        // Arrange
        var generator = new JsonPatchGenerator();
        var originalJson = "{\"property/with~special/chars\":\"old\"}";
        var updatedJson = "{\"property/with~special/chars\":\"new\"}";

        // Act
        var patch = generator.GeneratePatch(originalJson, updatedJson);

        // Assert
        patch.ShouldNotBeNull();
        patch.Count.ShouldBe(1);
        patch[0].Op.ShouldBe("replace");
        // Should properly escape / as ~1 and ~ as ~0 per RFC 6901
        patch[0].Path.ShouldBe("/property~1with~0special~1chars");
    }

    [Fact]
    public void GeneratePatch_WithNullValues_ShouldGenerateCorrectPatch()
    {
        // Arrange
        var generator = new JsonPatchGenerator();
        var originalJson = "{\"value\":\"test\"}";
        var updatedJson = "{\"value\":null}";

        // Act
        var patch = generator.GeneratePatch(originalJson, updatedJson);

        // Assert
        patch.ShouldNotBeNull();
        patch.Count.ShouldBe(1);
        patch[0].Op.ShouldBe("replace");
        patch[0].Path.ShouldBe("/value");
        patch[0].Value.ShouldNotBeNull(); // Contains JsonElement with null value
    }

    [Fact]
    public void GeneratePatch_WithComplexNestedStructures_ShouldGenerateCorrectPatch()
    {
        // Arrange
        var generator = new JsonPatchGenerator();
        var originalJson = @"{
                ""people"": [
                    {""name"": ""John"", ""age"": 30},
                    {""name"": ""Alice"", ""age"": 25}
                ],
                ""metadata"": {""version"": 1}
            }";

        var updatedJson = @"{
                ""people"": [
                    {""name"": ""John"", ""age"": 31},
                    {""name"": ""Bob"", ""age"": 40}
                ],
                ""metadata"": {""version"": 2}
            }";

        // Act
        var patch = generator.GeneratePatch(originalJson, updatedJson);

        // Assert
        patch.ShouldNotBeNull();
        patch.Count.ShouldBeGreaterThan(0);
        // Should contain operations for both the nested array and object changes
        patch.ShouldContain(op => op.Path.StartsWith("/people"));
        patch.ShouldContain(op => op.Path == "/metadata/version");
    }
}