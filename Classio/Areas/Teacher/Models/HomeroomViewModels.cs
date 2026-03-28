using Classio.Models;

namespace Classio.Areas.Teacher.Models
{
    public class ManageClassViewModel
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public string SchoolName { get; set; }
        public List<HomeroomStudentRow> Students { get; set; } = new();
    }

    public class HomeroomStudentRow
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public double OverallAverage { get; set; }
        public int TotalGrades { get; set; }
        public double WeightedAbsences { get; set; }
        public int TotalAbsences { get; set; }
    }

    public class ManageStudentViewModel
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public double OverallAverage { get; set; }
        public double WeightedAbsences { get; set; }

        public List<SubjectGradeGroup> SubjectGrades { get; set; } = new();
        public List<StudentAbsenceRow> Absences { get; set; } = new();

        // For adding new grades
        public List<SubjectOption> AvailableSubjects { get; set; } = new();
        public List<TeacherOption> AvailableTeachers { get; set; } = new();
    }

    public class SubjectGradeGroup
    {
        public string SubjectName { get; set; }
        public int SubjectId { get; set; }
        public double Average { get; set; }
        public List<HomeroomGradeRow> Grades { get; set; } = new();
    }

    public class HomeroomGradeRow
    {
        public int Id { get; set; }
        public double Value { get; set; }
        public GradeType Type { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public string TeacherName { get; set; }
        public int TeacherId { get; set; }
        public int SubjectId { get; set; }
    }

    public class StudentAbsenceRow
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string SubjectName { get; set; }
        public int SubjectId { get; set; }
        public AttendanceState State { get; set; }
        public double Weight => State == AttendanceState.Late ? 0.5 : 1.0;
    }

    public class SubjectOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class TeacherOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int SubjectId { get; set; }
    }
}
