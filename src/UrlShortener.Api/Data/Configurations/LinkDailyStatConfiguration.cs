using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UrlShortener.Api.Models;

namespace UrlShortener.Api.Data.Configurations;

public class LinkDailyStatConfiguration : IEntityTypeConfiguration<LinkDailyStat>
{
    public void Configure(EntityTypeBuilder<LinkDailyStat> builder)
    {
        builder.ToTable("link_daily_stats");

        // Chave composta: uma linha por link por dia. O worker faz "upsert" nela.
        builder.HasKey(s => new { s.LinkId, s.Day });
        builder.Property(s => s.LinkId).HasColumnName("link_id");
        builder.Property(s => s.Day).HasColumnName("day");
        builder.Property(s => s.Clicks).HasColumnName("clicks");

        builder.HasOne<Link>()
            .WithMany()
            .HasForeignKey(s => s.LinkId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
