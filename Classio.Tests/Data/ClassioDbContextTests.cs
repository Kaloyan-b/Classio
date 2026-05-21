using Classio.Areas.Teacher.Models;
using Classio.Models;
using Classio.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Classio.Tests.Data;

public class ClassioDbContextTests
{
    [Fact]
    public void CanPersistAndQueryStudentsAndGrades()
    {
        using var db = TestDbContextFactory.Create();
        TestData.SeedBasic(db);

        var alice = db.Students
            .Include(s => s.Grades)
            .Single(s => s.FirstName == "Alice");

        Assert.Equal(4, alice.Grades.Count);
        Assert.Equal(4.5, alice.Grades.Average(g => g.Value));
    }

    [Fact]
    public void StudentClassRelationship_LoadedWithInclude()
    {
        using var db = TestDbContextFactory.Create();
        TestData.SeedBasic(db);

        var bob = db.Students
            .Include(s => s.Class)
            .Single(s => s.FirstName == "Bob");

        Assert.NotNull(bob.Class);
        Assert.Equal("12A", bob.Class.Name);
    }

    [Fact]
    public void Absences_CountsByStateAreCorrect()
    {
        using var db = TestDbContextFactory.Create();
        TestData.SeedBasic(db);

        var aliceAbsences = db.Absences.Where(a => a.Student.FirstName == "Alice").ToList();

        Assert.Equal(3, aliceAbsences.Count);
        Assert.Equal(1, aliceAbsences.Count(a => a.AttendanceState == AttendanceState.Absent));
        Assert.Equal(2, aliceAbsences.Count(a => a.AttendanceState == AttendanceState.Late));
    }

    [Fact]
    public void ScheduleSlots_AreFilterableByClass()
    {
        using var db = TestDbContextFactory.Create();
        var seed = TestData.SeedBasic(db);

        var slots = db.ScheduleSlots.Where(s => s.ClassId == seed.Class.Id).ToList();

        Assert.Equal(2, slots.Count);
        Assert.Contains(slots, s => s.DayOfWeek == DayOfWeek.Monday);
        Assert.Contains(slots, s => s.DayOfWeek == DayOfWeek.Tuesday);
    }

    [Fact]
    public void ClassPeriods_FilterOutBreaks()
    {
        using var db = TestDbContextFactory.Create();
        TestData.SeedBasic(db);

        var teaching = db.ClassPeriods.Where(p => !p.IsBreak).OrderBy(p => p.Order).ToList();

        Assert.Equal(2, teaching.Count);
        Assert.All(teaching, p => Assert.False(p.IsBreak));
    }

    [Fact]
    public void HeadTeacher_ResolvedThroughNavigation()
    {
        using var db = TestDbContextFactory.Create();
        TestData.SeedBasic(db);

        var cls = db.Classes.Include(c => c.HeadTeacher).Single();

        Assert.NotNull(cls.HeadTeacher);
        Assert.Equal("Mary", cls.HeadTeacher.FirstName);
    }

    [Fact]
    public void NewGradeIsAssignedAutoIncrementId()
    {
        using var db = TestDbContextFactory.Create();
        var seed = TestData.SeedBasic(db);

        var g = new Grade
        {
            StudentId = seed.Bob.Id,
            TeacherId = seed.MathTeacher.Id,
            SubjectId = seed.Math.Id,
            Value = 5,
            Type = GradeType.Test,
            Date = DateTime.Now
        };

        db.Grades.Add(g);
        db.SaveChanges();

        Assert.NotEqual(0, g.Id);
        Assert.Equal(7, db.Grades.Count());
    }
}
