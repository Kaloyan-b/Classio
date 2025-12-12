using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Classio.Models
{
    public class User : IdentityUser
    {
        [Required]
        [MaxLength(20)]
        public string FirstName { get; set; }
        [Required]
        [MaxLength(20)]
        public string LastName { get; set; }

        //Navigation
        public Student? Student { get; set; }
        public Teacher? Teacher { get; set; }
        public Parent? Parent { get; set; }

    }
}
