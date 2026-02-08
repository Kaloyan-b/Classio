using System.ComponentModel.DataAnnotations;

namespace Classio.Models
{
    public class ClassPeriod
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } 

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        public bool IsBreak { get; set; } = false;

        public int Order { get; set; }
    }
}
