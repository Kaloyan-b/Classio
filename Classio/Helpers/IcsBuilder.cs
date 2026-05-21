using Classio.Models;
using System.Text;

namespace Classio.Helpers
{
    /// <summary>
    /// Builds an iCalendar (.ics) file from schedule slots.
    /// Each slot becomes a weekly recurring VEVENT — compatible with Google Calendar,
    /// Outlook, and Apple Calendar.
    /// </summary>
    public static class IcsBuilder
    {
        public record ScheduleEntry(
            DayOfWeek Day,
            TimeSpan StartTime,
            TimeSpan EndTime,
            string Summary,
            string Description
        );

        public static byte[] Build(IEnumerable<ScheduleEntry> entries, string calendarName)
        {
            var sb = new StringBuilder();

            sb.AppendLine("BEGIN:VCALENDAR");
            sb.AppendLine("VERSION:2.0");
            sb.AppendLine("PRODID:-//Classio//School Schedule//EN");
            sb.AppendLine("CALSCALE:GREGORIAN");
            sb.AppendLine("METHOD:PUBLISH");
            sb.AppendLine($"X-WR-CALNAME:{calendarName}");
            sb.AppendLine("X-WR-TIMEZONE:Europe/Sofia");

            foreach (var entry in entries)
            {
                // Find the next occurrence of this weekday starting from today
                var anchor = NextWeekday(DateTime.Today, entry.Day);
                var dtStart = anchor.Date + entry.StartTime;
                var dtEnd   = anchor.Date + entry.EndTime;

                var byday = entry.Day switch
                {
                    DayOfWeek.Monday    => "MO",
                    DayOfWeek.Tuesday   => "TU",
                    DayOfWeek.Wednesday => "WE",
                    DayOfWeek.Thursday  => "TH",
                    DayOfWeek.Friday    => "FR",
                    DayOfWeek.Saturday  => "SA",
                    DayOfWeek.Sunday    => "SU",
                    _ => "MO"
                };

                var uid = $"classio-{entry.Day}-{entry.StartTime.Ticks}@classio.local";

                sb.AppendLine("BEGIN:VEVENT");
                sb.AppendLine($"UID:{uid}");
                sb.AppendLine($"DTSTART;TZID=Europe/Sofia:{dtStart:yyyyMMddTHHmmss}");
                sb.AppendLine($"DTEND;TZID=Europe/Sofia:{dtEnd:yyyyMMddTHHmmss}");
                sb.AppendLine($"RRULE:FREQ=WEEKLY;BYDAY={byday}");
                sb.AppendLine($"SUMMARY:{Escape(entry.Summary)}");
                sb.AppendLine($"DESCRIPTION:{Escape(entry.Description)}");
                sb.AppendLine($"DTSTAMP:{DateTime.UtcNow:yyyyMMddTHHmmssZ}");
                sb.AppendLine("END:VEVENT");
            }

            sb.AppendLine("END:VCALENDAR");

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private static DateTime NextWeekday(DateTime from, DayOfWeek target)
        {
            int diff = ((int)target - (int)from.DayOfWeek + 7) % 7;
            return from.AddDays(diff == 0 ? 0 : diff);
        }

        private static string Escape(string s) =>
            s.Replace("\\", "\\\\").Replace(",", "\\,").Replace(";", "\\;").Replace("\n", "\\n");
    }
}
