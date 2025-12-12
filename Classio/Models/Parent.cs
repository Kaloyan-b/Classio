using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Classio.Models
{


    public class Parent
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set;}
        [Required]
        public string LastName { get; set; }

        public string UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        public ICollection<Student> StudentsAsParent1 { get; set; } = new List<Student>();
        public ICollection<Student> StudentsAsParent2 { get; set; } = new List<Student>();

    }
}