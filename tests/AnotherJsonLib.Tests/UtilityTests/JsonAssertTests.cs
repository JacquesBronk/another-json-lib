using System.Text.Json.Nodes;
using AnotherJsonLib.Tests.Utility;

namespace AnotherJsonLib.Tests.UtilityTests;

public class JsonAssertTests
{
   
    [Fact]
    public void Equal_NullValues_DoesNotThrow()
    {
        // Arrange
        JsonNode expected = null;
        JsonNode actual = null;

        // Act & Assert
        JsonAssert.Equal(expected, actual);
    }

    [Fact]
    public void Equal_StringValues_MatchingStrings_DoesNotThrow()
    {
        // Arrange
        var expected = JsonNode.Parse("\"test string\"");
        var actual = JsonNode.Parse("\"test string\"");

        // Act & Assert
        JsonAssert.Equal(expected, actual);
    }

    [Fact]
    public void Equal_StringValues_NonMatchingStrings_Throws()
    {
        // Arrange
        var expected = JsonNode.Parse("\"test string\"");
        var actual = JsonNode.Parse("\"different string\"");

        // Act & Assert
        Assert.Throws<Xunit.Sdk.EqualException>(() => JsonAssert.Equal(expected, actual));
    }

    [Fact]
    public void Equal_BooleanValues_MatchingBooleans_DoesNotThrow()
    {
        // Arrange
        var expected = JsonNode.Parse("true");
        var actual = JsonNode.Parse("true");

        // Act & Assert
        JsonAssert.Equal(expected, actual);
    }

    [Fact]
    public void Equal_BooleanValues_NonMatchingBooleans_Throws()
    {
        // Arrange
        var expected = JsonNode.Parse("true");
        var actual = JsonNode.Parse("false");

        // Act & Assert
        Assert.Throws<Xunit.Sdk.EqualException>(() => JsonAssert.Equal(expected, actual));
    }

    [Fact]
    public void Equal_NullJsonValues_DoesNotThrow()
    {
        // Arrange
        var expected = JsonNode.Parse("null");
        var actual = JsonNode.Parse("null");

        // Act & Assert
        JsonAssert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("42", "42")]         // Integer
    [InlineData("42.5", "42.5")]     // Decimal
    public void Equal_NumberValues_MatchingNumbers_DoesNotThrow(string expectedJson, string actualJson)
    {
        // Arrange
        var expected = JsonNode.Parse(expectedJson);
        var actual = JsonNode.Parse(actualJson);

        // Act & Assert
        JsonAssert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("42", "43")]
    [InlineData("42.5", "42.6")]
    public void Equal_NumberValues_NonMatchingNumbers_Throws(string expectedJson, string actualJson)
    {
        // Arrange
        var expected = JsonNode.Parse(expectedJson);
        var actual = JsonNode.Parse(actualJson);

        // Act & Assert
        Assert.Throws<Xunit.Sdk.EqualException>(() => JsonAssert.Equal(expected, actual));
    }

    [Fact]
    public void Equal_DifferentValueTypes_Throws()
    {
        // Arrange
        var expected = JsonNode.Parse("\"string value\"");
        var actual = JsonNode.Parse("42");

        // Act & Assert
        Assert.Throws<Xunit.Sdk.EqualException>(() => JsonAssert.Equal(expected, actual));
    }

    [Fact]
    public void Equal_EmptyObjects_DoesNotThrow()
    {
        // Arrange
        var expected = JsonNode.Parse("{}");
        var actual = JsonNode.Parse("{}");

        // Act & Assert
        JsonAssert.Equal(expected, actual);
    }

    [Fact]
    public void Equal_SimpleObjects_SameContent_DoesNotThrow()
    {
        // Arrange
        var expected = JsonNode.Parse("{\"name\":\"John\",\"age\":30}");
        var actual = JsonNode.Parse("{\"name\":\"John\",\"age\":30}");

        // Act & Assert
        JsonAssert.Equal(expected, actual);
    }

