using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Classio.Data;
using Classio.Models;

namespace Classio.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ClassioDbContext _context;

        public ChatHub(ClassioDbContext context)
        {
            _context = context;
        }

        public async Task SendPrivateMessage(string receiverId, string message)
        {
            var senderId = Context.UserIdentifier
                ?? throw new HubException("User is not authenticated.");

            var chatMessage = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = message,
                Timestamp = DateTime.Now
            };

            try
            {
                _context.ChatMessages.Add(chatMessage);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new HubException($"Save failed: {ex.Message} | Inner: {ex.InnerException?.Message}");
            }

            await Clients.User(receiverId).SendAsync("ReceivePrivateMessage", senderId, message);
        }
    }
}