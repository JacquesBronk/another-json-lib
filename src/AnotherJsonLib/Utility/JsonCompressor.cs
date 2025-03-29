using System.IO.Compression;
using System.Text;
using AnotherJsonLib.Domain;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Infra;
using Microsoft.Extensions.Logging;

namespace AnotherJsonLib.Utility;

/// <summary>
/// Provides methods for compressing and decompressing JSON data using various compression algorithms.
/// 
/// JSON data can often be very verbose, especially for large datasets. This utility helps reduce
/// data size for storage or transmission while preserving all the information. Key benefits include:
/// 
/// - Reduced storage requirements for JSON data
/// - Faster network transmission with smaller payloads
/// - Lower bandwidth usage for API responses
/// - Support for multiple compression algorithms (GZip, Deflate, Brotli)
/// - Configurable compression levels for size/speed tradeoffs
/// 
/// <example>
/// <code>
/// // Example: Compress a large JSON object with Brotli for maximum compression
/// string largeJson = @"{""users"":[{""id"":1,""name"":""Alice"",""email"":""alice@example.com"",""roles"":[""admin"",""user""]},
///                       {""id"":2,""name"":""Bob"",""email"":""bob@example.com"",""roles"":[""user""]}],
///                       ""metadata"":{""version"":""1.0"",""generated"":""2023-05-10T15:30:00Z""}}";
///                      
/// // Compress with Brotli for best compression ratio
/// byte[] compressed = largeJson.CompressJson(JsonCompressionMethod.Brotli, CompressionLevel.Optimal);
/// 
/// // Store or transmit the compressed data...
/// 
/// // Then decompress when needed
/// string decompressed = compressed.DecompressJson(JsonCompressionMethod.Brotli);
/// 
/// // Original and decompressed strings will be identical
/// Console.WriteLine(largeJson == decompressed);  // True
/// </code>
/// </example>
/// </summary>
public static class JsonCompressor
{
    private static readonly ILogger Logger = JsonLoggerFactory.Instance.GetLogger(nameof(JsonCompressor));
    
    /// <summary>
    /// Default encoding used for JSON string conversion.
    /// </summary>
    private static readonly Encoding DefaultEncoding = Encoding.UTF8;
    
    /// <summary>
    /// Default buffer size used for compression operations.
    /// </summary>
    private const int DefaultBufferSize = 4096;

    /// <summary>
    /// Compresses a JSON string using the specified compression method and level.
    /// 
    /// <para>
    /// Available compression methods:
    /// - GZip: Good balance of compression ratio and speed
    /// - Deflate: Slightly smaller output than GZip but less compatible
    /// - Brotli: Best compression ratio but slower
    /// </para>
    /// 
    /// <para>
    /// Compression levels:
    /// - Fastest: Prioritizes speed over compression ratio
    /// - Optimal: Balances speed and compression ratio
    /// - SmallestSize (for Brotli only): Maximizes compression ratio at the cost of speed
    /// </para>
    /// 
    /// <example>
    /// <code>
    /// // Compress with GZip (good general-purpose choice)
    /// string jsonContent = @"{""name"":""John"",""age"":30,""address"":{""city"":""New York"",""zip"":""10001""}}";
    /// byte[] gzipCompressed = jsonContent.CompressJson(JsonCompressionMethod.GZip);
    /// 
    /// // Compress with Brotli for maximum compression
    /// byte[] brotliCompressed = jsonContent.CompressJson(JsonCompressionMethod.Brotli, CompressionLevel.Optimal);
    /// 
    /// // Brotli typically achieves better compression than GZip
    /// Console.WriteLine($"GZip size: {gzipCompressed.Length}, Brotli size: {brotliCompressed.Length}");
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="json">The JSON string to compress.</param>
    /// <param name="method">The compression algorithm to use.</param>
    /// <param name="compressionLevel">
    /// The compression level. Use CompressionLevel.Fastest for speed or CompressionLevel.Optimal for best size reduction.
    /// </param>
    /// <returns>A byte array containing the compressed data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when json is null.</exception>
    /// <exception cref="JsonCompressionException">Thrown when compression fails.</exception>
    public static byte[] CompressJson(this string json, JsonCompressionMethod method, CompressionLevel compressionLevel = CompressionLevel.Fastest)
    {
        using var performance = new PerformanceTracker(Logger, nameof(CompressJson));
        
        // Validate inputs
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(json, nameof(json));
        
        return ExceptionHelpers.SafeExecute(() =>
        {
            Logger.LogDebug("Compressing JSON string of length {Length} using {Method} with {Level} compression level", 
                json.Length, method, compressionLevel);
                
            byte[] inputBytes = DefaultEncoding.GetBytes(json);
            
            using var outputStream = new MemoryStream();
            
            // Create the appropriate compression stream.
            using (Stream compressionStream = CreateCompressionStream(outputStream, method, compressionLevel))
            {
                compressionStream.Write(inputBytes, 0, inputBytes.Length);
            } // Dispose of the compressionStream to flush and close it properly
            
            byte[] result = outputStream.ToArray();
            float compressionRatio = inputBytes.Length > 0 ? (float)result.Length / inputBytes.Length : 0;
            
            Logger.LogDebug("Successfully compressed JSON from {OriginalSize} to {CompressedSize} bytes " +
                           "(compression ratio: {CompressionRatio:P2})",
                inputBytes.Length, result.Length, compressionRatio);
                
            return result;
        },
        (ex, msg) => new JsonCompressionException($"Failed to compress JSON data using {method}: {msg}", ex),
        $"Failed to compress JSON data") ?? Array.Empty<byte>();
    }

