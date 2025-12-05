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
        private readonly ILobbyManager _lobbyManager;
        private readonly ISessionManager _sessionManager;

        public LobbyHandler(ILobbyManager lobbyManager, ISessionManager sessionManager)
            : base(typeof(LobbyHandler))
        {
            _lobbyManager = lobbyManager ?? throw new ArgumentNullException(nameof(lobbyManager));
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        }

        private PlayerClient GetClientOrThrow(User user)
        {
            _logger.InfoFormat("[GetClient] Buscando sesión para {0} (ID {1}).", user.nickname, user.id_user);

            var client = _sessionManager.GetClient(user.id_user);

            if (client == null)
            {
                throw new UserNotConnectedException("No se encontró una sesión activa para realizar esta acción.");
            }

            return client;
        }

        public async Task<LobbyStateDto> CreateLobby(User currentUser)
        {
            if (currentUser == null) throw new ArgumentNullException(nameof(currentUser));

            return await ExecuteFaultSafeAsync(async () =>
            {
                _logger.InfoFormat("[CreateLobby] Intento de creación por {0}.", currentUser.nickname);

                var hostClient = GetClientOrThrow(currentUser);

                if (hostClient.CurrentLobby != null)
                {
                    throw new UserAlreadyInLobbyException("Ya te encuentras dentro de un lobby.");
                }

                var lobbyState = _lobbyManager.CreateLobby(hostClient);

                _logger.InfoFormat("[CreateLobby] Lobby creado: {0}", lobbyState.LobbyCode);

                await Task.CompletedTask;
                return lobbyState;

            }, "CreateLobby");
        }

        public async Task<LobbyStateDto> JoinLobby(User currentUser, string lobbyCode)
        {
            if (currentUser == null) throw new ArgumentNullException(nameof(currentUser));
            if (string.IsNullOrWhiteSpace(lobbyCode)) throw new ArgumentException("El código de lobby es inválido.");

            return await ExecuteFaultSafeAsync(async () =>
            {
                _logger.InfoFormat("[JoinLobby] {0} intenta unirse a {1}.", currentUser.nickname, lobbyCode);

                var playerClient = GetClientOrThrow(currentUser);

                if (playerClient.CurrentLobby != null)
                {
                    throw new UserAlreadyInLobbyException("Debes salir de tu lobby actual antes de unirte a otro.");
                }

                var lobbyState = _lobbyManager.JoinLobby(playerClient, lobbyCode);

                _logger.InfoFormat("[JoinLobby] Unión exitosa al lobby {0}.", lobbyCode);

                await Task.CompletedTask;
                return lobbyState;

            }, "JoinLobby");
        }

        public async Task LeaveLobby(User currentUser)
        {
            if (currentUser == null) throw new ArgumentNullException(nameof(currentUser));

            await ExecuteFaultSafeAsync(async () =>
            {
                _logger.InfoFormat("[LeaveLobby] {0} solicita salir.", currentUser.nickname);

                var client = GetClientOrThrow(currentUser);

                _lobbyManager.LeaveLobby(client);

                _logger.Info("[LeaveLobby] Salida exitosa.");
                await Task.CompletedTask;

            }, "LeaveLobby");
        }

        public async Task KickPlayer(User currentUser, int targetPlayerId)
        {
            if (currentUser == null) throw new ArgumentNullException(nameof(currentUser));

            await ExecuteFaultSafeAsync(async () =>
            {
                _logger.InfoFormat("[KickPlayer] {0} intenta expulsar al ID {1}.", currentUser.nickname, targetPlayerId);

                var hostClient = GetClientOrThrow(currentUser);

                if (hostClient.CurrentLobby == null)
                {
                    throw new LobbyException("No estás en un lobby para expulsar a alguien.");
                }

                _lobbyManager.KickPlayer(hostClient, targetPlayerId);

                _logger.InfoFormat("[KickPlayer] Jugador {0} expulsado.", targetPlayerId);
                await Task.CompletedTask;

            }, "KickPlayer");
        }
    }
}