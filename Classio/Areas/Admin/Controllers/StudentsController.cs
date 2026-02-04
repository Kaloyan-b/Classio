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
                .Include(s => s.School)
                .ToListAsync();
            return View(students);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Class)
                .Include(s => s.Parent1)
                .Include(s => s.Parent2)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null) return NotFound();

            var model = new EditStudentViewModel
            {
                Id = student.Id,
                FirstName = student.FirstName,
                LastName = student.LastName,
                Email = student.User?.Email,
                ClassId = student.ClassId,
                Parent1Id = student.Parent1Id,
                Parent2Id = student.Parent2Id,
                SchoolId = student.SchoolId
            };

            await PopulateDropdowns(model);
            return PartialView("_EditStudent", model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EditStudentViewModel model)
        {
            if (ModelState.IsValid)
            {
                var student = await _context.Students
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == model.Id);

                if (student == null) return NotFound();

                student.FirstName = model.FirstName;
                student.LastName = model.LastName;
                student.ClassId = model.ClassId;
                student.Parent1Id = model.Parent1Id;
                student.Parent2Id = model.Parent2Id;
                student.SchoolId = model.SchoolId;

                if (student.User != null)
                {
                    student.User.FirstName = model.FirstName;
                    student.User.LastName = model.LastName;
                    student.User.Email = model.Email;
                    student.User.UserName = model.Email;

                    var identityResult = await _userManager.UpdateAsync(student.User);
                    if (!identityResult.Succeeded)
                    {
                        foreach (var error in identityResult.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                        await PopulateDropdowns(model);
                        return PartialView("_EditStudent", model);
                    }
                }

                _context.Update(student);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(model);
            return PartialView("_EditStudent", model);
        }

        private async Task PopulateDropdowns(EditStudentViewModel model)
        {
            ViewData["SchoolId"] = new SelectList(await _context.Schools.ToListAsync(), "Id", "Name", model.SchoolId);
            ViewData["ClassId"] = new SelectList(await _context.Classes.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", model.ClassId);

            var parents = await _context.Parents
                .Include(p => p.User)
                .Select(p => new {
                    Id = p.Id,
                    FullName = p.User.FirstName + " " + p.User.LastName + " (" + p.User.Email + ")"
                })
                .ToListAsync();

            ViewData["ParentId"] = new SelectList(parents, "Id", "FullName");
        }
    }
}
