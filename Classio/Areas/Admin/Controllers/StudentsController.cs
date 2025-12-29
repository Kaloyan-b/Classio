using Classio.Data;
using Classio.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Classio.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class StudentsController : Controller
    {
        private readonly ClassioDbContext _context;
        private readonly UserManager<User> _userManager;
        public StudentsController(ClassioDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        
        public async Task<IActionResult> Index()
        {
            var students = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Class)
                .ToListAsync();
            return View(students);
        }
    }
}
