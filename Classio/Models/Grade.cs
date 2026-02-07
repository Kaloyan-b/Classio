using Classio.Core.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Classio.Models
{
    public class Grade
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public double Value { get; set; }
        public GradeType Type { get; set; } = GradeType.Other;
        public string? Description { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public int StudentId { get; set; }
        [ForeignKey(nameof(StudentId))]
        
        public Student Student { get; set; }
        public int TeacherId { get; set; }
        [ForeignKey(nameof(TeacherId))]
        public Teacher Teacher { get; set; }

        public int SubjectId { get; set; }
        [ForeignKey(nameof(SubjectId))]
        public Subject Subject { get; set; }
        
    }
}