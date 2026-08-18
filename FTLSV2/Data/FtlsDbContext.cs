using Microsoft.EntityFrameworkCore;
using FTLSV2.Models;

namespace FTLSV2.Data
{
    public class FtlsDbContext : DbContext
    {
        public FtlsDbContext(DbContextOptions<FtlsDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<School> Schools { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<FacultyLoadSummary> FacultyLoadSummaries { get; set; }
        public DbSet<SystemSettings> SystemSettings { get; set; }
        public DbSet<Curriculum> Curriculums { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Map User entity to the users table
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");

                // FacultyId is now the official Primary Key
                entity.HasKey(e => e.FacultyId);

                entity.Property(e => e.FacultyId).HasColumnName("faculty_id");
                entity.Property(e => e.Password).HasColumnName("password");
                entity.Property(e => e.Role).HasColumnName("role");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Property(e => e.FirstName).HasColumnName("first_name");
                entity.Property(e => e.LastName).HasColumnName("last_name");
                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.MaxUnits).HasColumnName("max_units");
                entity.Property(e => e.FacultyType).HasColumnName("faculty_type").HasMaxLength(20);
                entity.Property(e => e.CurrentUnits).HasColumnName("current_units");
                entity.Property(e => e.SchoolId).HasColumnName("school_id");
                entity.Property(e => e.DepartmentId).HasColumnName("department_id");
            });

            // Map School entity to the schools table
            modelBuilder.Entity<School>(entity =>
            {
                entity.ToTable("schools");

                entity.HasKey(e => e.Id);

                // FIXED: Changed "id" to "school_id"
                entity.Property(e => e.Id).HasColumnName("school_id");
                entity.Property(e => e.Code).HasColumnName("code");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });

            // Map Department entity to the departments table
            modelBuilder.Entity<Department>(entity =>
            {
                entity.ToTable("departments");

                entity.HasKey(e => e.Id);

                // FIXED: Changed "id" to "dept_id"
                entity.Property(e => e.Id).HasColumnName("dept_id");
                entity.Property(e => e.SchoolId).HasColumnName("school_id");
                entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(50);
                entity.Property(e => e.Name).HasColumnName("department").HasMaxLength(200);
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");

                entity.HasOne(d => d.School)
                      .WithMany()
                      .HasForeignKey(d => d.SchoolId);
            });

            // Map Room entity to the room_registry table
            modelBuilder.Entity<Room>(entity =>
            {
                entity.ToTable("room_registry");

                entity.HasKey(e => e.RoomId);

                entity.Property(e => e.RoomId).HasColumnName("room_id");
                entity.Property(e => e.Name).HasColumnName("room_name").HasMaxLength(200);
                entity.Property(e => e.Type).HasColumnName("type").HasMaxLength(100);
                entity.Property(e => e.Capacity).HasColumnName("capacity");
                entity.Property(e => e.Availability).HasColumnName("availability").HasMaxLength(255);
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
                entity.Property(e => e.DepartmentId).HasColumnName("department_id");

                entity.HasOne(s => s.Department)
                      .WithMany(d => d.Subjects)
                      .HasForeignKey(s => s.DepartmentId);
            });

            // Map Schedule entity to the schedule table
            modelBuilder.Entity<Schedule>(entity =>
            {
                entity.ToTable("schedule");

                entity.HasKey(e => e.ScheduleId);

                entity.Property(e => e.ScheduleId).HasColumnName("schedule_id");
                entity.Property(e => e.FacultyId).HasColumnName("faculty_id");
                entity.Property(e => e.SubjectId).HasColumnName("subject_id");
                entity.Property(e => e.RoomId).HasColumnName("room_id");
                entity.Property(e => e.TimeSlot).HasColumnName("time_slot");
                entity.Property(e => e.AssignedUnits).HasColumnName("assigned_units");
                entity.Property(e => e.OfferCode).HasColumnName("offer_code");
                entity.Property(e => e.AcademicYear).HasColumnName("academic_year");
                entity.Property(e => e.Semester).HasColumnName("semester");

                // Schedule.FacultyId points directly to User.FacultyId
                entity.HasOne(e => e.Faculty)
                      .WithMany()
                      .HasForeignKey(e => e.FacultyId);

                entity.HasOne(e => e.Subject)
                      .WithMany()
                      .HasForeignKey(e => e.SubjectId);

                entity.HasOne(e => e.Room)
                      .WithMany()
                      .HasForeignKey(e => e.RoomId);
            });

            // Map FacultyLoadSummary entity to the faculty_load_summary table/view
            modelBuilder.Entity<FacultyLoadSummary>()
                .HasNoKey()
                .ToView("faculty_load_summary");

            // Map Curriculum entity to the curriculum table
            modelBuilder.Entity<Curriculum>(entity =>
            {
                entity.ToTable("curriculum");

                entity.HasKey(e => e.CurriculumId);

                entity.Property(e => e.CurriculumId).HasColumnName("curriculum_id");
                entity.Property(e => e.SubjectId).HasColumnName("subject_id");
                entity.Property(e => e.DepartmentId).HasColumnName("department_id");
                entity.Property(e => e.YearLevel).HasColumnName("year_level");
                entity.Property(e => e.Semester).HasColumnName("semester");
                entity.Property(e => e.CurriculumYear).HasColumnName("curriculum_year");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

                entity.HasOne(c => c.Department)
                      .WithMany()
                      .HasForeignKey(c => c.DepartmentId);

                entity.Ignore(c => c.Subject);
            });
        }
    }
}