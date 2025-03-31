# AnotherJsonLib.Utility.Security

## Overview

The `AnotherJsonLib.Utility.Security` namespace provides security mechanisms for JSON data, focusing on encryption, decryption, and digital signatures. This namespace contains components designed to ensure data confidentiality and integrity when working with JSON.

## Key Components

### JsonEncryption

A utility class that provides methods for encrypting and decrypting JSON data.

#### Usage Examples

```csharp
using AnotherJsonLib.Utility.Security;

// Encrypting JSON data
string sensitiveJson = "{\"creditCard\":\"4111-1111-1111-1111\",\"cvv\":\"123\"}";
string encryptionKey = "YourSecureEncryptionKey";
string encryptedJson = JsonEncryption.Encrypt(sensitiveJson, encryptionKey);

// Decrypting JSON data
string decryptedJson = JsonEncryption.Decrypt(encryptedJson, encryptionKey);
```

### JsonSigner

A utility class that provides methods for signing JSON data and verifying signatures to ensure data integrity.

#### Usage Examples

```csharp
using AnotherJsonLib.Utility.Security;

// Signing JSON data
string jsonData = "{\"amount\":1000,\"recipient\":\"John Doe\"}";
string privateKey = "YourPrivateSigningKey";
string signedJson = JsonSigner.Sign(jsonData, privateKey);

// Verifying signature
string publicKey = "YourPublicVerificationKey";
bool isValid = JsonSigner.VerifySignature(signedJson, publicKey);
```

## Best Practices

### Secure Key Management

1. **Never hardcode encryption keys** in your source code
2. **Use secure key storage** mechanisms like Azure Key Vault or AWS KMS
3. **Rotate keys periodically** following your organization's security policy
4. **Use appropriate key lengths** - at least 256 bits for symmetric encryption

```csharp
// Bad practice - hardcoded key
string key = "MyHardcodedKey123";

// Good practice - retrieve from secure storage
string key = await keyVaultClient.GetSecretAsync("encryption-key-url");
```

### Error Handling

Always implement proper exception handling when working with encryption and signing:

```csharp
using AnotherJsonLib.Utility.Security;
using AnotherJsonLib.Exceptions;

try {
    string decryptedJson = JsonEncryption.Decrypt(encryptedData, encryptionKey);
    // Process decrypted data
}
catch (JsonEncryptionException ex) {
    // Handle decryption errors specifically
    logger.Error("Failed to decrypt JSON data", ex);
}
catch (Exception ex) {
    // Handle other unexpected errors
    logger.Error("Unexpected error during decryption", ex);
}
```

### Data Validation

Always validate the structure and content of decrypted JSON before using it:

```csharp
string decryptedJson = JsonEncryption.Decrypt(encryptedData, encryptionKey);

// Validate the JSON structure before using
if (JsonValidator.IsValid(decryptedJson, jsonSchema)) {
    // Process validated JSON data
} else {
    // Handle invalid data
    logger.Warning("Decrypted JSON failed validation");
}
```

### Performance Considerations

1. **Cache decryption results** when appropriate to avoid repeated decryption
2. **Consider encryption granularity** - encrypt only sensitive fields rather than entire objects when possible
3. **Use asynchronous methods** for encryption/decryption operations when processing large JSON documents

```csharp
// Encrypt only specific sensitive fields
var jsonObject = JsonDocument.Parse(jsonData);
var sensitiveField = jsonObject.RootElement.GetProperty("sensitiveData").GetString();
var encryptedField = JsonEncryption.Encrypt(sensitiveField, encryptionKey);

// Update the original document with encrypted value
// ...
```

## Security Notes

1. The `JsonEncryption` class uses industry-standard encryption algorithms to protect data
2. Digital signatures provided by `JsonSigner` ensure data integrity and authenticity
3. Always use HTTPS when transmitting encrypted data or signatures
4. Consider implementing additional security measures such as rate limiting and audit logging

## Related Components

The namespace works in conjunction with the `AnotherJsonLib.Exceptions` namespace, which includes the `JsonEncryptionException` class for handling encryption-specific errors.

This documentation provides a comprehensive overview of the `AnotherJsonLib.Utility.Security` namespace, including usage examples and best practices for securely handling JSON data in your applications.