using System.Text.Json;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Utility.Formatting;
using Shouldly;

namespace AnotherJsonLib.Tests.LibTests;

public class JsonFormatterTests
{
    private const string PrettyJson = @"{
  ""person"": {
    ""name"": ""John Smith"",
    ""age"": 30,
    ""address"": {
      ""street"": ""123 Main St"",
      ""city"": ""New York"",
      ""zip"": ""10001""
    },
    ""phones"": [
      ""212-555-1234"",
      ""646-555-5678""
    ]
  }
}";

    private const string MinifiedJson = @"{""person"":{""name"":""John Smith"",""age"":30,""address"":{""street"":""123 Main St"",""city"":""New York"",""zip"":""10001""},""phones"":[""212-555-1234"",""646-555-5678""]}}";

    private readonly TestPerson _testPerson = new TestPerson
    {
        Name = "John Smith",
        Age = 30,
        IsActive = true,
        Scores = new[] { 95, 88, 72 }
    };

    private class TestPerson
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public int[] Scores { get; set; }
    }

    [Fact]
    public void Minify_WithPrettyJson_ReturnsMinifiedJson()
    {
        // Act
        string result = PrettyJson.Minify();

        // Assert
        result.IsMinified().ShouldBeTrue();
        result.ShouldBe(MinifiedJson);
    }


    [Fact]
    public void Minify_WithAlreadyMinifiedJson_ReturnsSameMinifiedJson()
    {
        // Act
        string result = MinifiedJson.Minify();

        // Assert
        result.ShouldBe(MinifiedJson);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Minify_WithNullOrWhitespace_ThrowsJsonFormattingException(string input)
    {
        // Act & Assert
        Should.Throw<JsonFormattingException>(() => input.Minify());
    }

    [Fact]
    public void Minify_WithInvalidJson_ThrowsJsonParsingException()
    {
        // Arrange
        string invalidJson = "{\"name\":\"John\", \"age\":}";

        // Act & Assert
        Should.Throw<JsonParsingException>(() => invalidJson.Minify());
    }

    [Fact]
    public void Prettify_WithObject_ReturnsPrettifiedJson()
    {
        // Act
        string result = _testPerson.Prettify();

        // Assert
        result.ShouldContain("\n");
        result.ShouldContain("  ");

        // Verify content by parsing the JSON
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var deserializedPerson = JsonSerializer.Deserialize<TestPerson>(result, options);

        deserializedPerson.ShouldNotBeNull();
        deserializedPerson.Name.ShouldBe(_testPerson.Name);
        deserializedPerson.Age.ShouldBe(_testPerson.Age);
        deserializedPerson.IsActive.ShouldBe(_testPerson.IsActive);
        deserializedPerson.Scores.ShouldBeEquivalentTo(_testPerson.Scores);
    }

    [Fact]
    public void Prettify_WithNull_ReturnsNullLiteral()
    {
        // Act
        string result = ((TestPerson)null).Prettify();

        // Assert
        result.ShouldBe("null");
    }

    [Fact]
    public void PrettifyJson_WithMinifiedJson_ReturnsPrettifiedJson()
    {
        // Act
        string result = MinifiedJson.PrettifyJson();

        // Assert
        result.ShouldContain("\n");
        result.ShouldContain("  ");

        // Normalize by removing whitespace and comparing
        string normalizedResult = result.Replace(" ", "").Replace("\n", "").Replace("\r", "");
        string normalizedExpected = MinifiedJson.Replace(" ", "");

        normalizedResult.ShouldBe(normalizedExpected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void PrettifyJson_WithNullOrWhitespace_ThrowsJsonArgumentException(string input)
    {
        // Act & Assert
        Should.Throw<JsonArgumentException>(() => input.PrettifyJson());
    }

    [Fact]
    public void PrettifyJson_WithInvalidJson_ThrowsJsonParsingException()
    {
        // Arrange
        string invalidJson = "{\"name\":\"John\", \"age\":}";

        // Act & Assert
        Should.Throw<JsonParsingException>(() => invalidJson.PrettifyJson());
    }

    [Fact]
    public void PrettifyJson_WithAlreadyPrettifiedJson_ReturnsSimilarResult()
    {
        // Act
        string result = PrettyJson.PrettifyJson();

        // Assert
        result.ShouldContain("\n");
        result.ShouldContain("  ");

        // Normalize by removing whitespace and comparing
        string normalizedResult = result.Replace(" ", "").Replace("\n", "").Replace("\r", "");
        string normalizedExpected = PrettyJson.Replace(" ", "").Replace("\n", "").Replace("\r", "");

        normalizedResult.ShouldBe(normalizedExpected);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(4, false)]
    [InlineData(8, false)]
    [InlineData(2, true)]
    public void PrettifyJsonWithIndentation_WithValidSettings_ReturnsPrettifiedJson(int indentSize, bool useTabs)
    {
        // Act
        string result = MinifiedJson.PrettifyJsonWithIndentation(indentSize, useTabs);

        // Assert
        result.ShouldContain("\n");

        if (useTabs)
        {
            result.ShouldContain("\t");
        }
        else
        {
            string expectedIndent = new string(' ', indentSize);
            result.ShouldContain(expectedIndent);
        }

        // Normalize by removing whitespace and comparing
        string normalizedResult = result.Replace(" ", "").Replace("\n", "").Replace("\r", "").Replace("\t", "");
        string normalizedExpected = MinifiedJson.Replace(" ", "");

        normalizedResult.ShouldBe(normalizedExpected);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(9)]
    public void PrettifyJsonWithIndentation_WithInvalidIndentSize_ThrowsJsonArgumentException(int indentSize)
    {
        // Act & Assert
        Should.Throw<JsonArgumentException>(() =>
            MinifiedJson.PrettifyJsonWithIndentation(indentSize));
    }

    [Fact]
    public void TryMinify_WithValidJson_ReturnsTrue()
    {
        // Act
        bool result = PrettyJson.TryMinify(out string minified);

        // Assert
        result.ShouldBeTrue();
        minified.ShouldNotBeEmpty();
        minified.ShouldBe(MinifiedJson);
    }

    [Fact]
    public void TryMinify_WithInvalidJson_ReturnsFalse()
    {
        // Arrange
        string invalidJson = "{\"name\":\"John\", \"age\":}";

        // Act
        bool result = invalidJson.TryMinify(out string minified);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void TryPrettify_WithValidObject_ReturnsTrue()
    {
        // Act
        bool result = _testPerson.TryPrettify(out string prettified);

        // Assert
        result.ShouldBeTrue();
        prettified.ShouldNotBeNull();
        prettified.ShouldContain("John Smith");
        prettified.ShouldContain("\n");
    }

    [Fact]
    public void TryPrettifyJson_WithValidJson_ReturnsTrue()
    {
        // Act
        bool result = MinifiedJson.TryPrettifyJson(out string prettified);

        // Assert
        result.ShouldBeTrue();
        prettified.ShouldNotBeEmpty();
        prettified.ShouldContain("\n");
    }

    [Fact]
    public void TryPrettifyJson_WithInvalidJson_ReturnsFalse()
    {
        // Arrange
        string invalidJson = "{\"name\":\"John\", \"age\":}";

        // Act
        bool result = invalidJson.TryPrettifyJson(out string prettified);

        // Assert
        result.ShouldBeFalse();
        prettified.ShouldBe(invalidJson); // Original is returned on error
    }


    [Fact]
    public void IsMinified_WithMinifiedJson_ReturnsTrue()
    {
        // Act
        bool result = MinifiedJson.IsMinified();

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsMinified_WithPrettifiedJson_ReturnsFalse()
    {
        // Act
        bool result = PrettyJson.IsMinified();

        // Assert
        result.ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void IsMinified_WithNullOrWhitespace_ThrowsJsonArgumentException(string input)
    {
        // Act & Assert
        Should.Throw<JsonArgumentException>(() => input.IsMinified());
    }

    [Fact]
    public void IsPrettified_WithPrettifiedJson_ReturnsTrue()
    {
        // Act
        bool result = PrettyJson.IsPrettified();

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsPrettified_WithMinifiedJson_ReturnsFalse()
    {
        // Act
        bool result = MinifiedJson.IsPrettified();

        // Assert
        result.ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void IsPrettified_WithNullOrWhitespace_ThrowsJsonArgumentException(string input)
    {
        // Act & Assert
        Should.Throw<JsonArgumentException>(() => input.IsPrettified());
    }
}