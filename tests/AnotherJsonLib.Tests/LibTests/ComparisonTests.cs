using System.Text.Json.Nodes;
using AnotherJsonLib.Tests.Utility;
using Shouldly;

// Contains JsonFaker and JsonAssert


namespace AnotherJsonLib.Tests.LibTests;

public class ComparisonTests
{
    [Fact]
    public void ObjectsWithIdenticalContent_ShouldBeEqual()
    {
        // Arrange
        var json1 = JsonNode.Parse("{ \"name\": \"Alice\", \"age\": 30 }");
        var json2 = JsonNode.Parse("{ \"name\": \"Alice\", \"age\": 30 }");

        // Act & Assert
        Should.NotThrow(() => JsonAssert.Equal(json1, json2));
        json1.ToJsonString().ShouldBe(json2.ToJsonString());
    }

    [Fact]
    public void ObjectsWithDifferentPropertyValues_ShouldNotBeEqual()
    {
        // Arrange
        var json1 = JsonNode.Parse("{ \"name\": \"Alice\", \"age\": 30 }");
        var json2 = JsonNode.Parse("{ \"name\": \"Alice\", \"age\": 31 }");

        // Act & Assert
        Should.Throw<Exception>(() => JsonAssert.Equal(json1, json2));
    }

    [Theory]
    [MemberData(nameof(ArrayComparisonTestCases))]
    public void ArrayComparison_VariousCases(JsonArray array1, JsonArray array2, bool shouldBeEqual)
    {
        // Act & Assert
        if (shouldBeEqual)
        {
            Should.NotThrow(() => JsonAssert.Equal(array1, array2, ignoreArrayOrder: true));
        }
        else
        {
            Should.Throw<Exception>(() => JsonAssert.Equal(array1, array2, ignoreArrayOrder: true));
        }
    }

    public static IEnumerable<object[]> ArrayComparisonTestCases()
    {
        // Case 1: Same elements in different order should be equal when order is ignored.
        yield return new object[]
        {
            new JsonArray(1, 2, 3),
            new JsonArray(3, 1, 2),
            true
        };

        // Case 2: Arrays of different lengths should not be equal.
        yield return new object[]
        {
            new JsonArray(1, 2, 3),
            new JsonArray(1, 2),
            false
        };

        // Case 3: Arrays with duplicate elements arranged differently.
        yield return new object[]
        {
            new JsonArray(1, 2, 2, 3),
            new JsonArray(1, 2, 3, 3),
            false
        };

        // Case 4: Complex objects in arrays.
        var faker = new JsonFaker(42);
        var obj1 = faker.GenerateSimpleObject(2);
        var obj3 = faker.GenerateSimpleObject(2);
        // Get canonical JSON strings and parse independently to avoid parent conflicts.
        string obj1Json = obj1.ToJsonString();
        string obj3Json = obj3.ToJsonString();
        var array1 = new JsonArray(JsonNode.Parse(obj1Json)!, JsonNode.Parse(obj3Json)!);
        var array2 = new JsonArray(JsonNode.Parse(obj1Json)!, JsonNode.Parse(obj3Json)!);
        yield return new object[] { array1, array2, true };
    }

    [Fact]
    public void EmptyObjects_ShouldBeEqual()
    {
        // Arrange
        var json1 = JsonNode.Parse("{}");
        var json2 = JsonNode.Parse("{}");

        // Act & Assert
        Should.NotThrow(() => JsonAssert.Equal(json1, json2));
    }

    [Fact]
    public void EmptyArrays_ShouldBeEqual()
    {
        // Arrange
        var json1 = JsonNode.Parse("[]");
        var json2 = JsonNode.Parse("[]");

        // Act & Assert
        Should.NotThrow(() => JsonAssert.Equal(json1, json2, ignoreArrayOrder: true));
    }

    [Fact]
    public void EmptyObjectVsNonEmptyObject_ShouldNotBeEqual()
    {
        // Arrange
        var json1 = JsonNode.Parse("{}");
        var json2 = JsonNode.Parse("{ \"key\": \"value\" }");

        // Act & Assert
        Should.Throw<Exception>(() => JsonAssert.Equal(json1, json2));
    }

    [Fact]
    public void MixedTypeArrays_ShouldBeComparedCorrectly()
    {
        // Arrange: Create an array with numbers, strings, and booleans.
        var array1 = new JsonArray(1, "two", true, 3.14);
        var array2 = new JsonArray("two", true, 1, 3.14);

        // Act & Assert: With order ignored, both arrays should be considered equal.
        Should.NotThrow(() => JsonAssert.Equal(array1, array2, ignoreArrayOrder: true));
    }

    [Fact]
    public void NumericPrecision_ShouldBeRespected()
    {
        // Arrange: Two JSON objects with number values differing by a tiny amount.
        // Using values that differ only beyond 6 decimal places:
        var json1 = JsonNode.Parse("{ \"value\": 1.0000000001 }");
        var json2 = JsonNode.Parse("{ \"value\": 1.0000000002 }");
        // They should be considered equal (rounding to 6 decimals yields 1.000000).
        Should.NotThrow(() => JsonAssert.Equal(json1, json2));

        // Now, numbers differing at the 6th decimal place:
        var json3 = JsonNode.Parse("{ \"value\": 1.000001 }");
        var json4 = JsonNode.Parse("{ \"value\": 1.000002 }");
        // They should be considered not equal.
        Should.Throw<Exception>(() => JsonAssert.Equal(json3, json4));
    }

    [Fact]
    public void DeeplyNestedStructures_ShouldCompareCorrectly()
    {
        // Arrange: Create a deeply nested JSON object.
        int depth = 20;
        // Produce a fractional value to force the number to be treated as a double when parsed.
        JsonNode CreateDeepObject(int current, int max)
        {
            if (current == max)
                return JsonValue.Create(current + 0.1); 
            return new JsonObject { ["level" + current] = CreateDeepObject(current + 1, max) };
        }

        var deep1 = CreateDeepObject(0, depth);
        var deep2 = JsonNode.Parse(deep1.ToJsonString());
        // They should be equal.
        Should.NotThrow(() => JsonAssert.Equal(deep1, deep2));

        // Modify a deep value to force inequality.
        var deep3 = JsonNode.Parse(deep1.ToJsonString());
        var obj = deep3.AsObject();
        for (int i = 0; i < depth - 1; i++)
        {
            obj = obj["level" + i]!.AsObject();
        }
        obj["level" + (depth - 1)] = 999.0; // alter the leaf value (as double)
        Should.Throw<Exception>(() => JsonAssert.Equal(deep1, deep3));
    }

}