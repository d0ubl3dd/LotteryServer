using BusinessLogic.Exceptions;
using BusinessLogic.Models;
using Contracts.DTOs;
using log4net;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace BusinessLogic.Logic
{
    public class LobbyManager
    {
        private static readonly Lazy<LobbyManager> _instance = new Lazy<LobbyManager>(() => new LobbyManager());
        public static LobbyManager Instance => _instance.Value;

        private static readonly ILog _logger = LogManager.GetLogger(typeof(LobbyManager));

        private readonly ConcurrentDictionary<string, Lobby> _lobbies = new ConcurrentDictionary<string, Lobby>();

        private LobbyManager() { }

        public LobbyStateDto CreateLobby(PlayerClient host)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));

            var lobbyCode = GenerateLobbyCode();
            var lobby = new Lobby(lobbyCode, host);

            if (!_lobbies.TryAdd(lobbyCode, lobby))
            {
                throw new LobbyException("Error interno al registrar el lobby en memoria.");
            }

            _logger.Info($"[LobbyManager] Lobby creado: {lobbyCode} por host {host.UserId}");

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
                throw new ArgumentException("El código del lobby no puede ser vacío.");
            }

            if (!_lobbies.TryGetValue(lobbyCode, out var lobby))
            {
                throw new LobbyNotFoundException($"El lobby {lobbyCode} no existe.");
            }

            if (lobby.Players.Any(p => p.UserId == player.UserId))
            {
                throw new UserAlreadyInLobbyException("Ya te encuentras unido a este lobby.");
            }

            const int MAX_PLAYERS = 4;
            if (lobby.Players.Count >= MAX_PLAYERS)
            {
                throw new LobbyFullException("El lobby está lleno, no se admiten más jugadores.");
            }

            if (!lobby.AddPlayer(player))
            {
                throw new LobbyException("No se pudo unir al lobby (posible error de concurrencia).");
            }

            lobby.BroadcastPlayerJoined(player);
            _logger.Info($"[LobbyManager] Jugador {player.UserId} se unió al lobby {lobbyCode}");

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
                _logger.Info($"[LobbyManager] Host {player.UserId} cerró el lobby {lobby.LobbyCode}");
            }
            else
            {
                lobby.BroadcastPlayerLeft(player.UserId);
                _logger.Info($"[LobbyManager] Jugador {player.UserId} salió del lobby {lobby.LobbyCode}");
            }
        }

        public void KickPlayer(PlayerClient host, int targetPlayerId)
        {
            var lobby = host.CurrentLobby;

            if (lobby == null || lobby.Host.UserId != host.UserId)
            {
                throw new LobbyActionNotAllowedException("No tienes permisos de Host para expulsar jugadores.");
            }

            if (host.UserId == targetPlayerId)
            {
                throw new LobbyActionNotAllowedException("No puedes expulsarte a ti mismo.");
            }

            var playerToKick = GlobalSessionManager.Instance.GetClient(targetPlayerId);

            if (playerToKick == null || playerToKick.CurrentLobby != lobby)
            {
                throw new ClientNotFoundException("El jugador objetivo no se encuentra en tu lobby.");
            }

            lobby.RemovePlayer(playerToKick);
            lobby.BroadcastKicked(targetPlayerId);

            try
            {
                playerToKick.CallbackChannel.YouWereKicked();
            }
            catch (Exception ex)
            {
                _logger.Warn($"[LobbyManager] No se pudo enviar notificación de kick al usuario {targetPlayerId}: {ex.Message}");
            }

            _logger.Info($"[LobbyManager] Jugador {targetPlayerId} expulsado del lobby {lobby.LobbyCode}");
        }

        public Lobby FindLobbyByHostId(int hostUserId)
        {
            return _lobbies.Values.FirstOrDefault(lobby => lobby.Host.UserId == hostUserId);
        }

        public Lobby FindLobbyByPlayerId(int userId)
        {
            return _lobbies.Values.FirstOrDefault(lobby => lobby.Players.Any(p => p.UserId == userId));
        }

        private string GenerateLobbyCode()
        {
            var chars = "ABCDEFGHIJKLMNPQRSTUVWXYZ123456789";
            var random = new Random();
            string code;
            do
            {
                code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
            }
            while (_lobbies.ContainsKey(code));

            return code;
        }
    }
}