using Microsoft.EntityFrameworkCore;
using Classio.Core.Models;
using Classio.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Classio.Data
{
    public class ClassioDbContext : IdentityDbContext<User>
    {
        public ClassioDbContext(DbContextOptions<ClassioDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Teacher> Teachers { get; set; } = null!;
        public DbSet<Student> Students { get; set; } = null!;
        public DbSet<Parent> Parents { get; set; } = null!;
        public DbSet<School> Schools { get; set; } = null!;
        public DbSet<Class> Classes { get; set; } = null!;
        public DbSet<Subject> Subjects { get; set; } = null!;
        public DbSet<Grade> Grades { get; set; } = null!;
        public DbSet<Absence> Absences { get; set; } = null!;
        public DbSet<ClassPeriod> ClassPeriods { get; set; } = null!;
        public DbSet<ScheduleSlot> ScheduleSlots { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            // Teacher - Subject

            modelBuilder.Entity<Teacher>()
                .HasOne(t => t.Subject)
                .WithMany(s => s.Teachers)
                .HasForeignKey(t => t.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // Class -> Subjects
            modelBuilder.Entity<Subject>()
                .HasOne(s => s.Class)
                .WithMany(c => c.Subjects)
                .HasForeignKey(s => s.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            // Grade relationships
            modelBuilder.Entity<Grade>()
                .HasOne(g => g.Teacher)
                .WithMany(t => t.Grades)
                .HasForeignKey(g => g.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Grade>()
                .HasOne(g => g.Student)
                .WithMany(s => s.Grades)
                .HasForeignKey(g => g.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Grade>()
                .HasOne(g => g.Subject)
                .WithMany(s => s.Grades)
                .HasForeignKey(g => g.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // Absences

            modelBuilder.Entity<Absence>()
                .HasOne(a => a.Student)
                .WithMany(s => s.Absences)
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Absence>()
                .HasOne(a => a.Subject)
                .WithMany()
                .HasForeignKey(a => a.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);


            // Parent <-> Student
            modelBuilder.Entity<Student>()
                .HasOne(s => s.Parent1)
                .WithMany(p => p.StudentsAsParent1)
                .HasForeignKey(s => s.Parent1Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Student>()
                .HasOne(s => s.Parent2)
                .WithMany(p => p.StudentsAsParent2)
                .HasForeignKey(s => s.Parent2Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Class>()
                .HasOne(c => c.HeadTeacher)
                .WithMany(t => t.HeadOfClasses)
                .HasForeignKey(c => c.HeadTeacherId)
                .OnDelete(DeleteBehavior.SetNull);

            // Subject Teachers
            modelBuilder.Entity<Class>()
                .HasMany(c => c.SubjectTeachers)
                .WithMany(t => t.ClassesTaught)
                .UsingEntity(j => j.ToTable("ClassTeachers"));
        }


    }
}