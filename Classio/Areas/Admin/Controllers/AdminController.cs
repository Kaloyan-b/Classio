using Classio.Data;
using Classio.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Classio.Areas.Admin.Controllers
{
    public class AdminController: Controller
    {
        private readonly ClassioDbContext _context;
        private readonly UserManager<User> _userManager;

        public AdminController(ClassioDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Area("Student")]
        [Authorize(Roles = "Student")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