    /// <summary>
    /// Decompresses a byte array (compressed JSON) using the specified compression method.
    /// 
    /// <example>
    /// <code>
    /// // Assuming we have compressed JSON data from somewhere
    /// byte[] compressedData = GetCompressedJsonFromSource();
    /// 
    /// // Decompress using the same method that was used for compression
    /// string originalJson = compressedData.DecompressJson(JsonCompressionMethod.GZip);
    /// 
    /// // Now you can parse and use the JSON
    /// using (JsonDocument doc = JsonDocument.Parse(originalJson))
    /// {
    ///     // Work with the JSON data
    ///     var root = doc.RootElement;
    ///     // ...
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="compressedData">The compressed data as a byte array.</param>
    /// <param name="method">The compression algorithm that was used.</param>
    /// <returns>The decompressed JSON string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when compressedData is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the compressed data is empty.</exception>
    /// <exception cref="JsonCompressionException">Thrown when decompression fails.</exception>
    public static string DecompressJson(this byte[] compressedData, JsonCompressionMethod method)
    {
        using var performance = new PerformanceTracker(Logger, nameof(DecompressJson));
        
        // Validate inputs
        ExceptionHelpers.ThrowIfNull(compressedData, nameof(compressedData));
        ExceptionHelpers.ThrowIfFalse(compressedData.Length > 0, "Compressed data cannot be empty", nameof(compressedData));
        
        return ExceptionHelpers.SafeExecute(() =>
        {
            Logger.LogDebug("Decompressing {Length} bytes of data using {Method}", 
                compressedData.Length, method);
                
            using var inputStream = new MemoryStream(compressedData);
            
            // Create the appropriate decompression stream
            using var decompressionStream = CreateDecompressionStream(inputStream, method);
            using var resultStream = new MemoryStream();
            
            // Copy the decompressed data to the result stream
            decompressionStream.CopyTo(resultStream);
            
            byte[] resultBytes = resultStream.ToArray();
            string decompressedJson = DefaultEncoding.GetString(resultBytes);
            
            Logger.LogDebug("Successfully decompressed data to JSON string of length {DecompressedLength} characters",
                decompressedJson.Length);
                
            return decompressedJson;
        },
        (ex, msg) => new JsonCompressionException($"Failed to decompress data using {method}: {msg}", ex),
        $"Failed to decompress data") ?? string.Empty;
    }
    
    /// <summary>
    /// Creates a compression stream for the specified method and compression level.
    /// </summary>
    /// <param name="outputStream">The output stream to write compressed data to.</param>
    /// <param name="method">The compression method to use.</param>
    /// <param name="compressionLevel">The compression level to use.</param>
    /// <returns>A Stream that performs compression.</returns>
    /// <exception cref="NotSupportedException">Thrown when an unsupported compression method is specified.</exception>
    private static Stream CreateCompressionStream(Stream outputStream, JsonCompressionMethod method, CompressionLevel compressionLevel)
    {
        return method switch
        {
            JsonCompressionMethod.GZip => new GZipStream(outputStream, compressionLevel, leaveOpen: true),
            JsonCompressionMethod.Deflate => new DeflateStream(outputStream, compressionLevel, leaveOpen: true),
            JsonCompressionMethod.Brotli => new BrotliStream(outputStream, compressionLevel, leaveOpen: true),
            _ => throw new NotSupportedException($"Compression method {method} is not supported.")
        };
    }
    
