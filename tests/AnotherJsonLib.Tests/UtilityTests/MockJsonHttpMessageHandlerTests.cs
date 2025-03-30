using System.Net;
using System.Text.Json.Nodes;
using AnotherJsonLib.Tests.Utility;

namespace AnotherJsonLib.Tests.UtilityTests;

public class MockJsonHttpMessageHandlerTests
{
    private const string TestUrl = "https://example.com/api";
    
   
    [Fact]
    public async Task SendAsync_WithConfiguredUrl_ReturnsMockResponse()
    {
        // Arrange
        var handler = new MockJsonHttpMessageHandler();
        var jsonContent = JsonNode.Parse("{\"name\":\"Test User\",\"id\":123}");
        handler.AddMockResponse(TestUrl, HttpStatusCode.OK, jsonContent);
        
        var client = new HttpClient(handler);
        
        // Act
        var response = await client.GetAsync(TestUrl);
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var parsedJson = JsonNode.Parse(content);
        JsonAssert.Equal(jsonContent, parsedJson);
    }
    
    [Fact]
    public async Task SendAsync_WithNonConfiguredUrl_ReturnsNotFound()
    {
        // Arrange
        var handler = new MockJsonHttpMessageHandler();
        var client = new HttpClient(handler);
        
        // Act
        var response = await client.GetAsync("https://example.com/non-existent");
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        // Verify the content contains an error message
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(content);
        Assert.NotNull(json["error"]);
        Assert.Contains("No mock response configured", json["error"].GetValue<string>());
    }
    
    [Fact]
    public async Task AddMockErrorResponse_ConfiguresErrorResponse()
    {
        // Arrange
        const string errorMessage = "Custom error message";
        var handler = new MockJsonHttpMessageHandler();
        handler.AddMockErrorResponse(TestUrl, HttpStatusCode.BadRequest, errorMessage);
        
        var client = new HttpClient(handler);
        
        // Act
        var response = await client.GetAsync(TestUrl);
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = JsonNode.Parse(content);
        Assert.Equal(errorMessage, json["error"]?.GetValue<string>());
        Assert.Equal(400, json["status"]?.GetValue<int>());
    }
    
    [Fact]
    public async Task AddMockErrorResponse_WithoutMessage_UsesDefaultMessage()
    {
        // Arrange
        var handler = new MockJsonHttpMessageHandler();
        handler.AddMockErrorResponse(TestUrl, HttpStatusCode.InternalServerError);
        
        var client = new HttpClient(handler);
        
        // Act
        var response = await client.GetAsync(TestUrl);
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var json = JsonNode.Parse(content);
        Assert.Equal("Error occurred", json["error"]?.GetValue<string>());
    }
    
    [Fact]
    public async Task AddRandomResponse_ReturnsRandomizedJson()
    {
        // Arrange
        var handler = new MockJsonHttpMessageHandler(seed: 42); // Fixed seed for reproducibility
        handler.AddRandomResponse(TestUrl);
        
        var client = new HttpClient(handler);
        
        // Act
        var response = await client.GetAsync(TestUrl);
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // Verify it's parseable JSON
        var json = JsonNode.Parse(content);
        Assert.NotNull(json);
    }
    
