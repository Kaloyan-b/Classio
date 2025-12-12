using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Classio.Models
{
    public class Teacher
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }


        public string UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        public int SubjectId { get; set; }
        public Subject Subject { get; set; }

        public ICollection<Grade> Grades { get; set; } = new List<Grade>();


        public ICollection<Class> Classes { get; set; } = new List<Class>();
        
        public int SchoolId { get; set; }
        [ForeignKey(nameof(SchoolId))]
        public School School { get; set; }



    }
}