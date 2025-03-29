using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Infra;
using AnotherJsonLib.Utility.Formatting;
using Microsoft.Extensions.Logging;

namespace AnotherJsonLib.Utility.Security;

/// <summary>
/// Provides functionality for digitally signing and verifying JSON data for security and tamper protection.
/// 
/// Digital signatures ensure data integrity and authenticity by cryptographically signing JSON content, 
/// allowing verification that the data hasn't been modified since it was signed by the authorized party.
/// 
/// Use this utility when:
/// - You need to verify that JSON data hasn't been tampered with
/// - You want to ensure JSON data authenticity
/// - You're implementing secure data exchange using JSON
/// - You need to create verifiable audit trails with JSON data
/// 
/// Security considerations:
/// - Keep private keys secure and never expose them in client-side code
/// - Use appropriate key lengths (e.g., RSA 2048 bits or higher) for adequate security
/// - Consider key rotation policies for long-term systems
/// - For production systems, consider using a dedicated security library or service
/// - Be aware that the signature applies to the exact JSON string - even whitespace differences will invalidate it
/// 
/// <example>
/// <code>
/// // Example: Sign and verify JSON data
/// string jsonData = @"{""userId"":1234,""action"":""purchase"",""amount"":99.95}";
/// 
/// // Generate a key pair (in practice, you'd use a stored key)
/// using (RSA rsa = RSA.Create(2048))
/// {
///     // Sign the JSON
///     byte[] signature = jsonData.SignJson(rsa);
///     
///     // Later, verify the signature
///     bool isValid = jsonData.VerifyJsonSignature(signature, rsa);
///     // isValid will be true if the JSON hasn't been modified
///     
///     // If the data is modified...
///     string modifiedJson = @"{""userId"":1234,""action"":""purchase"",""amount"":199.95}";
///     bool isStillValid = modifiedJson.VerifyJsonSignature(signature, rsa);
///     // isStillValid will be false
/// }
/// </code>
/// </example>
/// </summary>
public static class JsonSigner
{
    private static readonly ILogger Logger = JsonLoggerFactory.Instance.GetLogger(nameof(JsonSigner));
    
    /// <summary>
    /// Default hash algorithm used for signatures when not specified.
    /// </summary>
    public static readonly HashAlgorithmName DefaultHashAlgorithm = HashAlgorithmName.SHA256;

    /// <summary>
    /// Signs a JSON string using the provided RSA private key.
    /// The method computes a cryptographic signature that can later be verified to ensure data integrity.
    /// 
    /// <example>
    /// <code>
    /// // Create a simple JSON object
    /// string jsonData = @"{""id"":""A12345"",""status"":""active""}";
    /// 
    /// // Sign the JSON using an RSA key
    /// using (RSA rsa = RSA.Create(2048))
    /// {
    ///     byte[] signature = jsonData.SignJson(rsa);
    ///     
    ///     // Typically, you would store or transmit both the JSON and its signature
    ///     string base64Signature = Convert.ToBase64String(signature);
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="json">The JSON string to sign.</param>
    /// <param name="privateKey">The RSA private key used for signing.</param>
    /// <param name="hashAlgorithm">The hash algorithm to use (defaults to SHA256).</param>
    /// <param name="padding">The RSA signature padding mode (defaults to PKCS#1 v1.5).</param>
    /// <returns>A byte array containing the digital signature.</returns>
    /// <exception cref="ArgumentNullException">Thrown when json or privateKey is null.</exception>
    /// <exception cref="JsonSigningException">Thrown when the signing operation fails.</exception>
    public static byte[] SignJson(this string json, RSA privateKey, HashAlgorithmName? hashAlgorithm = null, RSASignaturePadding? padding = null)
    {
        using var performance = new PerformanceTracker(Logger, nameof(SignJson));
        
        // Validate inputs
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(json, nameof(json));
        ExceptionHelpers.ThrowIfNull(privateKey, nameof(privateKey));
        
        // Use default algorithms if not specified
        hashAlgorithm ??= DefaultHashAlgorithm;
        padding ??= RSASignaturePadding.Pkcs1;

        return ExceptionHelpers.SafeExecute(() =>
            {
                Logger.LogDebug("Signing JSON string of length {Length} using {Algorithm} hash algorithm", 
                    json.Length, hashAlgorithm?.Name);
                
                byte[] data = Encoding.UTF8.GetBytes(json);
                Debug.Assert(hashAlgorithm != null, nameof(hashAlgorithm) + " != null");
                byte[] signature = privateKey.SignData(data, hashAlgorithm.Value, padding);
            
                Logger.LogDebug("Successfully created signature of length {SignatureLength} bytes", signature.Length);
                return signature;
            },
            (ex, msg) => new JsonSigningException($"Failed to sign JSON data: {msg}", ex),
            "Error signing JSON data") ?? Array.Empty<byte>();
    }

