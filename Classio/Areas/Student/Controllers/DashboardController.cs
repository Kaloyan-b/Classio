using Classio.Areas.Student.Models;
using Classio.Areas.Teacher.Models;
using Classio.Data;
using Classio.Helpers;
using Classio.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Classio.Areas.Student.Controllers
{
    [Area("Student")]
    [Authorize(Roles = "Student")]
    public class DashboardController : Controller
    {
        private readonly ClassioDbContext _context;
        private readonly UserManager<User> _userManager;

        public DashboardController(ClassioDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var student = await _context.Students
                .Include(s => s.Grades)
                .Include(s => s.Absences)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null) return NotFound("Student profile not found.");


            double overallAvg = student.Grades.Any() ? Math.Round(student.Grades.Average(g => g.Value), 2) : 0;

            //Rank List
            var allStudents = await _context.Students
                .Include(s => s.Grades)
                .ToListAsync();

            var rankedList = allStudents.Select(s => new
            {
                StudentId = s.Id,
                Gpa = s.Grades.Any() ? s.Grades.Average(g => g.Value) : 0
            })
                .OrderByDescending(x => x.Gpa)
                .ToList();

            int rank = rankedList.FindIndex(x => x.StudentId == student.Id) + 1;
            int totalStudentCount = rankedList.Count;


            int totalAbsences = student.Absences.Count(a => a.AttendanceState == AttendanceState.Absent);
            int totalLates = student.Absences.Count(a => a.AttendanceState == AttendanceState.Late);
            double weightedAbsences = totalAbsences + (totalLates * 0.5);

            var model = new StudentDashboardViewModel
            {
                StudentName = $"{student.FirstName} {student.LastName}",
                OverallAverage = overallAvg,
                WeightedAbsences = weightedAbsences,
                SchoolRank = rank,
                TotalStudents = totalStudentCount
            };

            return View(model);
        }

        // 2. GRADES DRILL-DOWN
        public async Task<IActionResult> Grades()
        {
            var userId = _userManager.GetUserId(User);
            var student = await _context.Students
                .Include(s => s.Grades)
                    .ThenInclude(g => g.Subject)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null) return NotFound();

            var model = new StudentGradesViewModel();

            // Group grades by SubjectId
            var groupedGrades = student.Grades.GroupBy(g => g.SubjectId);

            foreach (var group in groupedGrades)
            {
                var subjectName = group.First().Subject.Name;
                var perf = new SubjectPerformance
                {
                    SubjectName = subjectName,
                    AverageGrade = Math.Round(group.Average(g => g.Value), 2),
                    Grades = group.Select(g => new GradeDetail
                    {
                        Value = g.Value,
                        Type = g.Type,
                        Description = g.Description,
                        Date = g.Date
                    }).OrderByDescending(g => g.Date).ToList()
                };
                model.Subjects.Add(perf);
            }

            return View(model);
        }

        public async Task<IActionResult> Schedule()
        {
            var userId = _userManager.GetUserId(User);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null) return NotFound();

            var periods = await _context.ClassPeriods
                .Where(p => !p.IsBreak)
                .OrderBy(p => p.Order)
                .ToListAsync();

            var slots = await _context.ScheduleSlots
                .Include(s => s.Subject)
                .Include(s => s.Teacher)
                .Include(s => s.ClassPeriod)
                .Where(s => s.ClassId == student.ClassId)
                .ToListAsync();

            var days = new[] {
                DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                DayOfWeek.Thursday, DayOfWeek.Friday
            };

            var slotMap = days.ToDictionary(
                day => day,
                day => periods.ToDictionary(
                    p => p.Id,
                    p => slots
                        .Where(s => s.DayOfWeek == day && s.ClassPeriodId == p.Id)
                        .Select(s => new ScheduleSlotInfo
                        {
                            SubjectName = s.Subject.Name,
                            TeacherName = $"{s.Teacher.FirstName} {s.Teacher.LastName}",
                            StartTime = s.ClassPeriod.StartTime.ToString(@"hh\:mm"),
                            EndTime = s.ClassPeriod.EndTime.ToString(@"hh\:mm")
                        })
                        .FirstOrDefault()
                )
            );

            var model = new StudentScheduleViewModel
            {
                Periods = periods,
                Slots = slotMap
            };

            return View(model);
        }

        public async Task<IActionResult> ExportSchedule()
        {
            var userId = _userManager.GetUserId(User);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null) return NotFound();

            var slots = await _context.ScheduleSlots
                .Include(s => s.ClassPeriod)
                .Include(s => s.Subject)
                .Include(s => s.Teacher)
                .Where(s => s.ClassId == student.ClassId)
                .ToListAsync();

            var entries = slots.Select(s => new IcsBuilder.ScheduleEntry(
                Day: s.DayOfWeek,
                StartTime: s.ClassPeriod.StartTime,
                EndTime: s.ClassPeriod.EndTime,
                Summary: s.Subject.Name,
                Description: $"Teacher: {s.Teacher.FirstName} {s.Teacher.LastName}"
            ));

            var ics = IcsBuilder.Build(entries, $"Classio Schedule – {student.FirstName} {student.LastName}");
            return File(ics, "text/calendar", "classio-schedule.ics");
        }

        public async Task<IActionResult> Attendance()
        {
            var userId = _userManager.GetUserId(User);

            var student = await _context.Students
                .Include(s => s.Absences)
                    .ThenInclude(a => a.Subject)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null) return NotFound();

            var scheduleSlots = await _context.ScheduleSlots
                .Include(s => s.Teacher)
                .Where(s => s.ClassId == student.ClassId)
                .ToListAsync();

            var subjectTeacherMap = scheduleSlots
                .GroupBy(s => s.SubjectId)
                .ToDictionary(g => g.Key, g => $"{g.First().Teacher.FirstName} {g.First().Teacher.LastName}");

            var model = new StudentAttendanceViewModel
            {
                TotalWeightedAbsences = student.Absences.Count(a => a.AttendanceState == AttendanceState.Absent) * 1.0
                                      + student.Absences.Count(a => a.AttendanceState == AttendanceState.Late) * 0.5,

                Absences = student.Absences.Select(a => new AbsenceDetail
                {
                    Date = a.Date,
                    SubjectName = a.Subject?.Name ?? "Unknown Subject",
                    TeacherName = subjectTeacherMap.TryGetValue(a.SubjectId, out var name) ? name : "Unknown Teacher",
                    State = a.AttendanceState,
                }).OrderByDescending(a => a.Date).ToList()
            };

            return View(model);
        }
    }
}
