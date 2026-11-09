using System.Text.Json;
using EnesTV.Core.Models;
using EnesTV.Core.Services;

namespace EnesTV.Core.Data;

public sealed partial class LibraryStore
{
    public Task<LibraryLayout> LoadLayoutAsync(CancellationToken ct = default) => Run(LoadLayout, ct);
    private LibraryLayout LoadLayout(Microsoft.Data.Sqlite.SqliteConnection db)
    {
        using var cmd = db.CreateCommand(); cmd.CommandText = "SELECT value FROM layout WHERE id=1";
        return cmd.ExecuteScalar() is byte[] data
            ? JsonSerializer.Deserialize<LibraryLayout>(_protector.Unprotect(data)) ?? new()
            : new LibraryLayout();
    }

    public Task SaveLayoutAsync(LibraryLayout layout, CancellationToken ct = default) => Run(db =>
    {
        Execute(db, "INSERT INTO layout VALUES(1,$value) ON CONFLICT(id) DO UPDATE SET value=$value",
            ("$value", _protector.Protect(JsonSerializer.Serialize(layout))));
        return true;
    }, ct);
}
