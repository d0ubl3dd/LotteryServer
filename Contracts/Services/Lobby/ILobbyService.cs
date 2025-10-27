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
        Task<LobbyStateDTO> CreateLobby();

        [OperationContract]
        [FaultContract(typeof(Contracts.Faults.ServiceFault))]
        Task<LobbyStateDTO> JoinLobby(UserSessionDTO currentUser, string lobbyCode);

        [OperationContract(IsOneWay = true)]
        void LeaveLobby();

        [OperationContract]
        [FaultContract(typeof(Contracts.Faults.ServiceFault))]
        Task KickPlayer(int targetPlayerId);
    }
}