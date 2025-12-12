using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace Classio.Models
{


    public class Student
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        public int? ClassId { get; set; }
        [ForeignKey(nameof(ClassId))]
        public Class Class { get; set; }

        public int? Parent1Id { get; set; }
        [ForeignKey(nameof(Parent1Id))]
        [InverseProperty(nameof(Parent.StudentsAsParent1))]
        public Parent? Parent1 { get; set; }

        public int? Parent2Id { get; set; }
        [ForeignKey(nameof(Parent2Id))]
        [InverseProperty(nameof(Parent.StudentsAsParent2))]
        public Parent? Parent2 { get; set; }

        public ICollection<Grade> Grades { get; set; } = new List<Grade>();
        public ICollection<Absence> Absences { get; set; } = new List<Absence>();
    }
}