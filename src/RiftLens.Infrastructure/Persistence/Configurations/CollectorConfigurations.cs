using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiftLens.Domain.Entities;

namespace RiftLens.Infrastructure.Persistence.Configurations;

internal sealed class CollectorPlayerConfiguration : IEntityTypeConfiguration<CollectorPlayer>
{
    public void Configure(EntityTypeBuilder<CollectorPlayer> builder)
    {
        builder.HasKey(p => p.Puuid);
        builder.Property(p => p.Puuid).HasMaxLength(78).IsUnicode(false);
        builder.Property(p => p.Platform).HasConversion<string>().HasMaxLength(8);
        builder.Property(p => p.Tier).HasConversion<string>().HasMaxLength(16);
        builder.HasIndex(p => p.LastCrawledAt);
    }
}

internal sealed class QueuedMatchConfiguration : IEntityTypeConfiguration<QueuedMatch>
{
    public void Configure(EntityTypeBuilder<QueuedMatch> builder)
    {
        builder.HasKey(q => q.MatchId);
        builder.Property(q => q.MatchId).HasMaxLength(32).IsUnicode(false);
        builder.Property(q => q.SourceTier).HasConversion<string>().HasMaxLength(16);
        builder.Property(q => q.Status).HasConversion<string>().HasMaxLength(8);
        builder.HasIndex(q => new { q.Status, q.EnqueuedAt });
    }
}

internal sealed class StatConfigurations :
    IEntityTypeConfiguration<PatchTotals>,
    IEntityTypeConfiguration<ChampionStat>,
    IEntityTypeConfiguration<ChampionBanStat>,
    IEntityTypeConfiguration<MatchupStat>,
    IEntityTypeConfiguration<BuildStat>,
    IEntityTypeConfiguration<RuneStat>
{
    public void Configure(EntityTypeBuilder<PatchTotals> builder)
    {
        builder.HasKey(t => new { t.Patch, t.Bucket });
        Common(builder);
    }

    public void Configure(EntityTypeBuilder<ChampionStat> builder)
    {
        builder.HasKey(s => new { s.Patch, s.Bucket, s.ChampionId, s.Role });
        builder.Property(s => s.Role).HasConversion<string>().HasMaxLength(8);
        Common(builder);
    }

    public void Configure(EntityTypeBuilder<ChampionBanStat> builder)
    {
        builder.HasKey(s => new { s.Patch, s.Bucket, s.ChampionId });
        Common(builder);
    }

    public void Configure(EntityTypeBuilder<MatchupStat> builder)
    {
        builder.HasKey(s => new { s.Patch, s.Bucket, s.Role, s.ChampionId, s.OpponentId });
        builder.Property(s => s.Role).HasConversion<string>().HasMaxLength(8);
        Common(builder);
    }

    public void Configure(EntityTypeBuilder<BuildStat> builder)
    {
        builder.HasKey(s => new { s.Patch, s.Bucket, s.ChampionId, s.Role, s.ItemsKey });
        builder.Property(s => s.Role).HasConversion<string>().HasMaxLength(8);
        builder.Property(s => s.ItemsKey).HasMaxLength(32).IsUnicode(false);
        Common(builder);
    }

    public void Configure(EntityTypeBuilder<RuneStat> builder)
    {
        builder.HasKey(s => new { s.Patch, s.Bucket, s.ChampionId, s.Role, s.PerksKey });
        builder.Property(s => s.Role).HasConversion<string>().HasMaxLength(8);
        builder.Property(s => s.PerksKey).HasMaxLength(80).IsUnicode(false);
        Common(builder);
    }

    private static void Common<T>(EntityTypeBuilder<T> builder)
        where T : class
    {
        builder.Property<string>("Patch").HasMaxLength(8).IsUnicode(false);
        builder.Property<Domain.ValueObjects.RankTier>("Bucket").HasConversion<string>().HasMaxLength(16);
    }
}