    /// <summary>
    /// Verifies a digital signature for a JSON string using the provided RSA public key.
    /// Returns true if the signature is valid, indicating the JSON hasn't been modified since signing.
    /// 
    /// <example>
    /// <code>
    /// // Assume we have the original JSON and its signature
    /// string jsonData = @"{""id"":""A12345"",""status"":""active""}";
    /// byte[] signature = Convert.FromBase64String("ABCDEf123..."); // Actual signature bytes
    /// 
    /// // Verify using the public key
    /// using (RSA rsa = RSA.Create())
    /// {
    ///     // In practice, you would load the public key from a secure source
    ///     rsa.ImportParameters(publicKeyParams);
    ///     
    ///     bool isValid = jsonData.VerifyJsonSignature(signature, rsa);
    ///     
    ///     if (isValid)
    ///         Console.WriteLine("Signature is valid - JSON is authentic and unmodified");
    ///     else
    ///         Console.WriteLine("Invalid signature - JSON may have been tampered with");
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="json">The JSON string whose signature should be verified.</param>
    /// <param name="signature">The signature byte array.</param>
    /// <param name="publicKey">The RSA public key used for verification.</param>
    /// <param name="hashAlgorithm">The hash algorithm used (must match the one used for signing).</param>
    /// <param name="padding">The RSA signature padding mode (must match the one used for signing).</param>
    /// <returns>True if the signature is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
    /// <exception cref="ArgumentException">Thrown when signature is empty.</exception>
    /// <exception cref="JsonSigningException">Thrown when the verification operation fails unexpectedly.</exception>
    public static bool VerifyJsonSignature(
        this string json, 
        byte[] signature, 
        RSA publicKey, 
        HashAlgorithmName? hashAlgorithm = null, 
        RSASignaturePadding? padding = null)
    {
        using var performance = new PerformanceTracker(Logger, nameof(VerifyJsonSignature));
        
        // Validate inputs
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(json, nameof(json));
        ExceptionHelpers.ThrowIfNull(signature, nameof(signature));
        ExceptionHelpers.ThrowIfFalse(signature.Length > 0, "Signature cannot be empty", nameof(signature));
        ExceptionHelpers.ThrowIfNull(publicKey, nameof(publicKey));
        
        // Use default algorithms if not specified
        hashAlgorithm ??= DefaultHashAlgorithm;
        padding ??= RSASignaturePadding.Pkcs1;

        return ExceptionHelpers.SafeExecute(() =>
        {
            Logger.LogDebug("Verifying JSON signature for string of length {Length} using {Algorithm}", 
                json.Length, hashAlgorithm?.Name);
                
            byte[] data = Encoding.UTF8.GetBytes(json);
            Debug.Assert(hashAlgorithm != null, nameof(hashAlgorithm) + " != null");
            bool isValid = publicKey.VerifyData(data, signature, hashAlgorithm.Value, padding);
            
            Logger.LogDebug("Signature verification result: {Result}", isValid ? "Valid" : "Invalid");
            return isValid;
        },
        (ex, msg) => new JsonSigningException($"Failed to verify JSON signature: {msg}", ex),
        "Error verifying JSON signature");
    }
    
