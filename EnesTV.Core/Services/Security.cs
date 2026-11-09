namespace EnesTV.Core.Services;

/// <summary>
/// Encrypts library data at rest (accounts, channel lists, settings, progress). The core stays platform neutral:
/// Windows uses DPAPI (EnesTV.Platform.Windows); Android/iOS implementations will wrap a data key in Keystore/Keychain.
/// </summary>
public interface ISecretProtector
{
    byte[] Protect(string value);
    string Unprotect(byte[] value);
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
