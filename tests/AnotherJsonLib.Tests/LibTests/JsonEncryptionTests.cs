using System.Text;
using AnotherJsonLib.Exceptions;
using AnotherJsonLib.Utility.Security;
using Shouldly;

namespace AnotherJsonLib.Tests.LibTests;

public class JsonEncryptionTests
    {
        [Fact]
        public void EncryptDecrypt_RoundTrip_ShouldReturnOriginalJson()
        {
            // Arrange
            string originalJson = "{\"message\":\"Hello, world!\",\"value\":42}";
            // 32-byte key for AES-256
            byte[] key = Encoding.UTF8.GetBytes("0123456789ABCDEF0123456789ABCDEF");
            // 16-byte IV (AES block size)
            byte[] iv = Encoding.UTF8.GetBytes("ABCDEF0123456789");

            // Act
            string encrypted = originalJson.EncryptJson(key, iv);
            string decrypted = encrypted.DecryptJson(key, iv);

            // Assert: the decrypted JSON should match the original.
            decrypted.ShouldBe(originalJson);
        }

        [Fact]
        public void Encrypt_NullInput_ShouldThrowArgumentNullException()
        {
            // Arrange
            string json = null!;
            byte[] key = Encoding.UTF8.GetBytes("0123456789ABCDEF0123456789ABCDEF");
            byte[] iv = Encoding.UTF8.GetBytes("ABCDEF0123456789");

            // Act & Assert: passing a null JSON should throw.
            Should.Throw<ArgumentNullException>(() => json.EncryptJson(key, iv));
        }

        [Fact]
        public void Decrypt_InvalidKey_ShouldThrowJsonEncryptionException()
        {
            // Arrange
            string originalJson = "{\"data\":\"test\"}";
            byte[] key1 = Encoding.UTF8.GetBytes("0123456789ABCDEF0123456789ABCDEF");
            byte[] key2 = Encoding.UTF8.GetBytes("FEDCBA9876543210FEDCBA9876543210"); // different key
            byte[] iv = Encoding.UTF8.GetBytes("ABCDEF0123456789");
            string encrypted = originalJson.EncryptJson(key1, iv);

            // Act & Assert: decryption with the wrong key should fail.
            Should.Throw<JsonEncryptionException>(() => encrypted.DecryptJson(key2, iv));
        }

        [Fact]
        public void Decrypt_CorruptedCiphertext_ShouldThrowJsonEncryptionException()
        {
            // Arrange
            string originalJson = "{\"status\":\"ok\"}";
            byte[] key = Encoding.UTF8.GetBytes("0123456789ABCDEF0123456789ABCDEF");
            byte[] iv = Encoding.UTF8.GetBytes("ABCDEF0123456789");
            string encrypted = originalJson.EncryptJson(key, iv);
            // Corrupt the ciphertext (e.g., remove the last character)
            string corrupted = encrypted.Substring(0, encrypted.Length - 1);

            // Act & Assert: decryption of corrupted ciphertext should throw.
            Should.Throw<JsonEncryptionException>(() => corrupted.DecryptJson(key, iv));
        }
    }