    /// <summary>
    /// Signs a JSON string using the ECDSA private key.
    /// ECDSA (Elliptic Curve Digital Signature Algorithm) typically produces smaller signatures 
    /// than RSA while providing comparable security.
    /// 
    /// <example>
    /// <code>
    /// // Sign with ECDSA
    /// string jsonData = @"{""transactionId"":""T123"",""amount"":500}";
    /// 
    /// using (ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256))
    /// {
    ///     byte[] signature = jsonData.SignJsonWithECDsa(ecdsa);
    ///     // Signature is typically smaller than RSA for similar security levels
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="json">The JSON string to sign.</param>
    /// <param name="privateKey">The ECDsa private key used for signing.</param>
    /// <param name="hashAlgorithm">The hash algorithm to use (defaults to SHA256).</param>
    /// <returns>A byte array containing the digital signature.</returns>
    /// <exception cref="ArgumentNullException">Thrown when json or privateKey is null.</exception>
    /// <exception cref="JsonSigningException">Thrown when the signing operation fails.</exception>
    public static byte[] SignJsonWithEcDsa(this string json, ECDsa privateKey, HashAlgorithmName? hashAlgorithm = null)
    {
        using var performance = new PerformanceTracker(Logger, nameof(SignJsonWithEcDsa));
        
        // Validate inputs
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(json, nameof(json));
        ExceptionHelpers.ThrowIfNull(privateKey, nameof(privateKey));
        
        // Use default hash algorithm if not specified
        hashAlgorithm ??= DefaultHashAlgorithm;

        return ExceptionHelpers.SafeExecute(() =>
            {
                Logger.LogDebug("Signing JSON string of length {Length} using ECDsa with {Algorithm}", 
                    json.Length, hashAlgorithm?.Name);
                
                byte[] data = Encoding.UTF8.GetBytes(json);
                Debug.Assert(hashAlgorithm != null, nameof(hashAlgorithm) + " != null");
                byte[] signature = privateKey.SignData(data, hashAlgorithm.Value);
            
                Logger.LogDebug("Successfully created ECDsa signature of length {SignatureLength} bytes", signature.Length);
                return signature;
            },
            (ex, msg) => new JsonSigningException($"Failed to sign JSON data with ECDsa: {msg}", ex),
            "Error signing JSON data with ECDsa") ?? Array.Empty<byte>();
    }
    
    /// <summary>
    /// Verifies a digital signature for a JSON string using an ECDsa public key.
    /// 
    /// <example>
    /// <code>
    /// // Verify with ECDSA
    /// string jsonData = @"{""transactionId"":""T123"",""amount"":500}";
    /// byte[] signature = ...; // Previously generated signature
    /// 
    /// using (ECDsa ecdsa = ECDsa.Create())
    /// {
    ///     // Load the public key parameters
    ///     ecdsa.ImportParameters(publicKeyParams);
    ///     
    ///     bool isValid = jsonData.VerifyJsonSignatureWithECDsa(signature, ecdsa);
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="json">The JSON string whose signature should be verified.</param>
    /// <param name="signature">The signature byte array.</param>
    /// <param name="publicKey">The ECDsa public key used for verification.</param>
    /// <param name="hashAlgorithm">The hash algorithm used (must match the one used for signing).</param>
    /// <returns>True if the signature is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
    /// <exception cref="ArgumentException">Thrown when signature is empty.</exception>
    /// <exception cref="JsonSigningException">Thrown when the verification operation fails unexpectedly.</exception>
    public static bool VerifyJsonSignatureWithEcDsa(
        this string json, 
        byte[] signature, 
        ECDsa publicKey, 
        HashAlgorithmName? hashAlgorithm = null)
    {
        using var performance = new PerformanceTracker(Logger, nameof(VerifyJsonSignatureWithEcDsa));
        
        // Validate inputs
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(json, nameof(json));
        ExceptionHelpers.ThrowIfNull(signature, nameof(signature));
        ExceptionHelpers.ThrowIfFalse(signature.Length > 0, "Signature cannot be empty", nameof(signature));
        ExceptionHelpers.ThrowIfNull(publicKey, nameof(publicKey));
        
        // Use default hash algorithm if not specified
        hashAlgorithm ??= DefaultHashAlgorithm;

        return ExceptionHelpers.SafeExecute(() =>
        {
            Logger.LogDebug("Verifying ECDsa signature for JSON string of length {Length} using {Algorithm}", 
                json.Length, hashAlgorithm?.Name);
                
            byte[] data = Encoding.UTF8.GetBytes(json);
            Debug.Assert(hashAlgorithm != null, nameof(hashAlgorithm) + " != null");
            bool isValid = publicKey.VerifyData(data, signature, hashAlgorithm.Value);
            
            Logger.LogDebug("ECDsa signature verification result: {Result}", isValid ? "Valid" : "Invalid");
            return isValid;
        },
        (ex, msg) => new JsonSigningException($"Failed to verify JSON ECDsa signature: {msg}", ex),
        "Error verifying JSON ECDsa signature");
    }
    