    /// <summary>
    /// Creates a decompression stream for the specified method.
    /// </summary>
    /// <param name="inputStream">The input stream containing compressed data.</param>
    /// <param name="method">The compression method used.</param>
    /// <returns>A Stream that performs decompression.</returns>
    /// <exception cref="NotSupportedException">Thrown when an unsupported compression method is specified.</exception>
    private static Stream CreateDecompressionStream(Stream inputStream, JsonCompressionMethod method)
    {
        return method switch
        {
            JsonCompressionMethod.GZip => new GZipStream(inputStream, CompressionMode.Decompress),
            JsonCompressionMethod.Deflate => new DeflateStream(inputStream, CompressionMode.Decompress),
            JsonCompressionMethod.Brotli => new BrotliStream(inputStream, CompressionMode.Decompress),
            _ => throw new NotSupportedException($"Compression method {method} is not supported.")
        };
    }
    
    /// <summary>
    /// Compresses a JSON string using the specified method and writes the result to a stream.
    /// This is useful for streaming scenarios where you want to avoid allocating a byte array.
    /// 
    /// <example>
    /// <code>
    /// // Compress directly to a file stream
    /// string jsonData = GetLargeJsonData();
    /// using (var fileStream = File.Create("data.json.gz"))
    /// {
    ///     jsonData.CompressJsonToStream(fileStream, JsonCompressionMethod.GZip);
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="json">The JSON string to compress.</param>
    /// <param name="outputStream">The stream to write the compressed data to.</param>
    /// <param name="method">The compression method to use.</param>
    /// <param name="compressionLevel">The compression level to use.</param>
    /// <param name="leaveOpen">Whether to leave the output stream open after compression.</param>
    /// <exception cref="ArgumentNullException">Thrown when json or outputStream is null.</exception>
    /// <exception cref="ArgumentException">Thrown when outputStream is not writable.</exception>
    /// <exception cref="JsonCompressionException">Thrown when compression fails.</exception>
    public static void CompressJsonToStream(
        this string json, 
        Stream outputStream, 
        JsonCompressionMethod method, 
        CompressionLevel compressionLevel = CompressionLevel.Fastest,
        bool leaveOpen = false)
    {
        using var performance = new PerformanceTracker(Logger, nameof(CompressJsonToStream));
        
        // Validate inputs
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(json, nameof(json));
        ExceptionHelpers.ThrowIfNull(outputStream, nameof(outputStream));
        ExceptionHelpers.ThrowIfFalse(outputStream.CanWrite, "Output stream must be writable", nameof(outputStream));
        
        ExceptionHelpers.SafeExecute(() =>
        {
            Logger.LogDebug("Compressing JSON string of length {Length} to stream using {Method}", 
                json.Length, method);
                
            byte[] inputBytes = DefaultEncoding.GetBytes(json);
            
            // Create appropriate compression stream based on method
            using (var compressionStream = CreateCompressionStream(outputStream, method, compressionLevel))
            {
                compressionStream.Write(inputBytes, 0, inputBytes.Length);
            } // Dispose to ensure all data is flushed to the underlying stream
            
            if (!leaveOpen)
            {
                outputStream.Close();
            }
            
            Logger.LogDebug("Successfully compressed JSON to stream");
        },
        (ex, msg) => new JsonCompressionException($"Failed to compress JSON to stream using {method}: {msg}", ex),
        "Failed to compress JSON to stream");
    }
    
