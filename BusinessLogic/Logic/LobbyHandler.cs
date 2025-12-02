using BusinessLogic.Exceptions;
using BusinessLogic.Logic;
using BusinessLogic.Logic.Base;
using BusinessLogic.Models;
using Contracts.DTOs;
using DataAccess;
using System;
using System.Threading.Tasks;

namespace BusinessLogic.Logic
{
    public class LobbyHandler : BaseHandler
    {
        private readonly LobbyManager _lobbyManager;

        public LobbyHandler(LobbyManager lobbyManager) : base(typeof(LobbyHandler))
        {
            if (lobbyManager == null)
            {
                throw new ArgumentNullException(nameof(lobbyManager));
            }
            _lobbyManager = lobbyManager;
        }

        private PlayerClient GetClientOrThrow(User user)
        {
            PlayerClient client;

            _logger.Info($"[GetClient] Buscando sesión para {user.nickname} (ID {user.id_user}).");

            client = GlobalSessionManager.Instance.GetClient(user.id_user);

            if (client == null)
            {
                throw new UserNotConnectedException("No se encontró una sesión activa para realizar esta acción.");
            }

            return client;
        }

        public async Task<LobbyStateDto> CreateLobby(User currentUser)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                LobbyStateDto lobbyState;

                if (currentUser == null)
                {
                    throw new ArgumentNullException(nameof(currentUser));
                }

                _logger.Info($"[CreateLobby] Intento de creación por {currentUser.nickname}.");

                var hostClient = GetClientOrThrow(currentUser);

                if (hostClient.CurrentLobby != null)
                {
                    throw new UserAlreadyInLobbyException("Ya te encuentras dentro de un lobby.");
                }

                lobbyState = _lobbyManager.CreateLobby(hostClient);

                _logger.Info($"[CreateLobby] Lobby creado: {lobbyState.LobbyCode}");

                return lobbyState;

            }, "CreateLobby");
        }

        public async Task<LobbyStateDto> JoinLobby(User currentUser, string lobbyCode)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                LobbyStateDto lobbyState;

                if (currentUser == null)
                {
                    throw new ArgumentNullException(nameof(currentUser));
                }
                if (string.IsNullOrWhiteSpace(lobbyCode))
                {
                    throw new ArgumentException("El código de lobby es inválido.");
                }

                _logger.Info($"[JoinLobby] {currentUser.nickname} intenta unirse a {lobbyCode}.");

                var playerClient = GetClientOrThrow(currentUser);

                if (playerClient.CurrentLobby != null)
                {
                    throw new UserAlreadyInLobbyException("Debes salir de tu lobby actual antes de unirte a otro.");
                }

                lobbyState = _lobbyManager.JoinLobby(playerClient, lobbyCode);

                _logger.Info($"[JoinLobby] Unión exitosa al lobby {lobbyCode}.");

                return lobbyState;

            }, "JoinLobby");
        }

        public async Task LeaveLobby(User currentUser)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                if (currentUser == null)
                {
                    throw new ArgumentNullException(nameof(currentUser));
                }

                _logger.Info($"[LeaveLobby] {currentUser.nickname} solicita salir.");

                var client = GetClientOrThrow(currentUser);

                _lobbyManager.LeaveLobby(client);

                _logger.Info($"[LeaveLobby] Salida exitosa.");

            }, "LeaveLobby");
        }

        public async Task KickPlayer(User currentUser, int targetPlayerId)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                if (currentUser == null)
                {
                    throw new ArgumentNullException(nameof(currentUser));
                }

                _logger.Info($"[KickPlayer] {currentUser.nickname} intenta expulsar al ID {targetPlayerId}.");

                var hostClient = GetClientOrThrow(currentUser);

                if (hostClient.CurrentLobby == null)
                {
                    throw new LobbyException("No estás en un lobby para expulsar a alguien.");
                }

                _lobbyManager.KickPlayer(hostClient, targetPlayerId);

                _logger.Info($"[KickPlayer] Jugador {targetPlayerId} expulsado.");

            }, "KickPlayer");
        }
    }
}