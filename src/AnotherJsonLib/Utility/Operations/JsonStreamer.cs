using System.Text.Json;

namespace AnotherJsonLib.Utility.Operations;

public static class JsonStreamer
{
    /// <summary>
    /// Streams JSON data from a file and invokes the callback for each JSON token.
    /// This robust implementation handles tokens that may span across buffer boundaries.
    /// </summary>
    /// <param name="filePath">Path to the JSON file.</param>
    /// <param name="callback">Callback to process each JSON token.</param>
    public static void StreamJsonFile(this string filePath, Action<JsonTokenType, string?> callback)
    {
        using FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        fs.StreamJson(callback);
    }

    /// <summary>
    /// Streams JSON data from a Stream and invokes the callback for each JSON token.
    /// This implementation handles partial tokens across buffers.
    /// </summary>
    /// <param name="jsonStream">The JSON stream.</param>
    /// <param name="callback">Callback to process each JSON token.</param>
    public static void StreamJson(this Stream jsonStream, Action<JsonTokenType, string?> callback)
    {
        const int bufferSize = 4096;
        byte[] buffer = new byte[bufferSize];
        int bytesInBuffer = 0;
        var state = new JsonReaderState();

        while (true)
        {
            // Read as many bytes as possible into the remaining space.
            int bytesRead = jsonStream.Read(buffer, bytesInBuffer, bufferSize - bytesInBuffer);
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
    }

    private static void ProcessTokens(ref Utf8JsonReader reader, Action<JsonTokenType, string?> callback)
    {
        while (reader.Read())
        {
            string? tokenValue =
                (reader.TokenType == JsonTokenType.String || reader.TokenType == JsonTokenType.PropertyName)
                    ? reader.GetString()
                    : null;
            callback(reader.TokenType, tokenValue);
        }
    }
}