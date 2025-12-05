using BusinessLogic.Models;
using DataAccess;
using Contracts.Callbacks;

namespace BusinessLogic.Logic
{
    public interface ISessionManager
    {
        PlayerClient GetClient(int userId);
        void RegisterClient(User user, ILotteryCallback callback);
        PlayerClient UnregisterClient(int userId);
        int? GetUserIdFromContext();
    }
}