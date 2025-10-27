using Contracts.DTOs;
using System.ServiceModel;

namespace Contracts.Callbacks
{
    [ServiceContract]
    public interface ILotteryCallback
    {
        [OperationContract(IsOneWay = true)]
        void NotifyCard(int cardId);

        [OperationContract(IsOneWay = true)]
        void NotifyWinner(string nickname);

        [OperationContract(IsOneWay = true)]
        void ReceiveChatMessage(string nickname, string message);

        [OperationContract(IsOneWay = true)]
        void PlayerJoined(PlayerInfoDTO newPlayer);

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