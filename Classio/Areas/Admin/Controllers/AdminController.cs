using Classio.Data;
using Classio.Models;
using Classio.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Classio.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController: Controller
    {
        private readonly ClassioDbContext _context;
        private readonly UserManager<User> _userManager;

        public AdminController(ClassioDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        public IActionResult Index()
        {
            var model = new AdminDashboardViewModel
            {
                StudentsCount = _context.Students.Count(),
                TeachersCount = _context.Teachers.Count(),
                ParentsCount = _context.Parents.Count(),
                ClassesCount = _context.Classes.Count()
            };

            return View(model);
        }
    }
}
