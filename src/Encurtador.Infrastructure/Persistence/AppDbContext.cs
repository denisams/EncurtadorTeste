using Encurtador.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Encurtador.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<ShortenedUrl> ShortenedUrls => Set<ShortenedUrl>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShortenedUrl>(entity =>
        {
            entity.ToTable("shortened_urls");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(16).IsRequired();
            entity.Property(e => e.OriginalUrl).HasMaxLength(2048).IsRequired();

            // The redirect hot path always looks up by Code, so it is the only
            // index that matters for read throughput at scale.
            entity.HasIndex(e => e.Code).IsUnique();
        });
    }
}
