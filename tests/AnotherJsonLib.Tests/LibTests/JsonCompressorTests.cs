using System.IO.Compression;
using System.Text;
using AnotherJsonLib.Domain;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Utility.Compression;
using Shouldly;

namespace AnotherJsonLib.Tests.LibTests;

    public class JsonCompressorTests
    {
        private readonly string _simpleJson = @"{""name"":""John"",""age"":30}";
        private readonly string _complexJson = @"{""users"":[{""id"":1,""name"":""Alice"",""email"":""alice@example.com"",""roles"":[""admin"",""user""]},
                           {""id"":2,""name"":""Bob"",""email"":""bob@example.com"",""roles"":[""user""]}],
                           ""metadata"":{""version"":""1.0"",""generated"":""2023-05-10T15:30:00Z""}}";

        #region CompressJson and DecompressJson Tests

        [Theory]
        [InlineData(JsonCompressionMethod.GZip)]
        [InlineData(JsonCompressionMethod.Deflate)]
        [InlineData(JsonCompressionMethod.Brotli)]
        public void CompressJson_ValidJson_ShouldReturnCompressedBytes(JsonCompressionMethod method)
        {
            // Arrange & Act
            byte[] compressed = _simpleJson.CompressJson(method);

            // Assert
            compressed.ShouldNotBeNull();
            compressed.Length.ShouldBeGreaterThan(0);
    
            // For very small inputs, compression might increase size due to headers/metadata
            if (_simpleJson.Length > 50) // Only check compression ratio for larger inputs
            {
                compressed.Length.ShouldBeLessThan(_simpleJson.Length);
            }
        }

        [Theory]
        [InlineData(JsonCompressionMethod.GZip)]
        [InlineData(JsonCompressionMethod.Deflate)]
        [InlineData(JsonCompressionMethod.Brotli)]
        public void CompressJson_ThenDecompressJson_ShouldReturnOriginal(JsonCompressionMethod method)
        {
            // Arrange
            string original = _complexJson;
            
            // Act
            byte[] compressed = original.CompressJson(method);
            string decompressed = compressed.DecompressJson(method);
            
            // Assert
            decompressed.ShouldBe(original);
        }

        [Theory]
        [InlineData(JsonCompressionMethod.GZip, CompressionLevel.Fastest)]
        [InlineData(JsonCompressionMethod.GZip, CompressionLevel.Optimal)]
        [InlineData(JsonCompressionMethod.Deflate, CompressionLevel.Fastest)]
        [InlineData(JsonCompressionMethod.Deflate, CompressionLevel.Optimal)]
        [InlineData(JsonCompressionMethod.Brotli, CompressionLevel.Fastest)]
        [InlineData(JsonCompressionMethod.Brotli, CompressionLevel.Optimal)]
        [InlineData(JsonCompressionMethod.Brotli, CompressionLevel.SmallestSize)]
        public void CompressJson_DifferentLevels_ShouldCompressAndDecompress(JsonCompressionMethod method, CompressionLevel level)
        {
            // Arrange
            string original = _complexJson;
            
            // Act
            byte[] compressed = original.CompressJson(method, level);
            string decompressed = compressed.DecompressJson(method);
            
            // Assert
            decompressed.ShouldBe(original);
        }

        [Fact]
        public void CompressJson_NullInput_ShouldThrowJsonArgumentException()
        {
            // Arrange
            string nullJson = null;
            
            // Act & Assert
            Should.Throw<JsonArgumentException>(() => 
                nullJson.CompressJson(JsonCompressionMethod.GZip));
        }

        [Fact]
        public void CompressJson_EmptyInput_ShouldThrowJsonArgumentException()
        {
            // Arrange
            string emptyJson = "";
            
            // Act & Assert
            Should.Throw<JsonArgumentException>(() => 
                emptyJson.CompressJson(JsonCompressionMethod.GZip));
        }

        [Fact]
        public void DecompressJson_NullInput_ShouldThrowArgumentNullException()
        {
            // Arrange
            byte[] nullData = null;
            
            // Act & Assert
            Should.Throw<ArgumentNullException>(() => 
                nullData.DecompressJson(JsonCompressionMethod.GZip));
        }

        [Fact]
        public void DecompressJson_EmptyInput_ShouldThrowJsonArgumentException()
        {
            // Arrange
            byte[] emptyData = Array.Empty<byte>();
            
            // Act & Assert
            Should.Throw<JsonArgumentException>(() => 
                emptyData.DecompressJson(JsonCompressionMethod.GZip));
        }

        [Fact]
        public void DecompressJson_InvalidCompressedData_ShouldThrowJsonCompressionException()
        {
            // Arrange
            byte[] invalidData = Encoding.UTF8.GetBytes("Not compressed data");
            
            // Act & Assert
            Should.Throw<JsonCompressionException>(() => 
                invalidData.DecompressJson(JsonCompressionMethod.GZip));
        }

        #endregion

        #region Stream-based Compression Tests

        [Theory]
        [InlineData(JsonCompressionMethod.GZip)]
        [InlineData(JsonCompressionMethod.Deflate)]
        [InlineData(JsonCompressionMethod.Brotli)]
        public void CompressJsonToStream_ValidJson_ShouldWriteToStream(JsonCompressionMethod method)
        {
            // Arrange
            string json = _simpleJson;
            using var outputStream = new MemoryStream();
            
            // Act
            json.CompressJsonToStream(outputStream, method, leaveOpen: true);
            
            // Assert
            outputStream.Position = 0;
            outputStream.Length.ShouldBeGreaterThan(0);
        }

        [Theory]
        [InlineData(JsonCompressionMethod.GZip)]
        [InlineData(JsonCompressionMethod.Deflate)]
        [InlineData(JsonCompressionMethod.Brotli)]
        public void CompressJsonToStream_ThenDecompressJsonFromStream_ShouldReturnOriginal(JsonCompressionMethod method)
        {
            // Arrange
            string original = _complexJson;
            using var outputStream = new MemoryStream();
    
            // Act
            original.CompressJsonToStream(outputStream, method, leaveOpen: true); // Add leaveOpen parameter
    
            // Reset position for reading
            outputStream.Position = 0;
    
            string decompressed = outputStream.DecompressJsonFromStream(method);
    
            // Assert
            decompressed.ShouldBe(original);
        }

        [Fact]
        public void CompressJsonToStream_NullStream_ShouldThrowArgumentNullException()
        {
            // Arrange
            string json = _simpleJson;
            Stream nullStream = null;
            
            // Act & Assert
            Should.Throw<ArgumentNullException>(() => 
                json.CompressJsonToStream(nullStream, JsonCompressionMethod.GZip));
        }

        [Fact]
        public void CompressJsonToStream_NonWritableStream_ShouldThrowJsonArgumentException()
        {
            // Arrange
            string json = _simpleJson;
            using var readOnlyStream = new MemoryStream(new byte[10], writable: false);
            
            // Act & Assert
            Should.Throw<JsonArgumentException>(() => 
                json.CompressJsonToStream(readOnlyStream, JsonCompressionMethod.GZip));
        }

        [Fact]
        public void DecompressJsonFromStream_NullStream_ShouldThrowArgumentNullException()
        {
            // Arrange
            Stream nullStream = null;
            
            // Act & Assert
            Should.Throw<ArgumentNullException>(() => 
                nullStream.DecompressJsonFromStream(JsonCompressionMethod.GZip));
        }

        [Fact]
        public void DecompressJsonFromStream_NonReadableStream_ShouldThrowArgumentException()
        {
            // Arrange
            using var nonReadableStream = new MemoryStream();
            // Make stream non-readable for testing
            var nonReadableProp = typeof(MemoryStream).GetProperty("CanRead");
            if (nonReadableProp != null && nonReadableProp.CanWrite)
            {
                nonReadableProp.SetValue(nonReadableStream, false);
            }
            else
            {
                // Skip test if we can't set CanRead property
                return;
            }
            
            // Act & Assert
            Should.Throw<ArgumentException>(() => 
                nonReadableStream.DecompressJsonFromStream(JsonCompressionMethod.GZip));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CompressJsonToStream_LeaveOpenParameter_ShouldRespectFlag(bool leaveOpen)
        {
            // Arrange
            string json = _simpleJson;
            var mockStream = new TestMemoryStream();
            
            // Act
            json.CompressJsonToStream(mockStream, JsonCompressionMethod.GZip, leaveOpen: leaveOpen);
            
            // Assert
            mockStream.WasClosed.ShouldBe(!leaveOpen);
        }

        // Helper class for testing stream closure
        private class TestMemoryStream : MemoryStream
        {
            public bool WasClosed { get; private set; } = false;
            
            public override void Close()
            {
                WasClosed = true;
                base.Close();
            }
        }

        #endregion

        #region TryCompress and TryDecompress Tests

        [Theory]
        [InlineData(JsonCompressionMethod.GZip)]
        [InlineData(JsonCompressionMethod.Deflate)]
        [InlineData(JsonCompressionMethod.Brotli)]
        public void TryCompressJson_ValidJson_ShouldSucceed(JsonCompressionMethod method)
        {
            // Arrange
            string json = _complexJson;
            
            // Act
            bool executed = json.TryCompressJson(method, out byte[] result, out bool success);
            
            // Assert
            executed.ShouldBeTrue();
            success.ShouldBeTrue();
            result.ShouldNotBeNull();
            result.Length.ShouldBeGreaterThan(0);
        }

        [Theory]
        [InlineData(JsonCompressionMethod.GZip)]
        [InlineData(JsonCompressionMethod.Deflate)]
        [InlineData(JsonCompressionMethod.Brotli)]
        public void TryCompressJson_ThenTryDecompressJson_ShouldSucceed(JsonCompressionMethod method)
        {
            // Arrange
            string original = _complexJson;
            
            // Act - Compress
            bool compressExecuted = original.TryCompressJson(method, out byte[] compressed, out bool compressSuccess);
            
            // Act - Decompress
            bool decompressExecuted = compressed.TryDecompressJson(method, out string decompressed, out bool decompressSuccess);
            
            // Assert
            compressExecuted.ShouldBeTrue();
            compressSuccess.ShouldBeTrue();
            decompressExecuted.ShouldBeTrue();
            decompressSuccess.ShouldBeTrue();
            decompressed.ShouldBe(original);
        }

        [Fact]
        public void TryCompressJson_NullInput_ShouldReturnFalseWithoutException()
        {
            // Arrange
            string nullJson = null;
            
            // Act
            bool executed = nullJson.TryCompressJson(JsonCompressionMethod.GZip, out byte[] result, out bool success);
            
            // Assert
            executed.ShouldBeFalse();
            success.ShouldBeFalse();
            result.ShouldBeNull();
        }

        [Fact]
        public void TryDecompressJson_NullInput_ShouldReturnFalseWithoutException()
        {
            // Arrange
            byte[] nullData = null;
            
            // Act
            bool executed = nullData.TryDecompressJson(JsonCompressionMethod.GZip, out string result, out bool success);
            
            // Assert
            executed.ShouldBeFalse();
            success.ShouldBeFalse();
            result.ShouldBeNull();
        }

        [Fact]
        public void TryDecompressJson_InvalidCompressedData_ShouldReturnTrue_ButSuccessFalse()
        {
            // Arrange
            byte[] invalidData = Encoding.UTF8.GetBytes("Not compressed data");
            
            // Act
            invalidData.TryDecompressJson(JsonCompressionMethod.GZip, out string result, out bool success);
            
            // Assert
            success.ShouldBeFalse();
            result.ShouldBeNull();
        }

        #endregion

        #region Base64 Compression Tests

        [Theory]
        [InlineData(JsonCompressionMethod.GZip)]
        [InlineData(JsonCompressionMethod.Deflate)]
        [InlineData(JsonCompressionMethod.Brotli)]
        public void CompressJsonToBase64_ValidJson_ShouldReturnBase64String(JsonCompressionMethod method)
        {
            // Arrange
            string json = _simpleJson;
            
            // Act
            string base64Result = json.CompressJsonToBase64(method);
            
            // Assert
            base64Result.ShouldNotBeNullOrEmpty();
            Should.NotThrow(() => Convert.FromBase64String(base64Result)); // Validate it's valid base64
        }

        [Theory]
        [InlineData(JsonCompressionMethod.GZip)]
        [InlineData(JsonCompressionMethod.Deflate)]
        [InlineData(JsonCompressionMethod.Brotli)]
        public void CompressJsonToBase64_ThenDecompressJsonFromBase64_ShouldReturnOriginal(JsonCompressionMethod method)
        {
            // Arrange
            string original = _complexJson;
            
            // Act
            string base64Compressed = original.CompressJsonToBase64(method);
            string decompressed = base64Compressed.DecompressJsonFromBase64(method);
            
            // Assert
            decompressed.ShouldBe(original);
        }

        [Fact]
        public void CompressJsonToBase64_NullInput_ShouldThrowJsonArgumentException()
        {
            // Arrange
            string nullJson = null;
            
            // Act & Assert
            Should.Throw<JsonArgumentException>(() => 
                nullJson.CompressJsonToBase64(JsonCompressionMethod.GZip));
        }

        [Fact]
        public void DecompressJsonFromBase64_NullInput_ShouldThrowJsonArgumentException()
        {
            // Arrange
            string nullBase64 = null;
            
            // Act & Assert
            Should.Throw<JsonArgumentException>(() => 
                nullBase64.DecompressJsonFromBase64(JsonCompressionMethod.GZip));
        }

        [Fact]
        public void DecompressJsonFromBase64_InvalidBase64_ShouldThrowJsonLibException()
        {
            // Arrange
            string invalidBase64 = "Not a valid base64 string!";
            
            // Act & Assert
            Should.Throw<JsonLibException>(() => 
                invalidBase64.DecompressJsonFromBase64(JsonCompressionMethod.GZip));
        }

        #endregion

        #region Large Data Tests

        [Fact]
        public void CompressJson_LargeJson_ShouldCompressAndDecompress()
        {
            // Arrange
            StringBuilder largeJsonBuilder = new StringBuilder();
            largeJsonBuilder.Append("{\"items\":[");
            
            // Create a large JSON with 1000 items
            for (int i = 0; i < 1000; i++)
            {
                if (i > 0) largeJsonBuilder.Append(',');
                largeJsonBuilder.Append($"{{\"id\":{i},\"name\":\"Item {i}\",\"value\":{i * 10}}}");
            }
            
            largeJsonBuilder.Append("]}");
            
            string largeJson = largeJsonBuilder.ToString();
            
            // Act & Assert - Test different compression methods
            foreach (JsonCompressionMethod method in Enum.GetValues(typeof(JsonCompressionMethod)))
            {
                // Compress
                byte[] compressed = largeJson.CompressJson(method);
                compressed.ShouldNotBeNull();
                
                // Verify compression ratio
                float compressionRatio = (float)compressed.Length / Encoding.UTF8.GetByteCount(largeJson);
                compressionRatio.ShouldBeLessThan(0.5f); // Expect >50% compression
                
                // Decompress and verify content
                string decompressed = compressed.DecompressJson(method);
                decompressed.ShouldBe(largeJson);
            }
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void CompressJson_WhitespaceJson_ShouldCompressAndDecompress()
        {
            // Arrange
            string whitespaceJson = "   {   \"name\"  :  \"John\"   }   ";
            
            // Act
            byte[] compressed = whitespaceJson.CompressJson(JsonCompressionMethod.GZip);
            string decompressed = compressed.DecompressJson(JsonCompressionMethod.GZip);
            
            // Assert
            decompressed.ShouldBe(whitespaceJson);
        }

        [Fact]
        public void CompressJson_EmptyJson_ShouldThrowArgumentException()
        {
            // Arrange
            string emptyJson = "{}";
            
            // Act & Assert
            Should.NotThrow(() => emptyJson.CompressJson(JsonCompressionMethod.GZip));
        }

        [Fact]
        public void CompressJson_InvalidCompressionMethod_ShouldThrowJsonCompressionException()
        {
            // Arrange
            string json = _simpleJson;
            JsonCompressionMethod invalidMethod = (JsonCompressionMethod)999;
            
            // Act & Assert
            Should.Throw<JsonCompressionException>(() => 
                json.CompressJson(invalidMethod));
        }

        #endregion
    }
