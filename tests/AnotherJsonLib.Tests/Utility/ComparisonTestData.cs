using System.Text.Json.Nodes;

namespace AnotherJsonLib.Tests.Utility;

/// <summary>
/// Generates specific JSON structures for testing comparison functionality
/// </summary>
public class ComparisonTestData
{
    private readonly JsonFaker _faker;

    public ComparisonTestData(int? seed = null)
    {
        _faker = new JsonFaker(seed);
    }

    /// <summary>
    /// Generates pairs of JSON objects with different types of differences
    /// </summary>
    public IEnumerable<object[]> GetJsonPairsWithDifferences()
    {
        // Different property values
        var (original1, modified1, _) = _faker.GenerateDiffPair(2);
        yield return new object[] { original1, modified1, "Different property values" };

        // Different structure (added properties)
        var original2 = _faker.GenerateSimpleObject(3);
        var modified2 = JsonNode.Parse(original2.ToJsonString()).AsObject();
        modified2.Add("extraProperty1", "value1");
        modified2.Add("extraProperty2", 123);
        yield return new object[] { original2, modified2, "Added properties" };

        // Different structure (removed properties)
        var original3 = _faker.GenerateSimpleObject(5);
        var modified3 = JsonNode.Parse(original3.ToJsonString()).AsObject();
        var keys = original3.Select(p => p.Key).Take(2).ToArray();
        foreach (var key in keys)
        {
            modified3.Remove(key);
        }

        yield return new object[] { original3, modified3, "Removed properties" };

        // Different array ordering
        var original4 = new JsonObject();
        original4.Add("array", new JsonArray(1, 2, 3, 4, 5));
        var modified4 = new JsonObject();
        modified4.Add("array", new JsonArray(5, 4, 3, 2, 1));
        yield return new object[] { original4, modified4, "Different array ordering" };

        // Nested differences
        var original5 = _faker.GenerateComplexObject(3, 3);
        var modified5 = JsonNode.Parse(original5.ToJsonString()).AsObject();
        if (modified5.First().Value is JsonObject nestedObj)
        {
            nestedObj.Add("nestedExtra", "value");
        }

        yield return new object[] { original5, modified5, "Nested differences" };
    }

    /// <summary>
    /// Generates JSON data that contains arrays with different comparison challenges
    /// </summary>
    public IEnumerable<object[]> GetArrayComparisonTestCases()
    {
        // Same elements, different order
        yield return new object[]
        {
            new JsonArray(1, 2, 3),
            new JsonArray(3, 1, 2),
            true // should be considered equal when order doesn't matter
        };

        // Different lengths
        yield return new object[]
        {
            new JsonArray(1, 2, 3),
            new JsonArray(1, 2),
            false
        };

        // Duplicates
        yield return new object[]
        {
            new JsonArray(1, 2, 2, 3),
            new JsonArray(1, 2, 3, 3),
            false
        };

        // Complex objects in arrays
        var obj1 = _faker.GenerateSimpleObject(2);
        var obj2 = JsonNode.Parse(obj1.ToJsonString()).AsObject();
        var obj3 = _faker.GenerateSimpleObject(2);

        yield return new object[]
        {
            new JsonArray(obj1, obj3),
            new JsonArray(obj2, obj3),
            true // should be equal since obj1 and obj2 have same content
        };
    }
}

/// <summary>
/// Generates specific JSON structures for testing transformation functionality
/// </summary>
public class TransformationTestData
{
    private readonly JsonFaker _faker;

    public TransformationTestData(int? seed = null)
    {
        _faker = new JsonFaker(seed);
    }

    /// <summary>
    /// Generates JSON objects with various properties for testing property transformations
    /// </summary>
    public IEnumerable<object[]> GetPropertyTransformationTestCases()
    {
        // Case transformation
        var obj1 = new JsonObject
        {
            ["firstName"] = "John",
            ["lastName"] = "Doe",
            ["homeAddress"] = new JsonObject
            {
                ["streetName"] = "Main St",
                ["houseNumber"] = 123
            }
        };

        // Expected result for camelCase to snake_case
        var expected1 = new JsonObject
        {
            ["first_name"] = "John",
            ["last_name"] = "Doe",
            ["home_address"] = new JsonObject
            {
                ["street_name"] = "Main St",
                ["house_number"] = 123
            }
        };

        yield return new object[] { obj1, expected1, "camelCase to snake_case" };

        // Remove specific properties
        var obj2 = new JsonObject
        {
            ["id"] = 123,
            ["name"] = "Product",
            ["price"] = 99.99,
            ["secret"] = "confidential",
            ["internalUse"] = true
        };

        // Expected result after removing "secret" and properties starting with "internal"
        var expected2 = new JsonObject
        {
            ["id"] = 123,
            ["name"] = "Product",
            ["price"] = 99.99
        };

        yield return new object[] { obj2, expected2, "Remove specific properties" };
    }

    /// <summary>
    /// Generates JSON values with transformations to test value transformations
    /// </summary>
    public IEnumerable<object[]> GetValueTransformationTestCases()
    {
        // String transformations
        yield return new object[]
        {
            new JsonObject { ["text"] = "hello world" },
            new JsonObject { ["text"] = "HELLO WORLD" },
            "String to uppercase"
        };

        // Number transformations
        yield return new object[]
        {
            new JsonObject { ["price"] = 10.5 },
            new JsonObject { ["price"] = 11 },
            "Round number"
        };

        // Date transformations
        var date = DateTime.Now.ToString("yyyy-MM-dd");
        yield return new object[]
        {
            new JsonObject { ["date"] = date },
            new JsonObject { ["date"] = DateTime.Parse(date).ToString("MM/dd/yyyy") },
            "Date format change"
        };
    }
}

/// <summary>
/// Generates test data for schema validation testing
/// </summary>
public class SchemaTestData
{
    /// <summary>
    /// Returns pairs of schemas and valid/invalid JSON for testing validation
    /// </summary>
    public IEnumerable<object[]> GetSchemaValidationTestCases()
    {
        // Simple schema with type validation
        var schema1 = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["id"] = new JsonObject { ["type"] = "integer" },
                ["name"] = new JsonObject { ["type"] = "string" }
            },
            ["required"] = new JsonArray("id", "name")
        };

        // Valid for schema1
        var valid1 = new JsonObject
        {
            ["id"] = 123,
            ["name"] = "Test"
        };

        // Invalid for schema1 (wrong type)
        var invalid1 = new JsonObject
        {
            ["id"] = "123", // should be integer
            ["name"] = "Test"
        };

        // Invalid for schema1 (missing required)
        var invalid2 = new JsonObject
        {
            ["id"] = 123
            // missing "name"
        };

        yield return new object[] { schema1, valid1, true };
        yield return new object[] { schema1, invalid1, false };
        yield return new object[] { schema1, invalid2, false };

        // Schema with array validation
        var schema2 = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["items"] = new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = new JsonObject { ["type"] = "string" },
                    ["minItems"] = 1
                }
            },
            ["required"] = new JsonArray("items")
        };

        // Valid for schema2
        var valid2 = new JsonObject
        {
            ["items"] = new JsonArray("one", "two", "three")
        };

        // Invalid for schema2 (wrong item type)
        var invalid3 = new JsonObject
        {
            ["items"] = new JsonArray("one", 2, "three")
        };

        // Invalid for schema2 (empty array)
        var invalid4 = new JsonObject
        {
            ["items"] = new JsonArray()
        };

        yield return new object[] { schema2, valid2, true };
        yield return new object[] { schema2, invalid3, false };
        yield return new object[] { schema2, invalid4, false };
    }
}