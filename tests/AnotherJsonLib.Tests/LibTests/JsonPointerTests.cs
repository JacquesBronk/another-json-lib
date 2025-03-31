using System.Text.Json;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Utility;
using AnotherJsonLib.Utility.Operations;
using Shouldly;

namespace AnotherJsonLib.Tests.LibTests;

public class JsonPointerTests
{
    [Fact]
    public void EvaluatePointer_EmptyString_ReturnsRoot()
    {
        // Arrange
        string json = "{\"foo\": \"bar\", \"baz\": 123}";
        using var doc = JsonDocument.Parse(json);

        // Act: an empty pointer should return the entire root element.
        var result = doc.EvaluatePointer("");

        // Assert: the result should be equivalent to the root.
        result.ShouldNotBeNull();
        result.Value.ToJson().ShouldBe(doc.RootElement.ToJson());
    }

    [Fact]
    public void EvaluatePointer_NestedProperty_ReturnsValue()
    {
        // Arrange
        string json = "{\"person\": {\"name\": \"Alice\", \"age\": 30}}";
        using var doc = JsonDocument.Parse(json);

        // Act: evaluate pointer to retrieve the nested "name" property.
        var result = doc.EvaluatePointer("/person/name");

        // Assert: the value should be "Alice".
        result.ShouldNotBeNull();
        result.Value.GetString().ShouldBe("Alice");
    }

    [Fact]
    public void EvaluatePointer_ArrayIndex_ReturnsElement()
    {
        // Arrange
        string json = "{\"items\": [10, 20, 30]}";
        using var doc = JsonDocument.Parse(json);

        // Act: evaluate pointer to access the element at index 1.
        var result = doc.EvaluatePointer("/items/1");

        // Assert: the value should be 20.
        result.ShouldNotBeNull();
        result.Value.GetInt32().ShouldBe(20);
    }

    [Fact]
    public void EvaluatePointer_SpecialCharacters_ReturnsCorrectValue()
    {
        // Arrange: JSON object with keys containing "/" and "~"
        string json = "{\"a/b\": \"slash\", \"m~n\": \"tilde\"}";
        using var doc = JsonDocument.Parse(json);

        // Act & Assert:
        // "/a~1b" should return "slash"
        var resultSlash = doc.EvaluatePointer("/a~1b");
        resultSlash.ShouldNotBeNull();
        resultSlash.Value.GetString().ShouldBe("slash");

        // "/m~0n" should return "tilde"
        var resultTilde = doc.EvaluatePointer("/m~0n");
        resultTilde.ShouldNotBeNull();
        resultTilde.Value.GetString().ShouldBe("tilde");
    }

    [Fact]
    public void EvaluatePointer_NonexistentProperty_ReturnsNull()
    {
        // Arrange
        string json = "{\"foo\": \"bar\"}";
        using var doc = JsonDocument.Parse(json);

        // Act & Assert: evaluating a pointer for a missing property should return null.
        var result = doc.EvaluatePointer("/nonexistent");
        result.ShouldBeNull();
    }

    [Fact]
    public void EvaluatePointer_InvalidArrayIndex_ReturnsNull()
    {
        // Arrange
        string json = "{\"items\": [1, 2, 3]}";
        using var doc = JsonDocument.Parse(json);

        // Act & Assert: accessing an out-of-range array index should return null.
        var result = doc.EvaluatePointer("/items/10");
        result.ShouldBeNull();
    }

    [Fact]
    public void EvaluatePointer_CannotTraversePrimitive_ReturnsNull()
    {
        // Arrange
        string json = "{\"foo\": \"bar\"}";
        using var doc = JsonDocument.Parse(json);

        // Act & Assert: trying to traverse past a primitive should return null.
        var result = doc.EvaluatePointer("/foo/anything");
        result.ShouldBeNull();
    }

    [Fact]
    public void EvaluatePointer_InvalidPointerFormat_ShouldReturnBeNull()
    {
        // Arrange
        string json = "{\"foo\": \"bar\"}";
        using var doc = JsonDocument.Parse(json);

        // Act & Assert: a pointer that does not start with "/" should throw a JsonPointerException.
       doc.EvaluatePointer("invalidPointer").ShouldBeNull();
    }


    [Fact]
    public void TryEvaluatePointer_ValidPointer_ReturnsTrue()
    {
        // Arrange
        string json = "{\"foo\": \"bar\", \"numbers\": [1,2,3]}";
        using var doc = JsonDocument.Parse(json);

        // Act
        bool success = doc.TryEvaluatePointer("/numbers/0", out JsonElement? result);

        // Assert
        success.ShouldBeTrue();
        result.ShouldNotBeNull();
        result.Value.GetInt32().ShouldBe(1);
    }

    [Fact]
    public void TryEvaluatePointer_InvalidPointer_ReturnsFalse()
    {
        // Arrange
        string json = "{\"foo\": \"bar\"}";
        using var doc = JsonDocument.Parse(json);

        // Act
        bool success = doc.TryEvaluatePointer("/foo/extra", out JsonElement? result);

        // Assert
        success.ShouldBeFalse();
        result.ShouldBeNull();
    }

    [Fact]
    public void TryEvaluatePointer_NullDocument_ReturnsFalse()
    {
        // Arrange
        JsonDocument? doc = null;

        // Act
        bool success = doc.TryEvaluatePointer("/any", out JsonElement? result);

        // Assert
        success.ShouldBeFalse();
        result.ShouldBeNull();
    }

    [Fact]
    public void TryEvaluatePointer_NullPointer_ReturnsFalse()
    {
        // Arrange
        string json = "{\"foo\": \"bar\"}";
        using var doc = JsonDocument.Parse(json);

        // Act
        bool success = doc.TryEvaluatePointer(null!, out JsonElement? result);

        // Assert
        success.ShouldBeFalse();
        result.ShouldBeNull();
    }

    // ----- Create and Append Tests -----

    [Fact]
    public void CreatePointer_FromSegments_ReturnsProperPointer()
    {
        // Arrange & Act
        string pointer = JsonPointer.Create("users", "0", "user/name");

        // Assert: Check that special characters are escaped properly.
        pointer.ShouldBe("/users/0/user~1name");
    }

    [Fact]
    public void CreatePointer_NullSegments_ThrowsException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => JsonPointer.Create(null!));
    }

    [Fact]
    public void AppendPointer_AppendsSegmentCorrectly()
    {
        // Arrange
        string basePointer = "/users/0";

        // Act
        string appended = JsonPointer.Append(basePointer, "user/name");

        // Assert
        appended.ShouldBe("/users/0/user~1name");
    }

    [Fact]
    public void AppendPointer_ToEmptyPointer_ReturnsProperPointer()
    {
        // Arrange
        string basePointer = "";

        // Act
        string appended = JsonPointer.Append(basePointer, "data");

        // Assert
        appended.ShouldBe("/data");
    }

    [Fact]
    public void AppendPointer_NullInput_ThrowsException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => JsonPointer.Append(null!, "segment"));
        Should.Throw<ArgumentNullException>(() => JsonPointer.Append("/base", null!));
    }
}