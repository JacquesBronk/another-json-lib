using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Infra;
using Microsoft.Extensions.Logging;

namespace AnotherJsonLib.Utility.Security;

/// <summary>
/// Provides encryption and decryption functionality for JSON strings.
/// </summary>
public static class JsonEncryption
{
    private static readonly ILogger Logger = JsonLoggerFactory.Instance.GetLogger(nameof(JsonEncryption));

    /// <summary>
    /// Encrypts a JSON string using AES symmetric encryption.
    /// Returns a base64-encoded ciphertext.
    /// </summary>
    /// <param name="json">The JSON string to encrypt.</param>
    /// <param name="key">The AES key (e.g., 16, 24, or 32 bytes for AES-128/192/256).</param>
    /// <param name="iv">The initialization vector (IV), typically 16 bytes.</param>
    /// <returns>The encrypted JSON as a base64 string.</returns>
    /// <exception cref="JsonArgumentException">Thrown when input parameters are null or invalid.</exception>
    /// <exception cref="JsonOperationException">Thrown when the encryption operation fails.</exception>
    public static string EncryptJson(this string json, byte[] key, byte[] iv)
    {
        using var performance = new PerformanceTracker(Logger, nameof(EncryptJson));
        
        // Validate inputs
        ExceptionHelpers.ThrowIfNull(json, nameof(json));
        ExceptionHelpers.ThrowIfNull(key, nameof(key));
        ExceptionHelpers.ThrowIfNull(iv, nameof(iv));
        
        ExceptionHelpers.ThrowIfFalse(key.Length > 0, "Encryption key cannot be empty", nameof(key));
        ExceptionHelpers.ThrowIfFalse(iv.Length > 0, "Initialization vector cannot be empty", nameof(iv));
        
        return ExceptionHelpers.SafeExecute(() => 
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(json);
            
            using Aes aes = Aes.Create();
            Debug.Assert(aes != null, nameof(aes) + " != null");
            
            aes.Key = key;
            aes.IV = iv;
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.CBC;
            
            using MemoryStream outputStream = new MemoryStream();
            using (var cryptoStream = new CryptoStream(outputStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                cryptoStream.Write(inputBytes, 0, inputBytes.Length);
                cryptoStream.FlushFinalBlock();
            }
            
            string result = Convert.ToBase64String(outputStream.ToArray());
            Logger.LogDebug("Successfully encrypted JSON string (length: {Length})", json.Length);
            
            return result;
        }, (ex, msg) => 
        {
            if (ex is CryptographicException cryptoEx)
                return new JsonEncryptionException("Failed to encrypt JSON: " + cryptoEx.Message, cryptoEx);
                
            return new JsonOperationException("Failed to encrypt JSON: " + msg, ex);
        }, "Failed to encrypt JSON") ?? string.Empty;
    }
    
    /// <summary>
    /// Decrypts a base64-encoded encrypted JSON string using AES symmetric encryption.
    /// </summary>
    /// <param name="encryptedJson">The base64-encoded encrypted JSON.</param>
    /// <param name="key">The AES key used for decryption.</param>
    /// <param name="iv">The initialization vector (IV) used for decryption.</param>
    /// <returns>The decrypted JSON string.</returns>
    /// <exception cref="JsonArgumentException">Thrown when input parameters are null or invalid.</exception>
    /// <exception cref="JsonOperationException">Thrown when the decryption operation fails.</exception>
    /// <exception cref="JsonEncryptionException">Thrown when there is a cryptographic error during decryption.</exception>
    public static string DecryptJson(this string encryptedJson, byte[] key, byte[] iv)
    {
        using var performance = new PerformanceTracker(Logger, nameof(DecryptJson));
        
        // Validate inputs
        ExceptionHelpers.ThrowIfNull(encryptedJson, nameof(encryptedJson));
        ExceptionHelpers.ThrowIfNull(key, nameof(key));
        ExceptionHelpers.ThrowIfNull(iv, nameof(iv));
        
        ExceptionHelpers.ThrowIfFalse(key.Length > 0, "Encryption key cannot be empty", nameof(key));
        ExceptionHelpers.ThrowIfFalse(iv.Length > 0, "Initialization vector cannot be empty", nameof(iv));
        
        return ExceptionHelpers.SafeExecute(() => 
        {
            try
            {
                byte[] cipherBytes = Convert.FromBase64String(encryptedJson);
                
                using Aes aes = Aes.Create();
                Debug.Assert(aes != null, nameof(aes) + " != null");
                
                aes.Key = key;
                aes.IV = iv;
                aes.Padding = PaddingMode.PKCS7;
                aes.Mode = CipherMode.CBC;
                
                using MemoryStream inputStream = new MemoryStream(cipherBytes);
                using var cryptoStream = new CryptoStream(inputStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
                using var reader = new StreamReader(cryptoStream, Encoding.UTF8);
                
                string result = reader.ReadToEnd();
                Logger.LogDebug("Successfully decrypted JSON string to length: {Length}", result.Length);
                
                return result;
            }
            catch (FormatException fe)
            {
                throw new JsonEncryptionException("The encrypted JSON string is not valid Base64", fe);
            }
        }, (ex, msg) => 
        {
            if (ex is CryptographicException cryptoEx)
                return new JsonEncryptionException("Failed to decrypt JSON. This may indicate incorrect key, IV, or corrupted data: " + cryptoEx.Message, cryptoEx);
            if (ex is JsonEncryptionException exception)
                return exception;
                
            return new JsonOperationException("Failed to decrypt JSON: " + msg, ex);
        }, "Failed to decrypt JSON") ?? string.Empty;
    }
    
    /// <summary>
    /// Attempts to encrypt a JSON string using AES symmetric encryption.
    /// </summary>
    /// <param name="json">The JSON string to encrypt.</param>
    /// <param name="key">The AES key.</param>
    /// <param name="iv">The initialization vector.</param>
    /// <param name="result">When successful, contains the encrypted JSON as a base64 string; otherwise, null.</param>
    /// <returns>True if encryption was successful; otherwise, false.</returns>
    public static bool TryEncryptJson(this string json, byte[] key, byte[] iv, out string? result)
    {
        result = ExceptionHelpers.SafeExecuteWithDefault(
            () => EncryptJson(json, key, iv),
            null,
            "Failed to encrypt JSON"
        );
        
        return result != null;
    }
    
    /// <summary>
    /// Attempts to decrypt a base64-encoded encrypted JSON string.
    /// </summary>
    /// <param name="encryptedJson">The base64-encoded encrypted JSON.</param>
    /// <param name="key">The AES key.</param>
    /// <param name="iv">The initialization vector.</param>
    /// <param name="result">When successful, contains the decrypted JSON string; otherwise, null.</param>
    /// <returns>True if decryption was successful; otherwise, false.</returns>
    public static bool TryDecryptJson(this string encryptedJson, byte[] key, byte[] iv, out string? result)
    {
        result = ExceptionHelpers.SafeExecuteWithDefault(
            () => DecryptJson(encryptedJson, key, iv),
            null,
            "Failed to decrypt JSON"
        );
        
        return result != null;
    }
}
