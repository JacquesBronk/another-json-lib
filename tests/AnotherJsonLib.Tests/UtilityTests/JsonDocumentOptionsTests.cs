using System.Text.Json;
using AnotherJsonLib.Tests.Utility;

namespace AnotherJsonLib.Tests.UtilityTests;

public class JsonDocumentOptionsTests
{
    [Theory]
    [MemberData(nameof(ExceptionTestCases.GetInvalidJsonDocumentOptions), MemberType = typeof(ExceptionTestCases))]
    public void CreateJsonDocument_WithInvalidOptions_ThrowsException(int maxDepth, bool allowTrailingCommas,
        JsonCommentHandling commentHandling, Type expectedExceptionType)
    {
        // Arrange
        var validJson = "{ \"name\": \"Test\" }";

        if (expectedExceptionType == null)
        {
            // No exception expected - test that code runs without exception
            var options = new JsonDocumentOptions
            {
                AllowTrailingCommas = allowTrailingCommas,
                CommentHandling = commentHandling,
                MaxDepth = maxDepth
            };

            // This should not throw
            var document = JsonDocument.Parse(validJson, options);
            Assert.NotNull(document);
        }
        else
        {
            // Exception expected
            var exception = Record.Exception(() =>
            {
                var options = new JsonDocumentOptions
                {
                    AllowTrailingCommas = allowTrailingCommas,
                    CommentHandling = commentHandling
                };

                options.MaxDepth = maxDepth;
                JsonDocument.Parse(validJson, options);
            });

            // Assert the exception
            Assert.NotNull(exception);
            Assert.IsAssignableFrom(expectedExceptionType, exception);
        }
    }


    [Theory]
    [MemberData(nameof(ExceptionTestCases.GetMaxDepthExceededTestCases), MemberType = typeof(ExceptionTestCases))]
    public void Parse_WithDepthLimits_RespectsMaxDepth(string json, JsonDocumentOptions options,
        Type? expectedExceptionType)
    {
        if (expectedExceptionType != null)
        {
            // Should throw an exception
            var exception = Record.Exception(() => JsonDocument.Parse(json, options));

            // Verify an exception was thrown
            Assert.NotNull(exception);

            // Verify it's either the exact type or a derived type
            Assert.True(
                expectedExceptionType.IsAssignableFrom(exception.GetType()),
                $"Expected exception assignable to {expectedExceptionType}, but got {exception.GetType()}"
            );
        }
        else
        {
            // Should parse successfully
            var document = JsonDocument.Parse(json, options);
            Assert.NotNull(document);
        }
    }
}