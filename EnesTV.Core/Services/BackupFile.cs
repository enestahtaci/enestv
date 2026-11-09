using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using EnesTV.Core.Models;

namespace EnesTV.Core.Services;

/// <summary>
/// Passphrase-encrypted .enestv backup file (docs/CORE-SPEC.md, "Yedek"). Platform neutral, so a backup made on
/// Windows can be restored on Android/iOS. Envelope is plain JSON; the library payload is AES-256-GCM encrypted with a
/// key derived from the passphrase by PBKDF2-HMAC-SHA256.
/// </summary>
public static class BackupFile
{
    public const string Format = "enestv-backup";
    public const int Version = 1;
    public const int Iterations = 600_000;
    public const int MinPassphraseLength = 8;
    public const long MaxFileBytes = 64L * 1024 * 1024;
    private static readonly byte[] AssociatedData = Encoding.UTF8.GetBytes("enestv-backup/v1");
    public static readonly JsonSerializerOptions PayloadJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    public static byte[] Encrypt(LibraryBackup backup, string passphrase) => Encrypt(backup, passphrase, Iterations);
    // Iteration count is a parameter only so tests and fixtures can be fast; the app always uses Iterations.
    public static byte[] Encrypt(LibraryBackup backup, string passphrase, int iterations)
    {
        CheckPassphrase(passphrase);
        var salt = RandomNumberGenerator.GetBytes(16); var nonce = RandomNumberGenerator.GetBytes(12);
        var key = DeriveKey(passphrase, salt, iterations);
        var plain = JsonSerializer.SerializeToUtf8Bytes(backup, PayloadJson);
        var data = new byte[plain.Length + 16];
        using (var aes = new AesGcm(key, 16)) aes.Encrypt(nonce, plain, data.AsSpan(0, plain.Length), data.AsSpan(plain.Length), AssociatedData);
        CryptographicOperations.ZeroMemory(key); CryptographicOperations.ZeroMemory(plain);
        var envelope = new JsonObject
        {
            ["format"] = Format, ["version"] = Version, ["created"] = backup.Created.ToString("O"),
            ["kdf"] = new JsonObject { ["name"] = "PBKDF2-HMAC-SHA256", ["iterations"] = iterations, ["salt"] = Convert.ToBase64String(salt) },
            ["cipher"] = new JsonObject { ["name"] = "AES-256-GCM", ["nonce"] = Convert.ToBase64String(nonce), ["tagLength"] = 16 },
            ["data"] = Convert.ToBase64String(data),
        };
        return Encoding.UTF8.GetBytes(envelope.ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
    }

    public static LibraryBackup Decrypt(byte[] file, string passphrase)
    {
        if (file.LongLength > MaxFileBytes) throw new UserInputException("Yedek dosyası çok büyük.");
        JsonNode? envelope; int version;
        try
        {
            envelope = JsonNode.Parse(file);
            if (envelope?["format"]?.GetValue<string>() != Format) throw new UserInputException("Bu dosya bir enestv yedeği değil.");
            version = envelope["version"]?.GetValue<int>() ?? 0;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException) { throw new UserInputException("Bu dosya bir enestv yedeği değil."); }
        if (version is < 1 or > Version) throw new UserInputException("Bu yedek daha yeni bir enestv sürümüyle oluşturulmuş. enestv'yi güncelleyip tekrar deneyin.");
        byte[] salt, nonce, data; int iterations;
        try
        {
            iterations = envelope["kdf"]!["iterations"]!.GetValue<int>();
            salt = Convert.FromBase64String(envelope["kdf"]!["salt"]!.GetValue<string>());
            nonce = Convert.FromBase64String(envelope["cipher"]!["nonce"]!.GetValue<string>());
            data = Convert.FromBase64String(envelope["data"]!.GetValue<string>());
        }
        catch (Exception ex) when (ex is NullReferenceException or FormatException or InvalidOperationException) { throw new UserInputException("Yedek dosyası bozulmuş."); }
        if (iterations is < 10_000 or > 10_000_000 || salt.Length < 16 || nonce.Length != 12 || data.Length < 16) throw new UserInputException("Yedek dosyası bozulmuş.");
        var key = DeriveKey(passphrase, salt, iterations);
        var plain = new byte[data.Length - 16];
        try
        {
            using var aes = new AesGcm(key, 16);
            aes.Decrypt(nonce, data.AsSpan(0, plain.Length), data.AsSpan(plain.Length), plain, AssociatedData);
        }
        catch (AuthenticationTagMismatchException) { throw new UserInputException("Parola yanlış ya da yedek dosyası bozulmuş."); }
        finally { CryptographicOperations.ZeroMemory(key); }
        try
        {
            var backup = JsonSerializer.Deserialize<LibraryBackup>(plain, PayloadJson) ?? throw new UserInputException("Yedek dosyası bozulmuş.");
            if (backup.Version > LibraryBackup.CurrentVersion) throw new UserInputException("Bu yedek daha yeni bir enestv sürümüyle oluşturulmuş. enestv'yi güncelleyip tekrar deneyin.");
            return backup;
        }
        catch (JsonException) { throw new UserInputException("Yedek dosyası bozulmuş."); }
        finally { CryptographicOperations.ZeroMemory(plain); }
    }

    public static void CheckPassphrase(string passphrase)
    {
        if (Normalize(passphrase).Length < MinPassphraseLength) throw new UserInputException($"Yedek parolası en az {MinPassphraseLength} karakter olmalı.");
    }
    // NFC so the same passphrase typed on another device (or keyboard) derives the same key.
    private static byte[] DeriveKey(string passphrase, byte[] salt, int iterations) =>
        Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(Normalize(passphrase)), salt, iterations, HashAlgorithmName.SHA256, 32);
    private static string Normalize(string passphrase) => passphrase.Normalize(NormalizationForm.FormC);
}
