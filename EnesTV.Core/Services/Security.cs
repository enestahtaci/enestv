using System.Security.Cryptography;
using System.Text;

namespace EnesTV.Core.Services;

public static class SecretProtection
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("enestv/source/v1");
    public static byte[] Protect(string value) => ProtectedData.Protect(Encoding.UTF8.GetBytes(value), Entropy, DataProtectionScope.CurrentUser);
    public static string Unprotect(byte[] value) => Encoding.UTF8.GetString(ProtectedData.Unprotect(value, Entropy, DataProtectionScope.CurrentUser));
}

public sealed class SafeLog(string directory)
{
    private readonly object _gate = new();
    // Only fixed event labels and exception type names are accepted. Never log exception messages or URLs.
    public void Write(string eventName, Exception? exception = null)
    {
        lock (_gate)
        {
            try
            {
                Directory.CreateDirectory(directory);
                var path = Path.Combine(directory, "enestv.log");
                if (File.Exists(path) && new FileInfo(path).Length > 1_000_000)
                {
                    for (var i = 3; i >= 1; i--)
                    {
                        var old = i == 1 ? path : path + "." + (i - 1);
                        if (File.Exists(old)) File.Move(old, path + "." + i, true);
                    }
                }
                File.AppendAllText(path, $"{DateTimeOffset.UtcNow:O} {eventName} {exception?.GetType().Name}\n");
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
