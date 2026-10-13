using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RiftLens.Domain.Entities;
using RiftLens.Domain.ValueObjects;
using RiftLens.Infrastructure.Persistence;

namespace RiftLens.Infrastructure.Tests.Persistence;

public sealed class DbContextTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public DbContextTests() => _connection.Open();

    private RiftLensDbContext CreateContext()
    {
        var ctx = new RiftLensDbContext(new DbContextOptionsBuilder<RiftLensDbContext>().UseSqlite(_connection).Options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    [Fact]
    public async Task Match_et_participants_font_un_aller_retour_complet()
    {
        await using (var ctx = CreateContext())
        {
            var match = new Match { MatchId = "EUW1_1", GameVersion = "15.20.1.1", PatchMajor = 15, PatchMinor = 20, Duration = TimeSpan.FromMinutes(31) };
            match.Participants.Add(new MatchParticipant
            {
                MatchId = "EUW1_1", ParticipantId = 1, Puuid = "p1", ChampionId = 103, Role = Role.Middle, Win = true,
                Items = [3157, 3020, 4645, 0, 0, 0, 3364], Summoners = [4, 14], Perks = [8112, 8139, 8138, 8135, 8226, 8210, 5008, 5008, 5011],
            });
            ctx.Matches.Add(match);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CreateContext())
        {
            var loaded = await ctx.Matches.Include(m => m.Participants).SingleAsync();
            var p = Assert.Single(loaded.Participants);
            Assert.Equal([3157, 3020, 4645, 0, 0, 0, 3364], p.Items);
            Assert.Equal(Role.Middle, p.Role);
            Assert.Equal(TimeSpan.FromMinutes(31), loaded.Duration);
        }
    }

    public void Dispose() => _connection.Dispose();
}
