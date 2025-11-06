using Contracts.DTOs;
using System.ServiceModel;
using System.Threading.Tasks;

namespace Contracts.Services.Lobby
{
    [ServiceContract]
    public interface ILobbyService
    {
        [OperationContract]
        [FaultContract(typeof(Contracts.Faults.ServiceFault))]
        Task<LobbyStateDto> CreateLobby();

        [OperationContract]
        [FaultContract(typeof(Contracts.Faults.ServiceFault))]
        Task<LobbyStateDto> JoinLobby(UserDto currentUser, string lobbyCode);

        [OperationContract(IsOneWay = true)]
        void LeaveLobby();

        [OperationContract]
        [FaultContract(typeof(Contracts.Faults.ServiceFault))]
        Task KickPlayer(int targetPlayerId);
    }
}