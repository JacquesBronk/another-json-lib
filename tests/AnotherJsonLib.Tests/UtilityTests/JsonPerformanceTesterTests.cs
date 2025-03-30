using System.Text.Json.Nodes;
using AnotherJsonLib.Tests.Utility;
using Shouldly;

namespace AnotherJsonLib.Tests.UtilityTests
{
    public class JsonPerformanceTesterTests
    {
        [Fact]
        public void Constructor_ShouldNotThrow()
        {
            // Act & Assert
            Should.NotThrow(() => new JsonPerformanceTester());
            Should.NotThrow(() => new JsonPerformanceTester(seed: 42));
        }
        
        [Fact]
        public void MeasureParsingPerformance_ShouldReturnResults()
        {
            // Arrange
            var tester = new JsonPerformanceTester(seed: 42);
            var json = "{ \"name\": \"Test\", \"value\": 123 }";
            Func<string, object> parseFunction = jsonStr => JsonNode.Parse(jsonStr);
            
            // Act
            var result = tester.MeasureParsingPerformance(parseFunction, iterations: 5);
            
            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBeGreaterThan(0);
            foreach (var pair in result)
            {
                pair.Value.ShouldBeGreaterThanOrEqualTo(0); // Execution time should be positive
            }
        }
        
        [Fact]
        public void MeasureSerializationPerformance_ShouldReturnResults()
        {
            // Arrange
            var tester = new JsonPerformanceTester(seed: 42);
            Func<JsonNode, string> serializeFunction = node => node.ToJsonString();
            
            // Act
            var result = tester.MeasureSerializationPerformance(serializeFunction, iterations: 5);
            
            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBeGreaterThan(0);
            foreach (var pair in result)
            {
                pair.Value.ShouldBeGreaterThanOrEqualTo(0); // Execution time should be positive
            }
        }
        
        [Fact]
        public void MeasureMemoryUsage_ShouldReturnResults()
        {
            // Arrange
            var tester = new JsonPerformanceTester(seed: 42);
            Func<string, object> parseFunction = jsonStr => JsonNode.Parse(jsonStr);
            
            // Act
            var result = tester.MeasureMemoryUsage(parseFunction);
            
            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBeGreaterThan(0);
            foreach (var pair in result)
            {
                pair.Value.ShouldBeGreaterThanOrEqualTo(0); // Memory usage should be positive
            }
        }
        
        // If result is a complex structure where each implementation has nested data
        [Fact]
        public void CompareImplementations_ShouldCompareImplementations()
        {
            // Arrange
            var tester = new JsonPerformanceTester(seed: 42);

            // Using two different implementations
            Func<string, object> impl1 = jsonStr => JsonNode.Parse(jsonStr);
            Func<string, object> impl2 = jsonStr => JsonNode.Parse(jsonStr);

            // Act
            var result = tester.CompareImplementations(
                impl1, 
                impl2, 
                "System.Text.Json",
                "Custom",
                iterations: 2
            );

            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBe(3); 
    
            // Check that each size contains both implementations
            foreach (var sizeEntry in result)
            {
                sizeEntry.Value.ShouldContainKey("System.Text.Json");
                sizeEntry.Value.ShouldContainKey("Custom");
            }
    
            // Extract results for each implementation across all sizes
            var systemTextJsonResults = result.Values.Select(x => x["System.Text.Json"]);
            var customResults = result.Values.Select(x => x["Custom"]);
    
            // Verify we got results for each implementation
            systemTextJsonResults.ShouldNotBeEmpty();
            customResults.ShouldNotBeEmpty();
        }

        
        [Fact]
        public void GetTestJson_ShouldReturnNonEmptyJson()
        {
            // Arrange
            var tester = new JsonPerformanceTester(seed: 42);
            
            // Act & Assert
            foreach (var size in new[] { "Small", "Medium", "Large", "VeryLarge" })
            {
                var json = tester.GetTestJson(size);
                json.ShouldNotBeNullOrEmpty();
                Should.NotThrow(() => JsonNode.Parse(json));
            }
        }
        
        [Fact]
        public void GetTestJsonNode_ShouldReturnValidJsonNode()
        {
            // Arrange
            var tester = new JsonPerformanceTester(seed: 42);
            
            // Act & Assert
            foreach (var size in new[] { "Small", "Medium", "Large", "VeryLarge" })
            {
                var node = tester.GetTestJsonNode(size);
                node.ShouldNotBeNull();
            }
        }
        
        [Fact]
        public void CreateNestedObject_ShouldCreateNestedStructure()
        {
            // Arrange
            var tester = new JsonPerformanceTester(seed: 42);
            
            // Act
            var result = tester.CreateNestedObject(3);
            
            // Assert
            result.ShouldNotBeNull();
            // Could validate the nesting depth but that requires traversing the object
        }
        
        [Fact]
        public void CreateWideObject_ShouldCreateObjectWithManyProperties()
        {
            // Arrange
            var tester = new JsonPerformanceTester(seed: 42);
            int propertyCount = 10;
            
            // Act
            var result = tester.CreateWideObject(propertyCount);
            
            // Assert
            result.ShouldNotBeNull();
            result.AsObject().Count.ShouldBe(propertyCount);
        }
        
        [Fact]
        public void CreateLongArray_ShouldCreateArrayWithManyElements()
        {
            // Arrange
            var tester = new JsonPerformanceTester(seed: 42);
            int elementCount = 10;
            
            // Act
            var result = tester.CreateLongArray(elementCount);
            
            // Assert
            result.ShouldNotBeNull();
            result.AsArray().Count.ShouldBe(elementCount);
        }
    }
}