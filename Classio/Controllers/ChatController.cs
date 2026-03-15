using Classio.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Classio.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ClassioDbContext _context;

        public ChatController(ClassioDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetChatHistory(string contactId)
        {
            var myUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(myUserId) || string.IsNullOrEmpty(contactId))
            {
                return BadRequest("Invalid user IDs.");
            }

            var messages = await _context.ChatMessages
                .Where(m => (m.SenderId == myUserId && m.ReceiverId == contactId) ||
                            (m.SenderId == contactId && m.ReceiverId == myUserId))
                .OrderBy(m => m.Timestamp) // Oldest messages first
                .Select(m => new {
                    content = m.Content,
                    isMine = m.SenderId == myUserId 
                })
                .ToListAsync();

            return Json(messages); 
        }

        [HttpGet]
        public async Task<IActionResult> GetContacts()
        {
            var myUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var users = await _context.Users
                .Where(u => u.Id != myUserId)
                .Select(u => new {
                    id = u.Id,
                    name = u.FirstName + " " + u.LastName
                })
                .ToListAsync();

            return Json(users);
        }
    }
}
