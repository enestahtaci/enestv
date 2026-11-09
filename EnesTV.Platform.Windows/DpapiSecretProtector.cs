using System.Security.Cryptography;
using System.Text;
using EnesTV.Core.Services;

namespace EnesTV.Platform.Windows;

/// <summary>
/// DPAPI (current Windows user) protection, byte-for-byte compatible with enestv 1.0–1.4.1 libraries.
/// Do not change the entropy: existing library.db files were written with it.
/// </summary>
public sealed class DpapiSecretProtector : ISecretProtector
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("enestv/source/v1");
    public byte[] Protect(string value) => ProtectedData.Protect(Encoding.UTF8.GetBytes(value), Entropy, DataProtectionScope.CurrentUser);
    public string Unprotect(byte[] value) => Encoding.UTF8.GetString(ProtectedData.Unprotect(value, Entropy, DataProtectionScope.CurrentUser));
}
