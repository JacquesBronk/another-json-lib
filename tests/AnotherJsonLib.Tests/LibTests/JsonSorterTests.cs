using System.Text.Json;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Utility.Operations;
using Shouldly;

namespace AnotherJsonLib.Tests.LibTests;

public class JsonSorterTests
    {
        [Fact]
        public void SortJson_ShouldSortPropertiesLexicographically()
        {
            // Arrange
            string unsortedJson = "{\"c\":3,\"a\":1,\"b\":2}";
            
            // Act
            string result = JsonSorter.SortJson(unsortedJson);
            
            // Assert
            result.ShouldBe("{\"a\":1,\"b\":2,\"c\":3}");
        }
        
        [Fact]
        public void SortJson_ShouldSortNestedPropertiesRecursively()
        {
            // Arrange
            string unsortedJson = "{\"c\":3,\"a\":1,\"b\":{\"z\":26,\"y\":25,\"x\":24}}";
            
            // Act
            string result = JsonSorter.SortJson(unsortedJson);
            
            // Assert
            result.ShouldBe("{\"a\":1,\"b\":{\"x\":24,\"y\":25,\"z\":26},\"c\":3}");
        }
        
        [Fact]
        public void SortJson_WithIndentation_ShouldReturnIndentedJson()
        {
            // Arrange
            string unsortedJson = "{\"c\":3,\"a\":1,\"b\":2}";
            
            // Act
            string result = JsonSorter.SortJson(unsortedJson, true);
            
            // Assert
            result.ShouldContain("\n");
            result.ShouldContain("  ");
            
            // Parse and check structure is still correct
            var parsed = JsonSerializer.Deserialize<Dictionary<string, int>>(result);
            parsed.ShouldNotBeNull();
            parsed["a"].ShouldBe(1);
            parsed["b"].ShouldBe(2);
            parsed["c"].ShouldBe(3);
        }
        
        [Fact]
        public void SortJson_WithInvalidJson_ShouldThrowJsonParsingException()
        {
            // Arrange
            string invalidJson = "{\"a\":1,\"b\":2,}"; // Invalid trailing comma
            
            // Act & Assert
            Should.Throw<JsonParsingException>(() => JsonSorter.SortJson(invalidJson));
        }
        
        [Fact]
        public void SortJson_WithNullInput_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Should.Throw<JsonArgumentException>(() => JsonSorter.SortJson(null!));
        }
        
        [Fact]
        public void SortJsonWithComparer_ShouldSortUsingCustomComparer()
        {
            // Arrange
            string unsortedJson = "{\"Z\":3,\"a\":1,\"B\":2}";
            IComparer<string> caseInsensitiveComparer = StringComparer.OrdinalIgnoreCase;
            
            // Act
            string result = JsonSorter.SortJsonWithComparer(unsortedJson, caseInsensitiveComparer);
            
            // Assert
            // With case-insensitive comparison, we expect alphabetical order regardless of case
            result.ShouldBe("{\"a\":1,\"B\":2,\"Z\":3}");
        }
        
        [Fact]
        public void SortJsonWithComparer_WithNullComparer_ShouldThrowArgumentNullException()
        {
            // Arrange
            string json = "{\"a\":1}";
            
            // Act & Assert
            Should.Throw<ArgumentNullException>(() => JsonSorter.SortJsonWithComparer(json, null!));
        }
        
        [Fact]
        public void SortJsonDeep_ShouldSortObjectPropertiesAndArrayElements()
        {
            // Arrange
            string unsortedJson = @"{
                ""people"": [
                    {""name"": ""Zack"", ""id"": 3},
                    {""name"": ""Alice"", ""id"": 1},
                    {""name"": ""Bob"", ""id"": 2}
                ],
                ""version"": 1,
                ""created"": ""2023-01-01""
            }";
            
            // Act
            string result = JsonSorter.SortJsonDeep(unsortedJson, "name");
            
            // Assert
            // The result should have properties sorted AND people array sorted by name
            string expected = "{\"created\":\"2023-01-01\",\"people\":[{\"id\":1,\"name\":\"Alice\"},{\"id\":2,\"name\":\"Bob\"},{\"id\":3,\"name\":\"Zack\"}],\"version\":1}";
            result.ShouldBe(expected);
        }
        
        [Fact]
        public void SortJsonDeep_WithMissingArraySortProperty_ShouldNotSortArray()
        {
            // Arrange
            string unsortedJson = @"{
                ""people"": [
                    {""name"": ""Zack"", ""id"": 3},
                    {""name"": ""Alice"", ""id"": 1},
                    {""name"": ""Bob"", ""id"": 2}
                ],
                ""version"": 1
            }";
            
            // Act
            string result = JsonSorter.SortJsonDeep(unsortedJson, "non_existent_property");
            
            // Assert
            // Properties should be sorted but array elements should remain in original order
            // since the sort property doesn't exist
            string expected = "{\"people\":[{\"id\":3,\"name\":\"Zack\"},{\"id\":1,\"name\":\"Alice\"},{\"id\":2,\"name\":\"Bob\"}],\"version\":1}";
            result.ShouldBe(expected);
        }
        
        [Fact]
        public void SortJsonShallow_ShouldSortOnlyTopLevelProperties()
        {
            // Arrange
            string unsortedJson = @"{
                ""z"": [3, 2, 1],
                ""a"": {""c"": 3, ""b"": 2, ""a"": 1},
                ""m"": 13
            }";
            
            // Act
            string result = JsonSorter.SortJsonShallow(unsortedJson);
            
            // Assert
            // Only top-level properties should be sorted
            string expected = "{\"a\":{\"c\":3,\"b\":2,\"a\":1},\"m\":13,\"z\":[3,2,1]}";
            result.ShouldBe(expected);
        }
        
        [Fact]
        public void SortJsonShallow_WithNonObjectRoot_ShouldReturnOriginalJson()
        {
            // Arrange
            string arrayJson = "[1, 2, 3]";
            
            // Act
            string result = JsonSorter.SortJsonShallow(arrayJson);
            
            // Assert
            // Since root is not an object, original should be returned
            result.ShouldBe("[1,2,3]"); // Note: Whitespace may be normalized
        }
        
        [Fact]
        public void TrySortJson_WithValidJson_ShouldReturnTrueAndSortedJson()
        {
            // Arrange
            string unsortedJson = "{\"c\":3,\"a\":1,\"b\":2}";
            
            // Act
            bool success = JsonSorter.TrySortJson(unsortedJson, out string result);
            
            // Assert
            success.ShouldBeTrue();
            result.ShouldBe("{\"a\":1,\"b\":2,\"c\":3}");
        }
        
        [Fact]
        public void TrySortJson_WithInvalidJson_ShouldReturnFalseAndOriginalJson()
        {
            // Arrange
            string invalidJson = "{\"a\":1,\"b\":2,}"; // Invalid trailing comma
            
            // Act
            bool success = JsonSorter.TrySortJson(invalidJson, out string result);
            
            // Assert
            success.ShouldBeFalse();
            result.ShouldBe(invalidJson);
        }
        
        [Fact]
        public void TrySortJsonDeep_WithValidJson_ShouldReturnTrueAndDeeplySortedJson()
        {
            // Arrange
            string unsortedJson = @"{
                ""people"": [
                    {""name"": ""Zack"", ""id"": 3},
                    {""name"": ""Alice"", ""id"": 1}
                ],
                ""version"": 1
            }";
            
            // Act
            bool success = JsonSorter.TrySortJsonDeep(unsortedJson, "name", out string result);
            
            // Assert
            success.ShouldBeTrue();
            string expected = "{\"people\":[{\"id\":1,\"name\":\"Alice\"},{\"id\":3,\"name\":\"Zack\"}],\"version\":1}";
            result.ShouldBe(expected);
        }
        
        [Fact]
        public void TrySortJsonDeep_WithInvalidJson_ShouldReturnFalseAndOriginalJson()
        {
            // Arrange
            string invalidJson = "{\"people\": [{}],"; // Invalid JSON
            
            // Act
            bool success = JsonSorter.TrySortJsonDeep(invalidJson, "name", out string result);
            
            // Assert
            success.ShouldBeFalse();
            result.ShouldBe(invalidJson);
        }
        
        [Fact]
        public void SortJson_WithVariousJsonTypes_ShouldPreserveTypes()
        {
            // Arrange
            string complexJson = @"{
                ""string"": ""Hello"",
                ""number"": 42,
                ""decimal"": 3.14,
                ""boolean"": true,
                ""null_value"": null,
                ""array"": [1, 2, 3],
                ""nested"": {""a"": 1}
            }";
            
            // Act
            string result = JsonSorter.SortJson(complexJson);
            
            // Assert
            // Parse the result to verify types are preserved
            using var document = JsonDocument.Parse(result);
            var root = document.RootElement;
            
            root.GetProperty("string").ValueKind.ShouldBe(JsonValueKind.String);
            root.GetProperty("string").GetString().ShouldBe("Hello");
            
            root.GetProperty("number").ValueKind.ShouldBe(JsonValueKind.Number);
            root.GetProperty("number").GetInt32().ShouldBe(42);
            
            root.GetProperty("decimal").ValueKind.ShouldBe(JsonValueKind.Number);
            root.GetProperty("decimal").GetDouble().ShouldBe(3.14);
            
            root.GetProperty("boolean").ValueKind.ShouldBe(JsonValueKind.True);
            root.GetProperty("boolean").GetBoolean().ShouldBeTrue();
            
            root.GetProperty("null_value").ValueKind.ShouldBe(JsonValueKind.Null);
            
            root.GetProperty("array").ValueKind.ShouldBe(JsonValueKind.Array);
            
            root.GetProperty("nested").ValueKind.ShouldBe(JsonValueKind.Object);
            root.GetProperty("nested").GetProperty("a").GetInt32().ShouldBe(1);
        }
    }
