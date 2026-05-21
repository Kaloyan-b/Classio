using Classio.Areas.Teacher.Controllers;
using Classio.Areas.Teacher.Models;
using Classio.Models;
using Classio.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Classio.Tests.Controllers;

/// <summary>
/// Covers the Teacher gradebook session workflow: ClassGradebook (read)
/// and UpdateSession (POST that mutates absences and grades for one period).
/// </summary>
public class TeacherGradebookControllerTests
{
    private static DashboardController BuildController(
        Classio.Data.ClassioDbContext db,
        string returnedUserId)
    {
        var userManager = TestUserManager.Create(returnedUserId);
        return new DashboardController(db, userManager.Object).WithHttpContext();
    }

    [Fact]
    public async Task ClassGradebook_ReturnsErrorView_WhenNoRecentScheduledSessions()
    {
        using var db = TestDbContextFactory.Create();
        var seed = TestData.SeedBasic(db);

        // Remove all schedule slots for English so the English teacher has no recent sessions.
        db.ScheduleSlots.RemoveRange(db.ScheduleSlots.Where(s => s.SubjectId == seed.English.Id));
        db.SaveChanges();

        var ctrl = BuildController(db, seed.EnglishTeacher.UserId);
        var result = await ctrl.ClassGradebook(seed.Class.Id, null, null);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Error", view.ViewName);
    }

    [Fact]
    public async Task ClassGradebook_ReturnsForbid_WhenNoTeacher()
    {
        using var db = TestDbContextFactory.Create();
        var seed = TestData.SeedBasic(db);

        var ctrl = BuildController(db, "no-such-user");
        var result = await ctrl.ClassGradebook(seed.Class.Id, null, null);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UpdateSession_AddsNewAbsence_AndRecordsNewGrade()
    {
        using var db = TestDbContextFactory.Create();
        var seed = TestData.SeedBasic(db);

        var ctrl = BuildController(db, seed.MathTeacher.UserId);
        var sessionDate = new DateTime(2026, 4, 28);

        var model = new ClassGradebookViewModel
        {
            ClassId = seed.Class.Id,
            SubjectId = seed.Math.Id,
            SelectedDate = sessionDate,
            SelectedPeriodId = seed.Period1.Id,
            BatchGradeType = GradeType.Test,
            BatchDescription = "Surprise test",
            Students = new List<StudentGradeItem>
            {
                new() {
                    StudentId = seed.Bob.Id,
                    AttendanceToday = AttendanceState.Absent,
                    NewGrade = 4.5,
                }
            }
        };

        var gradeCountBefore = db.Grades.Count();
        var result = await ctrl.UpdateSession(model);

        Assert.IsType<RedirectToActionResult>(result);

        Assert.True(db.Absences.AsNoTracking().Any(a =>
            a.StudentId == seed.Bob.Id &&
            a.SubjectId == seed.Math.Id &&
            a.Date.Date == sessionDate.Date &&
            a.AttendanceState == AttendanceState.Absent));

        Assert.Equal(gradeCountBefore + 1, db.Grades.Count());
        var added = db.Grades.AsNoTracking()
            .Where(g => g.StudentId == seed.Bob.Id && g.Description == "Surprise test")
            .Single();
        Assert.Equal(4.5, added.Value);
        Assert.Equal(GradeType.Test, added.Type);
    }

    [Fact]
    public async Task UpdateSession_RemovesAbsence_WhenStudentMarkedPresent()
    {
        using var db = TestDbContextFactory.Create();
        var seed = TestData.SeedBasic(db);

        var sessionDate = new DateTime(2026, 4, 1); // Alice has an Absent record on this date
        Assert.True(db.Absences.Any(a =>
            a.StudentId == seed.Alice.Id && a.Date.Date == sessionDate));

        var ctrl = BuildController(db, seed.MathTeacher.UserId);
        var model = new ClassGradebookViewModel
        {
            ClassId = seed.Class.Id,
            SubjectId = seed.Math.Id,
            SelectedDate = sessionDate,
            SelectedPeriodId = seed.Period1.Id,
            Students = new List<StudentGradeItem>
            {
                new() {
                    StudentId = seed.Alice.Id,
                    AttendanceToday = AttendanceState.Present,
                }
            }
        };

        await ctrl.UpdateSession(model);

        Assert.False(db.Absences.AsNoTracking().Any(a =>
            a.StudentId == seed.Alice.Id &&
            a.SubjectId == seed.Math.Id &&
            a.Date.Date == sessionDate));
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(7.0)]
    public async Task UpdateSession_RejectsOutOfRangeGrades(double badValue)
    {
        using var db = TestDbContextFactory.Create();
        var seed = TestData.SeedBasic(db);

        var ctrl = BuildController(db, seed.MathTeacher.UserId);
        var before = db.Grades.Count();

        var model = new ClassGradebookViewModel
        {
            ClassId = seed.Class.Id,
            SubjectId = seed.Math.Id,
            SelectedDate = new DateTime(2026, 4, 28),
            SelectedPeriodId = seed.Period1.Id,
            BatchGradeType = GradeType.Test,
            Students = new List<StudentGradeItem>
            {
                new() { StudentId = seed.Bob.Id, AttendanceToday = AttendanceState.Present, NewGrade = badValue }
            }
        };

        await ctrl.UpdateSession(model);

        Assert.Equal(before, db.Grades.Count());
    }

    [Fact]
    public async Task UpdateSession_UpdatesExistingAbsence_WhenStateChanges()
    {
        using var db = TestDbContextFactory.Create();
        var seed = TestData.SeedBasic(db);

        // Alice already has an Absent on 2026-04-01 for Math.
        var sessionDate = new DateTime(2026, 4, 1);

        var ctrl = BuildController(db, seed.MathTeacher.UserId);
        var model = new ClassGradebookViewModel
        {
            ClassId = seed.Class.Id,
            SubjectId = seed.Math.Id,
            SelectedDate = sessionDate,
            SelectedPeriodId = seed.Period1.Id,
            Students = new List<StudentGradeItem>
            {
                new() {
                    StudentId = seed.Alice.Id,
                    AttendanceToday = AttendanceState.Late
                }
            }
        };

        await ctrl.UpdateSession(model);

        var rec = db.Absences.AsNoTracking().Single(a =>
            a.StudentId == seed.Alice.Id &&
            a.SubjectId == seed.Math.Id &&
            a.Date.Date == sessionDate);
        Assert.Equal(AttendanceState.Late, rec.AttendanceState);
    }
}
