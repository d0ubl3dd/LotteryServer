using BusinessLogic.Exceptions;
using BusinessLogic.Models;
using Contracts.DTOs;
using Contracts.Faults;
using DataAccess;
using log4net;
using System;
using System.ServiceModel;
using System.Threading.Tasks;

namespace BusinessLogic.Logic
{
    public class LobbyHandler
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(LobbyHandler));
        private readonly LobbyManager _lobbyManager;

        public LobbyHandler(LobbyManager lobbyManager)
        {
            _lobbyManager = lobbyManager ?? throw new ArgumentNullException(nameof(lobbyManager));
        }
        private PlayerClient GetClientOrThrow(User user)
        {
            _logger.Info($"[GetClient] Buscando sesión para {user.nickname} (ID {user.id_user}).");

            var client = GlobalSessionManager.Instance.GetClient(user.id_user);

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
                if (currentUser == null) throw new ArgumentNullException(nameof(currentUser));

                _logger.Info($"[CreateLobby] Intento de creación por {currentUser.nickname}.");

                var hostClient = GetClientOrThrow(currentUser);

                if (hostClient.CurrentLobby != null)
                {
                    throw new UserAlreadyInLobbyException("Ya te encuentras dentro de un lobby.");
                }

                var lobbyState = _lobbyManager.CreateLobby(hostClient);

                _logger.Info($"[CreateLobby] Lobby creado: {lobbyState.LobbyCode}");
                return lobbyState;

            }, "CreateLobby");
        }

        public async Task<LobbyStateDto> JoinLobby(User currentUser, string lobbyCode)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                if (currentUser == null) throw new ArgumentNullException(nameof(currentUser));
                if (string.IsNullOrWhiteSpace(lobbyCode)) throw new ArgumentException("El código de lobby es inválido.");

                _logger.Info($"[JoinLobby] {currentUser.nickname} intenta unirse a {lobbyCode}.");

                var playerClient = GetClientOrThrow(currentUser);

                if (playerClient.CurrentLobby != null)
                {
                    throw new UserAlreadyInLobbyException("Debes salir de tu lobby actual antes de unirte a otro.");
                }

                var lobbyState = _lobbyManager.JoinLobby(playerClient, lobbyCode);

                _logger.Info($"[JoinLobby] Unión exitosa al lobby {lobbyCode}.");
                return lobbyState;

            }, "JoinLobby");
        }

        public async Task LeaveLobby(User currentUser)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                if (currentUser == null) throw new ArgumentNullException(nameof(currentUser));

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
                if (currentUser == null) throw new ArgumentNullException(nameof(currentUser));

                _logger.Info($"[KickPlayer] {currentUser.nickname} intenta expulsar al ID {targetPlayerId}.");

                var hostClient = GetClientOrThrow(currentUser);

                if (hostClient.CurrentLobby == null)
                    throw new LobbyException("No estás en un lobby para expulsar a alguien.");

                _lobbyManager.KickPlayer(hostClient, targetPlayerId);

                _logger.Info($"[KickPlayer] Jugador {targetPlayerId} expulsado.");

            }, "KickPlayer");
        }

        private async Task ExecuteFaultSafeAsync(Func<Task> action, string operationName)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                HandleException(ex, operationName);
            }
        }

        private async Task<T> ExecuteFaultSafeAsync<T>(Func<Task<T>> action, string operationName)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                HandleException(ex, operationName);
                return default;
            }
        }

        private void HandleException(Exception ex, string operationName)
        {
            if (ex is FaultException<ServiceFault>)
                throw ex;

            string errorCode;
            string clientMessage;

            switch (ex)
            {
                case LobbyActionNotAllowedException _:
                    errorCode = "LOBBY_ACTION_DENIED";
                    clientMessage = ex.Message;
                    _logger.Warn($"[{operationName}] Acción denegada: {clientMessage}");
                    break;

                case LobbyNotFoundException _:
                    errorCode = "LOBBY_NOT_FOUND";
                    clientMessage = "El código ingresado no corresponde a ningún lobby activo.";
                    _logger.Warn($"[{operationName}] Intento de unión a lobby inexistente.");
                    break;

                case UserAlreadyInLobbyException _:
                    errorCode = "LOBBY_USER_ALREADY_IN";
                    clientMessage = ex.Message;
                    _logger.Warn($"[{operationName}] Usuario ya en lobby.");
                    break;

                case UserNotConnectedException _:
                    errorCode = "LOBBY_USER_OFFLINE";
                    clientMessage = "No hay conexión activa con el usuario.";
                    _logger.Warn($"[{operationName}] Usuario offline.");
                    break;

                case LobbyFullException _:
                    errorCode = "LOBBY_FULL";
                    clientMessage = "El lobby está lleno.";
                    _logger.Warn($"[{operationName}] Lobby lleno.");
                    break;

                case LobbyException _:
                    errorCode = "LOBBY_ERROR";
                    clientMessage = ex.Message;
                    _logger.Warn($"[{operationName}] Error de lobby: {ex.Message}");
                    break;

                case ArgumentNullException _:
                    errorCode = "LOBBY_BAD_REQUEST";
                    clientMessage = "Datos de solicitud inválidos.";
                    _logger.Error($"[{operationName}] Argumento inválido: {ex.Message}");
                    break;

                case ClientNotFoundException _:
                    errorCode = "LOBBY_SESSION_ERROR";
                    clientMessage = "Error de sesión.";
                    break;

                default:
                    errorCode = "LOBBY_INTERNAL_ERROR";
                    clientMessage = "Ocurrió un error inesperado.";
                    _logger.Fatal($"[{operationName}] Error crítico: {ex}", ex);
                    break;
            }

            throw new FaultException<ServiceFault>(
                new ServiceFault
                {
                    ErrorCode = errorCode,
                    Message = clientMessage
                },
                new FaultReason(clientMessage)
            );
        }
    }
}