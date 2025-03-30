using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Utility.Operations;
using Shouldly;

namespace AnotherJsonLib.Tests.LibTests;

public class JsonMergerTests
    {
        [Fact]
        public void Merge_IdenticalJson_ShouldReturnSameJson()
        {
            // Arrange
            string originalJson = "{\"name\":\"John\",\"age\":30}";
            string patchJson = "{\"name\":\"John\",\"age\":30}";
            
            // Act
            string merged = JsonMerger.Merge(originalJson, patchJson);
            
            // Assert: The merged JSON should be equivalent to the original.
            merged.ShouldBe(originalJson);
        }
        
        [Fact]
        public void Merge_PropertyAddition_ShouldAddNewProperties()
        {
            // Arrange
            string originalJson = "{\"name\":\"John\"}";
            string patchJson = "{\"name\":\"John\",\"city\":\"Paris\"}";
            
            // Act
            string merged = JsonMerger.Merge(originalJson, patchJson);
            
            // Assert: The merged JSON should include the added property.
            merged.ShouldContain("\"city\":\"Paris\"");
            merged.ShouldContain("\"name\":\"John\"");
        }
        
       
        [Fact]
        public void Merge_NestedObjects_ShouldMergeDeeply()
        {
            // Arrange: Nested JSON objects.
            string originalJson = @"
            {
                ""user"": {
                    ""name"": ""John"",
                    ""address"": { ""city"": ""London"", ""zip"": ""E1 6AN"" }
                }
            }";
            string patchJson = @"
            {
                ""user"": {
                    ""address"": { ""city"": ""Paris"" }
                }
            }";
            
            // Act
            string merged = JsonMerger.Merge(originalJson, patchJson);
            
            // Assert: The merged result should update only the specified nested properties.
            // Expected result: "name" remains unchanged; "address" is merged so that "city" becomes "Paris" while "zip" remains.
            merged.ShouldContain("\"name\":\"John\"");
            merged.ShouldContain("\"city\":\"Paris\"");
            merged.ShouldContain("\"zip\":\"E1 6AN\"");
        }
        
        [Fact]
        public void Merge_MultipleChanges_ShouldApplyAllUpdates()
        {
            // Arrange: Multiple simultaneous changes.
            string originalJson = "{\"a\":1,\"b\":2,\"c\":3}";
            string patchJson = "{\"b\":20,\"d\":4,\"c\":null}";
    
            // Act
            string merged = JsonMerger.Merge(originalJson, patchJson);
    
            // Assert:
            // - Property "b" should be updated.
            // - Property "d" should be added.
            // - Property "c" should be set to null.
            merged.ShouldContain("\"a\":1");
            merged.ShouldContain("\"b\":20");
            merged.ShouldContain("\"d\":4");
            merged.ShouldContain("\"c\":null");
        }

        
        [Fact]
        public void Merge_InvalidJson_ShouldThrowJsonParsingException()
        {
            // Arrange
            string originalJson = "{\"a\":1}";
            string invalidPatch = "{\"b\":2,}"; // invalid due to trailing comma
            
            // Act & Assert
            Should.Throw<JsonParsingException>(() => JsonMerger.Merge(originalJson, invalidPatch));
            Should.Throw<JsonParsingException>(() => JsonMerger.Merge(invalidPatch, originalJson));
        }
        
        [Fact]
        public void TryMerge_ValidInput_ShouldReturnTrueAndMergedJson()
        {
            // Arrange
            string originalJson = "{\"x\":100}";
            string patchJson = "{\"y\":200}";
            
            // Act
            bool success = JsonMerger.TryMerge(originalJson, patchJson, out string result);
            
            // Assert
            success.ShouldBeTrue();
            result.ShouldContain("\"x\":100");
            result.ShouldContain("\"y\":200");
        }
        
        [Fact]
        public void TryMerge_InvalidInput_ShouldReturnFalseAndEmptyResult()
        {
            // Arrange
            string invalidJson = "{\"x\":100"; // missing closing brace
            string patchJson = "{\"y\":200}";
            
            // Act
            bool success = JsonMerger.TryMerge(invalidJson, patchJson, out string result);
            
            // Assert
            success.ShouldBeFalse();
            result.ShouldBe("{\"x\":100");
        }
    }