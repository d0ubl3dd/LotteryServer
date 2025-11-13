using BusinessLogic.Models;
using Contracts.DTOs;
using Contracts.Faults;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.ServiceModel;

namespace BusinessLogic.Logic
{
    public class LobbyManager
    {
        private static readonly Lazy<LobbyManager> _instance =
            new Lazy<LobbyManager>(() => new LobbyManager());
        public static LobbyManager Instance => _instance.Value;

        private readonly ConcurrentDictionary<string, Lobby> _lobbies =
            new ConcurrentDictionary<string, Lobby>();

        private LobbyManager() { }

        public LobbyStateDto CreateLobby(PlayerClient host)
        {
            var lobbyCode = GenerateLobbyCode();
            var lobby = new Lobby(lobbyCode, host);
            _lobbies[lobbyCode] = lobby;

            return new LobbyStateDto
            {
                LobbyCode = lobbyCode,
                Players = lobby.GetPlayerDTOs()
            };
        }

        public LobbyStateDto JoinLobby(PlayerClient player, string lobbyCode)
        {
            if (string.IsNullOrEmpty(lobbyCode))
            {
                throw new FaultException<ServiceFault>(new ServiceFault { Message = "El código del lobby no puede ser nulo o vacío." });
            }

            if (!_lobbies.TryGetValue(lobbyCode, out var lobby))
            {
                throw new FaultException<ServiceFault>(new ServiceFault { Message = "El lobby no existe." });
            }

            if (!lobby.AddPlayer(player))
            {
                throw new FaultException<ServiceFault>(new ServiceFault { Message = "El lobby está lleno o ya estás en él." });
            }

            lobby.BroadcastPlayerJoined(player);

            return new LobbyStateDto
            {
                LobbyCode = lobbyCode,
                Players = lobby.GetPlayerDTOs()
            };
        }

        public void LeaveLobby(PlayerClient player)
        {
            if (player?.CurrentLobby == null) return;

            var lobby = player.CurrentLobby;
            lobby.RemovePlayer(player);

            if (player.UserId == lobby.Host.UserId)
            {
                lobby.BroadcastLobbyClosed();
                _lobbies.TryRemove(lobby.LobbyCode, out _);
            }
            else
            {
                lobby.BroadcastPlayerLeft(player.UserId);
            }
        }

        public void KickPlayer(PlayerClient host, int targetPlayerId)
        {
            var lobby = host.CurrentLobby;
            if (lobby == null || lobby.Host.UserId != host.UserId)
            {
                throw new Exception("No tienes permiso para expulsar jugadores.");
            }

            var playerToKick = GlobalSessionManager.Instance.GetClient(targetPlayerId);
            if (playerToKick == null || playerToKick.CurrentLobby != lobby)
            {
                throw new Exception("El jugador no está en tu lobby.");
            }
            if (playerToKick.UserId == host.UserId)
            {
                throw new Exception("No puedes expulsarte a ti mismo.");
            }

            lobby.RemovePlayer(playerToKick);
            lobby.BroadcastKicked(targetPlayerId);
            playerToKick.CallbackChannel.YouWereKicked();
        }

        public Lobby FindLobbyByHostId(int hostUserId)
        {
            return _lobbies.Values.FirstOrDefault(lobby => lobby.Host.UserId == hostUserId);
        }

        private string GenerateLobbyCode()
        {
            var chars = "ABCDEFGHIJKLMNPQRSTUVWXYZ123456789";
            var random = new Random();
            string code;
            do
            {
                code = new string(Enumerable.Repeat(chars, 6)
                  .Select(s => s[random.Next(s.Length)]).ToArray());
            } while (_lobbies.ContainsKey(code));
            return code;
        }
    }
}