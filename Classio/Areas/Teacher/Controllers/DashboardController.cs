using Classio.Areas.Teacher.Models;
using Classio.Core.Models;
using Classio.Data;
using Classio.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Classio.Areas.Teacher.Controllers
{
    [Area("Teacher")]
    [Authorize(Roles = "Teacher")]
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

            var teacher = await _context.Teachers
                .Include(t => t.HeadOfClasses)
                    .ThenInclude(c => c.School)
                .Include(t => t.ClassesTaught)
                    .ThenInclude(c => c.School)
                .Include(t => t.Subject)
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (teacher == null)
            {
                return View("Error", "Teacher profile not found.");
            }

            return View(teacher);
        }

        [HttpGet]
        public async Task<IActionResult> ClassGradebook(int id, DateTime? date, int? periodId)
        {
            var userId = _userManager.GetUserId(User);
            var teacher = await _context.Teachers
                .Include(t => t.Subject)
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (teacher == null) return Forbid();

            var scheduleSlots = await _context.ScheduleSlots
                .Include(s => s.ClassPeriod)
                .Where(s => s.ClassId == id && s.SubjectId == teacher.SubjectId)
                .ToListAsync();

            var availableSessions = new List<SessionOption>();
            var today = DateTime.Today;
            var now = DateTime.Now.TimeOfDay;

            for (int i = 0; i < 30; i++)
            {
                var checkDate = today.AddDays(-i);
                var slotsOnDay = scheduleSlots.Where(s => s.DayOfWeek == checkDate.DayOfWeek).ToList();

                foreach (var slot in slotsOnDay)
                {
                    if (i == 0 && slot.ClassPeriod.StartTime > now) continue;

                    availableSessions.Add(new SessionOption
                    {
                        Date = checkDate,
                        PeriodId = slot.ClassPeriodId,
                        Label = $"{checkDate:ddd, dd MMM} - {slot.ClassPeriod.Name} ({slot.ClassPeriod.StartTime:hh\\:mm})",
                        IsCurrent = (i == 0 && now >= slot.ClassPeriod.StartTime && now <= slot.ClassPeriod.EndTime)
                    });
                }
            }

            var selectedSession = availableSessions.FirstOrDefault(s => s.Date == date && s.PeriodId == periodId)
                                ?? availableSessions.FirstOrDefault(s => s.IsCurrent)
                                ?? availableSessions.FirstOrDefault();

            if (selectedSession == null)
            {
                return View("Error", "No scheduled classes found for this subject in the last 30 days.");
            }

            var schoolClass = await _context.Classes
                .Include(c => c.Students)
                    .ThenInclude(s => s.Grades)
                        .ThenInclude(g => g.Teacher)
                .Include(c => c.Students)
                    .ThenInclude(s => s.Absences)
                .FirstOrDefaultAsync(c => c.Id == id);

            var targetDate = selectedSession.Date.Date;

            var model = new ClassGradebookViewModel
            {
                ClassId = schoolClass.Id,
                ClassName = schoolClass.Name,
                SubjectName = teacher.Subject.Name,
                SubjectId = teacher.SubjectId,
                SelectedDate = selectedSession.Date,
                SelectedPeriodId = selectedSession.PeriodId,
                AvailableSessions = availableSessions,
                Students = schoolClass.Students.Select(s =>
                {
                    var absenceRecord = s.Absences.FirstOrDefault(a => a.Date.Date == targetDate);

                    return new StudentGradeItem
                    {
                        StudentId = s.Id,
                        StudentName = $"{s.FirstName} {s.LastName}",
                        AttendanceToday = absenceRecord != null ? absenceRecord.AttendanceState : AttendanceState.Present,
                        ExistingGrades = s.Grades
                            .Where(g => g.SubjectId == teacher.SubjectId)
                            .OrderByDescending(g => g.Date)
                            .Select(g => new SimpleGradeHistory
                            {
                                Id = g.Id,
                                Value = g.Value,
                                Type = g.Type,
                                Date = g.Date,
                                Description = g.Description,
                                TeacherName = g.Teacher != null ? $"{g.Teacher.FirstName} {g.Teacher.LastName}" : "Unknown"
                            }).ToList()
                    };
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSession(ClassGradebookViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);
            if (teacher == null) return Forbid();

            var sessionDate = model.SelectedDate;

            foreach (var item in model.Students)
            {
                var existingAbsence = await _context.Absences
                    .FirstOrDefaultAsync(a => a.StudentId == item.StudentId
                                           && a.Date.Date == sessionDate.Date
                                           && a.SubjectId == model.SubjectId);

                if (item.AttendanceToday == AttendanceState.Present)
                {
                    if (existingAbsence != null)
                    {
                        _context.Absences.Remove(existingAbsence);
                    }
                }
                else
                {
                    if (existingAbsence == null)
                    {
                        var abs = new Absence
                        {
                            StudentId = item.StudentId,
                            Date = sessionDate,
                            AttendanceState = item.AttendanceToday,
                            SubjectId = model.SubjectId
                        };
                        _context.Absences.Add(abs);
                    }
                    else
                    {
                        existingAbsence.AttendanceState = item.AttendanceToday;
                        _context.Update(existingAbsence);
                    }
                }

                // new grades
                if (item.NewGrade.HasValue)
                {
                    if (item.NewGrade.Value >= 2 && item.NewGrade.Value <= 6)
                    {
                        var newGrade = new Grade
                        {
                            StudentId = item.StudentId,
                            SubjectId = model.SubjectId,
                            TeacherId = teacher.Id,
                            Value = item.NewGrade.Value,
                            Type = model.BatchGradeType,
                            Description = model.BatchDescription,
                            Date = DateTime.Now
                        };
                        _context.Add(newGrade);
                    }
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Session saved successfully!";

            return RedirectToAction(nameof(ClassGradebook), new
            {
                id = model.ClassId,
                date = model.SelectedDate,
                periodId = model.SelectedPeriodId
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditGrade(int GradeId, double NewValue, GradeType NewType, string NewDescription, int ReturnClassId, DateTime ReturnDate, int ReturnPeriodId)
        {
            var grade = await _context.Grades.FindAsync(GradeId);
            if (grade != null)
            {
                grade.Value = NewValue;
                grade.Type = NewType;
                grade.Description = NewDescription;
                _context.Update(grade);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(ClassGradebook), new { id = ReturnClassId, date = ReturnDate, periodId = ReturnPeriodId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGrade(int GradeId, int ReturnClassId, DateTime ReturnDate, int ReturnPeriodId)
        {
            var grade = await _context.Grades.FindAsync(GradeId);
            if (grade != null)
            {
                _context.Grades.Remove(grade);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ClassGradebook), new { id = ReturnClassId, date = ReturnDate, periodId = ReturnPeriodId });
        }
    }
}