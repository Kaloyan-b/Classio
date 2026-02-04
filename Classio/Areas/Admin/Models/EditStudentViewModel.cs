using System.ComponentModel.DataAnnotations;

namespace Classio.Areas.Admin.Models
{
    public class EditStudentViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Display(Name = "Assigned Class")]
        public int? ClassId { get; set; }

        [Display(Name = "School")]
        public int? SchoolId { get; set; }

        // Optional: Assign Parents
        [Display(Name = "Primary Parent")]
        public int? Parent1Id { get; set; }

        [Display(Name = "Secondary Parent")]
        public int? Parent2Id { get; set; }
    }
}