using BusinessLogic.Exceptions;
using BusinessLogic.Logic;
using BusinessLogic.Logic.Base;
using BusinessLogic.Models;
using Contracts.DTOs;
using Contracts.GameData;
using DataAccess;
using System;
using System.Collections.Generic;
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

        public async Task ChooseBoard(User currentUser, int boardId)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                if (currentUser == null)
                {
                    throw new ArgumentNullException(nameof(currentUser));
                }

                var client = GetClientOrThrow(currentUser);

                if (client.CurrentLobby == null)
                {
                    throw new LobbyException("No estás en ningún lobby para elegir tablero.");
                }

                if (client.CurrentLobby.IsGameInProgress)
                {
                    throw new GameException("No puedes cambiar de tablero cuando la partida ya comenzó.");
                }

                var boardCards = BoardConfigurations.GetBoardById(boardId);

                if (boardCards == null || boardCards.Count == 0)
                {
                    throw new ArgumentException($"El tablero número {boardId} no es válido.");
                }

                client.SelectedBoardId = boardId;

                client.WinningCards = new HashSet<int>(boardCards);

                _logger.InfoFormat("[ChooseBoard] Jugador {0} eligió el tablero #{1}.", currentUser.nickname, boardId);

                await Task.CompletedTask;

            }, "ChooseBoard");
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
            return await ExecuteFaultSafeAsync(async () =>
            {
                if (currentUser == null)
                {
                    throw new ArgumentNullException(nameof(currentUser));
                }

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
            return await ExecuteFaultSafeAsync(async () =>
            {
                if (currentUser == null)
                {
                    throw new ArgumentNullException(nameof(currentUser));
                }

                if (string.IsNullOrWhiteSpace(lobbyCode))
                {
                    throw new ArgumentException("El código de lobby es inválido.");
                }

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
            await ExecuteFaultSafeAsync(async () =>
            {
                if (currentUser == null)
                {
                    throw new ArgumentNullException(nameof(currentUser));
                }

                _logger.InfoFormat("[LeaveLobby] {0} solicita salir.", currentUser.nickname);

                var client = GetClientOrThrow(currentUser);

                _lobbyManager.LeaveLobby(client);

                _logger.Info("[LeaveLobby] Salida exitosa.");
                await Task.CompletedTask;

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