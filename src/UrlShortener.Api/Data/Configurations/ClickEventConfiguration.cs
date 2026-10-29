using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UrlShortener.Api.Models;

namespace UrlShortener.Api.Data.Configurations;

public class ClickEventConfiguration : IEntityTypeConfiguration<ClickEvent>
{
    public void Configure(EntityTypeBuilder<ClickEvent> builder)
    {
        builder.ToTable("click_events");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.LinkId).HasColumnName("link_id");
        builder.Property(c => c.ClickedAt).HasColumnName("clicked_at");
        builder.Property(c => c.UserAgent).HasColumnName("user_agent").HasMaxLength(ClickEvent.UserAgentMaxLength);
        builder.Property(c => c.Referrer).HasColumnName("referrer").HasMaxLength(ClickEvent.ReferrerMaxLength);
        builder.Property(c => c.AggregatedAt).HasColumnName("aggregated_at");

        builder.HasOne<Link>()
            .WithMany()
            .HasForeignKey(c => c.LinkId)
            .OnDelete(DeleteBehavior.Cascade);

        // Índice parcial: só os eventos pendentes, que é exatamente o que o worker procura.
        // Fica pequeno mesmo com milhões de eventos já agregados.
        builder.HasIndex(c => c.Id)
            .HasDatabaseName("ix_click_events_pending")
            .HasFilter("aggregated_at IS NULL");
    }
}