    /// <summary>
    /// Attempts to sign a JSON string using an RSA private key without throwing exceptions.
    /// </summary>
    /// <param name="json">The JSON string to sign.</param>
    /// <param name="privateKey">The RSA private key used for signing.</param>
    /// <param name="signature">When successful, contains the signature; otherwise, null.</param>
    /// <param name="hashAlgorithm">The hash algorithm to use (defaults to SHA256).</param>
    /// <param name="padding">The RSA signature padding mode (defaults to PKCS#1 v1.5).</param>
    /// <returns>True if signing was successful; otherwise, false.</returns>
    public static bool TrySignJson(
        this string json, 
        RSA? privateKey, 
        out byte[]? signature, 
        HashAlgorithmName? hashAlgorithm = null, 
        RSASignaturePadding? padding = null)
    {
        if (string.IsNullOrWhiteSpace(json) || privateKey == null)
        {
            signature = null;
            return false;
        }
        
        try
        {
            signature = SignJson(json, privateKey, hashAlgorithm, padding);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Error signing JSON data");
            signature = null;
            return false;
        }
    }
    
    /// <summary>
    /// Attempts to verify a JSON signature using an RSA public key without throwing exceptions.
    /// </summary>
    /// <param name="json">The JSON string whose signature should be verified.</param>
    /// <param name="signature">The signature byte array.</param>
    /// <param name="publicKey">The RSA public key used for verification.</param>
    /// <param name="isValid">When this method returns, contains true if the signature is valid; otherwise, false.</param>
    /// <param name="hashAlgorithm">The hash algorithm used (must match the one used for signing).</param>
    /// <param name="padding">The RSA signature padding mode (must match the one used for signing).</param>
    /// <returns>True if verification was performed; false if parameters were invalid or an error occurred.</returns>
    public static bool TryVerifyJsonSignature(
        this string json, 
        byte[]? signature, 
        RSA? publicKey, 
        out bool isValid, 
        HashAlgorithmName? hashAlgorithm = null, 
        RSASignaturePadding? padding = null)
    {
        isValid = false;
        
        if (string.IsNullOrWhiteSpace(json) || signature == null || signature.Length == 0 || publicKey == null)
        {
            return false;
        }
        
        try
        {
            isValid = VerifyJsonSignature(json, signature, publicKey, hashAlgorithm, padding);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Error verifying JSON signature");
            return false;
        }
    }
    
