using Microsoft.EntityFrameworkCore;
using FTLSV2.Models;

namespace FTLSV2.Data
{
    public class FtlsDbContext : DbContext
    {
        public FtlsDbContext(DbContextOptions<FtlsDbContext> options) : base(options)
        {
        }

        // This tells Entity Framework to link the User model to the 'users' table
        public DbSet<User> Users { get; set; }

        // Rooms table mapped to the room_registry table in NeonDB
        public DbSet<Room> Rooms { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Map the Room entity to the existing `room_registry` table and columns
            modelBuilder.Entity<Room>(entity =>
            {
                entity.ToTable("room_registry");

                // The existing table uses `room_id` as the primary key (integer)
                entity.HasKey(e => e.RoomId);

                entity.Property(e => e.RoomId).HasColumnName("room_id");
                // Map the CLR `Name` property to the actual DB column `room_name`
                entity.Property(e => e.Name).HasColumnName("room_name").HasMaxLength(200);
                entity.Property(e => e.Type).HasColumnName("type").HasMaxLength(100);
                entity.Property(e => e.Capacity).HasColumnName("capacity");
                entity.Property(e => e.Availability).HasColumnName("availability").HasMaxLength(50);
            });
        }
    }
}