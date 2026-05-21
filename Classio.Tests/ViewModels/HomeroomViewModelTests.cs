using Classio.Areas.Teacher.Models;

namespace Classio.Tests.ViewModels;

public class HomeroomViewModelTests
{
    [Fact]
    public void StudentAbsenceRow_Weight_IsHalfForLate()
    {
        var row = new StudentAbsenceRow { State = AttendanceState.Late };
        Assert.Equal(0.5, row.Weight);
    }

    [Theory]
    [InlineData(AttendanceState.Absent)]
    [InlineData(AttendanceState.Present)]
    public void StudentAbsenceRow_Weight_IsOneForNonLateStates(AttendanceState state)
    {
        var row = new StudentAbsenceRow { State = state };
        Assert.Equal(1.0, row.Weight);
    }
}
