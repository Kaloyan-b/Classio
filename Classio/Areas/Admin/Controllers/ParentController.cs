using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Classio.Data;
using Classio.Areas.Admin.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Classio.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")] 
    public class ParentsController : Controller
    {
        private readonly ClassioDbContext _context;

        public ParentsController(ClassioDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var parents = await _context.Parents
                .Include(p => p.User) 
                .Include(p => p.StudentsAsParent1)
                .Include(p => p.StudentsAsParent2)
                .Select(p => new AdminParentListViewModel
                {
                    ParentId = p.Id,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    Email = p.User != null ? p.User.Email : "No Email Found",
                    LinkedChildrenCount = p.StudentsAsParent1.Count + p.StudentsAsParent2.Count
                })
                .ToListAsync();

            return View(parents);
        }
        public async Task<IActionResult> Edit(int id)
        {
            var parent = await _context.Parents.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == id);
            if (parent == null) return NotFound();

            // 1. Find the IDs of the students currently linked to this parent
            var linkedStudentIds = await _context.Students
                .Where(s => s.Parent1Id == id || s.Parent2Id == id)
                .Select(s => s.Id)
                .ToListAsync();

            // 2. Get all students to populate the dropdown menu
            var allStudents = await _context.Students
                .Select(s => new { s.Id, FullName = $"{s.FirstName} {s.LastName}" })
                .ToListAsync();

            var model = new AdminParentEditViewModel
            {
                ParentId = parent.Id,
                FirstName = parent.FirstName,
                LastName = parent.LastName,
                Email = parent.User?.Email ?? "No Email Found",
                SelectedStudentIds = linkedStudentIds,
                AvailableStudents = new MultiSelectList(allStudents, "Id", "FullName", linkedStudentIds)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AdminParentEditViewModel model)
        {
            if (id != model.ParentId) return NotFound();

            if (ModelState.IsValid)
            {
                var parent = await _context.Parents.FindAsync(id);
                if (parent == null) return NotFound();


                parent.FirstName = model.FirstName;
                parent.LastName = model.LastName;

                var currentStudents = await _context.Students.Where(s => s.Parent1Id == id || s.Parent2Id == id).ToListAsync();
                foreach (var s in currentStudents)
                {
                    if (s.Parent1Id == id) s.Parent1Id = null;
                    if (s.Parent2Id == id) s.Parent2Id = null;
                }

                if (model.SelectedStudentIds != null && model.SelectedStudentIds.Any())
                {
                    var selectedStudents = await _context.Students.Where(s => model.SelectedStudentIds.Contains(s.Id)).ToListAsync();
                    foreach (var s in selectedStudents)
                    {
                        if (s.Parent1Id == null) s.Parent1Id = id;
                        else if (s.Parent2Id == null) s.Parent2Id = id;
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            var allStudents = await _context.Students.Select(s => new { s.Id, FullName = s.FirstName + " " + s.LastName }).ToListAsync();
            model.AvailableStudents = new MultiSelectList(allStudents, "Id", "FullName", model.SelectedStudentIds);
            return View(model);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var parent = await _context.Parents.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == id);
            if (parent == null) return NotFound();

            return View(parent);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var parent = await _context.Parents.FindAsync(id);
            if (parent != null)
            {
                var linkedStudents = await _context.Students.Where(s => s.Parent1Id == id || s.Parent2Id == id).ToListAsync();
                foreach (var s in linkedStudents)
                {
                    if (s.Parent1Id == id) s.Parent1Id = null;
                    if (s.Parent2Id == id) s.Parent2Id = null;
                }

                _context.Parents.Remove(parent);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

    }
}