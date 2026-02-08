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

        // Drag-and-Drop Editor
        public async Task<IActionResult> Manage(int id)
        {
            var schoolClass = await _context.Classes.FindAsync(id);
            if (schoolClass == null) return NotFound();

            ViewBag.ClassId = id;
            ViewBag.ClassName = schoolClass.Name;

            // Subjects to drag
            ViewBag.Subjects = await _context.Subjects.ToListAsync();

            // Bell Schedule
            ViewBag.Periods = await _scheduleService.GetPeriodsAsync();

            // Existing filled slots
            var schedule = await _scheduleService.GetScheduleForClassAsync(id);

            return View(schedule);
        }

        // API to Save Data
        [HttpPost]
        public async Task<IActionResult> UpdateSlot([FromBody] UpdateSlotRequest request)
        {
            await _scheduleService.UpdateSlotAsync(
                request.ClassId,
                request.PeriodId,
                request.Day,
                request.SubjectId
            );
            return Ok(new { success = true });
        }
    }

    // Helper class for JSON data
    public class UpdateSlotRequest
    {
        public int ClassId { get; set; }
        public int PeriodId { get; set; }
        public DayOfWeek Day { get; set; }
        public int SubjectId { get; set; }
    }
}