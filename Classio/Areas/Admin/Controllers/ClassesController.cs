using Classio.Areas.Admin.Models;
using Classio.Data;
using Classio.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;

namespace Classio.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ClassesController: Controller
    {
        private readonly ClassioDbContext _context;
        private readonly UserManager<User> _userManager;

        public ClassesController(ClassioDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var classes = await _context.Classes
                .Include(c => c.School)
                .Include(c => c.HeadTeacher)
                .ToListAsync();
            return View(classes);
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            PopulateDropdowns();
            return PartialView("_CreateClass", new CreateClassViewModel());
        }
        [HttpPost]
        public async Task<IActionResult> Create(CreateClassViewModel model)
        {
            if (ModelState.IsValid)
            {
                var newClass = new Class
                {
                    Name = model.Name,
                    SchoolId = model.SchoolId,
                    HeadTeacherId = model.HeadTeacherId
                };

                _context.Add(newClass);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            PopulateDropdowns(model.SchoolId, model.HeadTeacherId);
            return PartialView("_CreateClass", model);
        }
        private void PopulateDropdowns(int? selectedSchool = null, int? selectedTeacher = null)
        {
            ViewData["SchoolId"] = new SelectList(_context.Schools, "Id", "Name", selectedSchool);

            var teachers = _context.Teachers
                .Select(t => new
                {
                    Id = t.Id,
                    FullName = t.FirstName + " " + t.LastName
                })
                .ToList();

            ViewData["HeadTeacherId"] = new SelectList(teachers, "Id", "FullName", selectedTeacher);
        }

    }
}
