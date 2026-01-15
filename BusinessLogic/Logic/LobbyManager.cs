using BusinessLogic.Exceptions;
using BusinessLogic.Models;
using Contracts.DTOs;
using DataAccess.DAOs;
using log4net;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace BusinessLogic.Logic
{
    public class LobbyManager : ILobbyManager
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(LobbyManager));
        private readonly ConcurrentDictionary<string, Lobby> _lobbies = new ConcurrentDictionary<string, Lobby>();

        private const string LobbyCodeChars = "ABCDEFGHIJKLMNPQRSTUVWXYZ123456789";

        private readonly ISessionManager _sessionManager;
        private readonly IUserDao _userDao;

        public LobbyManager(ISessionManager sessionManager, IUserDao userDao)
        {
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            _userDao = userDao ?? throw new ArgumentNullException(nameof(userDao));
        }

        public LobbyStateDto CreateLobby(PlayerClient host)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            var lobbyCode = GenerateLobbyCode();
            var lobby = new Lobby(lobbyCode, host, _userDao);

            if (!_lobbies.TryAdd(lobbyCode, lobby))
            {
                throw new LobbyException("Error interno al registrar el lobby en memoria.");
            }

            _logger.InfoFormat("[LobbyManager] Lobby creado: {0} por host {1}", lobbyCode, host.UserId);

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

            if (lobby.IsBanned(player.UserId))
            {
                throw new PlayerBannedException("No puedes unirte a este lobby porque has sido expulsado.");
            }

            if (lobby.Players.Any(p => p.UserId == player.UserId))
            {
                throw new UserAlreadyInLobbyException("Ya te encuentras unido a este lobby.");
            }

            if (lobby.Players.Count >= Lobby.MaxPlayers)
            {
                throw new LobbyFullException("El lobby está lleno.");
            }

            if (!lobby.AddPlayer(player))
            {
                throw new LobbyException("No se pudo unir al lobby (posible error de concurrencia).");
            }

            lobby.BroadcastPlayerJoined(player);
            _logger.InfoFormat("[LobbyManager] Jugador {0} se unió al lobby {1}", player.UserId, lobbyCode);

            return new LobbyStateDto
            {
                LobbyCode = lobbyCode,
                Players = lobby.GetPlayerDTOs()
            };
        }

        public void LeaveLobby(PlayerClient player)
        {
            if (player?.CurrentLobby != null)
            {
                var lobby = player.CurrentLobby;
                lobby.RemovePlayer(player);

                if (player.UserId == lobby.Host.UserId)
                {
                    CloseLobby(lobby);
                }
                else
                {
                    lobby.BroadcastPlayerLeft(player.UserId);
                    _logger.InfoFormat("[LobbyManager] Jugador {0} salió del lobby {1}", player.UserId, lobby.LobbyCode);
                }
            }
        }

        private void CloseLobby(Lobby lobby)
        {
            lobby.BroadcastLobbyClosed();

            foreach (var player in lobby.Players.ToList())
            {
                player.CurrentLobby = null;
                try
                {
                    player.CallbackChannel.LobbyClosed();
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error notificando cierre a {player.UserId}", ex);
                }
            }

            lobby.Players.Clear();
            _lobbies.TryRemove(lobby.LobbyCode, out _);
            _logger.InfoFormat("[LobbyManager] Lobby {0} cerrado por el host.", lobby.LobbyCode);
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

            var playerToKick = _sessionManager.GetClient(targetPlayerId);

            if (playerToKick == null || playerToKick.CurrentLobby != lobby)
            {
                throw new ClientNotFoundException("El jugador objetivo no se encuentra en tu lobby.");
            }

            lobby.BanPlayer(targetPlayerId);
            lobby.RemovePlayer(playerToKick);
            lobby.BroadcastKicked(targetPlayerId);

            try
            {
                playerToKick.CallbackChannel.YouWereKicked();
            }
            catch (Exception exception)
            {
                _logger.Warn($"No se pudo enviar notificación de kick al usuario {targetPlayerId}", exception);
            }

            _logger.InfoFormat("[LobbyManager] Jugador {0} expulsado del lobby {1}", targetPlayerId, lobby.LobbyCode);
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
            var random = new Random();
            string code;
            do
            {
                code = new string(Enumerable.Repeat(LobbyCodeChars, 6)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
            }
            while (_lobbies.ContainsKey(code));
            return code;
        }
    }
}