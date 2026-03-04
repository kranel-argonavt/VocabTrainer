using Microsoft.EntityFrameworkCore;
using System;
using VocabTrainer.Core.Entities;

namespace VocabTrainer.Infrastructure.Data
{
    public class VocabDbContext : DbContext
    {
        public DbSet<WordCard> WordCards { get; set; } = null!;

        public VocabDbContext(DbContextOptions<VocabDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WordCard>(entity =>
            {
                entity.ToTable("WordCards");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.German).IsRequired().HasMaxLength(200);
                entity.Property(e => e.English).HasMaxLength(200).HasDefaultValue(string.Empty);
                entity.Property(e => e.Ukrainian).HasMaxLength(200).HasDefaultValue(string.Empty);
                entity.Property(e => e.ExampleSentence).HasMaxLength(1000).HasDefaultValue(string.Empty);
                entity.Property(e => e.Tags).HasMaxLength(500).HasDefaultValue(string.Empty);
                entity.Property(e => e.EaseFactor).HasDefaultValue(2.5);
                entity.Property(e => e.IntervalDays).HasDefaultValue(1);
                entity.Property(e => e.NextReview).HasDefaultValueSql("datetime('now')");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");

                entity.HasIndex(e => e.German).IsUnique();
                entity.HasIndex(e => e.NextReview);
                entity.HasIndex(e => e.Tags);

                entity.Ignore(e => e.SuccessRate);
                entity.Ignore(e => e.IsDueToday);
                entity.Ignore(e => e.Difficulty);
            });
        }
    }
}