    [Fact]
    public void Equal_SimpleObjects_DifferentContent_Throws()
    {
        // Arrange
        var expected = JsonNode.Parse("{\"name\":\"John\",\"age\":30}");
        var actual = JsonNode.Parse("{\"name\":\"Jane\",\"age\":30}");

        // Act & Assert
        Assert.Throws<Xunit.Sdk.EqualException>(() => JsonAssert.Equal(expected, actual));
    }

    [Fact]
    public void Equal_SimpleObjects_MissingProperty_Throws()
    {
        // Arrange
        var expected = JsonNode.Parse("{\"name\":\"John\",\"age\":30}");
        var actual = JsonNode.Parse("{\"name\":\"John\"}");

        // Act & Assert
        Assert.Throws<Xunit.Sdk.EqualException>(() => JsonAssert.Equal(expected, actual));
    }

    [Fact]
    public void Equal_SimpleObjects_DifferentOrder_IgnoreOrderTrue_DoesNotThrow()
    {
        // Arrange
        var expected = JsonNode.Parse("{\"name\":\"John\",\"age\":30}");
        var actual = JsonNode.Parse("{\"age\":30,\"name\":\"John\"}");

        // Act & Assert
        JsonAssert.Equal(expected, actual, ignoreObjectOrder: true);
    }

    [Fact]
    public void Equal_NestedObjects_SameContent_DoesNotThrow()
    {
        // Arrange
        var expected = JsonNode.Parse("{\"person\":{\"name\":\"John\",\"age\":30}}");
        var actual = JsonNode.Parse("{\"person\":{\"name\":\"John\",\"age\":30}}");

        // Act & Assert
        JsonAssert.Equal(expected, actual);
    }

    [Fact]
    public void Equal_NestedObjects_DifferentContent_Throws()
    {
        // Arrange
        var expected = JsonNode.Parse("{\"person\":{\"name\":\"John\",\"age\":30}}");
        var actual = JsonNode.Parse("{\"person\":{\"name\":\"Jane\",\"age\":30}}");

        // Act & Assert
        Assert.Throws<Xunit.Sdk.EqualException>(() => JsonAssert.Equal(expected, actual));
    }


    [Fact]
    public void Equal_EmptyArrays_DoesNotThrow()
    {
        // Arrange
        var expected = JsonNode.Parse("[]");
        var actual = JsonNode.Parse("[]");

        // Act & Assert
        JsonAssert.Equal(expected, actual);
    }

    [Fact]
    public void Equal_SimpleArrays_SameContent_DoesNotThrow()
    {
        // Arrange
        var expected = JsonNode.Parse("[1,2,3]");
        var actual = JsonNode.Parse("[1,2,3]");

        // Act & Assert
        JsonAssert.Equal(expected, actual);
    }

    [Fact]
    public void Equal_SimpleArrays_DifferentContent_Throws()
    {
        // Arrange
        var expected = JsonNode.Parse("[1,2,3]");
        var actual = JsonNode.Parse("[1,2,4]");

        // Act & Assert
        Assert.Throws<Xunit.Sdk.EqualException>(() => JsonAssert.Equal(expected, actual));
    }

    [Fact]
    public void Equal_SimpleArrays_DifferentOrder_IgnoreArrayOrderFalse_Throws()
    {
        // Arrange
        var expected = JsonNode.Parse("[1,2,3]");
        var actual = JsonNode.Parse("[3,2,1]");

        // Act & Assert
        Assert.Throws<Xunit.Sdk.EqualException>(() => JsonAssert.Equal(expected, actual, ignoreArrayOrder: false));
    }

    [Fact]
    public void Equal_SimpleArrays_DifferentOrder_IgnoreArrayOrderTrue_DoesNotThrow()
    {
        // Arrange
        var expected = JsonNode.Parse("[1,2,3]");
        var actual = JsonNode.Parse("[3,2,1]");

        // Act & Assert
        JsonAssert.Equal(expected, actual, ignoreArrayOrder: true);
    }

