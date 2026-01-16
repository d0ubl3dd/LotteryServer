using BusinessLogic.Models;
using Contracts.Callbacks;
using DataAccess;
using System.Collections.Generic;

namespace BusinessLogic.Logic
{
    public interface ISessionManager
    {
        ILobbyManager LobbyManagerService { get; set; }

        PlayerClient GetClient(int userId);
        void RegisterClient(User user, ILotteryCallback callback);
        PlayerClient UnregisterClient(int userId);
        int? GetUserIdFromContext();
        bool IsUserOnline(int userId);
        void ReconnectUser(int userId, ILotteryCallback newCallback);
        IEnumerable<PlayerClient> GetAllOnlineUsers();
    }
}