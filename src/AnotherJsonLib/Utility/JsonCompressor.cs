using System.IO.Compression;
using System.Text;
using AnotherJsonLib.Domain;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Infra;
using Microsoft.Extensions.Logging;


namespace AnotherJsonLib.Utility;

/// <summary>
/// Provides methods for compressing and decompressing JSON data using various compression algorithms.
/// </summary>
public static class JsonCompressor
{
    private static readonly ILogger Logger = JsonLoggerFactory.Instance.GetLogger(nameof(JsonCompressor));
    /// <summary>
    /// Compresses a JSON string using the specified compression method and level.
    /// 
    /// <example>
    /// Example usage:
    /// <code>
    /// string jsonContent = "{\"name\":\"John\",\"age\":30}";
    /// byte[] compressed = JsonCompressor.CompressJson(jsonContent, JsonCompressionMethod.GZip);
    /// // Or using extension method syntax:
    /// byte[] compressed = jsonContent.CompressJson(JsonCompressionMethod.Brotli, CompressionLevel.Optimal);
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="json">The JSON string to compress.</param>
    /// <param name="method">The compression algorithm to use.</param>
    /// <param name="compressionLevel">
    /// The compression level. Use CompressionLevel.Fastest for speed or CompressionLevel.Optimal for best size reduction.
    /// </param>
    /// <returns>A byte array containing the compressed data.</returns>
    /// <exception cref="JsonArgumentException">Thrown when json is null.</exception>
    /// <exception cref="JsonOperationException">Thrown when compression fails.</exception>
    public static byte[] CompressJson(this string json, JsonCompressionMethod method, CompressionLevel compressionLevel = CompressionLevel.Fastest)
    {
        ExceptionHelpers.ThrowIfNull(json, nameof(json));
        using var performance = new PerformanceTracker(Logger, nameof(CompressJson));
        return ExceptionHelpers.SafeExecute(() =>
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(json);

                using var outputStream = new MemoryStream();
                // Create the appropriate compression stream.
                Stream compressionStream = method switch
                {
                    JsonCompressionMethod.GZip => new GZipStream(outputStream, compressionLevel, leaveOpen: true),
                    JsonCompressionMethod.Deflate => new DeflateStream(outputStream, compressionLevel, leaveOpen: true),
                    JsonCompressionMethod.Brotli => new BrotliStream(outputStream, compressionLevel, leaveOpen: true),
                    _ => throw new NotSupportedException($"Compression method {method} is not supported.")
                };

                using (compressionStream)
                {
                    compressionStream.Write(inputBytes, 0, inputBytes.Length);
                }

                return outputStream.ToArray();
            },
            (ex, message) => new JsonOperationException($"Failed to compress JSON data using {method}: {message}", ex),
            $"Failed to compress JSON data") ?? Array.Empty<byte>();
    }

    /// <summary>
    /// Decompresses a byte array (compressed JSON) using the specified compression method.
    /// 
    /// <example>
    /// Example usage:
    /// <code>
    /// // Assuming 'compressedData' is a byte array containing compressed JSON
    /// string originalJson = JsonCompressor.DecompressJson(compressedData, JsonCompressionMethod.GZip);
    /// // Or using extension method syntax:
    /// string originalJson = compressedData.DecompressJson(JsonCompressionMethod.Brotli);
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="compressedData">The compressed data.</param>
    /// <param name="method">The compression algorithm that was used.</param>
    /// <returns>The decompressed JSON string.</returns>
    /// <exception cref="JsonArgumentException">Thrown when compressedData is null.</exception>
    /// <exception cref="JsonOperationException">Thrown when decompression fails.</exception>
    public static string DecompressJson(this byte[] compressedData, JsonCompressionMethod method)
    {
        ExceptionHelpers.ThrowIfNull(compressedData, nameof(compressedData));
        using var performance = new PerformanceTracker(Logger, nameof(DecompressJson));
        return ExceptionHelpers.SafeExecute(() =>
            {
                using var inputStream = new MemoryStream(compressedData);
                // Create the appropriate decompression stream.
                Stream decompressionStream = method switch
                {
                    JsonCompressionMethod.GZip => new GZipStream(inputStream, CompressionMode.Decompress),
                    JsonCompressionMethod.Deflate => new DeflateStream(inputStream, CompressionMode.Decompress),
                    JsonCompressionMethod.Brotli => new BrotliStream(inputStream, CompressionMode.Decompress),
                    _ => throw new NotSupportedException($"Compression method {method} is not supported.")
                };

                using var decompressionMemoryStream = new MemoryStream();
                using (decompressionStream)
                {
                    decompressionStream.CopyTo(decompressionMemoryStream);
                }

                byte[] decompressedBytes = decompressionMemoryStream.ToArray();
                return Encoding.UTF8.GetString(decompressedBytes);
            },
            (ex, message) => new JsonOperationException($"Failed to decompress data using {method}: {message}", ex),
            $"Failed to decompress data") ?? string.Empty;
    }
    
    /// <summary>
    /// Attempts to compress a JSON string using the specified compression method and level,
    /// returning a default value if compression fails.
    /// 
    /// <example>
    /// Example usage:
    /// <code>
    /// string jsonContent = "{\"name\":\"John\",\"age\":30}";
    /// byte[]? result = JsonCompressor.TryCompressJson(jsonContent, JsonCompressionMethod.GZip, out bool success);
    /// if (success) {
    ///     // Use compressed data
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="json">The JSON string to compress.</param>
    /// <param name="method">The compression algorithm to use.</param>
    /// <param name="success">When this method returns, contains true if compression was successful, or false if it failed.</param>
    /// <param name="compressionLevel">The compression level to use.</param>
    /// <returns>The compressed data if successful; null otherwise.</returns>
    public static byte[]? TryCompressJson(this string json, JsonCompressionMethod method, out bool success, CompressionLevel compressionLevel = CompressionLevel.Fastest)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            success = false;
            return null;
        }
        using var performance = new PerformanceTracker(Logger, nameof(TryCompressJson));
        var result = ExceptionHelpers.SafeExecuteWithDefault(() => CompressJson(json, method, compressionLevel), 
        null,
        $"Failed to compress JSON data using {method}");
        
        success = result != null;
        return result;
    }

    /// <summary>
    /// Attempts to decompress a byte array (compressed JSON) using the specified compression method,
    /// returning a default value if decompression fails.
    /// 
    /// <example>
    /// Example usage:
    /// <code>
    /// // Assuming 'compressedData' is a byte array containing compressed JSON
    /// string? result = JsonCompressor.TryDecompressJson(compressedData, JsonCompressionMethod.GZip, out bool success);
    /// if (success) {
    ///     // Use decompressed JSON
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="compressedData">The compressed data.</param>
    /// <param name="method">The compression algorithm that was used.</param>
    /// <param name="success">When this method returns, contains true if decompression was successful, or false if it failed.</param>
    /// <returns>The decompressed JSON string if successful; null otherwise.</returns>
    public static string? TryDecompressJson(this byte[] compressedData, JsonCompressionMethod method, out bool success)
    {
        if (compressedData.Any() == false)
        {
            success = false;
            return null;
        }
        using var performance = new PerformanceTracker(Logger, nameof(TryDecompressJson));
        var result = ExceptionHelpers.SafeExecuteWithDefault(() => DecompressJson(compressedData, method),
        null,
        $"Failed to decompress data using {method}");
        
        success = result != null;
        return result;
    }
}