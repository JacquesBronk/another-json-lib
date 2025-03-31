using System.Text;
using System.Text.Json;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Utility.Operations;
using Shouldly;

namespace AnotherJsonLib.Tests.LibTests;

public class JsonStreamerTests : IDisposable
{
    private readonly string _tempJsonFilePath;
    private readonly string _validJsonContent = @"{""name"":""test"",""values"":[1,2,3],""nested"":{""key"":""value""}}";
    private readonly string _invalidJsonContent = @"{""name"":""test"",""values"":[1,2,3";

    public JsonStreamerTests()
    {
        // Create temporary file for testing
        _tempJsonFilePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.json");
        File.WriteAllText(_tempJsonFilePath, _validJsonContent);
    }

    public void Dispose()
    {
        if (File.Exists(_tempJsonFilePath))
        {
            File.Delete(_tempJsonFilePath);
        }
    }

    [Fact]
    public void StreamJsonFile_WithValidFile_ShouldProcessAllTokens()
    {
        // Arrange
        var tokens = new List<(JsonTokenType Type, string? Value)>();

        // Act
        _tempJsonFilePath.StreamJsonFile((type, value) => tokens.Add((type, value)));

        // Assert
        tokens.Count.ShouldBeGreaterThan(0);
        tokens.ShouldContain(t => t.Type == JsonTokenType.PropertyName && t.Value == "name");
        tokens.ShouldContain(t => t.Type == JsonTokenType.String && t.Value == "test");
        tokens.ShouldContain(t => t.Type == JsonTokenType.StartArray);
        tokens.ShouldContain(t => t.Type == JsonTokenType.EndArray);
        tokens.ShouldContain(t => t.Type == JsonTokenType.StartObject);
        tokens.ShouldContain(t => t.Type == JsonTokenType.EndObject);
    }

    [Fact]
    public void StreamJsonFile_WithNullFilePath_ShouldThrowJsonArgumentException()
    {
        // Arrange
        string? nullFilePath = null;
        Action action = () => nullFilePath!.StreamJsonFile((_, _) => { });

        // Act & Assert
        Should.Throw<JsonOperationException>(action);
    }

    [Fact]
    public void StreamJsonFile_WithEmptyFilePath_ShouldThrowJsonOperationException()
    {
        // Arrange
        string emptyFilePath = " ";
        Action action = () => emptyFilePath.StreamJsonFile((_, _) => { });

        // Act & Assert
        Should.Throw<JsonOperationException>(action);
    }

    [Fact]
    public void StreamJsonFile_WithNullCallback_ShouldThrowJsonArgumentException()
    {
        // Arrange
        Action<JsonTokenType, string?> nullCallback = null!;
        Action action = () => _tempJsonFilePath.StreamJsonFile(nullCallback);

        // Act & Assert
        Should.Throw<JsonArgumentException>(action);
    }

    [Fact]
    public void StreamJsonFile_WithNonExistentFile_ShouldThrowJsonLibException()
    {
        // Arrange
        string nonExistentFile = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.json");
        Action action = () => nonExistentFile.StreamJsonFile((_, _) => { });

        // Act & Assert
        var exception = Should.Throw<JsonLibException>(action);
        exception.Message.ShouldContain("not found");
    }

    [Fact]
    public void StreamJsonFile_WithCallbackThatThrows_JsonOperationException()
    {
        // Arrange
        Action action = () => _tempJsonFilePath.StreamJsonFile((_, _) => throw new InvalidOperationException("Test exception"));

        // Act & Assert
        Should.Throw<JsonOperationException>(action);
    }


    [Fact]
    public void StreamJson_WithValidStream_ShouldProcessAllTokens()
    {
        // Arrange
        var tokens = new List<(JsonTokenType Type, string? Value)>();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(_validJsonContent));

        // Act
        stream.StreamJson((type, value) => tokens.Add((type, value)));

