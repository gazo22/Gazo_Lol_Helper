using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RiftLens.Domain.Entities;

namespace RiftLens.Infrastructure.Persistence;

public sealed class RiftLensDbContext(DbContextOptions<RiftLensDbContext> options) : DbContext(options)
{
    public DbSet<Player> Players => Set<Player>();
    public DbSet<LeagueSnapshot> LeagueSnapshots => Set<LeagueSnapshot>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<MatchParticipant> MatchParticipants => Set<MatchParticipant>();
    public DbSet<MatchBan> MatchBans => Set<MatchBan>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite (tests) ne sait ni trier ni comparer des DateTimeOffset : stockage en ticks UTC.
        if (Database.ProviderName?.Contains("Sqlite", StringComparison.Ordinal) == true)
        {
            configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
            configurationBuilder.Properties<TimeSpan>().HaveConversion<TimeSpanToTicksConverter>();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("rl");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RiftLensDbContext).Assembly);
    }
}
