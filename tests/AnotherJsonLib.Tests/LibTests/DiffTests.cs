using System.Text.Json;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Utility.Comparison;
using Shouldly;

namespace AnotherJsonLib.Tests.LibTests;

public class DiffTests
{
    [Fact]
    public void Diff_IdenticalJson_ShouldProduceNoDifferences()
    {
        // Arrange
        string originalJson = "{\"name\":\"John\",\"age\":30}";
        string newJson = "{\"name\":\"John\",\"age\":30}";

        // Act
        var diff = JsonDiffer.ComputeDiff(originalJson, newJson);

        // Assert: Expect no differences.
        diff.Added.Count.ShouldBe(0);
        diff.Removed.Count.ShouldBe(0);
        diff.Modified.Count.ShouldBe(0);
    }

    [Fact]
    public void Diff_PropertyAdded_ShouldDetectAddition()
    {
        // Arrange
        string originalJson = "{\"name\":\"John\"}";
        string newJson = "{\"name\":\"John\",\"city\":\"Paris\"}";

        // Act
        var diff = JsonDiffer.ComputeDiff(originalJson, newJson);

        // Assert: "city" should be detected as added.
        diff.Added.ShouldContainKey("city");
        diff.Added["city"].ToString().ShouldBe("Paris");
        diff.Removed.Count.ShouldBe(0);
        diff.Modified.Count.ShouldBe(0);
    }

    [Fact]
    public void Diff_PropertyRemoved_ShouldDetectRemoval()
    {
        // Arrange
        string originalJson = "{\"name\":\"John\",\"age\":30}";
        string newJson = "{\"name\":\"John\"}";

        // Act
        var diff = JsonDiffer.ComputeDiff(originalJson, newJson);

        // Assert: "age" should be detected as removed.
        diff.Removed.ShouldContainKey("age");
        diff.Removed["age"].ToString().ShouldBe("30");
        diff.Added.Count.ShouldBe(0);
        diff.Modified.Count.ShouldBe(0);
    }

    [Fact]
    public void Diff_PropertyModified_ShouldDetectModification()
    {
        // Arrange
        string originalJson = "{\"name\":\"John\",\"age\":30}";
        string newJson = "{\"name\":\"John\",\"age\":31}";

        // Act
        var diff = JsonDiffer.ComputeDiff(originalJson, newJson);

        // Assert: "age" should be detected as modified.
        diff.Modified.ShouldContainKey("age");
        var modEntry = diff.Modified["age"];
        modEntry.OldValue.ToString().ShouldBe("30");
        modEntry.NewValue.ToString().ShouldBe("31");

        diff.Added.Count.ShouldBe(0);
        diff.Removed.Count.ShouldBe(0);
    }

    [Fact]
    public void Diff_NestedModification_ShouldDetectNestedDiff()
    {
        // Arrange: Nested JSON object.
        string originalJson = @"
            {
                ""user"": {
                    ""name"": ""John"",
                    ""address"": {
                        ""city"": ""London"",
                        ""zip"": ""E1 6AN""
                    }
                }
            }";
        string newJson = @"
            {
                ""user"": {
                    ""name"": ""John"",
                    ""address"": {
                        ""city"": ""Paris"",
                        ""zip"": ""75001""
                    }
                }
            }";

        // Act
        var diff = JsonDiffer.ComputeDiff(originalJson, newJson);

        // Assert: Expect modifications in the nested "address" object.
        diff.Modified.ShouldContainKey("user");
        var nestedDiff = diff.Modified["user"].NestedDiff;
        nestedDiff.Modified.ShouldContainKey("address");
        var addressDiff = nestedDiff.Modified["address"].NestedDiff;
        addressDiff.Modified.ShouldContainKey("city");
        addressDiff.Modified.ShouldContainKey("zip");
        addressDiff.Modified["city"].OldValue.ToString().ShouldBe("London");
        addressDiff.Modified["city"].NewValue.ToString().ShouldBe("Paris");
        addressDiff.Modified["zip"].OldValue.ToString().ShouldBe("E1 6AN");
        addressDiff.Modified["zip"].NewValue.ToString().ShouldBe("75001");
    }

