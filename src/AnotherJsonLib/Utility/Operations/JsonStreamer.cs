using System.Text.Json;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Helper;
using Microsoft.Extensions.Logging;

namespace AnotherJsonLib.Utility.Operations;

/// <summary>
/// Provides methods for efficiently streaming and processing JSON data from files or streams.
/// </summary>
/// <remarks>
/// The JsonStreamer class enables token-by-token processing of JSON data without loading
/// the entire content into memory, making it ideal for large files or memory-constrained
/// environments. It handles tokens that span across buffer boundaries correctly.
/// </remarks>
public static class JsonStreamer
{
    private static readonly ILogger Logger = JsonLoggerFactory.Instance.GetLogger(nameof(JsonStreamer));
    private const int DefaultBufferSize = 4096;

    /// <summary>
    /// Streams JSON data from a file and invokes the callback for each JSON token.
    /// This robust implementation handles tokens that may span across buffer boundaries.
    /// </summary>
    /// <param name="filePath">Path to the JSON file.</param>
    /// <param name="callback">Callback to process each JSON token.</param>
    /// <exception cref="ArgumentNullException">Thrown if filePath or callback is null.</exception>
    /// <exception cref="FileNotFoundException">Thrown if the specified file does not exist.</exception>
    /// <exception cref="JsonOperationException">Thrown when the streaming operation fails.</exception>
    /// <example>
    /// <code>
    /// // Process a large JSON file token by token
    /// string filePath = "large-data.json";
    /// 
    /// filePath.StreamJsonFile((tokenType, tokenValue) => 
    /// {
    ///     // Process string tokens, like property names and string values
    ///     if (tokenType == JsonTokenType.PropertyName || tokenType == JsonTokenType.String)
    ///     {
    ///         Console.WriteLine($"Found {tokenType}: {tokenValue}");
    ///     }
    ///     // Track object structure
    ///     else if (tokenType == JsonTokenType.StartObject)
    ///     {
    ///         Console.WriteLine("Start of object");
    ///     }
    /// });
    /// </code>
    /// </example>
    public static void StreamJsonFile(this string filePath, Action<JsonTokenType, string?> callback)
    {
        using var performance = new PerformanceTracker(Logger, nameof(StreamJsonFile));
        
        ExceptionHelpers.SafeExecute(() =>
        {
            // Validate parameters
            ExceptionHelpers.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));
            ExceptionHelpers.ThrowIfNull(callback, nameof(callback));
            
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"JSON file not found: {filePath}", filePath);
            }
            
            Logger.LogDebug("Beginning to stream JSON file: {FilePath}", filePath);
            
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            fs.StreamJson(callback);
            
            Logger.LogDebug("Completed streaming JSON file: {FilePath}", filePath);
        },
        (ex, msg) => {
            if (ex is FileNotFoundException fileEx)
                return new JsonLibException(fileEx.Message, fileEx); 
                
            if (ex is ArgumentException argEx)
                return new JsonArgumentException($"Invalid argument when streaming JSON file: {argEx.Message}", argEx);
                
            return new JsonOperationException($"Failed to stream JSON file: {msg}", ex);
        },
        $"Error streaming JSON file '{filePath}'");
    }

    /// <summary>
    /// Streams JSON data from a Stream and invokes the callback for each JSON token.
    /// This implementation handles partial tokens across buffers.
    /// </summary>
    /// <param name="jsonStream">The JSON stream.</param>
    /// <param name="callback">Callback to process each JSON token.</param>
    /// <exception cref="ArgumentNullException">Thrown if jsonStream or callback is null.</exception>
    /// <exception cref="JsonOperationException">Thrown when the streaming operation fails.</exception>
    /// <example>
    /// <code>
    /// // Process a JSON response from a web request
    /// using var httpClient = new HttpClient();
    /// using var response = await httpClient.GetAsync("https://api.example.com/data");
    /// using var stream = await response.Content.ReadAsStreamAsync();
    /// 
    /// // Count the total number of objects in the JSON
    /// int objectCount = 0;
    /// stream.StreamJson((tokenType, _) => 
    /// {
    ///     if (tokenType == JsonTokenType.StartObject)
    ///         objectCount++;
    /// });
    /// 
    /// Console.WriteLine($"Found {objectCount} objects in the API response");
    /// </code>
    /// </example>
    public static void StreamJson(this Stream jsonStream, Action<JsonTokenType, string?> callback)
    {
        using var performance = new PerformanceTracker(Logger, nameof(StreamJson));
        
        ExceptionHelpers.SafeExecute(() =>
        {
            // Validate parameters
            ExceptionHelpers.ThrowIfNull(jsonStream, nameof(jsonStream));
            ExceptionHelpers.ThrowIfNull(callback, nameof(callback));
            
            if (!jsonStream.CanRead)
            {
                throw new ArgumentException("Stream must be readable", nameof(jsonStream));
            }
            
            Logger.LogDebug("Beginning to stream JSON from stream");
            
            // Implement streaming from the given stream
            byte[] buffer = new byte[DefaultBufferSize];
            int bytesInBuffer = 0;
            var state = new JsonReaderState();

            while (true)
            {
                // Read as many bytes as possible into the remaining space.
                int bytesRead = jsonStream.Read(buffer, bytesInBuffer, buffer.Length - bytesInBuffer);
                if (bytesRead == 0)
                {
                    // End of stream: process remaining bytes as final block.
                    var finalReader = new Utf8JsonReader(new ReadOnlySpan<byte>(buffer, 0, bytesInBuffer),
                        isFinalBlock: true, state: state);
                    ProcessTokens(ref finalReader, callback);
                    break;
                }

                int totalBytes = bytesInBuffer + bytesRead;
                var span = new ReadOnlySpan<byte>(buffer, 0, totalBytes);
                var reader = new Utf8JsonReader(span, isFinalBlock: false, state: state);

                // Process tokens until we run out of data in the current span.
                ProcessTokens(ref reader, callback);

                // Update state and determine how many bytes were not consumed.
                state = reader.CurrentState;
                long bytesConsumed = reader.BytesConsumed;
                bytesInBuffer = totalBytes - (int)bytesConsumed;

                if (bytesInBuffer > 0)
                {
                    // Copy unconsumed bytes to the beginning of the buffer.
                    Buffer.BlockCopy(buffer, (int)bytesConsumed, buffer, 0, bytesInBuffer);
                }
            }
            
            Logger.LogDebug("Completed streaming JSON from stream");
        },
        (ex, msg) => {
            if (ex is ArgumentException argEx)
                return new JsonArgumentException($"Invalid argument when streaming JSON: {argEx.Message}", argEx);
                
            return new JsonOperationException($"Failed to stream JSON: {msg}", ex);
        },
        "Error streaming JSON from stream");
    }

    /// <summary>
    /// Processes tokens from a JSON reader and forwards them to the callback.
    /// </summary>
    /// <param name="reader">The JSON reader to process tokens from.</param>
    /// <param name="callback">The callback to invoke for each token.</param>
    private static void ProcessTokens(ref Utf8JsonReader reader, Action<JsonTokenType, string?> callback)
    {
        try
        {
            while (reader.Read())
            {
                string? tokenValue =
                    (reader.TokenType == JsonTokenType.String || reader.TokenType == JsonTokenType.PropertyName)
                        ? reader.GetString()
                        : null;
                        
                Logger.LogTrace("JSON token: {TokenType}, Value: {TokenValue}", 
                    reader.TokenType, tokenValue ?? "(null)");
                        
                callback(reader.TokenType, tokenValue);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing JSON tokens at position {Position}", reader.BytesConsumed);
            throw;
        }
    }
    
    /// <summary>
    /// Attempts to stream JSON data from a file without throwing exceptions.
    /// </summary>
    /// <param name="filePath">Path to the JSON file.</param>
    /// <param name="callback">Callback to process each JSON token.</param>
    /// <returns>True if streaming was successful; otherwise, false.</returns>
    public static bool TryStreamJsonFile(this string filePath, Action<JsonTokenType, string?> callback)
    {
        try
        {
            StreamJsonFile(filePath, callback);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Error streaming JSON file: {FilePath}", filePath);
            return false;
        }
    }
    
    /// <summary>
    /// Attempts to stream JSON data from a stream without throwing exceptions.
    /// </summary>
    /// <param name="jsonStream">The JSON stream.</param>
    /// <param name="callback">Callback to process each JSON token.</param>
    /// <returns>True if streaming was successful; otherwise, false.</returns>
    public static bool TryStreamJson(this Stream jsonStream, Action<JsonTokenType, string?> callback)
    {
        try
        {
            StreamJson(jsonStream, callback);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Error streaming JSON from stream");
            return false;
        }
    }
}