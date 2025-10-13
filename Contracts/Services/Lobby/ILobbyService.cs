using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Services.Lobby
{
    [ServiceContract]
    public interface ILobbyService
    {
        [OperationContract]
        Task<string> CreateLobby();

        [OperationContract]
        Task JoinLobby(string lobbyCode);
    }
}
