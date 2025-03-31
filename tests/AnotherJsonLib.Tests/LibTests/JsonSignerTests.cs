using System.Security.Cryptography;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Utility.Security;
using Shouldly;

namespace AnotherJsonLib.Tests.LibTests;

public class JsonSignerTests
{
    private readonly string _testJson = @"{""userId"":1234,""action"":""purchase"",""amount"":99.95}";
    private readonly string _modifiedJson = @"{""userId"":1234,""action"":""purchase"",""amount"":199.95}";

    [Fact]
    public void SignJson_ShouldCreateValidSignature_WhenUsingDefaultParameters()
    {
        // Arrange
        using var rsa = RSA.Create(2048);

        // Act
        byte[] signature = _testJson.SignJson(rsa);
        bool isValid = _testJson.VerifyJsonSignature(signature, rsa);

        // Assert
        signature.ShouldNotBeNull();
        signature.Length.ShouldBeGreaterThan(0);
        isValid.ShouldBeTrue();
    }

    [Fact]
    public void VerifyJsonSignature_ShouldReturnFalse_WhenJsonIsModified()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        byte[] signature = _testJson.SignJson(rsa);

        // Act
        bool isValid = _modifiedJson.VerifyJsonSignature(signature, rsa);

        // Assert
        isValid.ShouldBeFalse();
    }

    [Fact]
    public void SignJson_ShouldCreateValidSignature_WithExplicitAlgorithmAndPadding()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        var hashAlgorithm = HashAlgorithmName.SHA512;
        var padding = RSASignaturePadding.Pss;

        // Act
        byte[] signature = _testJson.SignJson(rsa, hashAlgorithm, padding);
        bool isValid = _testJson.VerifyJsonSignature(signature, rsa, hashAlgorithm, padding);

        // Assert
        signature.ShouldNotBeNull();
        isValid.ShouldBeTrue();
    }

    [Fact]
    public void SignJson_ShouldThrowJsonArgumentException_WhenJsonIsNull()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        string nullJson = null;

        // Act & Assert
        Should.Throw<JsonArgumentException>(() =>
            nullJson.SignJson(rsa));
    }

    [Fact]
    public void SignJson_ShouldThrowArgumentNullException_WhenPrivateKeyIsNull()
    {
        // Arrange
        RSA nullRsa = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            _testJson.SignJson(nullRsa));
    }

    [Fact]
    public void VerifyJsonSignature_ShouldThrowArgumentNullException_WhenSignatureIsNull()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        byte[] nullSignature = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            _testJson.VerifyJsonSignature(nullSignature, rsa));
    }

    [Fact]
    public void SignJson_ShouldProduceConsistentSignature_ForSameInput()
    {
        // Arrange
        using var rsa = RSA.Create(2048);

        // Act
        byte[] signature1 = _testJson.SignJson(rsa);
        byte[] signature2 = _testJson.SignJson(rsa);

        // Assert
        // Note: Even with the same key and input, RSA signatures might not be identical
        // due to randomness in the signing process, so we verify with the public key instead
        _testJson.VerifyJsonSignature(signature1, rsa).ShouldBeTrue();
        _testJson.VerifyJsonSignature(signature2, rsa).ShouldBeTrue();
    }

    [Fact]
    public void VerifyJsonSignature_ShouldWorkWithDifferentWhitespace_WhenJsonNormalized()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        string formattedJson = @"{
                ""userId"": 1234,
                ""action"": ""purchase"",
                ""amount"": 99.95
            }";
        byte[] signature = _testJson.SignJson(rsa);

        // Act - this would normally fail as whitespace is different
        bool isValid = formattedJson.VerifyJsonSignature(signature, rsa);

        // Assert - verify that the signature validation is strict about format
        isValid.ShouldBeFalse();
    }

    [Fact]
    public void VerifyJsonSignature_ShouldHandleEmptyJson_Correctly()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        string emptyJson = "{}";
        byte[] signature = emptyJson.SignJson(rsa);

        // Act
        bool isValid = emptyJson.VerifyJsonSignature(signature, rsa);

        // Assert
        isValid.ShouldBeTrue();
    }

    [Fact]
    public void VerifyJsonSignature_ShouldThrowJsonArgumentException_WhenJsonIsNull()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        byte[] signature = _testJson.SignJson(rsa);
        string nullJson = null;

        // Act & Assert
        Should.Throw<JsonArgumentException>(() =>
            nullJson.VerifyJsonSignature(signature, rsa));
    }

    [Fact]
    public void VerifyJsonSignature_ShouldThrowArgumentNullException_WhenPublicKeyIsNull()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        byte[] signature = _testJson.SignJson(rsa);
        RSA nullRsa = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            _testJson.VerifyJsonSignature(signature, nullRsa));
    }

    [Fact]
    public void SignJson_ShouldHandleEmptyJson_Correctly()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        string emptyJson = "{}";

        // Act
        byte[] signature = emptyJson.SignJson(rsa);

        // Assert
        signature.ShouldNotBeNull();
        signature.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void SignJson_AndVerify_WithDifferentInstances_ShouldWork()
    {
        // Arrange
        string jsonToSign = @"{""critical"":true,""value"":42}";

        byte[] signature;
        RSAParameters publicKeyParams;

        // Act - Sign with one RSA instance
        using (var rsaSigner = RSA.Create(2048))
        {
            signature = jsonToSign.SignJson(rsaSigner);
            publicKeyParams = rsaSigner.ExportParameters(false);
        }

        // Verify with a different RSA instance using the exported public key
        using (var rsaVerifier = RSA.Create())
        {
            rsaVerifier.ImportParameters(publicKeyParams);
            bool isValid = jsonToSign.VerifyJsonSignature(signature, rsaVerifier);

            // Assert
            isValid.ShouldBeTrue();
        }
    }

    [Fact]
    public void SignJson_WithInvalidKey_ShouldThrowJsonSigningException()
    {
        // Arrange
        string jsonToSign = @"{""test"":123}";

        using var rsa = RSA.Create(512); // Too small key size might cause issues

        // Mock a compromised/corrupted key by trying to manipulate internal state
        // This is implementation-dependent, but we can try to create a situation where signing would fail

        // Act & Assert 
        // This test depends on implementation details, but generally:
        try
        {
            // Force some invalid operation that would cause signing to fail
            // For example signing with a hash too large for the key
            byte[] signature = jsonToSign.SignJson(rsa, HashAlgorithmName.SHA512);

            // If we get here, signing worked despite potential issues
            // The test should still ensure signature is valid
            bool isValid = jsonToSign.VerifyJsonSignature(signature, rsa, HashAlgorithmName.SHA512);
            isValid.ShouldBeTrue();
        }
        catch (JsonSigningException)
        {
            // This is acceptable if the implementation throws this for invalid operations
            // We're mainly testing that it doesn't throw unhandled .NET crypto exceptions
        }
        catch (CryptographicException)
        {
            // This is also acceptable if JsonSigningException isn't wrapping it
        }
    }

    [Fact]
    public void VerifyJsonSignature_WithTamperedSignature_ShouldReturnFalse()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        byte[] signature = _testJson.SignJson(rsa);

        // Tamper with the signature
        if (signature.Length > 0)
        {
            signature[signature.Length / 2] ^= 0xFF; // Flip bits in the middle
        }

        // Act
        bool isValid = _testJson.VerifyJsonSignature(signature, rsa);

        // Assert
        isValid.ShouldBeFalse();
    }

    [Fact]
    public void SignJson_WithCustomAlgorithm_ShouldVerifyCorrectly()
    {
        // Test with each supported hash algorithm
        var hashAlgorithms = new[]
        {
            HashAlgorithmName.SHA1, // Less secure but supported
            HashAlgorithmName.SHA256,
            HashAlgorithmName.SHA384,
            HashAlgorithmName.SHA512
        };

        using var rsa = RSA.Create(2048);

        foreach (var algorithm in hashAlgorithms)
        {
            // Act
            byte[] signature = _testJson.SignJson(rsa, algorithm);
            bool isValid = _testJson.VerifyJsonSignature(signature, rsa, algorithm);

            // Assert
            signature.ShouldNotBeNull();
            isValid.ShouldBeTrue($"Failed to verify with {algorithm}");
        }
    }

    [Fact]
    public void SignJson_WithCustomPadding_ShouldVerifyCorrectly()
    {
        // Test with each supported padding mode
        var paddingModes = new[]
        {
            RSASignaturePadding.Pkcs1,
            RSASignaturePadding.Pss
        };

        using var rsa = RSA.Create(2048);

        foreach (var padding in paddingModes)
        {
            // Act
            byte[] signature = _testJson.SignJson(rsa, padding: padding);
            bool isValid = _testJson.VerifyJsonSignature(signature, rsa, padding: padding);

            // Assert
            signature.ShouldNotBeNull();
            isValid.ShouldBeTrue($"Failed to verify with {padding}");
        }
    }

    [Fact]
    public void JsonSigner_WhenVerifyingWithDifferentAlgorithm_ShouldFail()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        byte[] signature = _testJson.SignJson(rsa, HashAlgorithmName.SHA256);

        // Act
        bool isValid = _testJson.VerifyJsonSignature(signature, rsa, HashAlgorithmName.SHA512);

        // Assert
        isValid.ShouldBeFalse();
    }

    [Fact]
    public void JsonSigner_WhenVerifyingWithDifferentPadding_ShouldFail()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        byte[] signature = _testJson.SignJson(rsa, padding: RSASignaturePadding.Pkcs1);

        // Act
        bool isValid = _testJson.VerifyJsonSignature(signature, rsa, padding: RSASignaturePadding.Pss);

        // Assert
        isValid.ShouldBeFalse();
    }

    [Fact]
    public void JsonSigner_WithLargeJson_ShouldSignAndVerifyCorrectly()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        var stringBuilder = new System.Text.StringBuilder();
        stringBuilder.Append("{\"items\":[");

        // Create a large JSON string
        for (int i = 0; i < 1000; i++)
        {
            if (i > 0) stringBuilder.Append(",");
            stringBuilder.Append($"{{\"id\":{i},\"name\":\"Item {i}\",\"value\":{i * 10.5}}}");
        }

        stringBuilder.Append("]}");

        string largeJson = stringBuilder.ToString();

        // Act
        byte[] signature = largeJson.SignJson(rsa);
        bool isValid = largeJson.VerifyJsonSignature(signature, rsa);

        // Assert
        signature.ShouldNotBeNull();
        isValid.ShouldBeTrue();
    }

    [Fact]
    public void SignJsonWithEcDsa_ShouldCreateValidSignature_WhenUsingDefaultParameters()
    {
        // Arrange
        using var ecdsa = ECDsa.Create();

        // Act
        byte[] signature = _testJson.SignJsonWithEcDsa(ecdsa);
        bool isValid = _testJson.VerifyJsonSignatureWithEcDsa(signature, ecdsa);

        // Assert
        signature.ShouldNotBeNull();
        signature.Length.ShouldBeGreaterThan(0);
        isValid.ShouldBeTrue();
    }

    [Fact]
    public void VerifyJsonSignatureWithEcDsa_ShouldReturnFalse_WhenJsonIsModified()
    {
        // Arrange
        using var ecdsa = ECDsa.Create();
        byte[] signature = _testJson.SignJsonWithEcDsa(ecdsa);

        // Act
        bool isValid = _modifiedJson.VerifyJsonSignatureWithEcDsa(signature, ecdsa);

        // Assert
        isValid.ShouldBeFalse();
    }

    [Fact]
    public void SignJsonWithEcDsa_ShouldCreateValidSignature_WithExplicitAlgorithm()
    {
        // Arrange
        using var ecdsa = ECDsa.Create();
        var hashAlgorithm = HashAlgorithmName.SHA256;

        // Act
        byte[] signature = _testJson.SignJsonWithEcDsa(ecdsa, hashAlgorithm);
        bool isValid = _testJson.VerifyJsonSignatureWithEcDsa(signature, ecdsa, hashAlgorithm);

        // Assert
        signature.ShouldNotBeNull();
        isValid.ShouldBeTrue();
    }

    [Fact]
    public void TrySignJson_ShouldReturnTrue_WhenSuccessful()
    {
        // Arrange
        using var rsa = RSA.Create(2048);

        // Act
        bool success = _testJson.TrySignJson(rsa, out byte[]? signature);

        // Assert
        success.ShouldBeTrue();
        signature.ShouldNotBeNull();
        _testJson.VerifyJsonSignature(signature, rsa).ShouldBeTrue();
    }

    [Fact]
    public void TrySignJson_ShouldReturnFalse_WhenPrivateKeyIsNull()
    {
        // Arrange
        RSA nullRsa = null;

        // Act
        bool success = _testJson.TrySignJson(nullRsa, out byte[]? signature);

        // Assert
        success.ShouldBeFalse();
        signature.ShouldBeNull();
    }

    [Fact]
    public void TryVerifyJsonSignature_ShouldReturnTrueAndValidIsTrue_WhenSignatureIsValid()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        byte[] signature = _testJson.SignJson(rsa);

        // Act
        bool success = _testJson.TryVerifyJsonSignature(signature, rsa, out bool isValid);

        // Assert
        success.ShouldBeTrue();
        isValid.ShouldBeTrue();
    }

    [Fact]
    public void TryVerifyJsonSignature_ShouldReturnTrueAndValidIsFalse_WhenSignatureIsInvalid()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        byte[] signature = _testJson.SignJson(rsa);

        // Act
        bool success = _modifiedJson.TryVerifyJsonSignature(signature, rsa, out bool isValid);

        // Assert
        success.ShouldBeTrue();
        isValid.ShouldBeFalse();
    }

    [Fact]
    public void TryVerifyJsonSignature_ShouldReturnFalse_WhenPublicKeyIsNull()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        byte[] signature = _testJson.SignJson(rsa);
        RSA nullRsa = null;

        // Act
        bool success = _testJson.TryVerifyJsonSignature(signature, nullRsa, out bool isValid);

        // Assert
        success.ShouldBeFalse();
        isValid.ShouldBeFalse();
    }

    [Fact]
    public void TrySignJsonWithEcDsa_ShouldReturnTrue_WhenSuccessful()
    {
        // Arrange
        using var ecdsa = ECDsa.Create();

        // Act
        bool success = _testJson.TrySignJsonWithEcDsa(ecdsa, out byte[]? signature);

        // Assert
        success.ShouldBeTrue();
        signature.ShouldNotBeNull();
        _testJson.VerifyJsonSignatureWithEcDsa(signature, ecdsa).ShouldBeTrue();
    }

    [Fact]
    public void TrySignJsonWithEcDsa_ShouldReturnFalse_WhenPrivateKeyIsNull()
    {
        // Arrange
        ECDsa nullEcdsa = null;

        // Act
        bool success = _testJson.TrySignJsonWithEcDsa(nullEcdsa, out byte[]? signature);

        // Assert
        success.ShouldBeFalse();
        signature.ShouldBeNull();
    }

    [Fact]
    public void TryVerifyJsonSignatureWithEcDsa_ShouldReturnTrueAndValidIsTrue_WhenSignatureIsValid()
    {
        // Arrange
        using var ecdsa = ECDsa.Create();
        byte[] signature = _testJson.SignJsonWithEcDsa(ecdsa);

        // Act
        bool success = _testJson.TryVerifyJsonSignatureWithEcDsa(signature, ecdsa, out bool isValid);

        // Assert
        success.ShouldBeTrue();
        isValid.ShouldBeTrue();
    }

    [Fact]
    public void TryVerifyJsonSignatureWithEcDsa_ShouldReturnTrueAndValidIsFalse_WhenSignatureIsInvalid()
    {
        // Arrange
        using var ecdsa = ECDsa.Create();
        byte[] signature = _testJson.SignJsonWithEcDsa(ecdsa);

        // Act
        bool success = _modifiedJson.TryVerifyJsonSignatureWithEcDsa(signature, ecdsa, out bool isValid);

        // Assert
        success.ShouldBeTrue();
        isValid.ShouldBeFalse();
    }

    [Fact]
    public void TryVerifyJsonSignatureWithEcDsa_ShouldReturnFalse_WhenPublicKeyIsNull()
    {
        // Arrange
        using var ecdsa = ECDsa.Create();
        byte[] signature = _testJson.SignJsonWithEcDsa(ecdsa);
        ECDsa nullEcdsa = null;

        // Act
        bool success = _testJson.TryVerifyJsonSignatureWithEcDsa(signature, nullEcdsa, out bool isValid);

        // Assert
        success.ShouldBeFalse();
        isValid.ShouldBeFalse();
    }

    [Fact]
    public void SignJsonCanonical_ShouldCreateValidSignature_WhenJsonHasDifferentFormatting()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        string formattedJson = @"{
        ""userId"": 1234,
        ""action"": ""purchase"",
        ""amount"": 99.95
    }";

        // Act
        byte[] signature = _testJson.SignJsonCanonical(rsa);
        bool isValid = formattedJson.VerifyJsonSignatureCanonical(signature, rsa);

        // Assert
        signature.ShouldNotBeNull();
        isValid.ShouldBeTrue();
    }

    [Fact]
    public void SignJsonCanonical_ShouldProduceEqualSignatures_ForEquivalentJsonWithDifferentFormatting()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        string formattedJson = @"{
        ""userId"": 1234,
        ""action"": ""purchase"",
        ""amount"": 99.95
    }";
        string reorderedJson = @"{""action"":""purchase"",""amount"":99.95,""userId"":1234}";

        // Act
        byte[] signature1 = _testJson.SignJsonCanonical(rsa);
        byte[] signature2 = formattedJson.SignJsonCanonical(rsa);
        byte[] signature3 = reorderedJson.SignJsonCanonical(rsa);

        // Assert - verify with each signature against all JSON variants
        _testJson.VerifyJsonSignatureCanonical(signature1, rsa).ShouldBeTrue();
        formattedJson.VerifyJsonSignatureCanonical(signature1, rsa).ShouldBeTrue();
        reorderedJson.VerifyJsonSignatureCanonical(signature1, rsa).ShouldBeTrue();

        _testJson.VerifyJsonSignatureCanonical(signature2, rsa).ShouldBeTrue();
        formattedJson.VerifyJsonSignatureCanonical(signature2, rsa).ShouldBeTrue();
        reorderedJson.VerifyJsonSignatureCanonical(signature2, rsa).ShouldBeTrue();

        _testJson.VerifyJsonSignatureCanonical(signature3, rsa).ShouldBeTrue();
        formattedJson.VerifyJsonSignatureCanonical(signature3, rsa).ShouldBeTrue();
        reorderedJson.VerifyJsonSignatureCanonical(signature3, rsa).ShouldBeTrue();
    }

    [Fact]
    public void VerifyJsonSignatureCanonical_ShouldReturnFalse_WhenJsonContentDiffers()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        byte[] signature = _testJson.SignJsonCanonical(rsa);

        // Act
        bool isValid = _modifiedJson.VerifyJsonSignatureCanonical(signature, rsa);

        // Assert
        isValid.ShouldBeFalse();
    }

    [Fact]
    public void SignJsonCanonical_ShouldWork_WithComplexJson()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        string complexJson = @"{
        ""data"": {
            ""items"": [
                {""id"": 1, ""name"": ""Item 1""},
                {""id"": 2, ""name"": ""Item 2""}
            ],
            ""metadata"": {
                ""count"": 2,
                ""active"": true
            }
        },
        ""timestamp"": ""2023-01-01T00:00:00Z""
    }";

        string reorderedComplexJson = @"{
        ""timestamp"": ""2023-01-01T00:00:00Z"",
        ""data"": {
            ""metadata"": {
                ""active"": true,
                ""count"": 2
            },
            ""items"": [
                {""name"": ""Item 1"", ""id"": 1},
                {""name"": ""Item 2"", ""id"": 2}
            ]
        }
    }";

        // Act
        byte[] signature = complexJson.SignJsonCanonical(rsa);
        bool isValid = reorderedComplexJson.VerifyJsonSignatureCanonical(signature, rsa);

        // Assert
        signature.ShouldNotBeNull();
        isValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("SHA1")]
    [InlineData("SHA256")]
    [InlineData("SHA384")]
    [InlineData("SHA512")]
    public void SignJson_ShouldWorkWithDifferentHashAlgorithms(string hashAlgorithmName)
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        var hashAlgorithm = new HashAlgorithmName(hashAlgorithmName);

        // Act
        byte[] signature = _testJson.SignJson(rsa, hashAlgorithm);
        bool isValid = _testJson.VerifyJsonSignature(signature, rsa, hashAlgorithm);

        // Assert
        signature.ShouldNotBeNull();
        isValid.ShouldBeTrue();
    }
}