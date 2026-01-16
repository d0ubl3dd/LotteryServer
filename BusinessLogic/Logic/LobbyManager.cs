using BusinessLogic.Exceptions;
using BusinessLogic.Models;
using Contracts.DTOs;
using DataAccess.DAOs;
using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace BusinessLogic.Logic
{
    public class LobbyManager : ILobbyManager
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(LobbyManager));
        private readonly ConcurrentDictionary<string, Lobby> _lobbies = new ConcurrentDictionary<string, Lobby>();

        private const string LOBBY_CODE_CHARS = "ABCDEFGHIJKLMNPQRSTUVWXYZ123456789";

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

            lobby.HostLeft += () =>
            {
                _logger.InfoFormat("[LobbyManager] Evento HostLeft recibido del Lobby {0}. Cerrando...", lobbyCode);
                CloseLobby(lobby);
            };

            if (!_lobbies.TryAdd(lobbyCode, lobby))
            {
                throw new LobbyException("Error interno al registrar el lobby en memoria.");
            }

            host.CurrentLobby = lobby;

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

            lock (lobby.Players)
            {
                if (lobby.IsBanned(player.UserId))
                {
                    throw new PlayerBannedException("No puedes unirte a este lobby porque has sido expulsado.");
                }

                if (lobby.Players.Any(p => p.UserId == player.UserId))
                {
                    throw new UserAlreadyInLobbyException("Ya te encuentras unido a este lobby.");
                }

                if (lobby.Players.Count >= Lobby.MAX_PLAYERS)
                {
                    throw new LobbyFullException("El lobby está lleno.");
                }

                if (!lobby.AddPlayer(player))
                {
                    throw new LobbyException("No se pudo unir al lobby.");
                }
            }

            player.CurrentLobby = lobby;

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
            if (player?.CurrentLobby == null)
            {
                return;
            }

            var lobby = player.CurrentLobby;
            bool shouldCloseLobby = false;

            lock (lobby.Players)
            {
                lobby.RemovePlayer(player);
                player.CurrentLobby = null;

                if (lobby.Host.UserId == player.UserId || lobby.Players.Count == 0)
                {
                    shouldCloseLobby = true;
                }
            }

            if (shouldCloseLobby)
            {
                _logger.InfoFormat("[LobbyManager] El Host {0} ha salido. Cerrando Lobby {1}...", player.UserId, lobby.LobbyCode);
                CloseLobby(lobby);
            }
            else
            {
                lobby.BroadcastPlayerLeft(player.UserId);
                _logger.InfoFormat("[LobbyManager] Jugador {0} salió del lobby {1}", player.UserId, lobby.LobbyCode);
            }
        }

        private void CloseLobby(Lobby lobby)
        {
            if (!_lobbies.TryRemove(lobby.LobbyCode, out _))
            {
                _logger.WarnFormat("[CloseLobby] El lobby {0} ya había sido eliminado o no existe.", lobby.LobbyCode);
            }

            lobby.StopLobbyGame();

            List<PlayerClient> playersToNotify;

            lock (lobby.Players)
            {
                playersToNotify = lobby.Players.ToList();
                lobby.Players.Clear();
            }

            foreach (var player in playersToNotify)
            {
                player.CurrentLobby = null;
                NotifyLobbyClosedSafe(player);
            }

            _logger.InfoFormat("[LobbyManager] Lobby {0} cerrado y eliminado.", lobby.LobbyCode);
        }

        private void NotifyLobbyClosedSafe(PlayerClient player)
        {
            try
            {
                if (player.CallbackChannel is ICommunicationObject comm && comm.State == CommunicationState.Opened)
                {
                    player.CallbackChannel.LobbyClosed();
                }
            }
            catch (CommunicationException)
            {
                _logger.Warn($"[NotifyLobbyClosedSafe] Error de comunicación con {player.UserId}.");
            }
            catch (TimeoutException)
            {
                _logger.Warn($"[NotifyLobbyClosedSafe] Timeout al notificar a {player.UserId}.");
            }
            catch (Exception ex)
            {
                _logger.Warn($"[NotifyLobbyClosedSafe] Error genérico con {player.UserId}: {ex.Message}");
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

            PlayerClient playerToKick = null;

            lock (lobby.Players)
            {
                playerToKick = lobby.Players.FirstOrDefault(p => p.UserId == targetPlayerId);

                if (playerToKick == null)
                {
                    var globalClient = _sessionManager.GetClient(targetPlayerId);
                    if (globalClient != null && globalClient.CurrentLobby == lobby)
                    {
                        playerToKick = globalClient;
                    }
                }

                if (playerToKick == null)
                {
                    throw new ClientNotFoundException("El jugador objetivo no se encuentra en tu lobby.");
                }

                lobby.BanPlayer(targetPlayerId);
                lobby.RemovePlayer(playerToKick);
                playerToKick.CurrentLobby = null;
            }

            lobby.BroadcastKicked(targetPlayerId);
            NotifyKickedSafe(playerToKick);

            _logger.InfoFormat("[LobbyManager] Jugador {0} expulsado del lobby {1}", targetPlayerId, lobby.LobbyCode);
        }

        private void NotifyKickedSafe(PlayerClient player)
        {
            try
            {
                if (player.CallbackChannel is ICommunicationObject comm && comm.State == CommunicationState.Opened)
                {
                    player.CallbackChannel.YouWereKicked();
                }
            }
            catch (Exception exception)
            {
                _logger.Warn($"No se pudo enviar notificación de kick al usuario {player.UserId}: {exception.Message}");
            }
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
                code = new string(Enumerable.Repeat(LOBBY_CODE_CHARS, 6)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
            }
            while (_lobbies.ContainsKey(code));
            return code;
        }
    }
}