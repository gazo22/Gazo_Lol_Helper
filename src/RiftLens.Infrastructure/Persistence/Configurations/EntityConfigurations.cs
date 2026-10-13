using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiftLens.Domain.Entities;

namespace RiftLens.Infrastructure.Persistence.Configurations;

internal sealed class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.HasKey(p => p.Puuid);
        builder.Property(p => p.Puuid).HasMaxLength(78).IsUnicode(false);
        builder.Property(p => p.GameName).HasMaxLength(32);
        builder.Property(p => p.TagLine).HasMaxLength(8);
        builder.Property(p => p.Platform).HasConversion<string>().HasMaxLength(8);
        builder.HasIndex(p => new { p.GameName, p.TagLine });
        builder.HasMany(p => p.LeagueSnapshots).WithOne().HasForeignKey(s => s.Puuid);
    }
}

internal sealed class LeagueSnapshotConfiguration : IEntityTypeConfiguration<LeagueSnapshot>
{
    public void Configure(EntityTypeBuilder<LeagueSnapshot> builder)
    {
        builder.Property(s => s.Puuid).HasMaxLength(78).IsUnicode(false);
        builder.Property(s => s.Queue).HasConversion<string>().HasMaxLength(16);
        builder.Property(s => s.Tier).HasConversion<string>().HasMaxLength(16);
        builder.HasIndex(s => new { s.Puuid, s.Queue, s.CapturedAt });
    }
}

internal sealed class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.HasKey(m => m.MatchId);
        builder.Property(m => m.MatchId).HasMaxLength(32).IsUnicode(false);
        builder.Property(m => m.GameVersion).HasMaxLength(32).IsUnicode(false);
        builder.HasIndex(m => new { m.PatchMajor, m.PatchMinor, m.QueueId });
        builder.Property(m => m.SourceTier).HasConversion<string>().HasMaxLength(16);
        builder.HasMany(m => m.Participants).WithOne().HasForeignKey(p => p.MatchId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(m => m.Bans).WithOne().HasForeignKey(b => b.MatchId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class MatchParticipantConfiguration : IEntityTypeConfiguration<MatchParticipant>
{
    public void Configure(EntityTypeBuilder<MatchParticipant> builder)
    {
        builder.HasKey(p => new { p.MatchId, p.ParticipantId });
        builder.Property(p => p.Puuid).HasMaxLength(78).IsUnicode(false);
        builder.Property(p => p.RiotIdGameName).HasMaxLength(32);
        builder.Property(p => p.RiotIdTagline).HasMaxLength(8);
        builder.Property(p => p.Role).HasConversion<string>().HasMaxLength(8);
        builder.Ignore(p => p.TotalCs);
        MapIntArray(builder.Property(p => p.Items));
        MapIntArray(builder.Property(p => p.Summoners));
        MapIntArray(builder.Property(p => p.Perks));
        builder.HasIndex(p => p.Puuid);
        builder.HasIndex(p => new { p.ChampionId, p.Role });
    }

    /// <summary>Stocke un petit tableau d'entiers en CSV (lisible en SQL, pas besoin de table dédiée).</summary>
    private static void MapIntArray(PropertyBuilder<int[]> property) => property
        .HasConversion(
            v => string.Join(',', v),
            v => v.Length == 0 ? Array.Empty<int>() : v.Split(',', StringSplitOptions.None).Select(int.Parse).ToArray(),
            new ValueComparer<int[]>((a, b) => a!.SequenceEqual(b!), v => v.Aggregate(17, (h, i) => HashCode.Combine(h, i)), v => v.ToArray()))
        .HasMaxLength(96)
        .IsUnicode(false);
}

internal sealed class MatchBanConfiguration : IEntityTypeConfiguration<MatchBan>
{
    public void Configure(EntityTypeBuilder<MatchBan> builder) => builder.HasKey(b => new { b.MatchId, b.TeamId, b.PickTurn });
}
