using Classio.Models;

namespace Classio.ViewModels
{
    public class StudentDashboardViewModel
    {
        public string StudentName { get; set; }
        public string ClassName { get; set; }

        public List<GradeViewModel> Grades { get; set; } = new();
        public List<AbsenceViewModel> Absences { get; set; } = new();
        public List<ScheduleViewModel> Schedule { get; set; } = new();
    }
}
