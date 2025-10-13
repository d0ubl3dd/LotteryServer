using Contracts.Callbacks;
using Contracts.Services.Chat;
using Contracts.Services.Friends;
using Contracts.Services.Game;
using Contracts.Services.Lobby;
using Contracts.Services.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    [ServiceContract(CallbackContract = typeof(ILotteryCallback))]
    public interface ILotteryService : IChatService, IFriendService, IGameService, ILobbyService, IAuthenticationService, IUserService
    {
    }
}
