using Microsoft.AspNetCore.SignalR;

namespace Classio.Hubs
{
    public class ChatHub : Hub
    {
        public async Task SendMessage(string senderName, string message)
        {
            // For the MVP, we are broadcasting to everyone. 
            // Later, we can target specific users using their ConnectionId.
            await Clients.All.SendAsync("ReceiveMessage", senderName, message);
        }
    }
}
