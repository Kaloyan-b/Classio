using System.ComponentModel.DataAnnotations;

namespace Classio.Areas.Admin.Models
{
    public class CreateClassViewModel
    {
        [Required]
        [Display(Name = "Class Name")]
        public string Name { get; set; }
        [Required]
        [Display(Name = "School")]
        public int SchoolId { get; set; }

        [Display(Name = "Head Teacher")]
        public int? HeadTeacherId { get; set; }
    }
}
