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

        // Subjects table mapped to the subject_library table
        public DbSet<Subject> Subjects { get; set; }

        // Schedules table mapped to the schedule table
        public DbSet<Schedule> Schedules { get; set; }

        // Map faculty load summary view/table
        public DbSet<FacultyLoadSummary> FacultyLoadSummaries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ---> NEW: Map User entity to the users table & max_units column <---
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users"); // Ensure it connects to your users table
                entity.Property(e => e.MaxUnits).HasColumnName("max_units");
            });

            // Map the Room entity to the existing `room_registry` table and columns
            modelBuilder.Entity<Room>(entity =>
            {
                entity.ToTable("room_registry");
                entity.HasKey(e => e.RoomId);
                entity.Property(e => e.RoomId).HasColumnName("room_id");
                entity.Property(e => e.Name).HasColumnName("room_name").HasMaxLength(200);
                entity.Property(e => e.Type).HasColumnName("type").HasMaxLength(100);
                entity.Property(e => e.Capacity).HasColumnName("capacity");
                entity.Property(e => e.Availability).HasColumnName("availability").HasMaxLength(50);
            });

            // Map Subject entity to the subject_library table 
            modelBuilder.Entity<Subject>(entity =>
            {
                entity.ToTable("subject_library");
                entity.HasKey(e => e.SubjectId);
                entity.Property(e => e.SubjectId).HasColumnName("subject_id");
                entity.Property(e => e.Code).HasColumnName("subject_code").HasMaxLength(50);
                entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(300);
                entity.Property(e => e.Units).HasColumnName("units");
                entity.Property(e => e.Department).HasColumnName("department").HasMaxLength(200);
                entity.Property(e => e.Semester).HasColumnName("semester").HasMaxLength(50);
            });

            // Map Schedule entity to the schedule table & assigned_units column 
            modelBuilder.Entity<Schedule>(entity =>
            {
                entity.ToTable("schedule");
                entity.Property(e => e.AssignedUnits).HasColumnName("assigned_units");
            });

            // Map FacultyLoadSummary entity to the faculty_load_summary table/view
            modelBuilder.Entity<FacultyLoadSummary>(entity =>
            {
                entity.ToTable("faculty_load_summary");
                entity.HasNoKey(); // The summary is a view without a stable primary key
                entity.Property(e => e.FirstName).HasColumnName("first_name").HasMaxLength(200);
                entity.Property(e => e.LastName).HasColumnName("last_name").HasMaxLength(200);
                entity.Property(e => e.TotalLoad).HasColumnName("total_load");
                entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(200);
            });
        }
    }
}