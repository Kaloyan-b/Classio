//using Classio.Areas.Teacher.Models;
//using Classio.Core.Models;
//using Classio.Data;
//using Classio.Models;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;

//namespace Classio.Areas.Teacher.Controllers
//{
//    [Area("Teacher")]
//    [Authorize(Roles = "Teacher")]
//    public class DashboardController : Controller
//    {
//        private readonly ClassioDbContext _context;
//        private readonly UserManager<User> _userManager;

//        public DashboardController(ClassioDbContext context, UserManager<User> userManager)
//        {
//            _context = context;
//            _userManager = userManager;
//        }

//        // 1. DASHBOARD HOME
//        public async Task<IActionResult> Index()
//        {
//            var userId = _userManager.GetUserId(User);

//            var teacher = await _context.Teachers
//                .Include(t => t.HeadOfClasses)
//                    .ThenInclude(c => c.School)
//                .Include(t => t.ClassesTaught)
//                    .ThenInclude(c => c.School)
//                .Include(t => t.Subject)
//                .FirstOrDefaultAsync(t => t.UserId == userId);

//            if (teacher == null)
//            {
//                return View("Error", "Teacher profile not found.");
//            }

//            return View(teacher);
//        }

//        // 2. GET: GRADEBOOK VIEW
//        [HttpGet]
//        public async Task<IActionResult> ClassGradebook(int id)
//        {
//            var userId = _userManager.GetUserId(User);
//            var teacher = await _context.Teachers
//                .Include(t => t.Subject)
//                .FirstOrDefaultAsync(t => t.UserId == userId);

//            if (teacher == null) return Forbid();

//            // Load Class, Students, Grades (with Teacher info), and Absences
//            var schoolClass = await _context.Classes
//                .Include(c => c.Students)
//                    .ThenInclude(s => s.Grades)
//                        .ThenInclude(g => g.Teacher) // Include Teacher for the history popup
//                .Include(c => c.Students)
//                    .ThenInclude(s => s.Absences)    // Include Absences for the buttons
//                .FirstOrDefaultAsync(c => c.Id == id);

//            if (schoolClass == null) return NotFound();

//            var today = DateTime.Today;

//            var model = new ClassGradebookViewModel
//            {
//                ClassId = schoolClass.Id,
//                ClassName = schoolClass.Name,
//                SubjectName = teacher.Subject.Name,
//                SubjectId = teacher.SubjectId,
//                Students = schoolClass.Students.Select(s => new StudentGradeItem
//                {
//                    StudentId = s.Id,
//                    StudentName = $"{s.FirstName} {s.LastName}",

//                    // A. Calculate Attendance Status for Today
//                    AttendanceToday = s.Absences.Any(a => a.Date.Date == today && a.AttendanceState)
//                        ? AttendanceState.Late
//                        : (s.Absences.Any(a => a.Date.Date == today) ? AttendanceState.Absent : AttendanceState.Present),

//                    // B. Map Database Grades to View Model History
//                    ExistingGrades = s.Grades
//                        .Where(g => g.SubjectId == teacher.SubjectId)
//                        .OrderBy(g => g.Date)
//                        .Select(g => new SimpleGradeHistory
//                        {
//                            Id = g.Id,
//                            Value = g.Value,
//                            Type = g.Type,
//                            Description = g.Description,
//                            Date = g.Date,
//                            TeacherName = g.Teacher != null ? $"{g.Teacher.FirstName} {g.Teacher.LastName}" : "Unknown"
//                        })
//                        .ToList()
//                }).ToList()
//            };

//            return View(model);
//        }

//        // 3. POST: SAVE NEW GRADES AND ATTENDANCE
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> UpdateSession(ClassGradebookViewModel model)
//        {
//            var userId = _userManager.GetUserId(User);
//            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);
//            if (teacher == null) return Forbid();

//            var today = DateTime.Today;

//            foreach (var item in model.Students)
//            {
//                // --- A. HANDLE ATTENDANCE ---
//                var existingAbsence = await _context.Absences
//                    .FirstOrDefaultAsync(a => a.StudentId == item.StudentId && a.Date.Date == today);

//                if (item.AttendanceToday == AttendanceState.Present)
//                {
//                    // If marked Present, remove any existing absence record
//                    if (existingAbsence != null) _context.Absences.Remove(existingAbsence);
//                }
//                else
//                {
//                    // If Absent or Late
//                    if (existingAbsence == null)
//                    {
//                        var abs = new Absence
//                        {
//                            StudentId = item.StudentId,
//                            Date = DateTime.Now,
//                            AttendanceState = (item.AttendanceToday == AttendanceState.Late)
//                        };
//                        _context.Absences.Add(abs);
//                    }
//                    else
//                    {
//                        existingAbsence.AttendanceState = (item.AttendanceToday == AttendanceState.Late);
//                        _context.Update(existingAbsence);
//                    }
//                }

//                // --- B. HANDLE NEW GRADES ---
//                if (item.NewGrade.HasValue)
//                {
//                    // Basic validation
//                    if (item.NewGrade.Value >= 2 && item.NewGrade.Value <= 6)
//                    {
//                        var newGrade = new Grade
//                        {
//                            StudentId = item.StudentId,
//                            SubjectId = model.SubjectId,
//                            TeacherId = teacher.Id,
//                            Value = item.NewGrade.Value,
//                            Type = model.BatchGradeType,
//                            Description = model.BatchDescription,
//                            Date = DateTime.Now
//                        };
//                        _context.Add(newGrade);
//                    }
//                }
//            }

//            await _context.SaveChangesAsync();
//            TempData["SuccessMessage"] = "Session saved successfully!";
//            return RedirectToAction(nameof(ClassGradebook), new { id = model.ClassId });
//        }

//        // 4. POST: EDIT SINGLE GRADE (Used by Modal)
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> EditGrade(int GradeId, double NewValue, GradeType NewType, string NewDescription, int ReturnClassId)
//        {
//            var grade = await _context.Grades.FindAsync(GradeId);

//            // Optional: Check if the current teacher owns this grade before editing
//            // var userId = _userManager.GetUserId(User);
//            // ... check logic ...

//            if (grade != null)
//            {
//                grade.Value = NewValue;
//                grade.Type = NewType;
//                grade.Description = NewDescription;
//                _context.Update(grade);
//                await _context.SaveChangesAsync();
//            }
//            return RedirectToAction(nameof(ClassGradebook), new { id = ReturnClassId });
//        }

//        // 5. POST: DELETE SINGLE GRADE (Used by Modal)
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> DeleteGrade(int GradeId, int ReturnClassId)
//        {
//            var grade = await _context.Grades.FindAsync(GradeId);
//            if (grade != null)
//            {
//                _context.Grades.Remove(grade);
//                await _context.SaveChangesAsync();
//            }
//            return RedirectToAction(nameof(ClassGradebook), new { id = ReturnClassId });
//        }
//    }
//}