    /// <summary>
    /// Attempts to sign a JSON string using an ECDsa private key without throwing exceptions.
    /// </summary>
    /// <param name="json">The JSON string to sign.</param>
    /// <param name="privateKey">The ECDsa private key used for signing.</param>
    /// <param name="signature">When successful, contains the signature; otherwise, null.</param>
    /// <param name="hashAlgorithm">The hash algorithm to use (defaults to SHA256).</param>
    /// <returns>True if signing was successful; otherwise, false.</returns>
    public static bool TrySignJsonWithEcDsa(
        this string json, 
        ECDsa? privateKey, 
        out byte[]? signature, 
        HashAlgorithmName? hashAlgorithm = null)
    {
        if (string.IsNullOrWhiteSpace(json) || privateKey == null)
        {
            signature = null;
            return false;
        }
        
        try
        {
            signature = SignJsonWithEcDsa(json, privateKey, hashAlgorithm);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Error signing JSON data with ECDsa");
            signature = null;
            return false;
        }
    }
    
    /// <summary>
    /// Attempts to verify a JSON signature using an ECDsa public key without throwing exceptions.
    /// </summary>
    /// <param name="json">The JSON string whose signature should be verified.</param>
    /// <param name="signature">The signature byte array.</param>
    /// <param name="publicKey">The ECDsa public key used for verification.</param>
    /// <param name="isValid">When this method returns, contains true if the signature is valid; otherwise, false.</param>
    /// <param name="hashAlgorithm">The hash algorithm used (must match the one used for signing).</param>
    /// <returns>True if verification was performed; false if parameters were invalid or an error occurred.</returns>
    public static bool TryVerifyJsonSignatureWithEcDsa(
        this string json, 
        byte[]? signature, 
        ECDsa? publicKey, 
        out bool isValid, 
        HashAlgorithmName? hashAlgorithm = null)
    {
        isValid = false;
        
        if (string.IsNullOrWhiteSpace(json) || signature == null || signature.Length == 0 || publicKey == null)
        {
            return false;
        }
        
        try
        {
            isValid = VerifyJsonSignatureWithEcDsa(json, signature, publicKey, hashAlgorithm);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Error verifying JSON ECDsa signature");
            return false;
        }
    }
    
    /// <summary>
    /// Creates a canonical representation of the JSON before signing to ensure consistent signatures.
    /// This is useful when the JSON might have different formatting or property ordering.
    /// 
    /// <example>
    /// <code>
    /// // These JSONs are equivalent but formatted differently
    /// string json1 = @"{""user"":""john"",""role"":""admin""}";
    /// string json2 = @"{
    ///   ""role"": ""admin"",
    ///   ""user"": ""john""
    /// }";
    /// 
    /// // Sign consistently regardless of formatting
    /// using (RSA rsa = RSA.Create(2048))
    /// {
    ///     // Both will produce the same signature
    ///     byte[] sig1 = json1.SignJsonCanonical(rsa);
    ///     byte[] sig2 = json2.SignJsonCanonical(rsa);
    ///     
    ///     // sig1 and sig2 will be identical
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="json">The JSON string to canonicalize and sign.</param>
    /// <param name="privateKey">The RSA private key used for signing.</param>
    /// <param name="hashAlgorithm">The hash algorithm to use (defaults to SHA256).</param>
    /// <param name="padding">The RSA signature padding mode (defaults to PKCS#1 v1.5).</param>
    /// <returns>A byte array containing the digital signature of the canonicalized JSON.</returns>
    /// <exception cref="ArgumentNullException">Thrown when json or privateKey is null.</exception>
    /// <exception cref="JsonSigningException">Thrown when the signing operation fails.</exception>
    /// <exception cref="JsonCanonicalizationException">Thrown when the canonicalization fails.</exception>
    public static byte[] SignJsonCanonical(
        this string json, 
        RSA privateKey, 
        HashAlgorithmName? hashAlgorithm = null, 
        RSASignaturePadding? padding = null)
    {
        using var performance = new PerformanceTracker(Logger, nameof(SignJsonCanonical));
        
        // Validate inputs
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(json, nameof(json));
        ExceptionHelpers.ThrowIfNull(privateKey, nameof(privateKey));

        return ExceptionHelpers.SafeExecute(() =>
            {
                Logger.LogDebug("Canonicalizing and signing JSON string of length {Length}", json.Length);
            
                // Canonicalize the JSON first
                string canonicalJson = JsonCanonicalizer.Canonicalize(json);
            
                // Then sign it
                return SignJson(canonicalJson, privateKey, hashAlgorithm, padding);
            },
            (ex, msg) => {
                if (ex is JsonCanonicalizationException)
                    return (ex as JsonCanonicalizationException)!;
                return new JsonSigningException($"Failed to sign canonicalized JSON: {msg}", ex);
            },
            "Error signing canonicalized JSON")!;
    }
    
