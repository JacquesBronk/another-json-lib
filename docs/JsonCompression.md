# AnotherJsonLib.Utility.Compression

## Overview

The `AnotherJsonLib.Utility.Compression` namespace provides functionality for compressing and decompressing JSON data using various compression algorithms. It's designed to help reduce the size of JSON data for storage or transmission while preserving all information.

### Key Benefits

- Reduced storage requirements for JSON data
- Faster network transmission with smaller payloads
- Lower bandwidth usage for API responses
- Support for multiple compression algorithms (GZip, Deflate, Brotli)
- Configurable compression levels for size/speed tradeoffs

## Classes

### JsonCompressor

A static class that provides extension methods for compressing and decompressing JSON data.

#### Methods

##### CompressJson

```csharp
public static byte[] CompressJson(this string json, JsonCompressionMethod method, CompressionLevel compressionLevel = CompressionLevel.Optimal)
```

Compresses a JSON string using the specified compression method and level.

**Parameters:**
- `json`: The JSON string to compress.
- `method`: The compression algorithm to use (GZip, Deflate, or Brotli).
- `compressionLevel`: The compression level - Fastest (prioritizes speed), Optimal (balanced), or SmallestSize (Brotli only, maximizes compression).

**Returns:** A byte array containing the compressed JSON data.

**Exceptions:**
- `JsonArgumentException`: Thrown when the input JSON is null or empty.

##### DecompressJson

```csharp
public static string DecompressJson(this byte[] compressedJson, JsonCompressionMethod method)
```

Decompresses previously compressed JSON data back to its original string format.

**Parameters:**
- `compressedJson`: The compressed byte array to decompress.
- `method`: The compression algorithm that was used to compress the data.

**Returns:** The original JSON string.

**Exceptions:**
- `ArgumentNullException`: Thrown when the input compressed data is null.

## Usage Examples

### Basic Compression and Decompression

```csharp
// Compress with GZip (good general-purpose choice)
string jsonContent = @"{""name"":""John"",""age"":30,""address"":{""city"":""New York"",""zip"":""10001""}}";
byte[] gzipCompressed = jsonContent.CompressJson(JsonCompressionMethod.GZip);

// Later, decompress the data when needed
string decompressed = gzipCompressed.DecompressJson(JsonCompressionMethod.GZip);

// Original and decompressed strings will be identical
Console.WriteLine(jsonContent == decompressed);  // True
```

### Choosing Different Compression Algorithms

```csharp
// Compress with GZip (good balance of compression and compatibility)
byte[] gzipCompressed = jsonContent.CompressJson(JsonCompressionMethod.GZip);

// Compress with Deflate (slightly smaller output than GZip but less compatible)
byte[] deflateCompressed = jsonContent.CompressJson(JsonCompressionMethod.Deflate);

// Compress with Brotli (best compression ratio but slower)
byte[] brotliCompressed = jsonContent.CompressJson(JsonCompressionMethod.Brotli);

// Compare sizes
Console.WriteLine($"GZip size: {gzipCompressed.Length}, Deflate size: {deflateCompressed.Length}, Brotli size: {brotliCompressed.Length}");
```

### Using Different Compression Levels

```csharp
// Use Fastest level when speed is more important than size
byte[] fastCompressed = jsonContent.CompressJson(JsonCompressionMethod.GZip, CompressionLevel.Fastest);

// Use Optimal level (default) for balanced compression
byte[] optimalCompressed = jsonContent.CompressJson(JsonCompressionMethod.GZip, CompressionLevel.Optimal);

// Use SmallestSize for maximum compression with Brotli (slower but better compression)
byte[] smallestCompressed = jsonContent.CompressJson(JsonCompressionMethod.Brotli, CompressionLevel.SmallestSize);
```

### Compressing Large JSON Documents

```csharp
// Example: Compress a large JSON object with Brotli for maximum compression
string largeJson = @"{""users"":[{""id"":1,""name"":""Alice"",""email"":""alice@example.com"",""roles"":[""admin"",""user""]},
                     {""id"":2,""name"":""Bob"",""email"":""bob@example.com"",""roles"":[""user""]}],
                     ""metadata"":{""version"":""1.0"",""generated"":""2023-05-10T15:30:00Z""}}";
                   
// Compress with Brotli for best compression ratio
byte[] compressed = largeJson.CompressJson(JsonCompressionMethod.Brotli, CompressionLevel.Optimal);

// Store or transmit the compressed data...

// Then decompress when needed
string decompressed = compressed.DecompressJson(JsonCompressionMethod.Brotli);
```

## Edge Cases and Considerations

1. **Input Validation**:
    - Null or empty JSON strings will throw a `JsonArgumentException`
    - Null compressed data will throw an `ArgumentNullException`

2. **Small Inputs**:
    - For very small JSON inputs (less than 50 bytes), compression might actually increase the size due to compression headers/metadata

3. **Compression Algorithm Compatibility**:
    - GZip: Most widely compatible, supported by most platforms and browsers
    - Deflate: Slightly better compression than GZip but less compatible with some older systems
    - Brotli: Best compression ratio but requires newer systems/browsers that support it

4. **Performance Considerations**:
    - Compression level trade-offs:
        - Fastest: 5-10% larger files but much faster compression
        - Optimal: Good balance of speed and compression
        - SmallestSize (Brotli only): Maximum compression but significantly slower

5. **Memory Usage**:
    - The library uses a default buffer size of 4096 bytes for compression operations
    - For very large JSON documents, consider handling them in smaller chunks if memory usage is a concern

6. **Encoding**:
    - The library uses UTF-8 encoding by default for JSON string conversion

7. **Type Safety**:
    - Always ensure you're using the same compression method for both compression and decompression operations
    - Using a different method to decompress than was used to compress will result in corrupted data