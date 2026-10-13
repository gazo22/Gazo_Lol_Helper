using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RiftLens.Infrastructure.Persistence;

namespace RiftLens.Infrastructure.Tests.Support;

/// <summary>Base SQLite en mémoire partagée par les contextes d'un même test.</summary>
internal sealed class SqliteDb : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public SqliteDb()
    {
        _connection.Open();
        using var ctx = Create();
        ctx.Database.EnsureCreated();
    }

    public RiftLensDbContext Create() =>
        new(new DbContextOptionsBuilder<RiftLensDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}
