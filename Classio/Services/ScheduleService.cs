using Classio.Data;
using Classio.Models;
using Microsoft.EntityFrameworkCore;

namespace Classio.Services
{
    public interface IScheduleService
    {
        Task<List<ClassPeriod>> GetPeriodsAsync();
        Task<List<ScheduleSlot>> GetScheduleForClassAsync(int classId);
        Task UpdateSlotAsync(int classId, int periodId, DayOfWeek day, int subjectId, int teacherId);
    }

    public class ScheduleService : IScheduleService
    {
        private readonly ClassioDbContext _context;

        public ScheduleService(ClassioDbContext context)
        {
            _context = context;
        }

        public async Task<List<ClassPeriod>> GetPeriodsAsync()
        {
            return await _context.ClassPeriods.OrderBy(p => p.Order).ToListAsync();
        }

        public async Task<List<ScheduleSlot>> GetScheduleForClassAsync(int classId)
        {
            return await _context.ScheduleSlots
                .Include(s => s.Subject)
                .Include(s => s.Teacher)
                .Where(s => s.ClassId == classId)
                .ToListAsync();
        }

        public async Task UpdateSlotAsync(int classId, int periodId, DayOfWeek day, int subjectId, int teacherId)
        {
            var subject = await _context.Subjects
                .Include(s => s.Teachers)
                .FirstOrDefaultAsync(s => s.Id == subjectId);

            if (subject == null) throw new Exception("Subject not found");

            int finalTeacherId = teacherId;

            if (finalTeacherId == 0)
            {
                // select the first one
                finalTeacherId = subject.Teachers.FirstOrDefault()?.Id ?? 0;

                if (finalTeacherId == 0)
                    throw new Exception($"Subject '{subject.Name}' has no teachers assigned! Add a teacher to this subject first.");
            }

            var existingSlot = await _context.ScheduleSlots
                .FirstOrDefaultAsync(s => s.ClassId == classId
                                       && s.ClassPeriodId == periodId
                                       && s.DayOfWeek == day);

            if (existingSlot != null)
            {
                existingSlot.SubjectId = subjectId;
                existingSlot.TeacherId = finalTeacherId;
                _context.Update(existingSlot);
            }
            else
            {
                var newSlot = new ScheduleSlot
                {
                    ClassId = classId,
                    ClassPeriodId = periodId,
                    DayOfWeek = day,
                    SubjectId = subjectId,
                    TeacherId = finalTeacherId 
                };
                _context.Add(newSlot);
            }

            await _context.SaveChangesAsync();
        }
    }
}
