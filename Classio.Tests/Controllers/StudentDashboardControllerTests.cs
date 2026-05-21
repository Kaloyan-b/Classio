using Classio.Areas.Student.Controllers;
using Classio.Areas.Student.Models;
using Classio.Areas.Teacher.Models;
using Classio.Models;
using Classio.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Classio.Tests.Controllers;

public class StudentDashboardControllerTests
{
    private static DashboardController BuildController(
        Classio.Data.ClassioDbContext db,
        string returnedUserId)
    {
        var userManager = TestUserManager.Create(returnedUserId);
        return new DashboardController(db, userManager.Object).WithHttpContext();
    }

    [Fact]
    public async Task Index_ReturnsNotFound_WhenStudentMissing()
    {
        using var db = TestDbContextFactory.Create();
        TestData.SeedBasic(db);

        var ctrl = BuildController(db, "no-such-user");
        var result = await ctrl.Index();

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Index_ReturnsViewWithCorrectAggregates_ForKnownStudent()
    {
        using var db = TestDbContextFactory.Create();
        var seed = TestData.SeedBasic(db);

        var ctrl = BuildController(db, seed.Alice.UserId);
        var result = await ctrl.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<StudentDashboardViewModel>(view.Model);

        Assert.Equal("Alice Anderson", model.StudentName);
        Assert.Equal(4.5, model.OverallAverage);
        // Alice has 1 absent + 2 lates -> 1 + 2*0.5 = 2
        Assert.Equal(2.0, model.WeightedAbsences);
        Assert.Equal(1, model.SchoolRank);          // higher GPA than Bob
        Assert.Equal(2, model.TotalStudents);
    }

    [Fact]
    public async Task Index_RanksLowerAverageStudentBelow()
    {
        using var db = TestDbContextFactory.Create();
        var seed = TestData.SeedBasic(db);

        var ctrl = BuildController(db, seed.Bob.UserId);
        var result = await ctrl.Index();

        var view  = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<StudentDashboardViewModel>(view.Model);

        Assert.Equal(2, model.SchoolRank);
        Assert.Equal(3.0, model.OverallAverage);
    }

    [Fact]
    public async Task Grades_GroupsGradesBySubject()
    {
        using var db = TestDbContextFactory.Create();
        var seed = TestData.SeedBasic(db);

        var ctrl = BuildController(db, seed.Alice.UserId);
        var result = await ctrl.Grades();

        var view  = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<StudentGradesViewModel>(view.Model);

        Assert.Equal(2, model.Subjects.Count);

        var math = model.Subjects.Single(s => s.SubjectName == "Math");
        Assert.Equal(3, math.Grades.Count);
        Assert.Equal(5.0, math.AverageGrade);

        var english = model.Subjects.Single(s => s.SubjectName == "English");
        Assert.Single(english.Grades);
        Assert.Equal(3.0, english.AverageGrade);
    }

    [Fact]
    public async Task Grades_OrdersGradesNewestFirst()
    {
        using var db = TestDbContextFactory.Create();
        var seed = TestData.SeedBasic(db);

        var ctrl = BuildController(db, seed.Alice.UserId);
        var result = await ctrl.Grades();

        var view  = (ViewResult)result;
        var model = (StudentGradesViewModel)view.Model!;
        var math  = model.Subjects.Single(s => s.SubjectName == "Math");

        Assert.True(math.Grades[0].Date >= math.Grades[1].Date);
        Assert.True(math.Grades[1].Date >= math.Grades[2].Date);
    }

    [Fact]
    public async Task Schedule_ReturnsViewModel_WithSlotsKeyedByDayAndPeriod()
    {
        using var db = TestDbContextFactory.Create();
        var seed = TestData.SeedBasic(db);

        var ctrl = BuildController(db, seed.Alice.UserId);
        var result = await ctrl.Schedule();

        var view  = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<StudentScheduleViewModel>(view.Model);

        Assert.Equal(2, model.Periods.Count);                          // breaks excluded
        Assert.NotNull(model.Slots[DayOfWeek.Monday][seed.Period1.Id]);
        Assert.Equal("Math", model.Slots[DayOfWeek.Monday][seed.Period1.Id]!.SubjectName);
        Assert.Null(model.Slots[DayOfWeek.Monday][seed.Period2.Id]);
        Assert.Equal("English", model.Slots[DayOfWeek.Tuesday][seed.Period2.Id]!.SubjectName);
        Assert.Equal("08:00", model.Slots[DayOfWeek.Monday][seed.Period1.Id]!.StartTime);
    }

    [Fact]
    public async Task Schedule_ReturnsNotFound_WhenStudentMissing()
    {
        using var db = TestDbContextFactory.Create();
        TestData.SeedBasic(db);

        var ctrl = BuildController(db, "no-such-user");
        var result = await ctrl.Schedule();

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ExportSchedule_ReturnsIcsFileResult()
    {
        using var db = TestDbContextFactory.Create();
        var seed = TestData.SeedBasic(db);

        var ctrl = BuildController(db, seed.Alice.UserId);
        var result = await ctrl.ExportSchedule();

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/calendar", file.ContentType);
        Assert.Equal("classio-schedule.ics", file.FileDownloadName);

        var text = System.Text.Encoding.UTF8.GetString(file.FileContents);
        Assert.Contains("BEGIN:VCALENDAR", text);
        Assert.Contains("SUMMARY:Math", text);
        Assert.Contains("SUMMARY:English", text);
    }

    [Fact]
    public async Task Attendance_FixesAbsencesWithDefaultPresentState()
    {
        using var db = TestDbContextFactory.Create();
        var seed = TestData.SeedBasic(db);

        // Inject a "broken" absence whose state is the default Present (0).
        db.Absences.Add(new Absence
        {
            StudentId = seed.Alice.Id,
            SubjectId = seed.Math.Id,
            Date = new DateTime(2026, 4, 20),
            AttendanceState = AttendanceState.Present  // value 0 = "broken" per controller
        });
        db.SaveChanges();

        var ctrl = BuildController(db, seed.Alice.UserId);
        var result = await ctrl.Attendance();

        Assert.IsType<ViewResult>(result);
        Assert.Empty(db.Absences.Where(a => (int)a.AttendanceState == 0));
    }

    [Fact]
    public async Task Attendance_ProducesCorrectWeightedTotal()
    {
        using var db = TestDbContextFactory.Create();
        var seed = TestData.SeedBasic(db);

        var ctrl = BuildController(db, seed.Alice.UserId);
        var result = await ctrl.Attendance();

        var view  = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<StudentAttendanceViewModel>(view.Model);

        // 1 absent + 2 lates -> 1*1 + 2*0.5 = 2
        Assert.Equal(2.0, model.TotalWeightedAbsences);
        Assert.Equal(3, model.Absences.Count);
    }
}
