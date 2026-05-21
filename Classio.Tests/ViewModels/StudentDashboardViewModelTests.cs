using Classio.Areas.Student.Models;
using Classio.Areas.Teacher.Models;

namespace Classio.Tests.ViewModels;

public class StudentDashboardViewModelTests
{
    [Theory]
    [InlineData(0,   "-")]
    [InlineData(-1,  "-")]
    [InlineData(1,   "1st")]
    [InlineData(2,   "2nd")]
    [InlineData(3,   "3rd")]
    [InlineData(4,   "4th")]
    [InlineData(10,  "10th")]
    [InlineData(11,  "11th")]
    [InlineData(12,  "12th")]
    [InlineData(13,  "13th")]
    [InlineData(14,  "14th")]
    [InlineData(21,  "21st")]
    [InlineData(22,  "22nd")]
    [InlineData(23,  "23rd")]
    [InlineData(101, "101st")]
    [InlineData(111, "111th")]
    [InlineData(112, "112th")]
    [InlineData(113, "113th")]
    [InlineData(121, "121st")]
    public void FormattedRank_ProducesCorrectOrdinalSuffix(int rank, string expected)
    {
        var vm = new StudentDashboardViewModel { SchoolRank = rank };
        Assert.Equal(expected, vm.FormattedRank);
    }

    [Fact]
    public void AbsenceDetail_Weight_IsHalfForLate()
    {
        var d = new AbsenceDetail { State = AttendanceState.Late };
        Assert.Equal(0.5, d.Weight);
    }

    [Theory]
    [InlineData(AttendanceState.Absent)]
    [InlineData(AttendanceState.Present)]
    public void AbsenceDetail_Weight_IsOneForNonLateStates(AttendanceState state)
    {
        var d = new AbsenceDetail { State = state };
        Assert.Equal(1.0, d.Weight);
    }

    [Fact]
    public void StudentScheduleViewModel_HasFiveWeekdaysOnly()
    {
        var vm = new StudentScheduleViewModel();
        Assert.Equal(5, vm.Days.Count);
        Assert.Equal(DayOfWeek.Monday, vm.Days.First());
        Assert.Equal(DayOfWeek.Friday, vm.Days.Last());
        Assert.DoesNotContain(DayOfWeek.Saturday, vm.Days);
        Assert.DoesNotContain(DayOfWeek.Sunday, vm.Days);
    }
}
