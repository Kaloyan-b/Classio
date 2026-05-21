using Classio.Areas.Teacher.Controllers;
using Classio.Areas.Teacher.Models;
using Classio.Models;
using Classio.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClassioTeacher = Classio.Models.Teacher;

namespace Classio.Tests.Controllers;

public class TeacherDashboardControllerTests
{
    private static DashboardController BuildController(
        Classio.Data.ClassioDbContext db,
        string returnedUserId)
    {
        var userManager = TestUserManager.Create(returnedUserId);
        return new DashboardController(db, userManager.Object).WithHttpContext();
    }

    [Fact]
    public async Task Index_ReturnsErrorView_WhenTeacherMissing()
    {
        using var db = TestDbContextFactory.Create();
        TestData.SeedBasic(db);

        var ctrl = BuildController(db, "no-such-user");
        var result = await ctrl.Index();

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Error", view.ViewName);
    }

    [Fact]
    public async Task Index_ReturnsViewWithTeacherModel()
    {
        using var db = TestDbContextFactory.Create();
        var seed = TestData.SeedBasic(db);

        var ctrl = BuildController(db, seed.MathTeacher.UserId);
        var result = await ctrl.Index();

        var view  = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ClassioTeacher>(view.Model);
        Assert.Equal("Mary", model.FirstName);
        Assert.Equal("Math", model.Subject?.Name);
    }

