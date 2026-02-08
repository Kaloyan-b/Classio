using Classio.Core.Models;
using System.ComponentModel.DataAnnotations;

namespace Classio.Areas.Teacher.Models
{
    public class ClassGradebookViewModel
    {

        [Display(Name = "Assignment Type")]
        public GradeType BatchGradeType { get; set; } = GradeType.Test;

        [Display(Name = "Description")]
        public string? BatchDescription { get; set; }

        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public string SubjectName { get; set; }
        public int SubjectId { get; set; }

        public List<StudentGradeItem> Students { get; set; } = new List<StudentGradeItem>();
    }
    public enum AttendanceState
    {
        Present = 0,
        Absent = 1,
        Late = 2
    }

    public class StudentGradeItem
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }

        public AttendanceState AttendanceToday { get; set; } = AttendanceState.Present;

        public List<SimpleGradeHistory> ExistingGrades { get; set; } = new List<SimpleGradeHistory>();

        [Range(2, 6)]
        public double? NewGrade { get; set; }
    }

    public class SimpleGradeHistory
    {
        public int Id { get; set; } 
        public double Value { get; set; }
        public GradeType Type { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public string TeacherName { get; set; }
    }
}