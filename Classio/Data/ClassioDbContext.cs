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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
                .HasOne(c => c.Teacher)
                .WithMany(t => t.Classes)
                .HasForeignKey(c => c.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}