using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using StockTrading.IServices;

namespace StockTrading.Services;

public sealed class AesStringEncryptionService(IConfiguration configuration) : IStringEncryptionService
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public string Encrypt(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        var plaintext = Encoding.UTF8.GetBytes(value);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(GetKey(), TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var payload = new byte[NonceSize + TagSize + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, payload, NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, payload, NonceSize + TagSize, ciphertext.Length);

        return Convert.ToBase64String(payload);
    }

    public string Decrypt(string encryptedValue)
    {
        if (string.IsNullOrEmpty(encryptedValue))
        {
            return "";
        }

        var payload = Convert.FromBase64String(encryptedValue);
        if (payload.Length < NonceSize + TagSize)
        {
            throw new InvalidOperationException("Encrypted value payload is invalid.");
        }

        var nonce = payload[..NonceSize];
        var tag = payload[NonceSize..(NonceSize + TagSize)];
        var ciphertext = payload[(NonceSize + TagSize)..];
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(GetKey(), TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }

    private byte[] GetKey()
    {
        var key = configuration["BrokerSession:EncryptionKey"];
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("BrokerSession:EncryptionKey is required.");
        }

        return SHA256.HashData(Encoding.UTF8.GetBytes(key));
    }
}
