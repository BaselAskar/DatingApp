using System;
using System.Threading.Tasks;
using API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR
{
    [Authorize]
    public class PresenceHub : Hub
    {
        private readonly PresenceTracker _tracker;
        public PresenceHub(PresenceTracker tracker)
        {
            _tracker = tracker;
        }
        public override async Task OnConnectedAsync()
        {
            var isOnline = await _tracker.UserConnected(Context.User.GetUserName(),Context.ConnectionId);
            if (isOnline)
                await Clients.Others.SendAsync("UserIsOnline", Context.User.GetUserName());

            var currentUsers = await _tracker.GetUsersOnline();
            await Clients.Caller.SendAsync("GetOnlineUsers", currentUsers);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
           var isOfline = await _tracker.UserDisconnected(Context.User.GetUserName(), Context.ConnectionId);

           if (isOfline)
                await Clients.Others.SendAsync("UserIsOfLine", Context.User.GetUserName());

            await Clients.Others.SendAsync("GetOnlineUsers", Context.User.GetUserName());

            await base.OnDisconnectedAsync(exception);
        }
    }
}