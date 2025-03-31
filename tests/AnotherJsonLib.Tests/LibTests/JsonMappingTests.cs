using System.Text.Json;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Utility.Operations;
using Shouldly;

namespace AnotherJsonLib.Tests.LibTests;

public class JsonMappingTests
{
    [Fact]
    public void MapProperties_ShouldRenameSimpleProperties()
    {
        // Arrange
        var json = "{\"firstName\":\"John\",\"lastName\":\"Doe\",\"age\":30}";
        var mapping = new Dictionary<string, string>
        {
            { "firstName", "givenName" },
            { "lastName", "familyName" }
        };

        // Act
        var result = json.MapProperties(mapping);

        // Assert
        var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);
        resultObj.ShouldContainKey("givenName");
        resultObj.ShouldContainKey("familyName");
        resultObj.ShouldContainKey("age");
        resultObj.ShouldNotContainKey("firstName");
        resultObj.ShouldNotContainKey("lastName");
    }

    [Fact]
    public void MapProperties_ShouldHandleNestedObjects()
    {
        // Arrange
        var json = "{\"person\":{\"firstName\":\"John\",\"address\":{\"city\":\"New York\"}}}";
        var mapping = new Dictionary<string, string>
        {
            { "firstName", "givenName" },
            { "city", "location" }
        };

        // Act
        var result = json.MapProperties(mapping);

        // Assert
        result.ShouldContain("\"givenName\":\"John\"");
        result.ShouldContain("\"location\":\"New York\"");
        result.ShouldNotContain("\"firstName\"");
        result.ShouldNotContain("\"city\"");
    }

    [Fact]
    public void MapProperties_ShouldHandleArrays()
    {
        // Arrange
        var json = "{\"people\":[{\"firstName\":\"John\"},{\"firstName\":\"Jane\"}]}";
        var mapping = new Dictionary<string, string>
        {
            { "firstName", "givenName" }
        };

        // Act
        var result = json.MapProperties(mapping);

        // Assert
        result.ShouldContain("\"givenName\":\"John\"");
        result.ShouldContain("\"givenName\":\"Jane\"");
        result.ShouldNotContain("\"firstName\"");
    }

    [Fact]
    public void MapProperties_ShouldHandleEmptyMapping()
    {
        // Arrange
        var json = "{\"firstName\":\"John\",\"lastName\":\"Doe\"}";
        var mapping = new Dictionary<string, string>();

        // Act
        var result = json.MapProperties(mapping);

        // Assert
        var expectedResult = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        var actualResult = JsonSerializer.Deserialize<Dictionary<string, object>>(result);

        actualResult.Keys.ShouldBe(expectedResult.Keys);
    }

    [Fact]
    public void MapProperties_ShouldThrowOnNullJson()
    {
        // Arrange
        string? json = null;
        var mapping = new Dictionary<string, string> { { "firstName", "givenName" } };

        // Act & Assert
        Should.Throw<JsonArgumentException>(() => json!.MapProperties(mapping));
    }

    [Fact]
    public void MapProperties_ShouldThrowOnWhitespaceJson()
    {
        // Arrange
        var json = "   ";
        var mapping = new Dictionary<string, string> { { "firstName", "givenName" } };

        // Act & Assert
        Should.Throw<JsonArgumentException>(() => json.MapProperties(mapping));
    }

    [Fact]
    public void MapProperties_ShouldThrowOnNullMapping()
    {
        // Arrange
        var json = "{\"firstName\":\"John\"}";
        Dictionary<string, string>? mapping = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => json.MapProperties(mapping!));
    }

    [Fact]
    public void MapProperties_ShouldThrowOnInvalidJson()
    {
        // Arrange
        var json = "{\"firstName\":\"John\""; // Missing closing brace
        var mapping = new Dictionary<string, string> { { "firstName", "givenName" } };

        // Act & Assert
        Should.Throw<JsonParsingException>(() => json.MapProperties(mapping));
    }

    [Fact]
    public void MapProperties_WithPreserveFormatting_ShouldMaintainIndentation()
    {
        // Arrange
        var json = "{\n  \"firstName\": \"John\",\n  \"lastName\": \"Doe\"\n}";
        var mapping = new Dictionary<string, string> { { "firstName", "givenName" } };

        // Act
        var result = json.MapProperties(mapping, true);

        // Assert
        result.ShouldContain("\n");
        result.ShouldContain("  \"");
    }

    [Fact]
    public void MapProperties_WithoutPreserveFormatting_ShouldRemoveIndentation()
    {
        // Arrange
        var json = "{\n  \"firstName\": \"John\",\n  \"lastName\": \"Doe\"\n}";
        var mapping = new Dictionary<string, string> { { "firstName", "givenName" } };

        // Act
        var result = json.MapProperties(mapping, false);

        // Assert
        result.ShouldNotContain("\n");
        result.ShouldNotContain("  \"");
        result.ShouldContain("\"givenName\"");
    }

    [Fact]
    public void TryMapProperties_ShouldReturnTrueAndOutputResultWhenSuccessful()
    {
        // Arrange
        var json = "{\"firstName\":\"John\",\"lastName\":\"Doe\"}";
        var mapping = new Dictionary<string, string> { { "firstName", "givenName" } };

        // Act
        var success = json.TryMapProperties(mapping, out var result);

        // Assert
        success.ShouldBeTrue();
        result.ShouldNotBeNull();
        result.ShouldContain("\"givenName\"");
        result.ShouldNotContain("\"firstName\"");
    }

    [Fact]
    public void TryMapProperties_ShouldReturnFalseWhenJsonIsNull()
    {
        // Arrange
        string? json = null;
        var mapping = new Dictionary<string, string> { { "firstName", "givenName" } };

        // Act
        var success = json!.TryMapProperties(mapping, out var result);

        // Assert
        success.ShouldBeFalse();
        result.ShouldBeNull();
    }

    [Fact]
    public void TryMapProperties_ShouldReturnFalseWhenJsonIsEmpty()
    {
        // Arrange
        var json = "";
        var mapping = new Dictionary<string, string> { { "firstName", "givenName" } };

        // Act
        var success = json.TryMapProperties(mapping, out var result);

        // Assert
        success.ShouldBeFalse();
        result.ShouldBeNull();
    }

    [Fact]
    public void TryMapProperties_ShouldReturnFalseWhenMappingIsEmpty()
    {
        // Arrange
        var json = "{\"firstName\":\"John\"}";
        var mapping = new Dictionary<string, string>();

        // Act
        var success = json.TryMapProperties(mapping, out var result);

        // Assert
        success.ShouldBeFalse();
        result.ShouldBeNull();
    }

    [Fact]
    public void TryMapProperties_ShouldReturnFalseWhenJsonIsInvalid()
    {
        // Arrange
        var json = "{\"firstName\":\"John\""; // Missing closing brace
        var mapping = new Dictionary<string, string> { { "firstName", "givenName" } };

        // Act
        var success = json.TryMapProperties(mapping, out var result);

        // Assert
        success.ShouldBeFalse();
        result.ShouldBeNull();
    }

    [Fact]
    public void FilterProperties_ShouldKeepOnlySpecifiedProperties()
    {
        // Arrange
        var json = "{\"firstName\":\"John\",\"lastName\":\"Doe\",\"age\":30}";
        var propertiesToInclude = new[] { "firstName", "age" };

        // Act
        var result = json.FilterProperties(propertiesToInclude);

        // Assert
        var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(result);
        resultObj.ShouldContainKey("firstName");
        resultObj.ShouldContainKey("age");
        resultObj.ShouldNotContainKey("lastName");
    }

    [Fact]
    public void FilterProperties_ShouldHandleNestedObjects()
    {
        // Arrange
        var json = "{\"person\":{\"firstName\":\"John\",\"lastName\":\"Doe\",\"address\":{\"city\":\"New York\",\"zip\":\"10001\"}}}";
        var propertiesToInclude = new[] { "person", "firstName", "city" };

        // Act
        var result = json.FilterProperties(propertiesToInclude);

        // Assert
        result.ShouldContain("\"person\"");
        result.ShouldContain("\"firstName\":\"John\"");
        result.ShouldContain("\"city\":\"New York\"");
        result.ShouldNotContain("\"lastName\"");
        result.ShouldNotContain("\"zip\"");
    }

    [Fact]
    public void FilterProperties_ShouldHandleArrays()
    {
        // Arrange
        var json = "{\"people\":[{\"firstName\":\"John\",\"lastName\":\"Doe\"},{\"firstName\":\"Jane\",\"lastName\":\"Smith\"}]}";
        var propertiesToInclude = new[] { "people", "firstName" };

        // Act
        var result = json.FilterProperties(propertiesToInclude);

        // Assert
        result.ShouldContain("\"people\"");
        result.ShouldContain("\"firstName\":\"John\"");
        result.ShouldContain("\"firstName\":\"Jane\"");
        result.ShouldNotContain("\"lastName\"");
    }

    [Fact]
    public void FilterProperties_ShouldReturnEmptyObjectWhenNoPropertiesSpecified()
    {
        // Arrange
        var json = "{\"firstName\":\"John\",\"lastName\":\"Doe\"}";
        var propertiesToInclude = Array.Empty<string>();

        // Act
        var result = json.FilterProperties(propertiesToInclude);

        // Assert
        result.ShouldBe("{}");
    }

    [Fact]
    public void FilterProperties_ShouldThrowOnNullJson()
    {
        // Arrange
        string? json = null;
        var propertiesToInclude = new[] { "firstName" };

        // Act & Assert
        Should.Throw<JsonArgumentException>(() => json!.FilterProperties(propertiesToInclude));
    }

    [Fact]
    public void FilterProperties_ShouldThrowOnNullPropertiesToInclude()
    {
        // Arrange
        var json = "{\"firstName\":\"John\"}";
        IEnumerable<string>? propertiesToInclude = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => json.FilterProperties(propertiesToInclude!));
    }

    [Fact]
    public void FilterProperties_ShouldThrowOnInvalidJson()
    {
        // Arrange
        var json = "{\"firstName\":\"John\""; // Missing closing brace
        var propertiesToInclude = new[] { "firstName" };

        // Act & Assert
        Should.Throw<JsonParsingException>(() => json.FilterProperties(propertiesToInclude));
    }

    [Fact]
    public void MapProperties_ShouldHandleAllJsonValueTypes()
    {
        // Arrange
        var json = @"{
                ""stringProp"": ""text"",
                ""numberProp"": 42,
                ""boolProp"": true,
                ""nullProp"": null,
                ""arrayProp"": [1, 2, 3],
                ""objectProp"": {""nested"": ""value""}
            }";

        var mapping = new Dictionary<string, string>
        {
            { "stringProp", "text" },
            { "numberProp", "num" },
            { "boolProp", "flag" },
            { "nullProp", "empty" },
            { "arrayProp", "items" },
            { "objectProp", "obj" },
            { "nested", "prop" }
        };

        // Act
        var result = json.MapProperties(mapping);

        // Assert
        var resultObj = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(result);
        resultObj.ShouldContainKey("text");
        resultObj.ShouldContainKey("num");
        resultObj.ShouldContainKey("flag");
        resultObj.ShouldContainKey("empty");
        resultObj.ShouldContainKey("items");
        resultObj.ShouldContainKey("obj");

        resultObj["obj"].ValueKind.ShouldBe(JsonValueKind.Object);
        var nestedObj = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(resultObj["obj"].ToString());
        nestedObj.ShouldContainKey("prop");
    }

    [Fact]
    public void MapProperties_ShouldHandleDeepNestedStructures()
    {
        // Arrange
        var json = @"{
                ""level1"": {
                    ""level2"": {
                        ""level3"": {
                            ""deepProp"": ""value""
                        }
                    }
                }
            }";

        var mapping = new Dictionary<string, string>
        {
            { "level1", "l1" },
            { "level2", "l2" },
            { "level3", "l3" },
            { "deepProp", "prop" }
        };

        // Act
        var result = json.MapProperties(mapping);

        // Assert
        result.ShouldContain("\"l1\"");
        result.ShouldContain("\"l2\"");
        result.ShouldContain("\"l3\"");
        result.ShouldContain("\"prop\":\"value\"");
    }

    [Fact]
    public void MapProperties_ShouldHandleEmptyObjects()
    {
        // Arrange
        var json = "{\"emptyObj\":{},\"prop\":\"value\"}";
        var mapping = new Dictionary<string, string>
        {
            { "emptyObj", "empty" },
            { "prop", "property" }
        };

        // Act
        var result = json.MapProperties(mapping);

        // Assert
        result.ShouldContain("\"empty\":{}");
        result.ShouldContain("\"property\":\"value\"");
    }

    [Fact]
    public void MapProperties_ShouldHandleEmptyArrays()
    {
        // Arrange
        var json = "{\"emptyArray\":[],\"prop\":\"value\"}";
        var mapping = new Dictionary<string, string>
        {
            { "emptyArray", "items" },
            { "prop", "property" }
        };

        // Act
        var result = json.MapProperties(mapping);

        // Assert
        result.ShouldContain("\"items\":[]");
        result.ShouldContain("\"property\":\"value\"");
    }
}