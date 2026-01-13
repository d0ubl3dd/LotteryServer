using Contracts.DTOs;
using System.ServiceModel;

namespace Contracts.Callbacks
{
    [ServiceContract]
    public interface ILotteryCallback : IChatCallback
    {
        [OperationContract(IsOneWay = true)]
        void NotifyCard(int cardId);

        [OperationContract(IsOneWay = true)]
        void NotifyWinner(string nickname);

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

        [OperationContract(IsOneWay = true)]
        void BoardSelected(int userId, int boardId);

        [OperationContract(IsOneWay = true)]
        void OnGameStarted(GameSettingsDto settings);

        [OperationContract(IsOneWay = true)]
        void OnCardDrawn(CardDto card);

        [OperationContract(IsOneWay = true)]
        void OnGameFinished();
        [OperationContract(IsOneWay = true)]
        void LobbyStateUpdated(LobbyStateDto lobbyState);

        [OperationContract(IsOneWay = true)]
        void OnGameResumed();
        [OperationContract(IsOneWay = true)]
        void OnFalseLoteriaResult(string declarerNickname, string challengerNickname, bool wasCorrect);
    }
}