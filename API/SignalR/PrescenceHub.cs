using API.Extensions;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR
{
    public class PrescenceHub : Hub
    {
        public PrescenceTracker Tracker { get; }
        public PrescenceHub(PrescenceTracker tracker)
        {
            this.Tracker = tracker;
        }

        public override async Task OnConnectedAsync()
        {
            var isOnline = await Tracker.UserConnected(Context.User.GetUsername(), Context.ConnectionId);
            if (isOnline)
            {
                await Clients.Others.SendAsync("UserIsOnline", Context.User.GetUsername());
            }

            var currentUsers = await Tracker.GetOnlineUsers();
            await Clients.Caller.SendAsync("GetOnlineUsers", currentUsers);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var isOffline = await Tracker.UserDisconnected(Context.User.GetUsername(), Context.ConnectionId);

            if (isOffline) 
            {
                await Clients.Others.SendAsync("UserIsOffline", Context.User.GetUsername());
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}