    [Fact]
    public async Task AddRandomResponse_WithCustomStatusCode_ReturnsSpecifiedStatusCode()
    {
        // Arrange
        var handler = new MockJsonHttpMessageHandler();
        handler.AddRandomResponse(TestUrl, HttpStatusCode.Created);
        
        var client = new HttpClient(handler);
        
        // Act
        var response = await client.GetAsync(TestUrl);
        
        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
    
    [Fact]
    public async Task AddHeaders_AppendsCustomHeaders()
    {
        // Arrange
        var handler = new MockJsonHttpMessageHandler();
        var jsonContent = JsonNode.Parse("{\"data\":\"test\"}");
        handler.AddMockResponse(TestUrl, HttpStatusCode.OK, jsonContent);
        
        var headers = new Dictionary<string, string>
        {
            { "X-Custom-Header", "Custom Value" },
            { "Authorization", "Bearer test-token" }
        };
        
        handler.AddHeaders(TestUrl, headers);
        
        var client = new HttpClient(handler);
        
        // Act
        var response = await client.GetAsync(TestUrl);
        
        // Assert
        Assert.Equal("Custom Value", response.Headers.GetValues("X-Custom-Header").First());
        Assert.Equal("Bearer test-token", response.Headers.GetValues("Authorization").First());
    }
    
    [Fact]
    public async Task AddDelay_AddsConfiguredDelay()
    {
        // Arrange
        var handler = new MockJsonHttpMessageHandler();
        var jsonContent = JsonNode.Parse("{\"data\":\"test\"}");
        handler.AddMockResponse(TestUrl, HttpStatusCode.OK, jsonContent);
        
        // Add a small delay (100ms)
        var delay = TimeSpan.FromMilliseconds(100);
        handler.AddDelay(TestUrl, delay);
        
        var client = new HttpClient(handler);
        
        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await client.GetAsync(TestUrl);
        stopwatch.Stop();
        
        // Assert
        // The response time should be at least the configured delay
        Assert.True(stopwatch.ElapsedMilliseconds >= 100);
    }
    
    [Fact]
    public async Task ChainedConfiguration_ConfiguresResponseCorrectly()
    {
        // Arrange
        var jsonContent = JsonNode.Parse("{\"data\":\"test\"}");
        var headers = new Dictionary<string, string> { { "X-Test", "Value" } };
        var delay = TimeSpan.FromMilliseconds(10);
        
        var handler = new MockJsonHttpMessageHandler()
            .AddMockResponse(TestUrl, HttpStatusCode.OK, jsonContent)
            .AddHeaders(TestUrl, headers)
            .AddDelay(TestUrl, delay);
        
        var client = new HttpClient(handler);
        
        // Act
        var response = await client.GetAsync(TestUrl);
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Value", response.Headers.GetValues("X-Test").First());
        var parsedJson = JsonNode.Parse(content);
        JsonAssert.Equal(jsonContent, parsedJson);
    }
    
    [Fact]
    public async Task DifferentHttpMethods_WithSameUrl_ReturnsSameResponse()
    {
        // Arrange
        var handler = new MockJsonHttpMessageHandler();
        var jsonContent = JsonNode.Parse("{\"success\":true}");
        handler.AddMockResponse(TestUrl, HttpStatusCode.OK, jsonContent);
        
        var client = new HttpClient(handler);
        
        // Act - Test with different HTTP methods
        var getResponse = await client.GetAsync(TestUrl);
        var postResponse = await client.PostAsync(TestUrl, new StringContent("{}"));
        var putResponse = await client.PutAsync(TestUrl, new StringContent("{}"));
        
        // Assert - All should return the same mocked response
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);
        
        // Verify content is the same
        var getContent = await getResponse.Content.ReadAsStringAsync();
        var postContent = await postResponse.Content.ReadAsStringAsync();
        var putContent = await putResponse.Content.ReadAsStringAsync();
        
        Assert.Equal(getContent, postContent);
        Assert.Equal(getContent, putContent);
    }
    
    
    [Fact]
    public async Task MultipleConfiguredUrls_ReturnCorrectResponses()
    {
        // Arrange
        var handler = new MockJsonHttpMessageHandler();
        
        var url1 = "https://example.com/api/users";
        var url2 = "https://example.com/api/products";
        
        var content1 = JsonNode.Parse("[{\"id\":1,\"name\":\"User 1\"}]");
        var content2 = JsonNode.Parse("[{\"id\":101,\"title\":\"Product 1\"}]");
        
        handler.AddMockResponse(url1, HttpStatusCode.OK, content1);
        handler.AddMockResponse(url2, HttpStatusCode.OK, content2);
        
        var client = new HttpClient(handler);
        
        // Act
        var response1 = await client.GetAsync(url1);
        var response2 = await client.GetAsync(url2);
        
        var content1Str = await response1.Content.ReadAsStringAsync();
        var content2Str = await response2.Content.ReadAsStringAsync();
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        
        var parsedContent1 = JsonNode.Parse(content1Str);
        var parsedContent2 = JsonNode.Parse(content2Str);
        
        JsonAssert.Equal(content1, parsedContent1);
        JsonAssert.Equal(content2, parsedContent2);
    }
    
}