    [Fact]
    public async Task Schedule_ReturnsForbid_WhenTeacherMissing()
    {
        using var db = TestDbContextFactory.Create();
        TestData.SeedBasic(db);

        var ctrl = BuildController(db, "no-such-user");
        var result = await ctrl.Schedule();

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Schedule_PopulatesViewBag_WithPeriodsDaysAndSlots()
    {
        using var db = TestDbContextFactory.Create();
        var seed = TestData.SeedBasic(db);

        var ctrl = BuildController(db, seed.MathTeacher.UserId);
        var result = await ctrl.Schedule();

        Assert.IsType<ViewResult>(result);

        var periods = Assert.IsAssignableFrom<List<ClassPeriod>>(ctrl.ViewBag.Periods);
        Assert.Equal(2, periods.Count);

        var days = Assert.IsAssignableFrom<DayOfWeek[]>(ctrl.ViewBag.Days);
        Assert.Equal(5, days.Length);

        var slots = Assert.IsAssignableFrom<Dictionary<DayOfWeek, Dictionary<int, TeacherScheduleSlotInfo?>>>(
            ctrl.ViewBag.Slots);
        Assert.NotNull(slots[DayOfWeek.Monday][seed.Period1.Id]);
        Assert.Equal("Math", slots[DayOfWeek.Monday][seed.Period1.Id]!.SubjectName);
        Assert.Equal("12A", slots[DayOfWeek.Monday][seed.Period1.Id]!.ClassName);
    }

    [Fact]
    public async Task ExportSchedule_ReturnsIcsFile_ForTeacher()
    {
        using var db = TestDbContextFactory.Create();
        var seed = TestData.SeedBasic(db);

        var ctrl = BuildController(db, seed.MathTeacher.UserId);
        var result = await ctrl.ExportSchedule();

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/calendar", file.ContentType);

        var text = System.Text.Encoding.UTF8.GetString(file.FileContents);
        Assert.Contains("Math", text);
        Assert.Contains("12A", text);
    }

    [Fact]
    public async Task ManageClass_ReturnsNotFound_WhenTeacherIsNotHeadOfClass()
    {
        using var db = TestDbContextFactory.Create();
        var seed = TestData.SeedBasic(db);

        // English teacher is not the head teacher of class 100
        var ctrl = BuildController(db, seed.EnglishTeacher.UserId);
        var result = await ctrl.ManageClass(seed.Class.Id);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ManageClass_ReturnsViewModelWithStudentRows()
    {
        using var db = TestDbContextFactory.Create();
        var seed = TestData.SeedBasic(db);

        var ctrl = BuildController(db, seed.MathTeacher.UserId);
        var result = await ctrl.ManageClass(seed.Class.Id);

        var view  = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ManageClassViewModel>(view.Model);

        Assert.Equal("12A", model.ClassName);
        Assert.Equal(2, model.Students.Count);

        // Sorted by name -> Alice first
        Assert.Equal("Alice Anderson", model.Students[0].StudentName);
        Assert.Equal(4.5, model.Students[0].OverallAverage);
        Assert.Equal(4, model.Students[0].TotalGrades);
        Assert.Equal(2.0, model.Students[0].WeightedAbsences);
    }

    [Fact]
    public async Task AddHomeroomGrade_AddsNewGrade_WhenValueWithinRange()
    {
        using var db = TestDbContextFactory.Create();
        var seed = TestData.SeedBasic(db);

        var before = db.Grades.Count();
        var ctrl   = BuildController(db, seed.MathTeacher.UserId);

        var result = await ctrl.AddHomeroomGrade(
            StudentId: seed.Alice.Id,
            ClassId: seed.Class.Id,
            SubjectId: seed.Math.Id,
            TeacherId: seed.MathTeacher.Id,
            Value: 5.5,
            Type: GradeType.Test,
            Description: "Midterm");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ManageStudent", redirect.ActionName);
        Assert.Equal(before + 1, db.Grades.Count());
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(7.0)]
    [InlineData(0.0)]
    public async Task AddHomeroomGrade_RejectsOutOfRangeValues(double badValue)
    {
        using var db = TestDbContextFactory.Create();
        var seed = TestData.SeedBasic(db);

        var before = db.Grades.Count();
        var ctrl   = BuildController(db, seed.MathTeacher.UserId);

        await ctrl.AddHomeroomGrade(
            StudentId: seed.Alice.Id,
            ClassId: seed.Class.Id,
            SubjectId: seed.Math.Id,
            TeacherId: seed.MathTeacher.Id,
            Value: badValue,
            Type: GradeType.Test,
            Description: "Bad");

        Assert.Equal(before, db.Grades.Count());
    }

    [Fact]
    public async Task EditHomeroomGrade_UpdatesGrade()
    {
        using var db = TestDbContextFactory.Create();
        var seed = TestData.SeedBasic(db);

        var ctrl = BuildController(db, seed.MathTeacher.UserId);
        await ctrl.EditHomeroomGrade(
            GradeId: 5001,
            NewValue: 2.0,
            NewType: GradeType.Quiz,
            NewDescription: "Reset",
            ReturnClassId: seed.Class.Id,
            ReturnStudentId: seed.Alice.Id);

        var updated = db.Grades.AsNoTracking().Single(g => g.Id == 5001);
        Assert.Equal(2.0, updated.Value);
        Assert.Equal(GradeType.Quiz, updated.Type);
        Assert.Equal("Reset", updated.Description);
    }

    [Fact]
    public async Task DeleteHomeroomGrade_RemovesGrade()
    {
        using var db = TestDbContextFactory.Create();
        var seed = TestData.SeedBasic(db);

        var ctrl = BuildController(db, seed.MathTeacher.UserId);
        await ctrl.DeleteHomeroomGrade(
            GradeId: 5001,
            ReturnClassId: seed.Class.Id,
            ReturnStudentId: seed.Alice.Id);

        Assert.Null(db.Grades.AsNoTracking().FirstOrDefault(g => g.Id == 5001));
    }

    [Fact]
    public async Task DeleteHomeroomAbsence_RemovesAbsence()
    {
        using var db = TestDbContextFactory.Create();
        var seed = TestData.SeedBasic(db);

        var ctrl = BuildController(db, seed.MathTeacher.UserId);
        await ctrl.DeleteHomeroomAbsence(
            AbsenceId: 7001,
            ReturnClassId: seed.Class.Id,
            ReturnStudentId: seed.Alice.Id);

        Assert.Null(db.Absences.AsNoTracking().FirstOrDefault(a => a.Id == 7001));
    }
}
