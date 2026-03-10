using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Classio.Areas.Admin.Models
{
    public class AdminParentEditViewModel
    {
        public int ParentId { get; set; }

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty; 

        [Display(Name = "Linked Students")]
        public List<int> SelectedStudentIds { get; set; } = new List<int>();

        public MultiSelectList? AvailableStudents { get; set; }
    }
}