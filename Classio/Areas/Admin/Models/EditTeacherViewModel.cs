using System.ComponentModel.DataAnnotations;

namespace Classio.Areas.Admin.Models
{
    public class EditTeacherViewModel
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

        [Required]
        [Display(Name = "Subject")]
        public int SubjectId { get; set; }

        [Required]
        [Display(Name = "School")]
        public int? SchoolId { get; set; }

        [Display(Name = "Head of Classes")]
        public List<int> HeadOfClassIds { get; set; } = new List<int>();

        [Display(Name = "Classes Taught")]
        public List<int> SubjectClassIds { get; set; } = new List<int>();
    }
}
