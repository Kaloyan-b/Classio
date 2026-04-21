using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Classio.Areas.Parent.Models;
using Classio.Areas.Student.Models;
using Classio.Models;
using Classio.Data;
using Classio.Areas.Teacher.Models;

namespace Classio.Areas.Parent.Controllers
{
    [Area("Parent")]
    [Authorize(Roles = "Parent")]
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
            var user = await _userManager.GetUserAsync(User);
            var parentProfile = await _context.Parents.FirstOrDefaultAsync(p => p.UserId == user.Id);

            var children = await _context.Students
                .Include(s => s.Grades).Include(s => s.Absences)
                .Where(s => s.Parent1Id == parentProfile.Id || s.Parent2Id == parentProfile.Id).ToListAsync();

            var model = new ParentDashboardViewModel
            {
                ParentName = parentProfile.FirstName,
                Children = children.Select(c => new ChildSummaryViewModel
                {
                    StudentId = c.Id,
                    FullName = $"{c.FirstName} {c.LastName}",
                    OverallAverage = c.Grades.Any() ? Math.Round(c.Grades.Average(g => g.Value), 2) : 0,
                    WeightedAbsences = c.Absences.Count(a => a.AttendanceState == AttendanceState.Absent)
                                     + (c.Absences.Count(a => a.AttendanceState == AttendanceState.Late) * 0.5)
                }).ToList()
            };
            return View(model);
        }

        private async Task<bool> IsParentAuthorizedForStudent(int studentId, string userId)
        {
            var parent = await _context.Parents.FirstOrDefaultAsync(p => p.UserId == userId);
            if (parent == null) return false;

            return await _context.Students.AnyAsync(s => s.Id == studentId && (s.Parent1Id == parent.Id || s.Parent2Id == parent.Id));
        }

        public async Task<IActionResult> ChildDetails(int studentId)
        {
            var userId = _userManager.GetUserId(User);
            if (!await IsParentAuthorizedForStudent(studentId, userId)) return Unauthorized();

            var student = await _context.Students
                .Include(s => s.Grades)
                .Include(s => s.Absences)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            // Reusing student logic
            double overallAvg = student.Grades.Any() ? Math.Round(student.Grades.Average(g => g.Value), 2) : 0;
            var allStudents = await _context.Students.Include(s => s.Grades).ToListAsync();
            var rankedList = allStudents.Select(s => new { StudentId = s.Id, Gpa = s.Grades.Any() ? s.Grades.Average(g => g.Value) : 0 })
                .OrderByDescending(x => x.Gpa).ToList();

            int rank = rankedList.FindIndex(x => x.StudentId == student.Id) + 1;
            int totalAbsences = student.Absences.Count(a => a.AttendanceState == AttendanceState.Absent);
            int totalLates = student.Absences.Count(a => a.AttendanceState == AttendanceState.Late);

            var model = new StudentDashboardViewModel 
            {
                StudentName = $"{student.FirstName} {student.LastName}",
                OverallAverage = overallAvg,
                WeightedAbsences = totalAbsences + (totalLates * 0.5),
                SchoolRank = rank,
                TotalStudents = rankedList.Count
            };

            ViewBag.StudentId = studentId;
            return View(model);
        }

        public async Task<IActionResult> ChildGrades(int studentId)
        {
            var userId = _userManager.GetUserId(User);
            if (!await IsParentAuthorizedForStudent(studentId, userId)) return Unauthorized();

            var student = await _context.Students
                .Include(s => s.Grades).ThenInclude(g => g.Subject)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            var model = new StudentGradesViewModel();

            var groupedGrades = student.Grades.GroupBy(g => g.SubjectId);
            foreach (var group in groupedGrades)
            {
                model.Subjects.Add(new SubjectPerformance
                {
                    SubjectName = group.First().Subject.Name,
                    AverageGrade = Math.Round(group.Average(g => g.Value), 2),
                    Grades = group.Select(g => new GradeDetail
                    {
                        Value = g.Value,
                        Type = g.Type,
                        Description = g.Description,
                        Date = g.Date
                    }).OrderByDescending(g => g.Date).ToList()
                });
            }

            ViewBag.StudentId = studentId;
            return View(model);
        }
        public async Task<IActionResult> ChildAttendance(int studentId)
        {
            var userId = _userManager.GetUserId(User);
            if (!await IsParentAuthorizedForStudent(studentId, userId)) return Unauthorized();

            var student = await _context.Students
                .Include(s => s.Absences).ThenInclude(a => a.Subject)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            var model = new StudentAttendanceViewModel
            {
                TotalWeightedAbsences = student.Absences.Count(a => a.AttendanceState == AttendanceState.Absent)
                                      + (student.Absences.Count(a => a.AttendanceState == AttendanceState.Late) * 0.5),
                Absences = student.Absences.Select(a => new AbsenceDetail
                {
                    Date = a.Date,
                    SubjectName = a.Subject?.Name ?? "Unknown",
                    State = a.AttendanceState
                }).OrderByDescending(a => a.Date).ToList()
            };

            ViewBag.StudentId = studentId;
            return View(model);
        }
    }
}