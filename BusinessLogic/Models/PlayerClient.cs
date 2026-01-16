using Contracts.Callbacks;
using System.Collections.Generic;

namespace BusinessLogic.Models
{
    public class PlayerClient
    {
        public int UserId { get; }
        public string Nickname { get; }
        public int AvatarId { get; }

        public ILotteryCallback CallbackChannel { get; set; }

        public Lobby CurrentLobby { get; set; }

        public int SelectedBoardId { get; set; }

        public List<int> MarkedPositions { get; } = new List<int>();
        public HashSet<int> WinningCards { get; set; } = new HashSet<int>();

        public PlayerClient()
        {
        }

        public PlayerClient(int userId, string nickname, int avatarId, ILotteryCallback callbackChannel)
        {
            UserId = userId;
            Nickname = nickname;
            AvatarId = avatarId;
            CallbackChannel = callbackChannel;
        }
    }
}