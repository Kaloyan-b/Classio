using Classio.Areas.Teacher.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Classio.Models
{
    public class Absence
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public DateTime Date { get; set; } = DateTime.Now;
        public AttendanceState AttendanceState { get; set; } = 0;
        public int StudentId { get; set; }
        [ForeignKey(nameof(StudentId))]
        public Student Student { get; set; } = null!;

        public int SubjectId { get; set; }
        [ForeignKey(nameof(SubjectId))]
        public Subject Subject { get; set; } = null!;
    }
}