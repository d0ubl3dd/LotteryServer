using BusinessLogic.Models;
using Contracts.Callbacks;
using DataAccess;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace BusinessLogic.Logic
{
    public class GlobalSessionManager
    {
        private static readonly Lazy<GlobalSessionManager> _instance = 
            new Lazy<GlobalSessionManager>(() => new GlobalSessionManager());
        public static GlobalSessionManager Instance => _instance.Value;

        private readonly ConcurrentDictionary<int, PlayerClient> _onlineUsers =
            new ConcurrentDictionary<int, PlayerClient>();

        private GlobalSessionManager() { }

        public void RegisterClient(User user, ILotteryCallback callback)
        {
            var client = new PlayerClient(user, callback);
            _onlineUsers[user.id_user] = client;
        }

        public PlayerClient GetClient(int userId)
        {
            _onlineUsers.TryGetValue(userId, out var client);
            return client;
        }
            
        public PlayerClient UnregisterClient(int userId)
        {
            _onlineUsers.TryRemove(userId, out var client);
            return client;
        }
        public int? GetUserIdFromContext()
        {
            int? result = null;

            var callback = OperationContext.Current?.GetCallbackChannel<ILotteryCallback>();
            if (callback != null)
            {
                var entry = _onlineUsers.FirstOrDefault(x => x.Value.CallbackChannel == callback);

                if (!entry.Equals(default(KeyValuePair<int, PlayerClient>)))
                {
                    result = entry.Key;
                }
            }

            return result;
        }
    }
}