using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Utility.Formatting;
using Shouldly;

namespace AnotherJsonLib.Tests.LibTests;

public class JsonCanonicalizationCacheExtensionsAdvancedTests
{
    [Fact]
    public void CanonicalizeCached_WhenJsonCanonicalizerThrowsException_ShouldWrapInJsonCanonicalizationException()
    {
        // This test would require making JsonCanonicalizer mockable or using a tool like
        // Microsoft Fakes/Typemock to override its behavior
        
        // For now, you could test with invalid JSON
        string invalidJson = "{not valid json}";
        
        // Act & Assert
        Should.Throw<JsonCanonicalizationException>(() => invalidJson.CanonicalizeCached());
    }

    [Fact]
    public void ClearCanonicalizationCache_AfterCanonicalization_ShouldForceRecanonicalization()
    {
        // Arrange
        string json = @"{""name"": ""Test""}";
        
        // Act - First call should cache the result
        string firstResult = json.CanonicalizeCached();
        
        // Clear cache
        JsonCanonicalizationCacheExtensions.ClearCanonicalizationCache();
        
        // Second call should recanonicalize
        string secondResult = json.CanonicalizeCached();
        
        // Assert
        firstResult.ShouldBe(secondResult); // Results should still be equal
        // Again, we can't directly verify cache usage without modifying the code
    }

}