using Classio.Services;
using Classio.Data;
using Classio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Classio.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ScheduleController : Controller
    {
        private readonly IScheduleService _scheduleService;
        private readonly ClassioDbContext _context;

        public ScheduleController(IScheduleService scheduleService, ClassioDbContext context)
        {
            _scheduleService = scheduleService;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Classes.ToListAsync());
        }

        // Editor
        public async Task<IActionResult> Manage(int id)
        {
            var schoolClass = await _context.Classes.FindAsync(id);
            if (schoolClass == null) return NotFound();

            ViewBag.ClassId = id;
            ViewBag.ClassName = schoolClass.Name;

            // Subjects to drag
            ViewBag.Subjects = await _context.Subjects.ToListAsync();

            // Schedule
            ViewBag.Periods = await _scheduleService.GetPeriodsAsync();

            // Existing filled slots
            var schedule = await _scheduleService.GetScheduleForClassAsync(id);

            return View(schedule);
        }

        // API to Save Data
        [HttpPost]
        public async Task<IActionResult> UpdateSlot([FromBody] UpdateSlotRequest request)
        {
            try
            {
                await _scheduleService.UpdateSlotAsync(
                    request.ClassId,
                    request.PeriodId,
                    request.Day,
                    request.SubjectId,
                    request.TeacherId
                );
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet] // This specific name fixes the 404
        public async Task<IActionResult> GetTeachersForSubject(int subjectId)
        {
            // 1. Get the subject and include the list of Teachers assigned to it
            var subject = await _context.Subjects
                .Include(s => s.Teachers)
                .FirstOrDefaultAsync(s => s.Id == subjectId);

            if (subject == null)
            {
                return NotFound("Subject not found");
            }

            // 2. Transform the list into simple JSON (id, name)
            var teacherList = subject.Teachers
                .Select(t => new {
                    id = t.Id,
                    name = t.FirstName + " " + t.LastName // Adjust if your Teacher model uses different names
                })
                .ToList();

            return Json(teacherList);
        }
    }

    // Helper class for JSON data
    public class UpdateSlotRequest
    {
        public int ClassId { get; set; }
        public int PeriodId { get; set; }
        public DayOfWeek Day { get; set; }
        public int SubjectId { get; set; }
        public int TeacherId { get; set; }
    }
}