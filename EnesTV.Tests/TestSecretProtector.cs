using System.Security.Cryptography;
using System.Text;
using EnesTV.Core.Services;

namespace EnesTV.Tests;

// Platform-neutral stand-in for DPAPI/Keystore/Keychain: real AES-GCM encryption with a fixed test key, so the
// store tests run anywhere and still prove nothing is written in plain text. Not for production use.
internal sealed class TestSecretProtector : ISecretProtector
{
    public static readonly TestSecretProtector Instance = new();
    private static readonly byte[] Key = SHA256.HashData(Encoding.UTF8.GetBytes("enestv-test-key"));
    public byte[] Protect(string value)
    {
        var plain = Encoding.UTF8.GetBytes(value);
        var output = new byte[12 + 16 + plain.Length]; // nonce | tag | ciphertext
        RandomNumberGenerator.Fill(output.AsSpan(0, 12));
        using var aes = new AesGcm(Key, 16);
        aes.Encrypt(output.AsSpan(0, 12), plain, output.AsSpan(28), output.AsSpan(12, 16));
        return output;
    }
    public string Unprotect(byte[] value)
    {
        var plain = new byte[value.Length - 28];
        using var aes = new AesGcm(Key, 16);
        aes.Decrypt(value.AsSpan(0, 12), value.AsSpan(28), value.AsSpan(12, 16), plain);
        return Encoding.UTF8.GetString(plain);
    }
}