        // Assert
        tokens.Count.ShouldBeGreaterThan(0);
        tokens.ShouldContain(t => t.Type == JsonTokenType.PropertyName && t.Value == "name");
        tokens.ShouldContain(t => t.Type == JsonTokenType.String && t.Value == "test");
        tokens.ShouldContain(t => t.Type == JsonTokenType.StartArray);
        tokens.ShouldContain(t => t.Type == JsonTokenType.EndArray);
    }

    [Fact]
    public void StreamJson_WithNullStream_ShouldThrowJsonArgumentException()
    {
        // Arrange
        Stream? nullStream = null;
        Action action = () => nullStream!.StreamJson((_, _) => { });

        // Act & Assert
        Should.Throw<JsonArgumentException>(action);
    }

    [Fact]
    public void StreamJson_WithNullCallback_ShouldThrowJsonArgumentException()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(_validJsonContent));
        Action<JsonTokenType, string?> nullCallback = null!;
        Action action = () => stream.StreamJson(nullCallback);

        // Act & Assert
        Should.Throw<JsonArgumentException>(action);
    }

    [Fact]
    public void StreamJson_WithNonReadableStream_ShouldThrowArgumentException()
    {
        // Arrange
        using var stream = new MemoryStream();
        stream.Close(); // Makes the stream not readable
        Action action = () => stream.StreamJson((_, _) => { });

        // Act & Assert
        Should.Throw<JsonArgumentException>(action);
    }

    [Fact]
    public void StreamJson_WithInvalidJson_ShouldThrowJsonOperationException()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(_invalidJsonContent));
        Action action = () => stream.StreamJson((_, _) => { });

        // Act & Assert
        Should.Throw<JsonOperationException>(action);
    }

    [Fact]
    public void StreamJson_WithLargeJson_ShouldHandleBufferBoundaries()
    {
        // Arrange
        // Create a JSON string that will cross buffer boundaries
        var largeObj = new StringBuilder("{");
        for (int i = 0; i < 1000; i++)
        {
            largeObj.Append($"\"prop{i}\":\"value{i}\",");
        }

        largeObj.Append("\"last\":\"end\"}");

        var tokens = new List<(JsonTokenType Type, string? Value)>();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(largeObj.ToString()));

        // Act
        stream.StreamJson((type, value) => tokens.Add((type, value)));

        // Assert
        tokens.Count.ShouldBeGreaterThan(2000); // At least 1000 property names + 1000 values
        tokens.ShouldContain(t => t.Type == JsonTokenType.PropertyName && t.Value == "last");
        tokens.ShouldContain(t => t.Type == JsonTokenType.String && t.Value == "end");
    }

    [Fact]
    public void TryStreamJsonFile_WithValidFile_ShouldReturnTrue()
    {
        // Arrange
        var tokens = new List<(JsonTokenType Type, string? Value)>();

        // Act
        bool result = _tempJsonFilePath.TryStreamJsonFile((type, value) => tokens.Add((type, value)));

        // Assert
        result.ShouldBeTrue();
        tokens.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void TryStreamJsonFile_WithNonExistentFile_ShouldReturnFalse()
    {
        // Arrange
        string nonExistentFile = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.json");
        var tokens = new List<(JsonTokenType Type, string? Value)>();

        // Act
        bool result = nonExistentFile.TryStreamJsonFile((type, value) => tokens.Add((type, value)));

        // Assert
        result.ShouldBeFalse();
        tokens.Count.ShouldBe(0);
    }

    [Fact]
    public void TryStreamJsonFile_WithCallbackThatThrows_ShouldReturnFalse()
    {
        // Arrange
        bool result = false;

        // Act
        Action action = () => result = _tempJsonFilePath.TryStreamJsonFile((_, _) => throw new InvalidOperationException("Test exception"));

        // Assert
        Should.NotThrow(action);
        result.ShouldBeFalse();
    }

    [Fact]
    public void TryStreamJson_WithValidStream_ShouldReturnTrue()
    {
        // Arrange
        var tokens = new List<(JsonTokenType Type, string? Value)>();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(_validJsonContent));

        // Act
        bool result = stream.TryStreamJson((type, value) => tokens.Add((type, value)));

        // Assert
        result.ShouldBeTrue();
        tokens.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void TryStreamJson_WithInvalidJson_ShouldReturnFalse()
    {
        // Arrange
        var tokens = new List<(JsonTokenType Type, string? Value)>();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(_invalidJsonContent));

        // Act
        bool result = stream.TryStreamJson((type, value) => tokens.Add((type, value)));

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void TryStreamJson_WithNullStream_ShouldReturnFalse()
    {
        // Arrange
        Stream? nullStream = null;
        var tokens = new List<(JsonTokenType Type, string? Value)>();

        // Act
        bool result = nullStream!.TryStreamJson((type, value) => tokens.Add((type, value)));

        // Assert
        result.ShouldBeFalse();
        tokens.Count.ShouldBe(0);
    }

    [Fact]
    public void TryStreamJson_WithCallbackThatThrows_ShouldReturnFalse()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(_validJsonContent));
        bool result = false;

        // Act
        Action action = () => result = stream.TryStreamJson((_, _) => throw new InvalidOperationException("Test exception"));

        // Assert
        Should.NotThrow(action);
        result.ShouldBeFalse();
    }

    [Fact]
    public void StreamJson_WithAllTokenTypes_ShouldProcessCorrectly()
    {
        // Arrange
        string complexJson = @"{
                ""string"": ""text"",
                ""number"": 42,
                ""decimal"": 3.14,
                ""boolean"": true,
                ""null"": null,
                ""array"": [1, ""text"", false, null],
                ""nested"": {
                    ""key"": ""value""
                }
            }";

        var tokens = new List<(JsonTokenType Type, string? Value)>();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(complexJson));

        // Act
        stream.StreamJson((type, value) => tokens.Add((type, value)));

        // Assert
        tokens.ShouldContain(t => t.Type == JsonTokenType.StartObject);
        tokens.ShouldContain(t => t.Type == JsonTokenType.PropertyName && t.Value == "string");
        tokens.ShouldContain(t => t.Type == JsonTokenType.String && t.Value == "text");
        tokens.ShouldContain(t => t.Type == JsonTokenType.PropertyName && t.Value == "number");
        tokens.ShouldContain(t => t.Type == JsonTokenType.Number);
        tokens.ShouldContain(t => t.Type == JsonTokenType.PropertyName && t.Value == "boolean");
        tokens.ShouldContain(t => t.Type == JsonTokenType.True);
        tokens.ShouldContain(t => t.Type == JsonTokenType.PropertyName && t.Value == "null");
        tokens.ShouldContain(t => t.Type == JsonTokenType.Null);
        tokens.ShouldContain(t => t.Type == JsonTokenType.PropertyName && t.Value == "array");
        tokens.ShouldContain(t => t.Type == JsonTokenType.StartArray);
        tokens.ShouldContain(t => t.Type == JsonTokenType.EndArray);
        tokens.ShouldContain(t => t.Type == JsonTokenType.EndObject);
    }
}