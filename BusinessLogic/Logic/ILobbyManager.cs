using BusinessLogic.Models;
using Contracts.DTOs;

namespace BusinessLogic.Logic
{
    public interface ILobbyManager
    {
        LobbyStateDto CreateLobby(PlayerClient host);
        LobbyStateDto JoinLobby(PlayerClient player, string lobbyCode);
        void LeaveLobby(PlayerClient player);
        void KickPlayer(PlayerClient host, int targetPlayerId);
        Lobby FindLobbyByHostId(int hostUserId);
        Lobby FindLobbyByPlayerId(int userId);
    }
}