namespace AnotherJsonLib.Domain;

/// <summary>
/// Represents the available methods for compressing JSON data.
/// </summary>
public enum JsonCompressionMethod
{
    /// <summary>
    /// Specifies the GZip compression method for compressing JSON data.
    /// </summary>
    /// <remarks>
    /// GZip is a widely used compression format that provides a good balance between
    /// compression efficiency and resource usage. It works by compressing data into
    /// a single compressed stream using the GZip algorithm, which is built upon the
    /// Deflate compression method.
    /// </remarks>
    GZip,

    /// <summary>
    /// Specifies the Deflate compression method for compressing JSON data.
    /// </summary>
    /// <remarks>
    /// Deflate is a compression algorithm that efficiently compresses data
    /// by using a combination of the LZ77 algorithm and Huffman coding.
    /// It provides a lightweight and widely supported option for data compression,
    /// making it suitable for scenarios where compatibility and performance are important.
    /// </remarks>
    Deflate,

    /// <summary>
    /// Specifies the Brotli compression method for compressing JSON data.
    /// </summary>
    /// <remarks>
    /// Brotli is a modern and highly efficient compression format offering superior
    /// compression ratios compared to other methods like GZip and Deflate. It is
    /// particularly suitable for scenarios requiring efficient data transmission while
    /// minimizing storage and bandwidth requirements.
    /// </remarks>
    Brotli
}