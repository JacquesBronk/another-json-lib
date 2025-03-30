using System.Text.Json;
using System.Text.Json.Nodes;

namespace AnotherJsonLib.Tests.Utility;

/// <summary>
/// Helper methods for JSON-related assertions in tests
/// </summary>
public static class JsonAssert
{
    /// <summary>
    /// Asserts that two JSON nodes are equal in terms of their content
    /// </summary>
    public static void Equal(JsonNode expected, JsonNode actual, bool ignoreArrayOrder = false,
        bool ignoreObjectOrder = true)
    {
        if (expected == null && actual == null)
            return;

        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected.GetValueKind(), actual.GetValueKind());

        switch (expected.GetValueKind())
        {
            case JsonValueKind.Object:
                var expectedObj = expected.AsObject();
                var actualObj = actual.AsObject();

                Assert.Equal(expectedObj.Count, actualObj.Count);

                foreach (var prop in expectedObj)
                {
                    Assert.True(actualObj.ContainsKey(prop.Key), $"Missing property: {prop.Key}");
                    Equal(prop.Value, actualObj[prop.Key], ignoreArrayOrder, ignoreObjectOrder);
                }

                break;

            case JsonValueKind.Array:
                var expectedArr = expected.AsArray();
                var actualArr = actual.AsArray();

                Assert.Equal(expectedArr.Count, actualArr.Count);

                if (ignoreArrayOrder)
                {
                    // Instead of attempting to match elements via try/catch,
                    // sort both arrays by their canonical serialized form.
                    var sortedExpectedStrings = expectedArr.Select(e => e.ToJsonString()).OrderBy(s => s).ToArray();
                    var sortedActualStrings = actualArr.Select(e => e.ToJsonString()).OrderBy(s => s).ToArray();

                    for (int i = 0; i < sortedExpectedStrings.Length; i++)
                    {
                        // Parse the sorted JSON strings back to JsonNodes and compare recursively.
                        var nodeExpected = JsonNode.Parse(sortedExpectedStrings[i]);
                        var nodeActual = JsonNode.Parse(sortedActualStrings[i]);
                        Equal(nodeExpected, nodeActual, ignoreArrayOrder, ignoreObjectOrder);
                    }
                }
                else
                {
                    for (int i = 0; i < expectedArr.Count; i++)
                    {
                        Equal(expectedArr[i], actualArr[i], ignoreArrayOrder, ignoreObjectOrder);
                    }
                }
                break;

            case JsonValueKind.String:
                Assert.Equal(expected.GetValue<string>(), actual.GetValue<string>());
                break;

            case JsonValueKind.Number:
                if (IsIntegral(expected) && IsIntegral(actual))
                {
                    Assert.Equal(expected.GetValue<long>(), actual.GetValue<long>());
                }
                else
                {
                    Assert.Equal(expected.GetValue<double>(), actual.GetValue<double>(), 6); // 6 decimal precision
                }

                break;

            case JsonValueKind.True:
            case JsonValueKind.False:
                Assert.Equal(expected.GetValue<bool>(), actual.GetValue<bool>());
                break;

            case JsonValueKind.Null:
                // Both are null, so they're equal
                break;
        }
    }

    private static bool IsIntegral(JsonNode node)
    {
        try
        {
            var doubleValue = node.GetValue<double>();
            return Math.Abs(doubleValue - Math.Round(doubleValue)) < double.Epsilon;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Asserts that a JSON string is valid and can be parsed
    /// </summary>
    public static JsonNode AssertValidJson(string json)
    {
        try
        {
            return JsonNode.Parse(json);
        }
        catch (Exception ex)
        {
            throw new Xunit.Sdk.XunitException($"JSON is not valid: {ex.Message}\nJSON: {json}");
        }
    }

    /// <summary>
    /// Asserts that a JSON string is not valid and will throw when parsed
    /// </summary>
    public static void AssertInvalidJson(string json)
    {
        Assert.ThrowsAny<Exception>(() => JsonNode.Parse(json));
    }

    /// <summary>
    /// Asserts that a JSON object has a specific property
    /// </summary>
    public static void HasProperty(JsonObject jsonObject, string propertyName)
    {
        Assert.True(jsonObject.ContainsKey(propertyName), $"JSON object does not contain property '{propertyName}'");
    }

    /// <summary>
    /// Asserts that a JSON object has a specific property with the expected value
    /// </summary>
    public static void PropertyEquals<T>(JsonObject jsonObject, string propertyName, T expectedValue)
    {
        HasProperty(jsonObject, propertyName);
        var actualValue = jsonObject[propertyName].GetValue<T>();
        Assert.Equal(expectedValue, actualValue);
    }
}

/// <summary>
/// Provides helper methods for creating test execution contexts
/// </summary>
public static class TestContext
{
    /// <summary>
    /// Creates a JsonDocumentOptions object configured for testing
    /// </summary>
    public static JsonDocumentOptions CreateStandardOptions()
    {
        return new JsonDocumentOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow,
            MaxDepth = 64
        };
    }

    /// <summary>
    /// Creates a JsonDocumentOptions object configured for lenient parsing
    /// </summary>
    public static JsonDocumentOptions CreateLenientOptions()
    {
        return new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip,
            MaxDepth = 128
        };
    }

    /// <summary>
    /// Creates a JsonDocumentOptions object configured for strict parsing
    /// </summary>
    public static JsonDocumentOptions CreateStrictOptions()
    {
        return new JsonDocumentOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow,
            MaxDepth = 32
        };
    }

    /// <summary>
    /// Helper to create various performance test scenarios
    /// </summary>
    public static JsonNode CreatePerformanceTestJson(int size)
    {
        if (size <= 0) throw new ArgumentException("Size must be positive", nameof(size));

        var faker = new JsonFaker(42); // Fixed seed for reproducibility

        return size switch
        {
            <= 10 => faker.GenerateSimpleObject(10),
            <= 100 => faker.GenerateComplexObject(2, 10),
            <= 1000 => faker.GenerateComplexObject(3, 20),
            _ => faker.GenerateComplexObject(4, 30)
        };
    }
}

/// <summary>
/// Tracks test execution time for performance testing
/// </summary>
public class TestPerformanceTracker : IDisposable
{
    private readonly System.Diagnostics.Stopwatch _stopwatch;
    private readonly string _operationName;
    private readonly long _expectedMaxMilliseconds;

    public TestPerformanceTracker(string operationName, long expectedMaxMilliseconds = 0)
    {
        _operationName = operationName;
        _expectedMaxMilliseconds = expectedMaxMilliseconds;
        _stopwatch = System.Diagnostics.Stopwatch.StartNew();
    }

    public void Dispose()
    {
        _stopwatch.Stop();
        var elapsed = _stopwatch.ElapsedMilliseconds;

        // Output performance info
        Console.WriteLine($"[PERF] {_operationName}: {elapsed}ms");

        // Assert if needed
        if (_expectedMaxMilliseconds > 0)
        {
            Assert.True(elapsed <= _expectedMaxMilliseconds,
                $"Operation '{_operationName}' took {elapsed}ms which exceeds the limit of {_expectedMaxMilliseconds}ms");
        }
    }
}