using Microsoft.EntityFrameworkCore;
using RiftLens.Domain.Entities;

namespace RiftLens.Infrastructure.Persistence;

public sealed class RiftLensDbContext(DbContextOptions<RiftLensDbContext> options) : DbContext(options)
{
    public DbSet<Player> Players => Set<Player>();
    public DbSet<LeagueSnapshot> LeagueSnapshots => Set<LeagueSnapshot>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<MatchParticipant> MatchParticipants => Set<MatchParticipant>();
    public DbSet<MatchBan> MatchBans => Set<MatchBan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("rl");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RiftLensDbContext).Assembly);
    }
}
