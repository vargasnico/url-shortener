using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UrlShortener.Api.Models;
using UrlShortener.Api.Services;

namespace UrlShortener.Api.Data.Configurations;

// Mapeamento da entidade para a tabela. Fica fora da classe Link para o modelo não depender do EF.
public class LinkConfiguration : IEntityTypeConfiguration<Link>
{
    public void Configure(EntityTypeBuilder<Link> builder)
    {
        // Nomes em snake_case, que é a convenção do PostgreSQL.
        builder.ToTable("links");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id");

        builder.Property(l => l.Code)
            .HasColumnName("code")
            .HasMaxLength(RandomCodeGenerator.Length)
            .IsRequired();

        // Índice único: o banco garante que nunca existam dois links com o mesmo código,
        // mesmo com duas requisições gravando ao mesmo tempo.
        builder.HasIndex(l => l.Code).IsUnique();

        builder.Property(l => l.OriginalUrl)
            .HasColumnName("original_url")
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(l => l.CreatedAt).HasColumnName("created_at");
    }
}