    /// <summary>
    /// Verifies a signature against a canonical representation of the JSON.
    /// This ensures consistent verification regardless of formatting differences.
    /// 
    /// <example>
    /// <code>
    /// // Original signed JSON
    /// string originalJson = @"{""user"":""john"",""role"":""admin""}";
    /// byte[] signature = ...; // Previously generated signature
    /// 
    /// // Formatted version of the same JSON (different order, spacing)
    /// string formattedJson = @"{
    ///   ""role"": ""admin"",
    ///   ""user"": ""john""
    /// }";
    /// 
    /// using (RSA rsa = RSA.Create())
    /// {
    ///     // Load public key
    ///     rsa.ImportParameters(publicKeyParams);
    ///     
    ///     // Both will verify successfully with the same signature
    ///     bool isValid1 = originalJson.VerifyJsonSignatureCanonical(signature, rsa);
    ///     bool isValid2 = formattedJson.VerifyJsonSignatureCanonical(signature, rsa);
    ///     
    ///     // isValid1 and isValid2 will both be true
    /// }
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="json">The JSON string to canonicalize and verify.</param>
    /// <param name="signature">The signature byte array.</param>
    /// <param name="publicKey">The RSA public key used for verification.</param>
    /// <param name="hashAlgorithm">The hash algorithm used (must match the one used for signing).</param>
    /// <param name="padding">The RSA signature padding mode (must match the one used for signing).</param>
    /// <returns>True if the signature is valid for the canonicalized JSON; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
    /// <exception cref="ArgumentException">Thrown when signature is empty.</exception>
    /// <exception cref="JsonSigningException">Thrown when the verification operation fails unexpectedly.</exception>
    /// <exception cref="JsonCanonicalizationException">Thrown when the canonicalization fails.</exception>
    public static bool VerifyJsonSignatureCanonical(
        this string json, 
        byte[] signature, 
        RSA publicKey, 
        HashAlgorithmName? hashAlgorithm = null, 
        RSASignaturePadding? padding = null)
    {
        using var performance = new PerformanceTracker(Logger, nameof(VerifyJsonSignatureCanonical));
        
        // Validate inputs
        ExceptionHelpers.ThrowIfNullOrWhiteSpace(json, nameof(json));
        ExceptionHelpers.ThrowIfNull(signature, nameof(signature));
        ExceptionHelpers.ThrowIfFalse(signature.Length > 0, "Signature cannot be empty", nameof(signature));
        ExceptionHelpers.ThrowIfNull(publicKey, nameof(publicKey));

        return ExceptionHelpers.SafeExecute(() =>
        {
            Logger.LogDebug("Canonicalizing and verifying JSON string of length {Length}", json.Length);
            
            // Canonicalize the JSON first
            string canonicalJson = JsonCanonicalizer.Canonicalize(json);
            
            // Then verify it
            return VerifyJsonSignature(canonicalJson, signature, publicKey, hashAlgorithm, padding);
        },
        (ex, msg) => {
            if (ex is JsonCanonicalizationException)
                return (ex as JsonCanonicalizationException)!;
            return new JsonSigningException($"Failed to verify canonicalized JSON signature: {msg}", ex);
        },
        "Error verifying canonicalized JSON signature");
    }
}