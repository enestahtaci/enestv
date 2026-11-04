using System.Text.Json;
using EnesTV.Core.Models;
using EnesTV.Core.Services;

namespace EnesTV.Core.Data;

public sealed partial class LibraryStore
{
    public Task<LibraryLayout> LoadLayoutAsync(CancellationToken ct = default) => Run(db =>
    {
        using var cmd = db.CreateCommand(); cmd.CommandText = "SELECT value FROM layout WHERE id=1";
        return cmd.ExecuteScalar() is byte[] data
            ? JsonSerializer.Deserialize<LibraryLayout>(SecretProtection.Unprotect(data)) ?? new()
            : new LibraryLayout();
    }, ct);

    public Task SaveLayoutAsync(LibraryLayout layout, CancellationToken ct = default) => Run(db =>
    {
        Execute(db, "INSERT INTO layout VALUES(1,$value) ON CONFLICT(id) DO UPDATE SET value=$value",
            ("$value", SecretProtection.Protect(JsonSerializer.Serialize(layout))));
        return true;
    }, ct);
}
