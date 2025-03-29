using System.Security.Cryptography;
using System.Text;

namespace AnotherJsonLib.Utility;

public static class JsonSigner
{
    /// <summary>
    /// Signs a JSON string using the provided RSA private key.
    /// Returns the signature as a byte array.
    /// </summary>
    /// <param name="json">The JSON string to sign.</param>
    /// <param name="privateKey">The RSA private key.</param>
    /// <returns>A byte array containing the digital signature.</returns>
    public static byte[] SignJson(this string json, RSA privateKey)
    {
        if (json == null) throw new ArgumentNullException(nameof(json));
        if (privateKey == null) throw new ArgumentNullException(nameof(privateKey));

        byte[] data = Encoding.UTF8.GetBytes(json);
        // Using SHA256 and PKCS#1 v1.5 padding for the signature.
        return privateKey.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    /// <summary>
    /// Verifies a digital signature for a JSON string using the provided RSA public key.
    /// </summary>
    /// <param name="json">The JSON string whose signature should be verified.</param>
    /// <param name="signature">The signature byte array.</param>
    /// <param name="publicKey">The RSA public key.</param>
    /// <returns>True if the signature is valid; otherwise, false.</returns>
    public static bool VerifyJsonSignature(this string json, byte[] signature, RSA publicKey)
    {
        if (json == null) throw new ArgumentNullException(nameof(json));
        if (signature == null || signature.Length == 0) throw new ArgumentNullException(nameof(signature));
        if (publicKey == null) throw new ArgumentNullException(nameof(publicKey));

        byte[] data = Encoding.UTF8.GetBytes(json);
        return publicKey.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }
}