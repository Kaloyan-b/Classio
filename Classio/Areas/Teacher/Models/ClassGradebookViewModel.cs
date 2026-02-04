namespace Classio.Areas.Teacher.Models
{
    public class ClassGradebookViewModel
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public string SubjectName { get; set; }
        public int SubjectId { get; set; }

        public List<StudentGradeItem> Students { get; set; } = new List<StudentGradeItem>();
    }

    public class StudentGradeItem
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public List<double> ExistingGrades { get; set; } = new List<double>();


        [System.ComponentModel.DataAnnotations.Range(2, 6, ErrorMessage = "Grade must be between 2 and 6")]
        public double? NewGrade { get; set; }
    }
}