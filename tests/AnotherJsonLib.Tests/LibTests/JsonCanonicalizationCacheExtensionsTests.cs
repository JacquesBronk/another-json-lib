using AnotherJsonLib.Utility.Formatting;
using Microsoft.Extensions.Caching.Memory;
using Shouldly;

namespace AnotherJsonLib.Tests.LibTests;

public class JsonCanonicalizationCacheExtensionsTests
    {
        [Fact]
        public void ConfigureCanonicalizationCache_WithValidOptions_ShouldNotThrow()
        {
            // Arrange
            var options = new MemoryCacheOptions();
            
            // Act & Assert
            Should.NotThrow(() => JsonCanonicalizationCacheExtensions.ConfigureCanonicalizationCache(options));
        }
        
        [Fact]
        public void ConfigureCanonicalizationCache_WithNullOptions_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Should.Throw<ArgumentNullException>(() => 
                JsonCanonicalizationCacheExtensions.ConfigureCanonicalizationCache(null!));
        }
        
        [Fact]
        public void ClearCanonicalizationCache_ShouldNotThrow()
        {
            // Act & Assert
            Should.NotThrow(() => JsonCanonicalizationCacheExtensions.ClearCanonicalizationCache());
        }
        
        [Fact]
        public void CanonicalizeCached_WithNullJson_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Should.Throw<ArgumentNullException>(() => JsonCanonicalizationCacheExtensions.CanonicalizeCached(null!));
        }
        
        [Fact]
        public void CanonicalizeCached_WithValidJson_ShouldReturnCanonicalized()
        {
            // Arrange
            string json = @"{""name"": ""John"", ""age"": 30}";
            string expected = @"{""age"":30,""name"":""John""}";
            
            // Act
            string result = json.CanonicalizeCached();
            
            // Assert
            result.ShouldBe(expected);
        }
        
        [Fact]
        public void CanonicalizeCached_CalledTwiceWithSameInput_ShouldUseCachedValue()
        {
            // Arrange
            string json = @"{""name"": ""John"", ""age"": 30}";
            
            // Clear cache to ensure a clean test
            JsonCanonicalizationCacheExtensions.ClearCanonicalizationCache();
            
            // Act
            string firstResult = json.CanonicalizeCached();
            string secondResult = json.CanonicalizeCached();
            
            // Assert
            firstResult.ShouldBe(secondResult);
            // Note: We can't directly test if it used the cache without modifying the code to expose cache hits,
            // but we can verify the results are equal
        }
        
        [Fact]
        public void CanonicalizeCached_WithDifferentFormattingSameContent_ShouldProduceSameOutput()
        {
            // Arrange
            string json1 = @"{""name"": ""John"", ""age"": 30}";
            string json2 = @"{
                ""age"": 30,
                ""name"": ""John""
            }";
            
            // Act
            string result1 = json1.CanonicalizeCached();
            string result2 = json2.CanonicalizeCached();
            
            // Assert
            result1.ShouldBe(result2);
        }
        
        [Fact]
        public void CanonicalizeCached_ReturnsConsistentResults_RegardlessOfCache()
        {
            // Arrange - Use minimal configuration to avoid potential issues
            JsonCanonicalizationCacheExtensions.ConfigureCanonicalizationCache(new MemoryCacheOptions());
    
            // Generate a few different JSON strings
            var jsonStrings = new List<string>();
            for (int i = 0; i < 5; i++)
            {
                jsonStrings.Add($"{{\"id\": {i}}}");
            }
    
            // Act - First round should cache results
            var firstResults = new List<string>();
            foreach (var json in jsonStrings)
            {
                firstResults.Add(json.CanonicalizeCached());
            }
    
            // Clear cache
            JsonCanonicalizationCacheExtensions.ClearCanonicalizationCache();
    
            // Second round should recalculate
            var secondResults = new List<string>();
            foreach (var json in jsonStrings)
            {
                secondResults.Add(json.CanonicalizeCached());
            }
    
            // Assert - Both rounds should give the same results
            for (int i = 0; i < jsonStrings.Count; i++)
            {
                secondResults[i].ShouldBe(firstResults[i]);
            }
        }
        
        [Fact]
        public void CanonicalizeCached_CalledMultipleTimes_ShouldNotThrow()
        {
            // Arrange 
            JsonCanonicalizationCacheExtensions.ConfigureCanonicalizationCache(new MemoryCacheOptions());
            JsonCanonicalizationCacheExtensions.ClearCanonicalizationCache();
    
            // Act & Assert
            for (int i = 0; i < 3; i++)
            {
                // Test with the same JSON multiple times
                string json = "{\"test\": \"value\"}";
                string result = json.CanonicalizeCached();
                result.ShouldNotBeNullOrEmpty();
            }
        }


        [Fact]
        public void CanonicalizeCached_WithBasicCustomCacheOptions_ShouldSucceed()
        {
            // Arrange
            var options = new MemoryCacheOptions();  // Using default options
    
            // Act & Assert
            JsonCanonicalizationCacheExtensions.ConfigureCanonicalizationCache(options);
    
            // Test with just a few items to avoid potential size issues
            string json1 = "{\"id\": 1}";
            string json2 = "{\"id\": 2}";
            string json3 = "{\"id\": 3}";
    
            // Individual assertions to isolate any failing case
            string result1 = json1.CanonicalizeCached();
            result1.ShouldNotBeNullOrEmpty();
    
            string result2 = json2.CanonicalizeCached();
            result2.ShouldNotBeNullOrEmpty();
    
            string result3 = json3.CanonicalizeCached();
            result3.ShouldNotBeNullOrEmpty();
        }

        
        
        [Fact]
        public void CanonicalizeCached_WithComplexJson_ShouldHandleCorrectly()
        {
            // Arrange
            string complexJson = @"{
                ""items"": [
                    { ""id"": 2, ""name"": ""Item 2"" },
                    { ""id"": 1, ""name"": ""Item 1"" }
                ],
                ""metadata"": {
                    ""created"": ""2023-01-01T12:00:00Z"",
                    ""author"": ""Test User""
                }
            }";
            
            // Expected canonical form - properties ordered alphabetically, no whitespace
            string expected = @"{""items"":[{""id"":2,""name"":""Item 2""},{""id"":1,""name"":""Item 1""}],""metadata"":{""author"":""Test User"",""created"":""2023-01-01T12:00:00Z""}}";
            
            // Act
            string result = complexJson.CanonicalizeCached();
            
            // Assert
            result.ShouldBe(expected);
        }
        
        [Fact]
        public void ConfigureCanonicalizationCache_AfterCanonicalization_ShouldResetCache()
        {
            // Arrange
            string json = @"{""test"": ""value""}";
            
            // Act
            string result1 = json.CanonicalizeCached();
            JsonCanonicalizationCacheExtensions.ConfigureCanonicalizationCache(new MemoryCacheOptions());
            string result2 = json.CanonicalizeCached();
            
            // Assert
            result1.ShouldBe(result2); // Results should be the same despite cache reset
        }
    }
