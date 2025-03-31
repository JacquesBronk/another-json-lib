using System.Text;
using System.Text.Json;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Utility.Operations;
using Shouldly;

namespace AnotherJsonLib.Tests.LibTests;

 public class JsonStreamerIntegrationTests : IDisposable
    {
        private readonly string _testFilePath;
        private readonly List<string> _createdFiles = new();
        
        public JsonStreamerIntegrationTests()
        {
            _testFilePath = Path.Combine(Path.GetTempPath(), $"integration_test_{Guid.NewGuid()}.json");
            _createdFiles.Add(_testFilePath);
            
            // Create a test file with a more complex structure
            File.WriteAllText(_testFilePath, @"{
                ""id"": ""12345"",
                ""name"": ""Integration Test"",
                ""active"": true,
                ""score"": 95.5,
                ""tags"": [""test"", ""integration"", ""json""],
                ""metadata"": {
                    ""created"": ""2023-06-15T14:30:00Z"",
                    ""version"": 2,
                    ""settings"": {
                        ""debug"": false,
                        ""timeout"": 30
                    }
                },
                ""items"": [
                    {
                        ""id"": 1,
                        ""value"": ""first""
                    },
                    {
                        ""id"": 2,
                        ""value"": ""second""
                    }
                ]
            }");
        }
        
        public void Dispose()
        {
            foreach (var file in _createdFiles)
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
        }
        
        [Fact]
        public void StreamJsonFile_ShouldHandleNestedStructures()
        {
            // Arrange
            var propertyNames = new List<string>();
            var stringValues = new List<string>();
            var objectDepth = 0;
            var maxObjectDepth = 0;
            
            // Act
            _testFilePath.StreamJsonFile((tokenType, tokenValue) => {
                if (tokenType == JsonTokenType.PropertyName)
                {
                    propertyNames.Add(tokenValue!);
                }
                else if (tokenType == JsonTokenType.String)
                {
                    stringValues.Add(tokenValue!);
                }
                else if (tokenType == JsonTokenType.StartObject)
                {
                    objectDepth++;
                    maxObjectDepth = Math.Max(maxObjectDepth, objectDepth);
                }
                else if (tokenType == JsonTokenType.EndObject)
                {
                    objectDepth--;
                }
            });
            
            // Assert
            propertyNames.ShouldContain("id");
            propertyNames.ShouldContain("name");
            propertyNames.ShouldContain("metadata");
            propertyNames.ShouldContain("settings");
            
            stringValues.ShouldContain("12345");
            stringValues.ShouldContain("Integration Test");
            stringValues.ShouldContain("test");
            stringValues.ShouldContain("first");
            stringValues.ShouldContain("second");
            
            maxObjectDepth.ShouldBe(3); // Deepest nesting level
        }
        
        [Fact]
        public async Task StreamJson_WithVeryLargeFile_ShouldHandleEfficiently()
        {
            // Arrange
            var largeFilePath = Path.Combine(Path.GetTempPath(), $"large_test_{Guid.NewGuid()}.json");
            _createdFiles.Add(largeFilePath);
            
            // Generate a large JSON file (around 5MB)
            await using (var writer = File.CreateText(largeFilePath))
            {
                await writer.WriteAsync("{\"items\":[");
                for (int i = 0; i < 100000; i++)
                {
                    await writer.WriteAsync($"{{\"id\":{i},\"value\":\"item{i}\"{(i < 99999 ? "}," : "}")}");
                }
                await writer.WriteAsync("]}");
            }
            
            // Act
            var itemCount = 0;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            largeFilePath.StreamJsonFile((tokenType, tokenValue) => {
                if (tokenType == JsonTokenType.PropertyName && tokenValue == "id")
                {
                    itemCount++;
                }
            });
            
            sw.Stop();
            
            // Assert
            itemCount.ShouldBe(100000);
            // Ensure processing is reasonably efficient (adjust threshold as needed)
            sw.ElapsedMilliseconds.ShouldBeLessThan(5000); // Should process in under 5 seconds
        }
        
        [Fact]
        public void StreamJson_WithMultipleJsonDocuments_ShouldHandleCorrectly()
        {
            // Arrange
            var multiDocJson = @"{""doc"":1}  {""doc"":2}";
            var docIds = new List<int>();
            int expectedExceptionCount = 0;
            
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(multiDocJson));
            
            try
            {
                // Act
                stream.StreamJson((tokenType, tokenValue) => {
                    if (tokenType == JsonTokenType.PropertyName && tokenValue == "doc")
                    {
                        docIds.Add(1); // We'll never reach this point for the second document
                    }
                });
            }
            catch (JsonOperationException)
            {
                // This should throw because there's extra data after the first document
                expectedExceptionCount++;
            }
            
            // Assert
            expectedExceptionCount.ShouldBe(1); // Should have thrown exception
            docIds.Count.ShouldBe(1); // Should only process first document
        }
    }
