using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Utility;
using Shouldly;

namespace AnotherJsonLib.Tests.LibTests;

public class SerializationTests
{
    private class TestObject
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FromJson_WithEmptyOrWhitespace_ThrowsJsonArgumentException(string input)
    {
        // Act & Assert
        Should.Throw<JsonArgumentException>(() => input.FromJson<TestObject>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TryFromJson_WithEmptyOrWhitespace_ReturnsFalse(string input)
    {
        // Act
        bool success = input.TryFromJson<TestObject>(out var result);

        // Assert
        success.ShouldBeFalse();
        result.ShouldBeNull();
    }

    [Fact]
    public void FromJson_WithValidJson_DeserializesCorrectly()
    {
        // Arrange
        string json = "{\"Name\":\"Test\",\"Value\":42}";

        // Act
        var result = json.FromJson<TestObject>();

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Test");
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void TryFromJson_WithValidJson_ReturnsTrueAndDeserializes()
    {
        // Arrange
        string json = "{\"Name\":\"Test\",\"Value\":42}";

        // Act
        bool success = json.TryFromJson<TestObject>(out var result);

        // Assert
        success.ShouldBeTrue();
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Test");
        result.Value.ShouldBe(42);
    }
}
