using Microsoft.EntityFrameworkCore;
using TaskManagerTelegramBot_Bulatov.Classes;

namespace TaskManagerTelegramBot_Bulatov.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Users> Users { get; set; }
        public DbSet<Events> Events { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Users>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Id).ValueGeneratedOnAdd();
                entity.Property(u => u.IdUser).IsRequired();
                entity.HasIndex(u => u.IdUser).IsUnique();
                entity.Property(u => u.Username).HasMaxLength(100);
                entity.HasMany(u => u.Events)
                      .WithOne(e => e.User)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Events>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Time).IsRequired();
                entity.Property(e => e.Message).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.IsRecurring).HasDefaultValue(false);
                entity.Property(e => e.RecurrencePattern).HasMaxLength(50);
                entity.HasIndex(e => e.Time);
                entity.HasIndex(e => e.UserId);
            });
        }
    }
}