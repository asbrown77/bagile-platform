using System.Security.Cryptography;

namespace Bagile.Infrastructure.Configuration;

/// <summary>
/// Decrypts AES-256-GCM values written by the TypeScript PA service.
/// Stored format: {iv_hex}:{ciphertext_hex}:{authtag_hex}
/// Key: 64-char hex string (32 bytes), from PA_ENCRYPTION_KEY env var.
/// </summary>
public static class CredentialDecryptor
{
    public static string Decrypt(string encoded, string hexKey)
    {
        var parts = encoded.Split(':');
        if (parts.Length != 3)
            throw new FormatException($"Expected iv:ciphertext:tag format, got {parts.Length} parts.");

        var iv         = Convert.FromHexString(parts[0]);
        var ciphertext = Convert.FromHexString(parts[1]);
        var tag        = Convert.FromHexString(parts[2]);
        var key        = Convert.FromHexString(hexKey);

        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, tagSizeInBytes: 16);
        aes.Decrypt(iv, ciphertext, tag, plaintext);

        return System.Text.Encoding.UTF8.GetString(plaintext);
    }
}
