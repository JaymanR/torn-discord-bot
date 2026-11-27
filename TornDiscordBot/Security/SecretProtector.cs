using System.Security.Cryptography;
using System.Text;

namespace TornDiscordBot.Security;

/// <summary>
/// Encrypts small secrets (API keys) with AES-256-GCM.
/// Blob layout: nonce (12) | tag (16) | ciphertext.
/// The owner id is bound in as associated data, so a blob copied onto another user's row fails to decrypt.
/// </summary>
public sealed class SecretProtector
{
    private const int KeySize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] _masterKey;

    /// <summary>Creates a protector from a raw master key.</summary>
    /// <param name="masterKey">The 32-byte AES key.</param>
    /// <exception cref="ArgumentException">The key is not 32 bytes.</exception>
    public SecretProtector(byte[] masterKey)
    {
        if (masterKey.Length != KeySize)
            throw new ArgumentException($"Master key must be exactly {KeySize} bytes.", nameof(masterKey));

        _masterKey = masterKey;
    }

    /// <summary>Creates a protector from a base64 master key, as produced by <c>openssl rand -base64 32</c>.</summary>
    /// <param name="base64Key">The base64-encoded 32-byte key.</param>
    /// <returns>A protector using the decoded key.</returns>
    /// <exception cref="InvalidOperationException">The value is not valid base64 or not 32 bytes.</exception>
    public static SecretProtector FromBase64(string base64Key)
    {
        byte[] key;
        try
        {
            key = Convert.FromBase64String(base64Key.Trim());
        }
        catch (FormatException)
        {
            throw new InvalidOperationException(
                "Security master key is not valid base64. Generate one with: openssl rand -base64 32");
        }

        if (key.Length != KeySize)
            throw new InvalidOperationException(
                $"Security master key must decode to {KeySize} bytes (got {key.Length}). " +
                "Generate one with: openssl rand -base64 32");

        return new SecretProtector(key);
    }

    /// <summary>Encrypts a secret for a specific owner.</summary>
    /// <param name="plaintext">The secret to encrypt.</param>
    /// <param name="ownerId">The owner the ciphertext is bound to.</param>
    /// <returns>The encrypted blob.</returns>
    public byte[] Protect(string plaintext, ulong ownerId)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var data = Encoding.UTF8.GetBytes(plaintext);
        var cipher = new byte[data.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_masterKey, TagSize);
        aes.Encrypt(nonce, data, cipher, tag, AssociatedData(ownerId));

        var blob = new byte[NonceSize + TagSize + cipher.Length];
        nonce.CopyTo(blob, 0);
        tag.CopyTo(blob, NonceSize);
        cipher.CopyTo(blob, NonceSize + TagSize);
        return blob;
    }

    /// <summary>Decrypts a blob created by <see cref="Protect"/>.</summary>
    /// <param name="blob">The encrypted blob.</param>
    /// <param name="ownerId">The owner the blob was encrypted for.</param>
    /// <returns>The original secret.</returns>
    /// <exception cref="CryptographicException">The blob is corrupt, was tampered with, belongs to another owner, or the master key changed.</exception>
    public string Unprotect(byte[] blob, ulong ownerId)
    {
        if (blob.Length < NonceSize + TagSize)
            throw new CryptographicException("Encrypted blob is too short.");

        var nonce = blob.AsSpan(0, NonceSize);
        var tag = blob.AsSpan(NonceSize, TagSize);
        var cipher = blob.AsSpan(NonceSize + TagSize);
        var plain = new byte[cipher.Length];

        using var aes = new AesGcm(_masterKey, TagSize);
        aes.Decrypt(nonce, cipher, tag, plain, AssociatedData(ownerId));
        return Encoding.UTF8.GetString(plain);
    }

    /// <summary>Converts an owner ID into the associated data authenticated with the ciphertext.</summary>
    /// <param name="ownerId">The owner of the secret.</param>
    /// <returns>The ID as bytes.</returns>
    private static byte[] AssociatedData(ulong ownerId) => BitConverter.GetBytes(ownerId);
}
