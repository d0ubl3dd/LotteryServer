using Contracts.DTOs;
using System.ServiceModel;

namespace Contracts.Callbacks
{
    public interface ILobbyCallback
    {
        [OperationContract(IsOneWay = true)]
        void PlayerJoined(UserDto newPlayer);

        [OperationContract(IsOneWay = true)]
        void PlayerLeft(int playerId);

        [OperationContract(IsOneWay = true)]
        void PlayerKicked(int playerId);

        [OperationContract(IsOneWay = true)]
        void YouWereKicked();

        [OperationContract(IsOneWay = true)]
        void LobbyClosed();

        [OperationContract(IsOneWay = true)]
        void ReceiveLobbyInvite(string inviterNickname, string lobbyCode);
    }
}