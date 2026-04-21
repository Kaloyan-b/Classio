
using Classio.Areas.Teacher.Models;
using Classio.Models;

namespace Classio.Areas.Student.Models
{
    public class StudentDashboardViewModel
    {
        public string StudentName { get; set; }
        public double OverallAverage { get; set; }
        public double WeightedAbsences { get; set; }

        public int SchoolRank { get; set; }
        public int TotalStudents { get; set; }
        public string FormattedRank
        {
            get
            {
                if (SchoolRank <= 0) return "-";
                if (SchoolRank % 100 >= 11 && SchoolRank % 100 <= 13) return SchoolRank + "th";
                return (SchoolRank % 10) switch
                {
                    1 => SchoolRank + "st",
                    2 => SchoolRank + "nd",
                    3 => SchoolRank + "rd",
                    _ => SchoolRank + "th"
                };
            }
        }
    }

    public class StudentGradesViewModel
    {
        public List<SubjectPerformance> Subjects { get; set; } = new();
    }

    public class SubjectPerformance
    {
        public string SubjectName { get; set; }
        public double AverageGrade { get; set; }
        public List<GradeDetail> Grades { get; set; } = new();
    }

    public class GradeDetail
    {
        public double Value { get; set; }
        public GradeType Type { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
    }

    public class StudentAttendanceViewModel
    {
        public double TotalWeightedAbsences { get; set; }
        public List<AbsenceDetail> Absences { get; set; } = new();

    }

    public class AbsenceDetail
    {
        public DateTime Date { get; set; }
        public string SubjectName { get; set; }
        public string TeacherName { get; set; }
        public AttendanceState State { get; set; }
        public double Weight => State == AttendanceState.Late ? 0.5 : 1.0;
    }

    public class StudentScheduleViewModel
    {
        // Ordered list of class periods (non-break)
        public List<ClassPeriod> Periods { get; set; } = new();
        // day -> periodId -> slot (null if no class)
        public Dictionary<DayOfWeek, Dictionary<int, ScheduleSlotInfo?>> Slots { get; set; } = new();
        public List<DayOfWeek> Days { get; } = new()
        {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
            DayOfWeek.Thursday, DayOfWeek.Friday
        };
    }

    public class ScheduleSlotInfo
    {
        public string SubjectName { get; set; }
        public string TeacherName { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
    }
}