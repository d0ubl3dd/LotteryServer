using Contracts.Callbacks;
using DataAccess;

namespace BusinessLogic.Models
{
    public class PlayerClient
    {
        public int UserId { get; }
        public string Nickname { get; }
        public int AvatarId { get; }
        public ILotteryCallback CallbackChannel { get; }
        public Lobby CurrentLobby { get; set; }

        public PlayerClient(User user, ILotteryCallback callback)
        {
            UserId = user.id_user;
            Nickname = user.nickname;
            AvatarId = user.id_avatar;
            CallbackChannel = callback;
        }
    }
}