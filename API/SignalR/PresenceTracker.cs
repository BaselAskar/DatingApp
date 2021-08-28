using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.SignalR
{
    public class PresenceTracker
    {
        private static readonly Dictionary<string,List<string>> OnlineUsers = new Dictionary<string, List<string>>();

        public Task<bool> UserConnected(string userName, string connectionId)
        {
            bool isOnline = false;
            lock (OnlineUsers)
            {
                if (OnlineUsers.ContainsKey(userName))
                {
                    OnlineUsers[userName].Add(connectionId);
                }
                else
                {
                    OnlineUsers.Add(userName,new List<string>{connectionId});
                    isOnline = true;
                }
            }

            return Task.FromResult(isOnline);
        }

        public Task<bool> UserDisconnected(string userName, string connectionId)
        {
           bool isOfline = false;
            lock(OnlineUsers)
            {
                if (!OnlineUsers.ContainsKey(userName)) return Task.FromResult(isOfline);

                OnlineUsers[userName].Remove(connectionId);

                if (OnlineUsers[userName].Count == 0)
                {
                    OnlineUsers.Remove(userName);
                    isOfline = true;
                }
            }

            return Task.FromResult(isOfline);
        }

        public Task<string[]> GetUsersOnline()
        {
            string[] onlineUsers;

            lock(OnlineUsers)
            {
                onlineUsers = OnlineUsers.OrderBy(k => k.Key).Select(k => k.Key).ToArray();
            }

            return Task.FromResult(onlineUsers);
        }

        public Task<List<string>> GetConnectionForUser(string userName)
        {
            List<string> connectionsIds;
            lock(OnlineUsers)
            {
                connectionsIds = OnlineUsers.GetValueOrDefault(userName);
            }

            return Task.FromResult(connectionsIds);
        }
    }
}