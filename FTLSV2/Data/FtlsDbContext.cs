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

        public DbSet<School> Schools { get; set; }
        public DbSet<Department> Departments { get; set; }

        // Rooms table mapped to the room_registry table in NeonDB
        public DbSet<Room> Rooms { get; set; }

        // Subjects table mapped to the subject table
        public DbSet<Subject> Subjects { get; set; }

        // Schedules table mapped to the schedule table
        public DbSet<Schedule> Schedules { get; set; }

        // Map faculty load summary view/table
        public DbSet<FacultyLoadSummary> FacultyLoadSummaries { get; set; }

        // For Settings Variable
        public DbSet<SystemSettings> SystemSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ---> Map User entity to the users table & max_units column <---
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
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
            });

            // Map Department entity to the departments table
            modelBuilder.Entity<Department>(entity =>
            {
                // FIXED: Changed from "department" to "departments" to match PostgreSQL plural table conventions
                entity.ToTable("departments");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.SchoolId).HasColumnName("school_id");
                entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(50);
                entity.Property(e => e.Name).HasColumnName("department").HasMaxLength(200);
            });

            // Map Subject entity to the subject table 
            modelBuilder.Entity<Subject>(entity =>
            {
                entity.ToTable("subject");
                entity.HasKey(e => e.SubjectId);
                entity.Property(e => e.SubjectId).HasColumnName("subject_id");
                entity.Property(e => e.Code).HasColumnName("subject_code").HasMaxLength(50);
                entity.Property(e => e.Title).HasColumnName("subject_title").HasMaxLength(300);
                entity.Property(e => e.Units).HasColumnName("units");
                

                // Map the actual integer foreign key column correctly
                entity.Property(e => e.DepartmentId).HasColumnName("department_id");

                // Explicitly define the relationship blueprint for EF Core
                entity.HasOne(s => s.Department)
                      .WithMany(d => d.Subjects)
                      .HasForeignKey(s => s.DepartmentId);
            });

            // Map Schedule entity to the schedule table & assigned_units column 
            modelBuilder.Entity<Schedule>(entity =>
            {
                entity.ToTable("schedule");
                entity.Property(e => e.AssignedUnits).HasColumnName("assigned_units");
            });

            // Map FacultyLoadSummary entity to the faculty_load_summary table/view
            modelBuilder.Entity<FacultyLoadSummary>()
                .HasNoKey()
                .ToView("faculty_load_summary");
        }
    }
}