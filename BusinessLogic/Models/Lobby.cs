using Contracts.Callbacks;
using Contracts.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BusinessLogic.Models
{
    public class Lobby
    {
        public string LobbyCode { get; }
        public PlayerClient Host { get; }
        public List<PlayerClient> Players { get; } = new List<PlayerClient>();
        public const int MaxPlayers = 4;

        public Lobby(string code, PlayerClient host)
        {
            LobbyCode = code;
            Host = host;
            AddPlayer(host);
        }

        public bool AddPlayer(PlayerClient player)
        {
            if (Players.Count >= MaxPlayers || Players.Any(p => p.UserId == player.UserId))
            {
                return false;
            }
            Players.Add(player);
            player.CurrentLobby = this;
            return true;
        }

        public void RemovePlayer(PlayerClient player)
        {
            Players.Remove(player);
            player.CurrentLobby = null;
        }

        // --- Métodos de Broadcast (para Callbacks) ---

        public void BroadcastPlayerJoined(PlayerClient newPlayer)
        {
            var dto = newPlayer.ToPlayerInfoDTO(newPlayer.UserId == Host.UserId);
            BroadcastToAll(client => client.PlayerJoined(dto));
        }

        public void BroadcastPlayerLeft(int playerId)
        {
            BroadcastToAll(client => client.PlayerLeft(playerId));
        }

        public void BroadcastKicked(int playerId)
        {
            BroadcastToAll(client => client.PlayerKicked(playerId));
        }

        public void BroadcastChatMessage(string nickname, string message)
        {
            BroadcastToAll(client => client.ReceiveChatMessage(nickname, message));
        }

        public void BroadcastLobbyClosed()
        {
            BroadcastToAll(client => client.LobbyClosed());
        }

        // --- Helpers ---
        public List<PlayerInfoDTO> GetPlayerDTOs()
        {
            return Players.Select(p => p.ToPlayerInfoDTO(p.UserId == Host.UserId)).ToList();
        }

        private void BroadcastToAll(Action<ILotteryCallback> action)
        {
            foreach (var player in Players)
            {
                try
                {
                    action(player.CallbackChannel);
                }
                catch (Exception)
                {
                }
            }
        }
    }

    public static class PlayerClientExtensions
    {
        public static PlayerInfoDTO ToPlayerInfoDTO(this PlayerClient player, bool isHost)
        {
            return new PlayerInfoDTO
            {
                UserId = player.UserId,
                Nickname = player.Nickname,
                AvatarId = player.AvatarId,
                IsHost = isHost
            };
        }
    }
}