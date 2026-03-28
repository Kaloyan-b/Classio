using Classio.Areas.Teacher.Models;
using Classio.Models;
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

        // Homeroom
        [HttpGet]
        public async Task<IActionResult> ManageClass(int id)
        {
            var userId = _userManager.GetUserId(User);
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);
            if (teacher == null) return Forbid();

            var cls = await _context.Classes
                .Include(c => c.School)
                .Include(c => c.Students).ThenInclude(s => s.Grades)
                .Include(c => c.Students).ThenInclude(s => s.Absences)
                .FirstOrDefaultAsync(c => c.Id == id && c.HeadTeacherId == teacher.Id);

            if (cls == null) return NotFound();

            var model = new ManageClassViewModel
            {
                ClassId = cls.Id,
                ClassName = cls.Name,
                SchoolName = cls.School?.Name ?? "",
                Students = cls.Students.Select(s => new HomeroomStudentRow
                {
                    StudentId = s.Id,
                    StudentName = $"{s.FirstName} {s.LastName}",
                    OverallAverage = s.Grades.Any() ? Math.Round(s.Grades.Average(g => g.Value), 2) : 0,
                    TotalGrades = s.Grades.Count,
                    WeightedAbsences = s.Absences.Sum(a => a.AttendanceState == Areas.Teacher.Models.AttendanceState.Late ? 0.5 : 1.0),
                    TotalAbsences = s.Absences.Count
                }).OrderBy(s => s.StudentName).ToList()
            };

            return View(model);
        }

        //Student detail
        [HttpGet]
        public async Task<IActionResult> ManageStudent(int classId, int studentId)
        {
            var userId = _userManager.GetUserId(User);
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);
            if (teacher == null) return Forbid();

            var cls = await _context.Classes.FirstOrDefaultAsync(c => c.Id == classId && c.HeadTeacherId == teacher.Id);
            if (cls == null) return NotFound();

            var student = await _context.Students
                .Include(s => s.Grades).ThenInclude(g => g.Subject)
                .Include(s => s.Grades).ThenInclude(g => g.Teacher)
                .Include(s => s.Absences).ThenInclude(a => a.Subject)
                .FirstOrDefaultAsync(s => s.Id == studentId && s.ClassId == classId);

            if (student == null) return NotFound();

            var subjects = await _context.Subjects.ToListAsync();
            var teachers = await _context.Teachers.Include(t => t.Subject).ToListAsync();

            var model = new ManageStudentViewModel
            {
                ClassId = classId,
                ClassName = cls.Name,
                StudentId = student.Id,
                StudentName = $"{student.FirstName} {student.LastName}",
                OverallAverage = student.Grades.Any() ? Math.Round(student.Grades.Average(g => g.Value), 2) : 0,
                WeightedAbsences = student.Absences.Sum(a => a.AttendanceState == Areas.Teacher.Models.AttendanceState.Late ? 0.5 : 1.0),
                SubjectGrades = student.Grades
                    .GroupBy(g => g.SubjectId)
                    .Select(grp => new SubjectGradeGroup
                    {
                        SubjectId = grp.Key,
                        SubjectName = grp.First().Subject?.Name ?? "Unknown",
                        Average = Math.Round(grp.Average(g => g.Value), 2),
                        Grades = grp.OrderByDescending(g => g.Date).Select(g => new HomeroomGradeRow
                        {
                            Id = g.Id,
                            Value = g.Value,
                            Type = g.Type,
                            Description = g.Description,
                            Date = g.Date,
                            TeacherName = g.Teacher != null ? $"{g.Teacher.FirstName} {g.Teacher.LastName}" : "Unknown",
                            TeacherId = g.TeacherId,
                            SubjectId = g.SubjectId
                        }).ToList()
                    }).ToList(),
                Absences = student.Absences.OrderByDescending(a => a.Date).Select(a => new StudentAbsenceRow
                {
                    Id = a.Id,
                    Date = a.Date,
                    SubjectName = a.Subject?.Name ?? "Unknown",
                    SubjectId = a.SubjectId,
                    State = (AttendanceState)(int)a.AttendanceState
                }).ToList(),
                AvailableSubjects = subjects.Select(s => new SubjectOption { Id = s.Id, Name = s.Name }).ToList(),
                AvailableTeachers = teachers.Select(t => new TeacherOption
                {
                    Id = t.Id,
                    Name = $"{t.FirstName} {t.LastName} ({t.Subject?.Name})",
                    SubjectId = t.SubjectId
                }).ToList()
            };

            return View(model);
        }

        // Add grade 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddHomeroomGrade(int StudentId, int ClassId, int SubjectId, int TeacherId, double Value, GradeType Type, string Description)
        {
            var userId = _userManager.GetUserId(User);
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);
            if (teacher == null) return Forbid();

            var cls = await _context.Classes.FirstOrDefaultAsync(c => c.Id == ClassId && c.HeadTeacherId == teacher.Id);
            if (cls == null) return NotFound();

            if (Value >= 2 && Value <= 6)
            {
                _context.Grades.Add(new Grade
                {
                    StudentId = StudentId,
                    SubjectId = SubjectId,
                    TeacherId = TeacherId,
                    Value = Value,
                    Type = Type,
                    Description = Description,
                    Date = DateTime.Now
                });
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Grade added successfully.";
            }

            return RedirectToAction(nameof(ManageStudent), new { classId = ClassId, studentId = StudentId });
        }

        // Edit grade
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditHomeroomGrade(int GradeId, double NewValue, GradeType NewType, string NewDescription, int ReturnClassId, int ReturnStudentId)
        {
            var userId = _userManager.GetUserId(User);
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);
            if (teacher == null) return Forbid();

            var cls = await _context.Classes.FirstOrDefaultAsync(c => c.Id == ReturnClassId && c.HeadTeacherId == teacher.Id);
            if (cls == null) return NotFound();

            var grade = await _context.Grades.FindAsync(GradeId);
            if (grade != null)
            {
                grade.Value = NewValue;
                grade.Type = NewType;
                grade.Description = NewDescription;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Grade updated.";
            }

            return RedirectToAction(nameof(ManageStudent), new { classId = ReturnClassId, studentId = ReturnStudentId });
        }

        // Delete grade
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteHomeroomGrade(int GradeId, int ReturnClassId, int ReturnStudentId)
        {
            var userId = _userManager.GetUserId(User);
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);
            if (teacher == null) return Forbid();

            var cls = await _context.Classes.FirstOrDefaultAsync(c => c.Id == ReturnClassId && c.HeadTeacherId == teacher.Id);
            if (cls == null) return NotFound();

            var grade = await _context.Grades.FindAsync(GradeId);
            if (grade != null)
            {
                _context.Grades.Remove(grade);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Grade deleted.";
            }

            return RedirectToAction(nameof(ManageStudent), new { classId = ReturnClassId, studentId = ReturnStudentId });
        }

        //  Add absence
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddHomeroomAbsence(int StudentId, int ClassId, int SubjectId, DateTime Date, AttendanceState State)
        {
            var userId = _userManager.GetUserId(User);
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);
            if (teacher == null) return Forbid();

            var cls = await _context.Classes.FirstOrDefaultAsync(c => c.Id == ClassId && c.HeadTeacherId == teacher.Id);
            if (cls == null) return NotFound();

            _context.Absences.Add(new Absence
            {
                StudentId = StudentId,
                SubjectId = SubjectId,
                Date = Date,
                AttendanceState = (Classio.Areas.Teacher.Models.AttendanceState)(int)State
            });
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Absence added.";

            return RedirectToAction(nameof(ManageStudent), new { classId = ClassId, studentId = StudentId });
        }

        //  Delete absence 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteHomeroomAbsence(int AbsenceId, int ReturnClassId, int ReturnStudentId)
        {
            var userId = _userManager.GetUserId(User);
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);
            if (teacher == null) return Forbid();

            var cls = await _context.Classes.FirstOrDefaultAsync(c => c.Id == ReturnClassId && c.HeadTeacherId == teacher.Id);
            if (cls == null) return NotFound();

            var absence = await _context.Absences.FindAsync(AbsenceId);
            if (absence != null)
            {
                _context.Absences.Remove(absence);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Absence removed.";
            }

            return RedirectToAction(nameof(ManageStudent), new { classId = ReturnClassId, studentId = ReturnStudentId });
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