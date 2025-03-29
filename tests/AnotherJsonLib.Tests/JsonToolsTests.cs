using System.Text;
using System.Text.Json;
using Shouldly;
using AnotherJsonLib.Utility;

namespace AnotherJsonLib.Tests;

public class JsonToolsTests
{
    [Fact]
    public void AreEqual_NullAndEmptyJsonStrings_ShouldReturnFalse()
    {
        // Arrange
        string json1 = null!;
        string json2 = "{}";

        // Act
        bool result = json1.AreEqual(json2);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void AreEqual_CaseSensitiveEqualJsonStrings_ShouldReturnTrue()
    {
        // Arrange
        string json1 = "{\"Name\":\"John\",\"Age\":30}";
        string json2 = "{\"Name\":\"John\",\"Age\":30}";

        // Act
        bool result = json1.AreEqual(json2);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void AreEqual_CaseSensitiveDifferentJsonStrings_ShouldReturnFalse()
    {
        // Arrange
        string json1 = "{\"Name\":\"John\",\"Age\":30}";
        string json2 = "{\"Name\":\"Jane\",\"Age\":25}";

        // Act
        bool result = json1.AreEqual(json2);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void AreEqual_CaseInsensitiveEqualJsonStrings_ShouldReturnTrue()
    {
        // Arrange
        string json1 = "{\"Name\":\"John\",\"Age\":30}";
        string json2 = "{\"name\":\"John\",\"age\":30}";

        // Act
        bool result = json1.AreEqual(json2, ignoreCase: true);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void AreEqual_CaseInsensitiveDifferentJsonStrings_ShouldReturnFalse()
    {
        // Arrange
        string json1 = "{\"Name\":\"John\",\"Age\":30}";
        string json2 = "{\"name\":\"Jane\",\"age\":25}";

        // Act
        bool result = json1.AreEqual(json2, ignoreCase: true);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void AreEqual_WhitespaceInsensitiveEqualJsonStrings_ShouldReturnTrue()
    {
        // Arrange
        string json1 = "{\"Name\":\"John\",\"Age\":30}";
        string json2 = "{\"Name\":\"John\",\"Age\":30}";

        // Act
        bool result = json1.AreEqual(json2, ignoreWhitespace: true);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void AreEqual_WhitespaceInsensitiveDifferentJsonStrings_ShouldReturnTrue()
    {
        // Arrange
        string json1 = "{\"Name\":\" John \",\"Age\":30}";
        string json2 = "{\"Name\":\"John\",\"Age\":30}";

        // Act
        bool result = json1.AreEqual(json2, ignoreWhitespace: true);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void FindDifferences_ShouldReturnEmptyDictionary_WhenJsonsAreEqual()
    {
        // Arrange
        string json1 = "{\"Name\":\"John\",\"Age\":30}";
        string json2 = "{\"Name\":\"John\",\"Age\":30}";

        // Act
        var differences = json1.FindDifferences(json2);

        // Assert
        differences.ShouldBeEmpty();
    }

    [Fact]
    public void FindDifferences_ShouldReturnNewKeyValuePair_WhenJson2ContainsNewKey()
    {
        // Arrange
        string json1 = "{\"Name\":\"John\",\"Age\":30}";
        string json2 = "{\"Name\":\"John\",\"Age\":30,\"City\":\"New York\"}";

        // Act
        var differences = json1.FindDifferences(json2);

        // Assert
        differences.ShouldContainKey("City");
        differences["City"].ShouldBe("New York");
    }

    [Fact]
    public void FindDifferences_ShouldReturnAllDifferences_WhenJson2ContainsDifferentValueForExistingKey()
    {
        // Arrange
        string json1 = "{\"Name\":\"John\",\"Age\":30}";
        string json2 = "{\"Name\":\"Jane\",\"Age\":25}";

        // Act
        var differences = json1.FindDifferences(json2);

        // Assert
        differences.ShouldBe(new Dictionary<string, object>
        {
            {"Name", "Jane"},
            {"Age", 25}
        });
    }

    [Fact]
    public void FindDifferences_ShouldReturnEmptyDictionary_WhenJsonsAreEmpty()
    {
        // Arrange
        string json1 = "{}";
        string json2 = "{}";

        // Act
        var differences = json1.FindDifferences(json2);

        // Assert
        differences.ShouldBeEmpty();
    }

    [Fact]
    public void FindDifferences_ShouldReturnNewKeyValuePair_WhenJson2ContainsNewKeyAndValueIsNull()
    {
        // Arrange
        string json1 = "{\"Name\":\"John\"}";
        string json2 = "{\"Name\":\"John\",\"Age\":null}";

        // Act
        var differences = json1.FindDifferences(json2);

        // Assert
        differences.ShouldContainKey("Age");
        differences["Age"].ShouldBe(null);
    }

    [Fact]
    public void QueryJsonElement_ShouldReturnMatchingElement_WhenJsonPathHasDescendantsOperator()
    {
        // Arrange
        var json = "{\"Name\":\"John\",\"Address\":{\"City\":\"New York\",\"Zip\":\"12345\"}}";
        var jsonDocument = JsonDocument.Parse(json);
        var jsonPath = "$##.City";

        // Act
        var results = jsonDocument.QueryJsonElement(jsonPath).ToList();

        // Assert
        results.Count.ShouldBe(1);
        results.Single()?.GetString().ShouldBe("New York");
    }

    [Fact]
    public void QueryJsonElement_ShouldReturnMatchingElement_WhenJsonPathHasPropertyWithIndex()
    {
        // Arrange
        var json = "{\"Items\":[{\"Name\":\"Item1\"},{\"Name\":\"Item2\"}]}";
        var jsonPath = "$.Items[1].Name";
        var jsonDocument = JsonDocument.Parse(json);
        
        // Act
        var result = jsonDocument.QueryJsonElement(jsonPath).ToList();

        // Assert
        result.Count.ShouldBe(1);
        result.Single()?.GetProperty("Name").ToString().ShouldBe("Item2");
    }

    [Fact]
    public void QueryJsonElement_ShouldReturnMatchingElement_WhenJsonPathIsSingleProperty()
    {
        // Arrange
        var json = "{\"Name\":\"John\",\"Age\":30}";
        var jsonPath = "$.Name";
        var jsonDocument = JsonDocument.Parse(json);
        
        // Act
        var results = jsonDocument.QueryJsonElement(jsonPath).ToList();

        // Assert
        results.Count.ShouldBe(1);
        results.Single()?.GetString().ShouldBe("John");
    }

    [Fact]
    public void QueryJsonElement_ShouldReturnMatchingElements_WhenJsonPathHasMultipleIndexes()
    {
        // Arrange
        var json = "{\"Numbers\":[1,2,3,4,5]}";
        var jsonPath = "$.Numbers[0,2,4]";
        var jsonDocument = JsonDocument.Parse(json);
        
        // Act
        var result = jsonDocument.QueryJsonElement(jsonPath).ToList();

        // Assert
        result.Count.ShouldBe(3);
        result.Select(e => e!.Value.GetInt32())
              .ShouldBe(new List<int> { 1, 3, 5 });
    }

    [Theory]
    [InlineData("{\"name\":\"John\",\"age\":30}", "{\"age\":40}", "{\"name\":\"John\",\"age\":40}")]
    [InlineData("{\"name\":\"John\",\"age\":30,\"city\":\"New York\"}", "{\"age\":40}", "{\"name\":\"John\",\"age\":40,\"city\":\"New York\"}")]
    [InlineData("{\"name\":\"John\",\"age\":30}", "{\"name\":\"Doe\",\"city\":\"New York\"}", "{\"name\":\"Doe\",\"age\":30,\"city\":\"New York\"}")]
    [InlineData("{\"names\":[\"John\",\"Alice\"],\"ages\":[30,25]}", "{\"names\":[\"Bob\"],\"ages\":[35,28,22]}", "{\"names\":[\"John\",\"Alice\",\"Bob\"],\"ages\":[30,25,35,28,22]}")]
    public void Merge_ShouldMergeJsonStrings(string originalJson, string patchJson, string expectedJson)
    {
        // Act
        var mergedJson = originalJson.Merge(patchJson);

        // Assert
        mergedJson.ShouldBe(expectedJson);
    }

    [Fact]
    public void Merge_ShouldThrowArgumentNullException_WhenOriginalJsonIsNull()
    {
        // Arrange
        string originalJson = null!;
        string patchJson = "{\"age\":40}";

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => originalJson.Merge(patchJson));
    }

    [Fact]
    public void Merge_ShouldThrowArgumentNullException_WhenPatchJsonIsNull()
    {
        // Arrange
        string originalJson = "{\"name\":\"John\",\"age\":30}";
        string patchJson = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => originalJson.Merge(patchJson));
    }

    [Theory]
    [InlineData("{\"name\":\"John\",\"age\":30}", "{\"name\":\"John\",\"age\":30}")]
    [InlineData("  {   \"name\"   :  \"John\"   ,   \"age\"   :  30   }   ", "{\"name\":\"John\",\"age\":30}")]
    [InlineData("{\"name\":\"John\",\n\"age\":30}", "{\"name\":\"John\",\"age\":30}")]
    [InlineData("{\"person\":{\"name\":\"John\",\"age\":30}}", "{\"person\":{\"name\":\"John\",\"age\":30}}")]
    [InlineData("{}", "{}")]
    public void Minify_ShouldMinifyJsonString(string originalJson, string expectedJson)
    {
        // Act
        var minifiedJson = originalJson.Minify();

        // Assert
        minifiedJson.ShouldBe(expectedJson);
    }

    [Fact]
    public void Minify_ShouldThrowJsonException_WhenInvalidJson()
    {
        // Arrange
        string invalidJson = "{name\":\"John\",\"age\":30}";

        // Act & Assert
        var exception = Should.Throw<JsonException>(() => invalidJson.Minify());
        exception.Message.ShouldContain("An error occurred while minifying the JSON.");
    }

    [Fact]
    public void Minify_ShouldThrowJsonException_WhenJsonIsNull()
    {
        // Arrange
        string json = null!;

        // Act & Assert
        Should.Throw<JsonException>(() => json.Minify());
    }

    [Fact]
    public void EvaluatePointer_WithValidPointer_ReturnsExpectedValue()
    {
        // Arrange
        string json = @"{
                ""name"": ""John"",
                ""age"": 30,
                ""city"": ""New York"",
                ""scores"": [10, 20, 30]
            }";
        using var doc = JsonDocument.Parse(json);

        // Act
        var result = doc.EvaluatePointer("/name");

        // Assert
        result.HasValue.ShouldBeTrue();
        result!.Value.GetString().ShouldBe("John");
    }

    [Fact]
    public void EvaluatePointer_WithValidArrayIndex_ReturnsExpectedValue()
    {
        // Arrange
        string json = @"{
                ""scores"": [10, 20, 30]
            }";
        using var doc = JsonDocument.Parse(json);

        // Act
        var result = doc.EvaluatePointer("/scores/1");

        // Assert
        result.HasValue.ShouldBeTrue();
        result!.Value.GetInt32().ShouldBe(20);
    }

    [Fact]
    public void EvaluatePointer_WithInvalidArrayIndex_ReturnsNull()
    {
        // Arrange
        string json = @"{
                ""scores"": [10, 20, 30]
            }";
        using var doc = JsonDocument.Parse(json);

        // Act
        var result = doc.EvaluatePointer("/scores/3");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void EvaluatePointer_WithValidNestedObject_ReturnsExpectedValue()
    {
        // Arrange
        string json = @"{
                ""person"": {
                    ""name"": ""Alice"",
                    ""age"": 25
                }
            }";
        using var doc = JsonDocument.Parse(json);

        // Act
        var result = doc.EvaluatePointer("/person/age");

        // Assert
        result.HasValue.ShouldBeTrue();
        result!.Value.GetInt32().ShouldBe(25);
    }

    [Fact]
    public void EvaluatePointer_WithInvalidPath_ReturnsNull()
    {
        // Arrange
        string json = @"{
                ""name"": ""John"",
                ""age"": 30
            }";
        using var doc = JsonDocument.Parse(json);

        // Act
        var result = doc.EvaluatePointer("/city");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void EvaluatePointer_WithEmptyPointer_ReturnsNull()
    {
        // Arrange
        string json = @"{
                ""name"": ""John"",
                ""age"": 30
            }";
        using var doc = JsonDocument.Parse(json);

        // Act
        var result = doc.EvaluatePointer("");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void EvaluatePointer_WithNullDocument_ReturnsNull()
    {
        // Act
        var result = JsonTools.EvaluatePointer(null, "/name");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void EvaluatePointer_WithNullPointer_ReturnsNull()
    {
        // Arrange
        string json = @"{
                ""name"": ""John"",
                ""age"": 30
            }";
        using var doc = JsonDocument.Parse(json);

        // Act
        var result = doc.EvaluatePointer(null!);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Prettify_WithValidData_ReturnsFormattedJson()
    {
        // Arrange
        var data = new
        {
            Name = "John",
            Age = 30,
            Address = new
            {
                Street = "123 Main St",
                City = "New York"
            }
        };

        // Act
        var prettifiedJson = data.Prettify();

        // Assert
        var expectedJson = @"{
  ""Name"": ""John"",
  ""Age"": 30,
  ""Address"": {
    ""Street"": ""123 Main St"",
    ""City"": ""New York""
  }
}";
        prettifiedJson.ShouldBe(expectedJson);
    }

    [Fact]
    public void Prettify_WithNullData_ReturnsEmptyString()
    {
        // Arrange
        object data = null!;

        // Act
        var prettifiedJson = data.Prettify();

        // Assert
        prettifiedJson.ShouldBe("null"); // JSON representation of null
    }

    [Fact]
    public void StreamJsonFile_ShouldExecuteCallbackForEachTokenInFile()
    {
        // Arrange
        string jsonFilePath = "largeFile.json";
        string jsonContent = "{\"name\":\"John\",\"age\":30}";
        File.WriteAllText(jsonFilePath, jsonContent);

        var expectedTokens = new[]
        {
            JsonTokenType.StartObject, JsonTokenType.PropertyName, JsonTokenType.String,
            JsonTokenType.PropertyName, JsonTokenType.Number, JsonTokenType.EndObject
        };
        var actualTokens = new List<JsonTokenType>();
        void Callback(JsonTokenType tokenType, string? _) => actualTokens.Add(tokenType);

        // Act
        jsonFilePath.StreamJsonFile(Callback);

        // Assert
        actualTokens.ShouldBe(expectedTokens);
        
        // Cleanup
        File.Delete(jsonFilePath);
    }

    [Fact]
    public void StreamJson_ShouldExecuteCallbackForEachTokenInStream()
    {
        // Arrange
        var jsonString = "{\"name\":\"John\",\"age\":30}";
        var jsonBytes = Encoding.UTF8.GetBytes(jsonString);
        using var jsonStream = new MemoryStream(jsonBytes);
        var expectedTokens = new[]
        {
            JsonTokenType.StartObject, JsonTokenType.PropertyName, JsonTokenType.String,
            JsonTokenType.PropertyName, JsonTokenType.Number, JsonTokenType.EndObject
        };
        var actualTokens = new List<JsonTokenType>();
        Action<JsonTokenType, string> callback = (tokenType, _) => actualTokens.Add(tokenType);

        // Act
        jsonStream.StreamJson(callback!);

        // Assert
        actualTokens.ShouldBe(expectedTokens);
    }

    [Theory]
    [InlineData("abc", "abc", StringComparison.Ordinal, true)]
    [InlineData("abc", "ABC", StringComparison.Ordinal, false)]
    [InlineData("abc", "ABC", StringComparison.OrdinalIgnoreCase, true)]
    public void Equals_ShouldCompareStrings(string str1, string str2, StringComparison comparison, bool expectedResult)
    {
        // Arrange
        var comparer = new StringComparisonEqualityComparer(comparison);

        // Act
        var result = comparer.Equals(str1, str2);

        // Assert
        result.ShouldBe(expectedResult);
    }

    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(1, 2, false)]
    public void Equals_ShouldCompareBytes(byte byte1, byte byte2, bool expectedResult)
    {
        // Arrange
        var comparer = new StringComparisonEqualityComparer(StringComparison.Ordinal);

        // Act
        var result = comparer.Equals(byte1, byte2);

        // Assert
        result.ShouldBe(expectedResult);
    }

    [Theory]
    [InlineData(new byte[] {1, 2, 3}, new byte[] {1, 2, 3}, StringComparison.Ordinal, true)]
    [InlineData(new byte[] {1, 2, 3}, new byte[] {3, 2, 1}, StringComparison.Ordinal, false)]
    [InlineData(new byte[] {1, 2, 3}, new byte[] {3, 2, 1}, StringComparison.OrdinalIgnoreCase, false)]
    public void Equals_ShouldCompareByteArrays(byte[] bytes1, byte[] bytes2, StringComparison comparison,
        bool expectedResult)
    {
        // Arrange
        var comparer = new StringComparisonEqualityComparer(comparison);

        // Act
        var result = comparer.Equals(bytes1, bytes2);

        // Assert
        result.ShouldBe(expectedResult);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("def")]
    public void GetHashCode_ShouldReturnSameHashCodeForEqualStrings(string str)
    {
        // Arrange
        var comparer = new StringComparisonEqualityComparer(StringComparison.Ordinal);

        // Act
        var hashCode1 = comparer.GetHashCode(str);
        var hashCode2 = comparer.GetHashCode(str);

        // Assert
        hashCode1.ShouldBe(hashCode2);
    }

    [Theory]
    [InlineData(new byte[] {1, 2, 3})]
    [InlineData(new byte[] {4, 5, 6})]
    public void GetHashCode_ShouldReturnNonZeroHashCodeForByteArrays(byte[] bytes)
    {
        // Arrange
        var comparer = new StringComparisonEqualityComparer(StringComparison.Ordinal);

        // Act
        var hashCode = comparer.GetHashCode(bytes);

        // Assert
        hashCode.ShouldNotBe(0);
    }

    [Theory]
    [InlineData("null", "null", true)]
    [InlineData("true", "true", true)]
    [InlineData("false", "false", true)]
    [InlineData("42.0", "42", true)]
    [InlineData("\"foo\"", "\"foo\"", true)]
    [InlineData("[]", "[]", true)]
    [InlineData("[1, 2, 3]", "[3, 2, 1]", false)]
    [InlineData("{}", "{}", true)]
    [InlineData("{\"a\": 1, \"b\": 2}", "{\"b\": 2, \"a\": 1}", true)]
    [InlineData("null", "true", false)]
    [InlineData("42.0", "42.5", false)]
    [InlineData("\"foo\"", "\"bar\"", false)]
    [InlineData("[]", "{}", false)]
    [InlineData("[1, 2, 3]", "[1, 2]", false)]
    [InlineData("{\"a\": 1, \"b\": 2}", "{\"a\": 1, \"c\": 2}", false)]
    public void Equals_ShouldCompareJsonElements(string json1, string json2, bool expected)
    {
        // Arrange
        JsonElementComparer comparer = new JsonElementComparer();
        var element1 = JsonDocument.Parse(json1).RootElement;
        var element2 = JsonDocument.Parse(json2).RootElement;

        // Act
        var result = comparer.Equals(element1, element2);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void GetHashCode_ShouldReturnSameHashCodeForEqualJsonElements()
    {
        // Arrange
        JsonElementComparer comparer = new JsonElementComparer();
        var element1 = JsonDocument.Parse("{\"a\": 1, \"b\": 2}").RootElement;
        var element2 = JsonDocument.Parse("{\"b\": 2, \"a\": 1}").RootElement;

        // Act
        var hashCode1 = comparer.GetHashCode(element1);
        var hashCode2 = comparer.GetHashCode(element2);

        // Assert
        hashCode1.ShouldBe(hashCode2);
    }

    [Fact]
    public void GetHashCode_ShouldReturnDifferentHashCodeForDifferentJsonElements()
    {
        // Arrange
        JsonElementComparer comparer = new JsonElementComparer();
        var element1 = JsonDocument.Parse("{\"a\": 1, \"b\": 2}").RootElement;
        var element2 = JsonDocument.Parse("{\"a\": 1, \"c\": 2}").RootElement;

        // Act
        var hashCode1 = comparer.GetHashCode(element1);
        var hashCode2 = comparer.GetHashCode(element2);

        // Assert
        hashCode1.ShouldNotBe(hashCode2);
    }
}
