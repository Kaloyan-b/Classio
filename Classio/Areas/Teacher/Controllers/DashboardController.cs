using Classio.Areas.Teacher.Models;
using Classio.Data;
using Classio.Models;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.DependencyResolver;

namespace Classio.Areas.Teacher.Controllers
{
    [Area("Teacher")]
    [Authorize(Roles ="Teacher")]

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

        // ----------GRADE BOOK LOGIC----------------
        [HttpGet]
        public async Task<IActionResult> ClassGradebook(int id)
        {
            var userId = _userManager.GetUserId(User);
            var teacher = await _context.Teachers
                .Include(t => t.Subject)
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (teacher == null) return Forbid();

            var schoolClass = await _context.Classes
                .Include(c => c.Students)
                    .ThenInclude(s => s.Grades) // Load ALL grades
                .FirstOrDefaultAsync(c => c.Id == id);

            if (schoolClass == null) return NotFound();

            var model = new ClassGradebookViewModel
            {
                ClassId = schoolClass.Id,
                ClassName = schoolClass.Name,
                SubjectName = teacher.Subject.Name,
                SubjectId = teacher.SubjectId,
                Students = schoolClass.Students.Select(s => new StudentGradeItem
                {
                    StudentId = s.Id,
                    StudentName = $"{s.FirstName} {s.LastName}",

                    // Filter: Only show grades for THIS teacher's subject
                    ExistingGrades = s.Grades
                        .Where(g => g.SubjectId == teacher.SubjectId)
                        .OrderBy(g => g.Date)
                        .Select(g => g.Value)
                        .ToList()
                }).ToList()
            };

            ViewBag.SubjectId = teacher.SubjectId;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateGrades(ClassGradebookViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);

            if (teacher == null) return Forbid();

            foreach (var item in model.Students)
            {
                if (item.NewGrade.HasValue)
                {
                    if (item.NewGrade.Value < 2 || item.NewGrade.Value > 6)
                    {
                        continue;
                    }

                    var newGrade = new Grade
                    {
                        StudentId = item.StudentId,
                        SubjectId = model.SubjectId,
                        TeacherId = teacher.Id,    
                        Value = item.NewGrade.Value,
                        Date = DateTime.Now
                    };

                    _context.Add(newGrade);
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "New grades added successfully!";
            return RedirectToAction(nameof(ClassGradebook), new { id = model.ClassId });
        }
    }
}