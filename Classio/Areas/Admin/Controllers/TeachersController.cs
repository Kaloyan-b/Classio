using Classio.Areas.Admin.Models;
using Classio.Data;
using Classio.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Classio.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class TeachersController : Controller
    {
        private readonly ClassioDbContext _context;
        private readonly UserManager<User> _userManager;

        public TeachersController(ClassioDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var teachers = await _context.Teachers
                .Include(t => t.Subject)
                .Include(t => t.School)
                .Include(t => t.User)
                .ToListAsync();
            return View(teachers);
        }
        [HttpGet]
        public IActionResult Create()
        {
            PopulateDropdowns();
            return PartialView("_CreateTeacher", new CreateTeacherViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateTeacherViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new User
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PasswordHash = model.Password
                };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Teacher");

                    var teacher = new Teacher
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        UserId = user.Id,
                        SubjectId = model.SubjectId,
                        SchoolId = model.SchoolId
                    };
                    _context.Add(teacher);
                    await _context.SaveChangesAsync();
                    return Redirect(nameof(Index));

                }
            }
            return RedirectToAction(nameof(Index));
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var teacher = await _context.Teachers
                .Include(t => t.User)
                .Include(t => t.HeadOfClasses) 
                .Include(t => t.ClassesTaught) 
                .FirstOrDefaultAsync(t => t.Id == id);

            if (teacher == null) return NotFound();

            var model = new EditTeacherViewModel
            {
                Id = teacher.Id,
                FirstName = teacher.FirstName,
                LastName = teacher.LastName,
                Email = teacher.User?.Email,
                SubjectId = teacher.SubjectId,
                SchoolId = teacher.SchoolId,

                HeadOfClassIds = teacher.HeadOfClasses.Select(c => c.Id).ToList(),
                SubjectClassIds = teacher.ClassesTaught.Select(c => c.Id).ToList()
            };

            PopulateDropdownsEdit(model);
            return PartialView("_EditTeacher", model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EditTeacherViewModel model)
        {
            if (ModelState.IsValid)
            {
                var teacher = await _context.Teachers
                    .Include(t => t.User)
                    .Include(t => t.HeadOfClasses)
                    .Include(t => t.ClassesTaught)
                    .FirstOrDefaultAsync(t => t.Id == model.Id);

                if (teacher == null) return NotFound();

                teacher.FirstName = model.FirstName;
                teacher.LastName = model.LastName;
                teacher.SubjectId = model.SubjectId;
                teacher.SchoolId = model.SchoolId;

                if (teacher.User != null)
                {
                    teacher.User.FirstName = model.FirstName;
                    teacher.User.LastName = model.LastName;
                    teacher.User.Email = model.Email;
                    teacher.User.UserName = model.Email;

                    var identityResult = await _userManager.UpdateAsync(teacher.User);
                    if (!identityResult.Succeeded)
                    {
                        foreach (var error in identityResult.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                        PopulateDropdownsEdit(model);
                        return PartialView("_EditTeacher", model);
                    }
                }

                // update head teacher
                //Remove from classes he is not leading
                var classesToRemove = teacher.HeadOfClasses
                    .Where(c => !model.HeadOfClassIds.Contains(c.Id))
                    .ToList();

                foreach (var c in classesToRemove)
                {
                    c.HeadTeacherId = null; // Remove head status
                }

                // Assign to new classes
                if (model.HeadOfClassIds.Any())
                {
                    var classesToAssign = await _context.Classes
                        .Where(c => model.HeadOfClassIds.Contains(c.Id))
                        .ToListAsync();

                    foreach (var c in classesToAssign)
                    {
                        c.HeadTeacherId = teacher.Id; // Make head
                    }
                }

                // update subject classes
                teacher.ClassesTaught.Clear();

                if (model.SubjectClassIds.Any())
                {
                    var classesToAdd = await _context.Classes
                        .Where(c => model.SubjectClassIds.Contains(c.Id))
                        .ToListAsync();

                    foreach (var c in classesToAdd)
                    {
                        teacher.ClassesTaught.Add(c);
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            PopulateDropdownsEdit(model);
            return PartialView("_EditTeacher", model);
        }

        private void PopulateDropdownsEdit(EditTeacherViewModel model)
        {
            ViewData["SubjectId"] = new SelectList(_context.Set<Subject>(), "Id", "Name", model.SubjectId);
            ViewData["SchoolId"] = new SelectList(_context.Set<School>(), "Id", "Name", model.SchoolId);

            // Grab all classes for the multi-select lists
            var allClasses = _context.Classes.OrderBy(c => c.Name).ToList();

            ViewData["HeadOfClassesList"] = new MultiSelectList(allClasses, "Id", "Name", model.HeadOfClassIds);
            ViewData["SubjectClassesList"] = new MultiSelectList(allClasses, "Id", "Name", model.SubjectClassIds);
        }
        private void PopulateDropdowns()
        {
            //grabs all Subjects and Schools
            ViewData["SubjectId"] = new SelectList(_context.Set<Subject>(), "Id", "Name");
            ViewData["SchoolId"] = new SelectList(_context.Set<School>(), "Id", "Name");
        }

    }
}