    /// <summary>
    /// Decompresses JSON data from a stream using the specified method.
    /// This is useful for streaming scenarios where you want to decompress directly from a source stream.
    /// 
    /// <example>
    /// <code>
    /// // Decompress directly from a file stream
    /// using (var fileStream = File.OpenRead("data.json.gz"))
    /// {
    ///     string jsonData = fileStream.DecompressJsonFromStream(JsonCompressionMethod.GZip);
    ///     // Use the decompressed JSON
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="inputStream">The stream containing compressed data.</param>
    /// <param name="method">The compression method that was used.</param>
    /// <returns>The decompressed JSON string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when inputStream is null.</exception>
    /// <exception cref="ArgumentException">Thrown when inputStream is not readable.</exception>
    /// <exception cref="JsonCompressionException">Thrown when decompression fails.</exception>
    public static string DecompressJsonFromStream(this Stream inputStream, JsonCompressionMethod method)
    {
        using var performance = new PerformanceTracker(Logger, nameof(DecompressJsonFromStream));
        
        // Validate inputs
        ExceptionHelpers.ThrowIfNull(inputStream, nameof(inputStream));
        ExceptionHelpers.ThrowIfFalse(inputStream.CanRead, "Input stream must be readable", nameof(inputStream));
        
        return ExceptionHelpers.SafeExecute(() =>
        {
            Logger.LogDebug("Decompressing JSON data from stream using {Method}", method);
            
            // Create the appropriate decompression stream
            using var decompressionStream = CreateDecompressionStream(inputStream, method);
            using var resultStream = new MemoryStream();
            
            // Copy the data using a buffer for efficiency
            decompressionStream.CopyTo(resultStream, DefaultBufferSize);
            
            byte[] resultBytes = resultStream.ToArray();
            string decompressedJson = DefaultEncoding.GetString(resultBytes);
            
            Logger.LogDebug("Successfully decompressed stream to JSON string of length {DecompressedLength} characters",
                decompressedJson.Length);
                
            return decompressedJson;
        },
        (ex, msg) => new JsonCompressionException($"Failed to decompress stream using {method}: {msg}", ex),
        "Failed to decompress stream data") ?? string.Empty;
    }
    
    /// <summary>
    /// Attempts to compress a JSON string using the specified compression method and level,
    /// returning a success indicator instead of throwing exceptions.
    /// 
    /// <example>
    /// <code>
    /// string jsonContent = GetJsonData();
    /// 
    /// if (jsonContent.TryCompressJson(JsonCompressionMethod.Brotli, out byte[] compressedData, out bool success))
    /// {
    ///     // Use compressedData
    ///     Console.WriteLine($"Compressed to {compressedData.Length} bytes");
    /// }
    /// else
    /// {
    ///     Console.WriteLine("Compression failed");
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="json">The JSON string to compress.</param>
    /// <param name="method">The compression algorithm to use.</param>
    /// <param name="result">When successful, contains the compressed data; otherwise, null.</param>
    /// <param name="success">When this method returns, contains true if compression was successful, or false if it failed.</param>
    /// <param name="compressionLevel">The compression level to use.</param>
    /// <returns>True if the method was executed without throwing exceptions; otherwise, false.</returns>
    public static bool TryCompressJson(this string json, JsonCompressionMethod method, out byte[]? result, out bool success, CompressionLevel compressionLevel = CompressionLevel.Fastest)
    {
        success = false;
        
        if (string.IsNullOrWhiteSpace(json))
        {
            result = null;
            return false;
        }
        
        try
        {
            result = CompressJson(json, method, compressionLevel);
            if (result.Length == 0)
            {
                Logger.LogDebug("Compression resulted in an empty byte array");
                result = null;
            }
            success = result is { Length: > 0 };
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Error compressing JSON data using {Method}", method);
            result = null;
            return false;
        }
    }

    /// <summary>
    /// Attempts to decompress a byte array (compressed JSON) using the specified compression method,
    /// returning a success indicator instead of throwing exceptions.
    /// 
    /// <example>
    /// <code>
    /// byte[] compressedData = GetCompressedData();
    /// 
    /// if (compressedData.TryDecompressJson(JsonCompressionMethod.GZip, out string jsonResult, out bool success))
    /// {
    ///     // Use jsonResult
    ///     Console.WriteLine($"Decompressed to {jsonResult.Length} characters");
    /// }
    /// else
    /// {
    ///     Console.WriteLine("Decompression failed");
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="compressedData">The compressed data.</param>
    /// <param name="method">The compression algorithm that was used.</param>
    /// <param name="result">When successful, contains the decompressed JSON string; otherwise, null.</param>
    /// <param name="success">When this method returns, contains true if decompression was successful, or false if it failed.</param>
    /// <returns>True if the method was executed without throwing exceptions; otherwise, false.</returns>
    public static bool TryDecompressJson(this byte[]? compressedData, JsonCompressionMethod method, out string? result, out bool success)
    {
        success = false;
        
        if (compressedData == null || compressedData.Length == 0)
        {
            result = null;
            return false;
        }
        
        try
        {
            result = DecompressJson(compressedData, method);
            success = !string.IsNullOrEmpty(result);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Error decompressing data using {Method}", method);
            result = null;
            return false;
        }
    }
    
