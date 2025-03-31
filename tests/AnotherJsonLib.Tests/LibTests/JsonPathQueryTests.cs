using System.Text.Json;
using AnotherJsonLib.Utility.Operations;
using Shouldly;

namespace AnotherJsonLib.Tests.LibTests;

public class JsonPathQueryTests
{
    private readonly string _testJson = @"{
        ""store"": {
            ""books"": [
                {
                    ""category"": ""fiction"",
                    ""title"": ""The Night Dragon"",
                    ""price"": 19.99,
                    ""available"": true
                },
                {
                    ""category"": ""fiction"",
                    ""title"": ""Sword of Destiny"",
                    ""price"": 15.99,
                    ""available"": false
                },
                {
                    ""category"": ""non-fiction"",
                    ""title"": ""The History of Computing"",
                    ""price"": 29.99,
                    ""available"": true
                }
            ],
            ""bicycle"": {
                ""color"": ""red"",
                ""price"": 199.99
            },
            ""location"": {
                ""city"": ""New York"",
                ""zipcode"": ""10001""
            }
        },
        ""expensive"": 20,
        ""specialChars"": {
            ""key.with.dots"": ""value"",
            ""empty"": null
        }
    }";

    private JsonDocument GetTestDocument() => JsonDocument.Parse(_testJson);

    [Fact]
    public void QueryJsonElement_RootPath_ReturnsEntireDocument()
    {
        // Arrange
        using var doc = GetTestDocument();

        // Act
        var results = doc.QueryJsonElement("$").ToList();

        // Assert
        results.Count.ShouldBe(1);
        results[0].ShouldNotBeNull();
        results[0].Value.ValueKind.ShouldBe(JsonValueKind.Object);
    }

    [Fact]
    public void QueryJsonElement_SimpleProperty_ReturnsCorrectValue()
    {
        // Arrange
        using var doc = GetTestDocument();

        // Act
        var results = doc.QueryJsonElement("$.expensive").ToList();

        // Assert
        results.Count.ShouldBe(1);
        results[0].ShouldNotBeNull();
        results[0].Value.GetInt32().ShouldBe(20);
    }

    [Fact]
    public void QueryJsonElement_NestedProperty_ReturnsCorrectValue()
    {
        // Arrange
        using var doc = GetTestDocument();

        // Act
        var results = doc.QueryJsonElement("$.store.bicycle.color").ToList();

        // Assert
        results.Count.ShouldBe(1);
        results[0].ShouldNotBeNull();
        results[0].Value.GetString().ShouldBe("red");
    }

    [Fact]
    public void QueryJsonElement_ArrayIndex_ReturnsCorrectElement()
    {
        // Arrange
        using var doc = GetTestDocument();

        // Act
        var results = doc.QueryJsonElement("$.store.books[1].title").ToList();

        // Assert
        results.Count.ShouldBe(1);
        results[0].ShouldNotBeNull();
        results[0].Value.GetString().ShouldBe("Sword of Destiny");
    }

    [Fact]
    public void QueryJsonElement_MultipleArrayIndices_ReturnsMultipleElements()
    {
        // Arrange
        using var doc = GetTestDocument();

        // Act
        var results = doc.QueryJsonElement("$.store.books[0,2].title").ToList();

        // Assert
        results.Count.ShouldBe(2);
        results[0].ShouldNotBeNull();
        results[1].ShouldNotBeNull();
        results[0].Value.GetString().ShouldBe("The Night Dragon");
        results[1].Value.GetString().ShouldBe("The History of Computing");
    }

    [Fact]
    public void QueryJsonElement_Wildcard_ReturnsAllMatchingElements()
    {
        // Arrange
        using var doc = GetTestDocument();

        // Act
        var results = doc.QueryJsonElement("$.store.books[*].title").ToList();

        // Assert
        results.Count.ShouldBe(3);
        var titles = results.Select(r => r?.GetString()).ToList();
        titles.ShouldContain("The Night Dragon");
        titles.ShouldContain("Sword of Destiny");
        titles.ShouldContain("The History of Computing");
    }

    [Fact]
    public void QueryJsonElement_RecursiveDescent_FindsAllMatches()
    {
        // Arrange
        using var doc = GetTestDocument();

        // Act
        var results = doc.QueryJsonElement("$##.price").ToList();

        // Assert
        results.Count.ShouldBe(4); // 3 books + 1 bicycle
        var prices = results.Select(r => r?.GetDecimal()).ToList();
        prices.ShouldContain(19.99m);
        prices.ShouldContain(15.99m);
        prices.ShouldContain(29.99m);
        prices.ShouldContain(199.99m);
    }

    [Fact]
    public void QueryJsonElement_NonExistentPath_ReturnsEmptyCollection()
    {
        // Arrange
        using var doc = GetTestDocument();

        // Act
        var results = doc.QueryJsonElement("$.nonexistent.path").ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public void QueryJson_StringInput_ReturnsCorrectResults()
    {
        // Act
        var results = JsonPathQuery.QueryJson(_testJson, "$.store.location.city").ToList();

        // Assert
        results.Count.ShouldBe(1);
        results[0].ShouldNotBeNull();
        results[0].Value.GetString().ShouldBe("New York");
    }

    [Fact]
    public void TryQueryJson_ValidInput_ReturnsTrue()
    {
        // Act
        bool success = JsonPathQuery.TryQueryJson(
            _testJson, 
            "$.store.bicycle.price", 
            out var results);
        var resultsList = results.ToList();

        // Assert
        success.ShouldBeTrue();
        resultsList.Count.ShouldBe(1);
        resultsList[0].ShouldNotBeNull();
        resultsList[0].Value.GetDecimal().ShouldBe(199.99m);
    }

    [Fact]
    public void TryQueryJson_InvalidPath_ReturnsFalseWithEmptyCollection()
    {
        // Act
        bool success = JsonPathQuery.TryQueryJson(
            _testJson, 
            "this is not a valid path", 
            out var results);
        
        // Assert
        success.ShouldBeFalse();
        results.ShouldBeEmpty();
    }

    [Fact]
    public void CacheManagement_ConfigureCacheAndClear_WorksAsExpected()
    {
        // Configure cache
        JsonPathQuery.ConfigureCache(maxCacheSize: 100, cacheExpiration: TimeSpan.FromMinutes(5));
        
        // Use cache by querying
        var results = JsonPathQuery.QueryJson(_testJson, "$.store.books[*].title").ToList();
        results.Count.ShouldBe(3);
        
        // Clear cache
        JsonPathQuery.ClearCache();
        
        // Should still work after clearing
        results = JsonPathQuery.QueryJson(_testJson, "$.store.books[*].price").ToList();
        results.Count.ShouldBe(3);
    }

    [Fact]
    public void NullElement_HandledGracefully_ReturnsNullAsElement()
    {
        // Arrange
        using var doc = GetTestDocument();

        // Act
        var results = doc.QueryJsonElement("$.specialChars.empty").ToList();

        // Assert
        results.Count.ShouldBe(1);
        results[0].ShouldNotBeNull(); // The JsonElement? wrapper isn't null
        results[0].Value.ValueKind.ShouldBe(JsonValueKind.Null); // But it contains a null JSON value
    }

    [Fact]
    public void QueryJsonElement_ComplexQuery_ReturnsExpectedResults()
    {
        // Arrange
        using var doc = GetTestDocument();
        
        // Act: Find all available books
        var results = doc.QueryJsonElement("$.store.books[*]").Where(el => 
            el.HasValue && 
            el.Value.TryGetProperty("available", out var available) && 
            available.GetBoolean()
        ).ToList();
        
        // Assert
        results.Count.ShouldBe(2); // Two books are marked as available
        
        var titles = results
            .Select(el => el.Value.GetProperty("title").GetString())
            .ToList();
            
        titles.ShouldContain("The Night Dragon");
        titles.ShouldContain("The History of Computing");
    }
}