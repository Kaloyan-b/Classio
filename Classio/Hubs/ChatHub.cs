using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Classio.Data; 
using Classio.Models;

namespace Classio.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ClassioDbContext _context;

        public ChatHub(ClassioDbContext context)
        {
            _context = context;
        }

        public async Task SendPrivateMessage(string receiverId, string message)
        {
            var senderId = Context.UserIdentifier;

            if (senderId != null)
            {
                var chatMessage = new ChatMessage
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Content = message,
                    Timestamp = DateTime.Now
                };

                _context.ChatMessages.Add(chatMessage);
                await _context.SaveChangesAsync();

                await Clients.User(receiverId).SendAsync("ReceivePrivateMessage", senderId, message);
            }
        }
    }
}