    /// <summary>
    /// Compresses a JSON string and returns it as a Base64 encoded string.
    /// This is useful when you need to embed compressed data in text formats like JSON or XML.
    /// 
    /// <example>
    /// <code>
    /// string jsonData = @"{""name"":""John"",""age"":30,""details"":{""address"":""123 Main St"",""city"":""New York""}}";
    /// 
    /// // Compress and encode as Base64
    /// string base64Compressed = jsonData.CompressJsonToBase64(JsonCompressionMethod.GZip);
    /// 
    /// // The result can be safely included in another JSON document
    /// string wrapper = $"{{\"compressedData\":\"{base64Compressed}\"}}";
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="json">The JSON string to compress.</param>
    /// <param name="method">The compression method to use.</param>
    /// <param name="compressionLevel">The compression level to use.</param>
    /// <returns>A Base64 encoded string containing the compressed data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when json is null.</exception>
    /// <exception cref="JsonCompressionException">Thrown when compression fails.</exception>
    public static string CompressJsonToBase64(this string json, JsonCompressionMethod method, CompressionLevel compressionLevel = CompressionLevel.Fastest)
    {
        using var performance = new PerformanceTracker(Logger, nameof(CompressJsonToBase64));
        
        // Validate input
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(json, nameof(json));
        
        return ExceptionHelpers.SafeExecute(() => 
        {
            Logger.LogDebug("Compressing JSON string of length {Length} to Base64 using {Method}", 
                json.Length, method);
                
            byte[] compressed = CompressJson(json, method, compressionLevel);
            string base64 = Convert.ToBase64String(compressed);
            
            Logger.LogDebug("Successfully compressed JSON to Base64 string of length {Base64Length} characters", 
                base64.Length);
                
            return base64;
        },
        (ex, msg) => new JsonCompressionException($"Failed to compress JSON to Base64 using {method}: {msg}", ex),
        "Failed to compress JSON to Base64") ?? string.Empty;
    }
    
    /// <summary>
    /// Decompresses a Base64 encoded string back to the original JSON string.
    /// This is the counterpart to CompressJsonToBase64.
    /// 
    /// <example>
    /// <code>
    /// // Assuming we have a Base64 encoded compressed JSON string
    /// string base64Compressed = GetBase64CompressedJson();
    /// 
    /// // Decode and decompress
    /// string originalJson = base64Compressed.DecompressJsonFromBase64(JsonCompressionMethod.GZip);
    /// 
    /// // Now we can use the original JSON
    /// Console.WriteLine(originalJson);
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="base64Compressed">The Base64 encoded compressed JSON data.</param>
    /// <param name="method">The compression method that was used.</param>
    /// <returns>The original JSON string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when base64Compressed is null.</exception>
    /// <exception cref="FormatException">Thrown when the input is not valid Base64.</exception>
    /// <exception cref="JsonCompressionException">Thrown when decompression fails.</exception>
    public static string DecompressJsonFromBase64(this string base64Compressed, JsonCompressionMethod method)
    {
        using var performance = new PerformanceTracker(Logger, nameof(DecompressJsonFromBase64));
        
        // Validate input
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(base64Compressed, nameof(base64Compressed));
        
        return ExceptionHelpers.SafeExecute(() => 
        {
            Logger.LogDebug("Decompressing Base64 string of length {Length} using {Method}", 
                base64Compressed.Length, method);
                
            byte[] compressed = Convert.FromBase64String(base64Compressed);
            string json = DecompressJson(compressed, method);
            
            Logger.LogDebug("Successfully decompressed Base64 to JSON string of length {JsonLength} characters", 
                json.Length);
                
            return json;
        },
        (ex, msg) => {
            if (ex is FormatException)
                return new JsonLibException("The input string is not a valid Base64 encoded string", ex);
            return new JsonCompressionException($"Failed to decompress JSON from Base64 using {method}: {msg}", ex);
        },
        "Failed to decompress JSON from Base64") ?? string.Empty;
    }
}