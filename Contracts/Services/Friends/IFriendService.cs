using Contracts.DTOs;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;

namespace Contracts.Services.Friends
{
    [ServiceContract]
    public interface IFriendService
    {
        [OperationContract]
        [FaultContract(typeof(Contracts.Faults.ServiceFault))]
        Task SendRequestFriendship(int currentUserId, int targetUserId);

        [OperationContract]
        [FaultContract(typeof(Contracts.Faults.ServiceFault))]
        Task AcceptFriendRequest(int currentUserId, int requesterId);

        [OperationContract]
        [FaultContract(typeof(Contracts.Faults.ServiceFault))]
        Task RejectFriendRequest(int currentUserId, int requesterId);

        [OperationContract]
        [FaultContract(typeof(Contracts.Faults.ServiceFault))]
        Task RemoveFriend(int currentUserId, int friendUserId);

        [OperationContract]
        [FaultContract(typeof(Contracts.Faults.ServiceFault))]
        Task<List<FriendDTO>> GetFriends(int currentUserId);

        [OperationContract]
        [FaultContract(typeof(Contracts.Faults.ServiceFault))]
        Task<List<FriendRequestDTO>> GetPendingRequests(int currentUserId);

        [OperationContract]
        [FaultContract(typeof(Contracts.Faults.ServiceFault))]
        Task InviteFriendToLobby(string lobbyCode, int targetFriendId);
    }
}