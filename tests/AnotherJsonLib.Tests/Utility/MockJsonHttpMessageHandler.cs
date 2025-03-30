using System.Net;
using System.Text;
using System.Text.Json.Nodes;


namespace AnotherJsonLib.Tests.Utility;

/// <summary>
/// Provides mock HTTP responses for testing JSON API clients
/// </summary>
public class MockJsonHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, MockResponse> _responses = new(StringComparer.OrdinalIgnoreCase);
    private readonly JsonFaker _faker;

    public MockJsonHttpMessageHandler(int? seed = null)
    {
        _faker = new JsonFaker(seed);
    }

    /// <summary>
    /// Adds a mock response for a specific URL
    /// </summary>
    public MockJsonHttpMessageHandler AddMockResponse(string url, HttpStatusCode statusCode, JsonNode content)
    {
        _responses[url] = new MockResponse
        {
            StatusCode = statusCode,
            Content = content.ToJsonString(),
            Headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" }
            }
        };

        return this;
    }

    /// <summary>
    /// Adds a mock error response for a specific URL
    /// </summary>
    public MockJsonHttpMessageHandler AddMockErrorResponse(string url, HttpStatusCode statusCode,
        string errorMessage = null)
    {
        var errorContent = new JsonObject
        {
            ["error"] = errorMessage ?? "Error occurred",
            ["status"] = (int)statusCode
        };

        return AddMockResponse(url, statusCode, errorContent);
    }

    /// <summary>
    /// Adds a mock random JSON response for a specific URL
    /// </summary>
    public MockJsonHttpMessageHandler AddRandomResponse(string url, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return AddMockResponse(url, statusCode, _faker.GenerateComplexObject());
    }

    /// <summary>
    /// Adds custom headers to a mock response
    /// </summary>
    public MockJsonHttpMessageHandler AddHeaders(string url, Dictionary<string, string> headers)
    {
        if (_responses.TryGetValue(url, out var response))
        {
            foreach (var header in headers)
            {
                response.Headers[header.Key] = header.Value;
            }
        }

        return this;
    }

    /// <summary>
    /// Adds a delay to a mock response to simulate network latency
    /// </summary>
    public MockJsonHttpMessageHandler AddDelay(string url, TimeSpan delay)
    {
        if (_responses.TryGetValue(url, out var response))
        {
            response.Delay = delay;
        }

        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.RequestUri == null)
            throw new ArgumentNullException(nameof(request.RequestUri));

        var url = request.RequestUri.ToString();

        // Check if we have a mock response for this URL
        if (_responses.TryGetValue(url, out var mockResponse))
        {
            // Simulate network delay
            if (mockResponse.Delay > TimeSpan.Zero)
            {
                await Task.Delay(mockResponse.Delay, cancellationToken);
            }

            var response = new HttpResponseMessage(mockResponse.StatusCode);

            if (mockResponse.Content != null)
            {
                response.Content = new StringContent(
                    mockResponse.Content,
                    Encoding.UTF8,
                    "application/json");
            }

            // Add custom headers
            foreach (var header in mockResponse.Headers)
            {
                response.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return response;
        }

        // No mock response found
        return new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(
                $"{{\"error\":\"No mock response configured for URL: {url}\"}}",
                Encoding.UTF8,
                "application/json")
        };
    }

    private class MockResponse
    {
        public HttpStatusCode StatusCode { get; set; }
        public string Content { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public TimeSpan Delay { get; set; } = TimeSpan.Zero;
    }
}

/// <summary>
/// Extension methods to simplify working with HttpClient for JSON testing
/// </summary>
public static class HttpClientJsonExtensions
{
    /// <summary>
    /// Creates a HttpClient with a JsonHttpMessageHandler configured with predefined responses
    /// </summary>
    public static HttpClient CreateMockJsonClient(Action<MockJsonHttpMessageHandler> configure = null)
    {
        var handler = new MockJsonHttpMessageHandler();
        configure?.Invoke(handler);

        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.test.com/")
        };
    }
}