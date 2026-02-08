using System.ComponentModel.DataAnnotations;

namespace Classio.Models
{
    public class ScheduleSlot
    {
        public int Id { get; set; }

        [Required]
        public DayOfWeek DayOfWeek { get; set; } 


        public int ClassId { get; set; }
        public Class SchoolClass { get; set; }

        public int ClassPeriodId { get; set; }
        public ClassPeriod ClassPeriod { get; set; }

        public int SubjectId { get; set; }
        public Subject Subject { get; set; }

        public int TeacherId { get; set; }
        public Teacher Teacher { get; set; }

        // Setup for Google Calendar
        public string? GoogleEventId { get; set; }
    }
}
