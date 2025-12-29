using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Classio.Models
{
    public class Class
    {
        [Key]
        public int Id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; }

        //Head teacher = Klasen
        public int? HeadTeacherId { get; set; }
        [ForeignKey(nameof(HeadTeacherId))]
        public Teacher HeadTeacher { get; set; }

        // All teachers that have classes with this class
        public ICollection<Teacher> SubjectTeachers { get; set; } = new List<Teacher>();

        public ICollection<Student> Students { get; set; } = new List<Student>();
        public int SchoolId { get; set; }
        [ForeignKey(nameof(SchoolId))]
        public School School { get; set; }

        public ICollection<Subject> Subjects { get; set; } = new List<Subject>();

        public ICollection<Absence> Absences { get; set; } = new List<Absence>();
    }
}