using BusinessLogic.Exceptions;
using BusinessLogic.Logic;
using BusinessLogic.Models;
using Contracts.DTOs;
using Contracts.Faults;
using DataAccess;
using log4net;
using System;
using System.ServiceModel;
using System.Threading.Tasks;

namespace BusinessLogic.Handlers
{
    public class GameHandler
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(GameHandler));
        private readonly LobbyManager _lobbyManager;

        public GameHandler(LobbyManager lobbyManager)
        {
            _lobbyManager = lobbyManager ?? throw new ArgumentNullException(nameof(lobbyManager));
        }

        public async Task StartGame(User hostUser, GameSettingsDto settings)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                if (hostUser == null) throw new ArgumentNullException(nameof(hostUser));
                if (settings == null) throw new ArgumentNullException(nameof(settings));

                _logger.Info($"[StartGame] Host {hostUser.nickname} intenta iniciar partida.");

                Lobby lobby = _lobbyManager.FindLobbyByHostId(hostUser.id_user);

                if (lobby == null)
                    throw new LobbyNotFoundException("No se encontró un lobby donde seas el host.");

                if (lobby.IsGameInProgress)
                    throw new GameAlreadyRunningException("El juego ya está en curso en este lobby.");

                lobby.StartLobbyGame(settings);

                _logger.Info($"[StartGame] Juego iniciado exitosamente por {hostUser.nickname}.");

            }, "StartGame");
        }

        public async Task UpdateGameSettings(User hostUser, GameSettingsDto settings)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                if (hostUser == null) throw new ArgumentNullException(nameof(hostUser));

                _logger.Info($"[UpdateGameSettings] Host {hostUser.nickname} intenta actualizar configuración.");

                Lobby lobby = _lobbyManager.FindLobbyByHostId(hostUser.id_user);

                if (lobby == null)
                    throw new LobbyNotFoundException("No se encontró el lobby para editar.");

                if (lobby.IsGameInProgress)
                    throw new GameAlreadyRunningException("No se puede cambiar la configuración durante una partida.");


                _logger.Info($"[UpdateGameSettings] Configuración actualizada.");

            }, "UpdateGameSettings");
        }

        public async Task GetScoreboard()
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                _logger.Info("[GetScoreboard] Scoreboard solicitado.");

                await Task.CompletedTask;

            }, "GetScoreboard");
        }
        /*
        public async Task DeclareWin(int userId)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                _logger.Info($"[DeclareWin] Usuario {userId} canta victoria (¡BUENAS!).");

                Lobby lobby = _lobbyManager.FindLobbyByPlayerId(userId);

                if (lobby == null)
                    throw new LobbyNotFoundException("No estás en una partida activa.");

                bool esVictoriaValida = lobby.ValidateWinCondition(userId);

                if (!esVictoriaValida)
                {
                    throw new InvalidGameActionException("Tu tabla no cumple las condiciones para ganar.");
                }

                lobby.NotifyGameWin(userId);
                _logger.Info($"[DeclareWin] Victoria validada para {userId}.");

            }, "DeclareWin");
        }*/

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

        private void HandleException(Exception ex, string operationName)
        {
            if (ex is FaultException<ServiceFault>)
                throw ex;

            string errorCode;
            string clientMessage;

            switch (ex)
            {
                case LobbyNotFoundException _:
                    errorCode = "GAME_LOBBY_NOT_FOUND";
                    clientMessage = ex.Message;
                    _logger.Warn($"[{operationName}] Lobby no encontrado.");
                    break;

                case GameAlreadyRunningException _:
                    errorCode = "GAME_ALREADY_ACTIVE";
                    clientMessage = ex.Message;
                    _logger.Warn($"[{operationName}] Intento de modificar juego activo.");
                    break;

                case InvalidGameActionException _:
                    errorCode = "GAME_ACTION_INVALID";
                    clientMessage = ex.Message;
                    _logger.Warn($"[{operationName}] Acción de juego inválida: {ex.Message}");
                    break;

                case ArgumentNullException _:
                    errorCode = "GAME_BAD_REQUEST";
                    clientMessage = "Datos de partida incompletos.";
                    _logger.Error($"[{operationName}] Argumento nulo.");
                    break;

                default:
                    errorCode = "GAME_INTERNAL_ERROR";
                    clientMessage = "Error interno del servidor de juego.";
                    _logger.Fatal($"[{operationName}] Error inesperado: {ex}", ex);
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