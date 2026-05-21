using Classio.Areas.Teacher.Models;
using Classio.Data;
using Classio.Models;

namespace Classio.Tests.Infrastructure;

/// <summary>
/// Convenience seeding for tests. Builds a small but realistic graph
/// (school → class → students/teacher → subject → grades/absences/schedule)
/// and saves it. Callers receive the populated entities they care about.
/// </summary>
public static class TestData
{
    public class Seed
    {
        public School School { get; set; } = null!;
        public Class Class { get; set; } = null!;
        public Subject Math { get; set; } = null!;
        public Subject English { get; set; } = null!;
        public Teacher MathTeacher { get; set; } = null!;
        public Teacher EnglishTeacher { get; set; } = null!;
        public Student Alice { get; set; } = null!;
        public Student Bob { get; set; } = null!;
        public ClassPeriod Period1 { get; set; } = null!;
        public ClassPeriod Period2 { get; set; } = null!;
    }

    public static Seed SeedBasic(ClassioDbContext db)
    {
        var school = new School { Id = 1, Name = "Test High" };
        db.Schools.Add(school);

        var math = new Subject { Id = 1, Name = "Math" };
        var english = new Subject { Id = 2, Name = "English" };
        db.Subjects.AddRange(math, english);

        var mathTeacher = new Teacher
        {
            Id = 10,
            FirstName = "Mary",
            LastName = "Math",
            UserId = "teacher-math",
            Subject = math,
            SubjectId = math.Id,
            User = new User { Id = "teacher-math", FirstName = "Mary", LastName = "Math", UserName = "mary@test", Email = "mary@test" }
        };
        var englishTeacher = new Teacher
        {
            Id = 11,
            FirstName = "Eric",
            LastName = "English",
            UserId = "teacher-eng",
            Subject = english,
            SubjectId = english.Id,
            User = new User { Id = "teacher-eng", FirstName = "Eric", LastName = "English", UserName = "eric@test", Email = "eric@test" }
        };
        db.Teachers.AddRange(mathTeacher, englishTeacher);

        var schoolClass = new Class
        {
            Id = 100,
            Name = "12A",
            School = school,
            SchoolId = school.Id,
            HeadTeacher = mathTeacher,
            HeadTeacherId = mathTeacher.Id
        };
        db.Classes.Add(schoolClass);

        var alice = new Student
        {
            Id = 1000,
            FirstName = "Alice",
            LastName = "Anderson",
            UserId = "student-alice",
            ClassId = schoolClass.Id,
            SchoolId = school.Id,
            User = new User { Id = "student-alice", FirstName = "Alice", LastName = "Anderson", UserName = "alice@test", Email = "alice@test" }
        };
        var bob = new Student
        {
            Id = 1001,
            FirstName = "Bob",
            LastName = "Brown",
            UserId = "student-bob",
            ClassId = schoolClass.Id,
            SchoolId = school.Id,
            User = new User { Id = "student-bob", FirstName = "Bob", LastName = "Brown", UserName = "bob@test", Email = "bob@test" }
        };
        db.Students.AddRange(alice, bob);

        // Alice grades: 6, 5, 4 in Math; 3 in English -> avg = 4.5
        db.Grades.AddRange(
            new Grade { Id = 5001, StudentId = alice.Id, TeacherId = mathTeacher.Id, SubjectId = math.Id, Value = 6, Type = GradeType.Test, Date = new DateTime(2026, 4, 1) },
            new Grade { Id = 5002, StudentId = alice.Id, TeacherId = mathTeacher.Id, SubjectId = math.Id, Value = 5, Type = GradeType.Quiz, Date = new DateTime(2026, 4, 5) },
            new Grade { Id = 5003, StudentId = alice.Id, TeacherId = mathTeacher.Id, SubjectId = math.Id, Value = 4, Type = GradeType.Homework, Date = new DateTime(2026, 4, 10) },
            new Grade { Id = 5004, StudentId = alice.Id, TeacherId = englishTeacher.Id, SubjectId = english.Id, Value = 3, Type = GradeType.Test, Date = new DateTime(2026, 4, 12) }
        );

        // Bob grades: 3, 3 in Math -> avg = 3
        db.Grades.AddRange(
            new Grade { Id = 5101, StudentId = bob.Id, TeacherId = mathTeacher.Id, SubjectId = math.Id, Value = 3, Type = GradeType.Test, Date = new DateTime(2026, 4, 2) },
            new Grade { Id = 5102, StudentId = bob.Id, TeacherId = mathTeacher.Id, SubjectId = math.Id, Value = 3, Type = GradeType.Quiz, Date = new DateTime(2026, 4, 9) }
        );

        // Alice: 2 absences (1 absent + 2 lates) -> weighted = 1 + 2*0.5 = 2
        db.Absences.AddRange(
            new Absence { Id = 7001, StudentId = alice.Id, SubjectId = math.Id, Date = new DateTime(2026, 4, 1), AttendanceState = AttendanceState.Absent },
            new Absence { Id = 7002, StudentId = alice.Id, SubjectId = math.Id, Date = new DateTime(2026, 4, 2), AttendanceState = AttendanceState.Late },
            new Absence { Id = 7003, StudentId = alice.Id, SubjectId = english.Id, Date = new DateTime(2026, 4, 3), AttendanceState = AttendanceState.Late }
        );

        var p1 = new ClassPeriod { Id = 1, Name = "1st Period", StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(8, 45, 0), Order = 1, IsBreak = false };
        var p2 = new ClassPeriod { Id = 2, Name = "2nd Period", StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(9, 45, 0), Order = 2, IsBreak = false };
        var pBreak = new ClassPeriod { Id = 3, Name = "Break", StartTime = new TimeSpan(8, 45, 0), EndTime = new TimeSpan(9, 0, 0), Order = 3, IsBreak = true };
        db.ClassPeriods.AddRange(p1, p2, pBreak);

        // Schedule: Monday p1 = Math (mary), Tuesday p2 = English (eric)
        db.ScheduleSlots.AddRange(
            new ScheduleSlot
            {
                Id = 9001, ClassId = schoolClass.Id, ClassPeriodId = p1.Id,
                SubjectId = math.Id, TeacherId = mathTeacher.Id, DayOfWeek = DayOfWeek.Monday
            },
            new ScheduleSlot
            {
                Id = 9002, ClassId = schoolClass.Id, ClassPeriodId = p2.Id,
                SubjectId = english.Id, TeacherId = englishTeacher.Id, DayOfWeek = DayOfWeek.Tuesday
            }
        );

        db.SaveChanges();

        return new Seed
        {
            School = school,
            Class = schoolClass,
            Math = math,
            English = english,
            MathTeacher = mathTeacher,
            EnglishTeacher = englishTeacher,
            Alice = alice,
            Bob = bob,
            Period1 = p1,
            Period2 = p2
        };
    }
}
