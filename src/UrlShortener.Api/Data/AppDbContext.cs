using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Models;

namespace UrlShortener.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Link> Links => Set<Link>();

    // Aplica todas as classes IEntityTypeConfiguration<T> deste projeto (ver Data/Configurations/).
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
