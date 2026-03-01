using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Classio.Areas.Parent.Models;
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
            if (user == null) return NotFound();

            var parentProfile = await _context.Parents.FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (parentProfile == null)
                return NotFound("Parent profile not found. Please contact administration.");

            var children = await _context.Students
                .Include(s => s.Grades)
                .Include(s => s.Absences)
                .Where(s => s.Parent1Id == parentProfile.Id || s.Parent2Id == parentProfile.Id)
                .ToListAsync();

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
    }
}