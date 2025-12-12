using Classio.Data;
using Classio.Models;
using Classio.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Classio.Controllers
{
    public class StudentController : Controller
    {
        private readonly ClassioDbContext _context;
        private readonly UserManager<User> _userManager;

        public StudentController(ClassioDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if(user == null)
            {
                return Challenge();
            }
            var student = await _context.Students
        .Include(s => s.Class)
        .ThenInclude(c => c.Subjects)
        .Include(s => s.Grades)
        .ThenInclude(g => g.Subject)
        .Include(s => s.Absences)
        .ThenInclude(a => a.Subject)
        .FirstOrDefaultAsync(s => s.UserId == user.Id);

            if (student == null)
            {
                return RedirectToAction("NoStudentProfile");
            }

            // Build ViewModel
            var vm = new StudentDashboardViewModel
            {
                StudentName = $"{student.FirstName} {student.LastName}",
                ClassName = student.Class?.Name ?? "Няма клас",

                Grades = student.Grades.Select(g => new GradeViewModel
                {
                    SubjectName = g.Subject?.Name ?? "Без предмет",
                    Value = g.Value,
                    Date = g.Date
                }).ToList(),

                Absences = student.Absences.Select(a => new AbsenceViewModel
                {
                    SubjectName = a.Subject?.Name ?? "Без предмет",
                    Date = a.Date
                }).ToList(),

                Schedule = student.Class?.Subjects
                    .Select(subj => new ScheduleViewModel
                    {
                        Day = "Пон",   // You can change later
                        SubjectName = subj.Name,
                        Hour = 1       // You can replace with your schedule logic
                    })
                    .ToList() ?? new List<ScheduleViewModel>()
            };

            return View(vm);
        }
        [HttpGet]
        public IActionResult Absences()
        {
            return View();
        }
    }
}