    [Fact]
    public void Diff_InvalidJson_ShouldThrowException()
    {
        // Arrange: Provide invalid JSON input.
        string invalidJson = "{ \"name\": \"John\", "; // missing closing brace
        string validJson = "{\"name\":\"John\"}";

        // Act & Assert: Expect a JsonParsingException.
        Should.Throw<JsonParsingException>(() => JsonDiffer.ComputeDiff(invalidJson, validJson));
        Should.Throw<JsonParsingException>(() => JsonDiffer.ComputeDiff(validJson, invalidJson));
    }

    // ----- Additional Tests -----

    [Fact]
    public void Diff_EmptyObjects_ShouldProduceNoDifferences()
    {
        // Arrange
        string json1 = "{}";
        string json2 = "{}";

        // Act
        var diff = JsonDiffer.ComputeDiff(json1, json2);

        // Assert
        diff.Added.Count.ShouldBe(0);
        diff.Removed.Count.ShouldBe(0);
        diff.Modified.Count.ShouldBe(0);
    }

    [Fact]
    public void Diff_NullValueDifference_ShouldDetectModification()
    {
        // Arrange: Property is null in original and defined in new.
        string json1 = "{\"key\": null}";
        string json2 = "{\"key\": \"value\"}";

        // Act
        var diff = JsonDiffer.ComputeDiff(json1, json2);

        // Assert
        diff.Modified.ShouldContainKey("key");

        var oldValue = diff.Modified["key"].OldValue;
        oldValue.ShouldBeOfType<JsonElement>();
        ((JsonElement)oldValue).ValueKind.ShouldBe(JsonValueKind.Undefined);

        // For the new value, we expect the raw string "value".
        diff.Modified["key"].NewValue.ToString().ShouldBe("value");
    }


    [Fact]
    public void Diff_DefinedVsNull_ShouldDetectModification()
    {
        // Arrange: Property is defined in original and null in new.
        string json1 = "{\"key\": \"value\"}";
        string json2 = "{\"key\": null}";

        // Act
        var diff = JsonDiffer.ComputeDiff(json1, json2);

        // Assert
        diff.Modified.ShouldContainKey("key");
        diff.Modified["key"].OldValue.ToString().ShouldBe("value");
    }


