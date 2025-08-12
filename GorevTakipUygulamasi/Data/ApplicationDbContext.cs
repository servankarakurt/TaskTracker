using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using GorevTakipUygulamasi.Models;

namespace GorevTakipUygulamasi.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<TaskItem> TaskItems { get; set; } = null!;
        public DbSet<ReminderItem> Reminders { get; set; } = null!; // Hatırlatıcılar için DbSet

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // TaskItem konfigürasyonları
            builder.Entity<TaskItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.CreatedDate).IsRequired();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => new { e.UserId, e.Status });
            });

            // ReminderItem konfigürasyonları (SQL Server için)
            builder.Entity<ReminderItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => new { e.UserId, e.Date });
                entity.HasIndex(e => new { e.Date, e.Time });

                // DateOnly ve TimeOnly için conversion (EF Core 6.0 için)
                entity.Property(e => e.Date)
                    .HasConversion(
                        v => v.ToDateTime(TimeOnly.MinValue),
                        v => DateOnly.FromDateTime(v)
                    );

                entity.Property(e => e.Time)
                    .HasConversion(
                        v => v.ToTimeSpan(),
                        v => TimeOnly.FromTimeSpan(v)
                    );

                // Status enum için string conversion
                entity.Property(e => e.Status)
                    .HasConversion(
                        v => v.ToString(),
                        v => Enum.Parse<ReminderStatus>(v)
                    );
            });
        }
    }
}