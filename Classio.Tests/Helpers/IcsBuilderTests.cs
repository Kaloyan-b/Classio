using System.Text;
using Classio.Helpers;

namespace Classio.Tests.Helpers;

public class IcsBuilderTests
{
    private static string BuildString(IEnumerable<IcsBuilder.ScheduleEntry> entries, string name = "Test")
        => Encoding.UTF8.GetString(IcsBuilder.Build(entries, name));

    [Fact]
    public void Build_EmptyEntries_ProducesValidVCalendarShell()
    {
        var ics = BuildString(Array.Empty<IcsBuilder.ScheduleEntry>(), "MyCal");

        Assert.StartsWith("BEGIN:VCALENDAR", ics);
        Assert.Contains("VERSION:2.0", ics);
        Assert.Contains("PRODID:-//Classio//School Schedule//EN", ics);
        Assert.Contains("X-WR-CALNAME:MyCal", ics);
        Assert.Contains("X-WR-TIMEZONE:Europe/Sofia", ics);
        Assert.EndsWith("END:VCALENDAR" + Environment.NewLine, ics);
        Assert.DoesNotContain("BEGIN:VEVENT", ics);
    }

    [Fact]
    public void Build_SingleEntry_EmitsOneVEventWithCorrectFields()
    {
        var entries = new[]
        {
            new IcsBuilder.ScheduleEntry(
                DayOfWeek.Monday,
                new TimeSpan(9, 0, 0),
                new TimeSpan(9, 45, 0),
                "Math",
                "Teacher: Mary")
        };

        var ics = BuildString(entries);

        Assert.Single(System.Text.RegularExpressions.Regex.Matches(ics, "BEGIN:VEVENT"));
        Assert.Contains("SUMMARY:Math", ics);
        Assert.Contains("DESCRIPTION:Teacher: Mary", ics);
        Assert.Contains("RRULE:FREQ=WEEKLY;BYDAY=MO", ics);
        Assert.Matches(@"DTSTART;TZID=Europe/Sofia:\d{8}T090000", ics);
        Assert.Matches(@"DTEND;TZID=Europe/Sofia:\d{8}T094500", ics);
        Assert.Matches(@"DTSTAMP:\d{8}T\d{6}Z", ics);
    }

    [Theory]
    [InlineData(DayOfWeek.Monday,    "MO")]
    [InlineData(DayOfWeek.Tuesday,   "TU")]
    [InlineData(DayOfWeek.Wednesday, "WE")]
    [InlineData(DayOfWeek.Thursday,  "TH")]
    [InlineData(DayOfWeek.Friday,    "FR")]
    [InlineData(DayOfWeek.Saturday,  "SA")]
    [InlineData(DayOfWeek.Sunday,    "SU")]
    public void Build_MapsAllWeekdaysToCorrectByDayCode(DayOfWeek day, string expectedCode)
    {
        var entry = new IcsBuilder.ScheduleEntry(day, TimeSpan.FromHours(9), TimeSpan.FromHours(10), "S", "D");
        var ics = BuildString(new[] { entry });

        Assert.Contains($"BYDAY={expectedCode}", ics);
    }

    [Fact]
    public void Build_MultipleEntries_EmitsAllVEventsAndUniqueUids()
    {
        var entries = new[]
        {
            new IcsBuilder.ScheduleEntry(DayOfWeek.Monday,  new TimeSpan(8,0,0),  new TimeSpan(8,45,0), "Math",    "x"),
            new IcsBuilder.ScheduleEntry(DayOfWeek.Tuesday, new TimeSpan(9,0,0),  new TimeSpan(9,45,0), "English", "y"),
            new IcsBuilder.ScheduleEntry(DayOfWeek.Friday,  new TimeSpan(10,0,0), new TimeSpan(10,45,0),"Physics", "z")
        };

        var ics = BuildString(entries);

        var begins = System.Text.RegularExpressions.Regex.Matches(ics, "BEGIN:VEVENT").Count;
        var ends   = System.Text.RegularExpressions.Regex.Matches(ics, "END:VEVENT").Count;
        Assert.Equal(3, begins);
        Assert.Equal(3, ends);

        var uids = System.Text.RegularExpressions.Regex.Matches(ics, @"UID:[^\r\n]+")
            .Select(m => m.Value).ToList();
        Assert.Equal(3, uids.Count);
        Assert.Equal(uids.Count, uids.Distinct().Count());
    }

    [Fact]
    public void Build_EscapesSpecialCharactersInSummaryAndDescription()
    {
        var entries = new[]
        {
            new IcsBuilder.ScheduleEntry(
                DayOfWeek.Monday,
                new TimeSpan(8, 0, 0),
                new TimeSpan(9, 0, 0),
                @"Math, Period; Year\1",
                "line1\nline2")
        };

        var ics = BuildString(entries);

        Assert.Contains(@"SUMMARY:Math\, Period\; Year\\1", ics);
        Assert.Contains(@"DESCRIPTION:line1\nline2", ics);
    }

    [Fact]
    public void Build_OutputIsUtf8Encoded()
    {
        var bytes = IcsBuilder.Build(Array.Empty<IcsBuilder.ScheduleEntry>(), "Календар");
        var text = Encoding.UTF8.GetString(bytes);

        // Cyrillic preserved (UTF-8 round-trip).
        Assert.Contains("Календар", text);
    }
}