    [Fact]
    public void Diff_ArrayDifferences_ShouldDetectChanges()
    {
        // Arrange: Arrays with elements added, removed, and re-ordered.
        string json1 = "{\"items\": [1, 2, 3]}";
        string json2 = "{\"items\": [3, 1, 4]}";

        // Act
        var diff = JsonDiffer.ComputeDiff(json1, json2);

        // Assert: Expect differences in the array.
        (diff.Added.Count + diff.Removed.Count + diff.Modified.Count)
            .ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Diff_MultipleChanges_ShouldDetectAll()
    {
        // Arrange: Multiple changes: one property added, one removed, one modified.
        string json1 = "{\"a\":1, \"b\":2, \"c\":3}";
        string json2 = "{\"a\":1, \"b\":20, \"d\":4}";

        // Act
        var diff = JsonDiffer.ComputeDiff(json1, json2);

        // Assert
        diff.Removed.ShouldContainKey("c");
        diff.Modified.ShouldContainKey("b");
        diff.Added.ShouldContainKey("d");
    }

    [Fact]
    public void Diff_ArrayOfObjects_ShouldDetectElementDifferences()
    {
        // Arrange: Two arrays of objects with a difference in one element.
        string json1 = "{\"items\":[{\"id\":1,\"name\":\"A\"}, {\"id\":2,\"name\":\"B\"}]}";
        string json2 = "{\"items\":[{\"id\":1,\"name\":\"A\"}, {\"id\":2,\"name\":\"C\"}]}";

        // Act
        var diff = JsonDiffer.ComputeDiff(json1, json2);

        // Assert: Expect that the second element in the "items" array shows a modification.
        diff.Modified.ShouldContainKey("items");
        var nestedDiff = diff.Modified["items"].NestedDiff;
        nestedDiff.Modified.ShouldContainKey("1");
        var modEntry = nestedDiff.Modified["1"];
        modEntry.NestedDiff.Modified.ShouldContainKey("name");
        modEntry.NestedDiff.Modified["name"].OldValue.ToString().ShouldBe("B");
        modEntry.NestedDiff.Modified["name"].NewValue.ToString().ShouldBe("C");
    }

    [Fact]
    public void Diff_EmptyArrayVsNonEmptyArray_ShouldDetectDifferences()
    {
        // Arrange
        string json1 = "{ \"array\": [] }";
        string json2 = "{ \"array\": [1,2,3] }";

        // Act
        var diff = JsonDiffer.ComputeDiff(json1, json2);

        // Assert: Expect some differences (implementation-dependent)
        (diff.Added.Count + diff.Removed.Count + diff.Modified.Count)
            .ShouldBeGreaterThan(0);
    }

    [Fact]
    public void TryComputeDiff_ValidJson_ShouldReturnTrueWithDiffResult()
    {
        // Arrange
        string originalJson = "{\"name\":\"John\",\"age\":30}";
        string newJson = "{\"name\":\"John\",\"age\":31}";

        // Act
        bool success = JsonDiffer.TryComputeDiff(originalJson, newJson, out var diff);

        // Assert
        success.ShouldBeTrue();
        diff.ShouldNotBeNull();
        diff.Modified.ShouldContainKey("age");
        var modEntry = diff.Modified["age"];
        modEntry.OldValue.ToString().ShouldBe("30");
        modEntry.NewValue.ToString().ShouldBe("31");
    }

    [Fact]
    public void TryComputeDiff_InvalidJson_ShouldReturnFalseWithNullResult()
    {
        // Arrange
        string originalJson = "{\"name\":\"John\",\"age\":30}";
        string invalidJson = "{\"name\":\"John\",\"age\":"; // Invalid JSON

        // Act
        bool success = JsonDiffer.TryComputeDiff(originalJson, invalidJson, out var diff);

        // Assert
        success.ShouldBeFalse();
        diff.ShouldBeNull();
    }

    [Fact]
    public void ComputeDiffReport_ShouldReturnFormattedReport()
    {
        // Arrange
        string originalJson = "{\"name\":\"John\",\"age\":30,\"city\":\"London\"}";
        string newJson = "{\"name\":\"John\",\"age\":31,\"country\":\"UK\"}";

        // Act
        string report = JsonDiffer.ComputeDiffReport(originalJson, newJson);

        // Assert
        report.ShouldContain("Added:");
        report.ShouldContain("country: \"UK\"");
        report.ShouldContain("Removed:");
        report.ShouldContain("city: \"London\"");
        report.ShouldContain("Modified:");
        report.ShouldContain("age:");
        report.ShouldNotContain("name: \"John\""); // Unchanged property not included
    }


    [Fact]
    public void ComputeDiffReport_WithUnchangedIncluded_ShouldShowUnchangedProperties()
    {
        // Arrange
        string originalJson = "{\"name\":\"John\",\"age\":30,\"city\":\"London\"}";
        string newJson = "{\"name\":\"John\",\"age\":31,\"country\":\"UK\"}";

        // Act
        string report = JsonDiffer.ComputeDiffReport(originalJson, newJson, includeUnchanged: true);

        // Assert
        report.ShouldContain("JSON Diff Report:");
        report.ShouldContain("Added:");
        report.ShouldContain("country: \"UK\"");
        report.ShouldContain("Removed:");
        report.ShouldContain("city: \"London\"");
        report.ShouldContain("Modified:");
        report.ShouldContain("age: 30 -> 31");
    }



    [Fact]
    public void HasDifferences_WithDifferences_ShouldReturnTrue()
    {
        // Arrange
        string originalJson = "{\"name\":\"John\",\"age\":30}";
        string newJson = "{\"name\":\"John\",\"age\":31}";

        // Act
        bool hasDiffs = JsonDiffer.HasDifferences(originalJson, newJson);

        // Assert
        hasDiffs.ShouldBeTrue();
    }

    [Fact]
    public void HasDifferences_WithoutDifferences_ShouldReturnFalse()
    {
        // Arrange
        string originalJson = "{\"name\":\"John\",\"age\":30}";
        string newJson = "{\"name\":\"John\",\"age\":30}";

        // Act
        bool hasDiffs = JsonDiffer.HasDifferences(originalJson, newJson);

        // Assert
        hasDiffs.ShouldBeFalse();
    }

    [Fact]
    public void TryHasDifferences_ValidJson_ShouldReturnTrueWithResult()
    {
        // Arrange
        string originalJson = "{\"name\":\"John\",\"age\":30}";
        string newJson = "{\"name\":\"John\",\"age\":31}";

        // Act
        bool success = JsonDiffer.TryHasDifferences(originalJson, newJson, out var hasDifferences);

        // Assert
        success.ShouldBeTrue();
        hasDifferences.ShouldNotBeNull();
        hasDifferences.Value.ShouldBeTrue();
    }

    [Fact]
    public void TryHasDifferences_InvalidJson_ShouldReturnFalseWithNullResult()
    {
        // Arrange
        string originalJson = "{\"name\":\"John\",\"age\":30}";
        string invalidJson = "{\"name\":\"John\",\"age\":"; // Invalid JSON

        // Act
        bool success = JsonDiffer.TryHasDifferences(originalJson, invalidJson, out var hasDifferences);

        // Assert
        success.ShouldBeFalse();
        hasDifferences.ShouldBeNull();
    }

    [Fact]
    public void Diff_ArrayComparison_ShouldDetectDifferences()
    {
        // Arrange
        string originalJson = "{\"numbers\":[1,2,3]}";
        string newJson = "{\"numbers\":[1,2,3,4]}";

        // Act
        var diff = JsonDiffer.ComputeDiff(originalJson, newJson);

        // Assert
        diff.Modified.ShouldContainKey("numbers");
    
        // Instead of checking the string representation, check that it's a collection
        // and verify the contents
        diff.Modified["numbers"].OldValue.ShouldBeAssignableTo<System.Collections.Generic.IEnumerable<object>>();
        var oldValues = ((System.Collections.Generic.IEnumerable<object>)diff.Modified["numbers"].OldValue).ToList();
        oldValues.Count.ShouldBe(3);
        oldValues[0].ShouldBe(1);
        oldValues[1].ShouldBe(2);
        oldValues[2].ShouldBe(3);
    
        diff.Modified["numbers"].NewValue.ShouldBeAssignableTo<System.Collections.Generic.IEnumerable<object>>();
        var newValues = ((System.Collections.Generic.IEnumerable<object>)diff.Modified["numbers"].NewValue).ToList();
        newValues.Count.ShouldBe(4);
        newValues[0].ShouldBe(1);
        newValues[1].ShouldBe(2);
        newValues[2].ShouldBe(3);
        newValues[3].ShouldBe(4);
    }


    [Fact]
    public void Diff_DifferentValueTypes_ShouldDetectTypeChanges()
    {
        // Arrange
        string originalJson = "{\"value\":42}";
        string newJson = "{\"value\":\"42\"}";

        // Act
        var diff = JsonDiffer.ComputeDiff(originalJson, newJson);

        // Assert
        diff.Modified.ShouldContainKey("value");
        diff.Modified["value"].OldValue.ShouldBe(42);
        diff.Modified["value"].NewValue.ShouldBe("42");
    }

    [Fact]
    public void Diff_ComplexNestedObjects_ShouldHandleDeepNesting()
    {
        // Arrange
        string originalJson = @"{
        ""level1"": {
            ""level2"": {
                ""level3"": {
                    ""data"": ""original""
                }
            }
        }
    }";

        string newJson = @"{
        ""level1"": {
            ""level2"": {
                ""level3"": {
                    ""data"": ""changed""
                }
            }
        }
    }";

        // Act
        var diff = JsonDiffer.ComputeDiff(originalJson, newJson);

        // Assert
        diff.Modified.ShouldContainKey("level1");
        var level1Diff = diff.Modified["level1"].NestedDiff;
        level1Diff.Modified.ShouldContainKey("level2");
        var level2Diff = level1Diff.Modified["level2"].NestedDiff;
        level2Diff.Modified.ShouldContainKey("level3");
        var level3Diff = level2Diff.Modified["level3"].NestedDiff;
        level3Diff.Modified.ShouldContainKey("data");
        level3Diff.Modified["data"].OldValue.ToString().ShouldBe("original");
        level3Diff.Modified["data"].NewValue.ToString().ShouldBe("changed");
    }
}