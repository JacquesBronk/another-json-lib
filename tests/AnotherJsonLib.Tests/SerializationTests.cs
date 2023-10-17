using System.Diagnostics;
using AnotherJsonLib.Infra;
using AnotherJsonLib.Tests.Helpers;
using AnotherJsonLib.Tests.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit.Abstractions;
using LoggerFactory = AnotherJsonLib.Infra.LoggerFactory;

namespace AnotherJsonLib.Tests;

public class SerializationTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public SerializationTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void ToJson_SerializeSimpleObject_ReturnsValidJson()
    {
        // Arrange
        var simpleObject = JsonTestDummies.CreateSimpleObject();

        // Act
        var json = simpleObject.ToJson();

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("Id");
        json.Should().Contain("Name");
    }

    [Fact]
    public void FromJson_DeserializeSimpleObject_ReturnsValidObject()
    {
        // Arrange
        var simpleObject = JsonTestDummies.CreateSimpleObject();
        var json = simpleObject.ToJson();

        // Act
        var deserializedObject = json.FromJson<SimpleObject>();

        // Assert
        deserializedObject.Should().NotBeNull();
        deserializedObject.Should().BeEquivalentTo(simpleObject);
    }

    // Similar tests for ComplexObject and LargeObject serialization and deserialization.

    [Fact]
    public void ToJson_SerializeObjectWithinTimeFrame_ShouldCompleteInTime()
    {
        // Arrange
        var complexObject = JsonTestDummies.CreateComplexObject();

        // Act & Assert
        var stopwatch = Stopwatch.StartNew();
        Action action = () => complexObject.ToJson();
        stopwatch.Stop();

        // Assert
        action.ExecutionTime().Should().BeLessOrEqualTo(TimeSpan.FromMilliseconds(100)); // Adjust as needed
        action.Should().NotThrow();
    }

    [Fact]
    public void FromJson_DeserializeObjectWithinTimeFrame_ShouldCompleteInTime()
    {
        // Arrange
        var complexObject = JsonTestDummies.CreateComplexObject();
        var json = complexObject.ToJson();

        // Act & Assert
        var stopwatch = Stopwatch.StartNew();
        Action action = () => json.FromJson<ComplexObject>();
        stopwatch.Stop();

        // Assert
        action.ExecutionTime().Should().BeLessOrEqualTo(TimeSpan.FromMilliseconds(100)); // Adjust as needed
        action.Should().NotThrow();
    }

    [Fact]
    public void LoggerFactory_GetLogger_ShouldReturnLoggerInstance()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Instance;

        // Act
        var logger = loggerFactory.GetLogger<SerializationTests>();

        // Assert
        logger.Should().NotBeNull();
        logger.Should().BeAssignableTo<ILogger>();
    }

    // You can also consider tests to ensure proper exception handling/logging when serialization/deserialization fails.

    [Fact]
    public void ToJson_SerializeInvalidObject_ShouldLogError()
    {
        // Arrange
        var invalidObject = new InvalidObject(); // An object that will cause serialization to fail

        // Act
        var json = invalidObject.ToJson();

        // Assert
        // Check if the error was logged, e.g., using a logger spy or mock.
        // json should be empty or contain an error message.
        json.Should().Be("{}");
    }

    [Fact]
    public void FromJson_DeserializeInvalidJson_ShouldLogError()
    {
        // Arrange
        var invalidJson = "invalid_json_string"; // Invalid JSON

        // Act
        var deserializedObject = invalidJson.FromJson<ComplexObject>();

        // Assert
        // Check if the error was logged, e.g., using a logger spy or mock.
        deserializedObject.Should().BeNull();
    }
    
        [Fact]
        public void ToJson_SerializeValidObject_ShouldReturnValidJson()
        {
            // Arrange
            var simpleObject = JsonTestDummies.CreateSimpleObject();

            // Act
            var json = simpleObject.ToJson();

            // Assert
            json.Should().NotBeNullOrEmpty();
            json.Should().Contain("\"Id\"");
            json.Should().Contain("\"Name\"");
        }

        [Fact]
        public void ToJson_SerializeInvalidObject_ShouldReturnEmptyObject()
        {
            // Arrange
            var invalidObject = new InvalidObject(); // An object that will cause serialization to fail

            // Act
            var json = invalidObject.ToJson();

            // Assert
            json.Should().Be("{}"); // Expect an empty JSON object
        }

        [Fact]
        public void ToJson_SerializeNullObject_ShouldReturnEmptyString()
        {
            // Arrange
            object? nullObject = null;

            // Act
            var json = nullObject.ToJson();

            // Assert
            json.Should().Be("null"); // Expect the JSON representation of null
        }


        [Fact]
        public void ToJson_SerializeWithPerformance_TestPerformance()
        {
            var simpleObject = JsonTestDummies.CreateSimpleObject();
            // Measure the time it takes to serialize a large number of objects
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 10000; i++)
            {
                simpleObject.ToJson();
            }
            stopwatch.Stop();

            // Act & Assert
            var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            _testOutputHelper.WriteLine($"Serialization performance: {elapsedMilliseconds} ms for 10000 objects");

            // You can set a performance threshold and assert that the serialization time is within that threshold
            // For example:
            elapsedMilliseconds.Should().BeLessOrEqualTo(120); // Check if it's less than or equal to 120 ms
        }

}