    [Fact]
    public void Equal_ObjectArrays_SameContent_DoesNotThrow()
    {
        // Arrange
        var expected = JsonNode.Parse("[{\"name\":\"John\"},{\"name\":\"Jane\"}]");
        var actual = JsonNode.Parse("[{\"name\":\"John\"},{\"name\":\"Jane\"}]");

        // Act & Assert
        JsonAssert.Equal(expected, actual);
    }

    [Fact]
    public void Equal_ObjectArrays_DifferentOrder_IgnoreArrayOrderTrue_DoesNotThrow()
    {
        // Arrange
        var expected = JsonNode.Parse("[{\"name\":\"John\"},{\"name\":\"Jane\"}]");
        var actual = JsonNode.Parse("[{\"name\":\"Jane\"},{\"name\":\"John\"}]");

        // Act & Assert
        JsonAssert.Equal(expected, actual, ignoreArrayOrder: true);
    }

    [Fact]
    public void Equal_ObjectArrays_DifferentLength_Throws()
    {
        // Arrange
        var expected = JsonNode.Parse("[{\"name\":\"John\"},{\"name\":\"Jane\"}]");
        var actual = JsonNode.Parse("[{\"name\":\"John\"}]");

        // Act & Assert
        Assert.Throws<Xunit.Sdk.EqualException>(() => JsonAssert.Equal(expected, actual));
    }


    [Fact]
    public void Equal_ComplexStructure_SameContent_DoesNotThrow()
    {
        // Arrange
        var expectedJson = @"{
            ""name"": ""John"",
            ""age"": 30,
            ""isActive"": true,
            ""address"": {
                ""street"": ""Main St"",
                ""city"": ""New York""
            },
            ""hobbies"": [""reading"", ""cycling"", ""swimming""],
            ""contacts"": [
                {
                    ""type"": ""email"",
                    ""value"": ""john@example.com""
                },
                {
                    ""type"": ""phone"",
                    ""value"": ""+1234567890""
                }
            ]
        }";
        
        var actualJson = @"{
            ""name"": ""John"",
            ""age"": 30,
            ""isActive"": true,
            ""address"": {
                ""street"": ""Main St"",
                ""city"": ""New York""
            },
            ""hobbies"": [""reading"", ""cycling"", ""swimming""],
            ""contacts"": [
                {
                    ""type"": ""email"",
                    ""value"": ""john@example.com""
                },
                {
                    ""type"": ""phone"",
                    ""value"": ""+1234567890""
                }
            ]
        }";

        var expected = JsonNode.Parse(expectedJson);
        var actual = JsonNode.Parse(actualJson);

        // Act & Assert
        JsonAssert.Equal(expected, actual);
    }

    [Fact]
    public void Equal_ComplexStructure_DifferentArrayOrders_IgnoreArrayOrderTrue_DoesNotThrow()
    {
        // Arrange
        var expectedJson = @"{
            ""name"": ""John"",
            ""hobbies"": [""reading"", ""cycling"", ""swimming""],
            ""contacts"": [
                {
                    ""type"": ""email"",
                    ""value"": ""john@example.com""
                },
                {
                    ""type"": ""phone"",
                    ""value"": ""+1234567890""
                }
            ]
        }";
        
        var actualJson = @"{
            ""name"": ""John"",
            ""hobbies"": [""swimming"", ""reading"", ""cycling""],
            ""contacts"": [
                {
                    ""type"": ""phone"",
                    ""value"": ""+1234567890""
                },
                {
                    ""type"": ""email"",
                    ""value"": ""john@example.com""
                }
            ]
        }";

        var expected = JsonNode.Parse(expectedJson);
        var actual = JsonNode.Parse(actualJson);

        // Act & Assert
        JsonAssert.Equal(expected, actual, ignoreArrayOrder: true);
    }

}