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
            _lobbyManager = lobbyManager;
        }

        public async Task StartGame(User hostUser, GameSettingsDto settings)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                _logger.Info($"[StartGame] Host {hostUser.nickname} intenta iniciar partida.");

                Lobby lobby = _lobbyManager.FindLobbyByHostId(hostUser.id_user);

                if (lobby == null)
                    throw new LobbyNotFoundException("No se encontró un lobby donde seas el host.");

                if (lobby.IsGameInProgress)
                    throw new GameAlreadyRunningException("El juego en este lobby ya está en curso.");

                lobby.StartLobbyGame(settings);

                _logger.Info($"[StartGame] Juego iniciado por {hostUser.nickname}.");

                await Task.CompletedTask;

            }, "StartGame");
        }

        public async Task UpdateGameSettings(User hostUser, GameSettingsDto settings)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                _logger.Info($"[UpdateGameSettings] Host {hostUser.nickname} intenta actualizar configuración.");

                Lobby lobby = _lobbyManager.FindLobbyByHostId(hostUser.id_user);

                if (lobby == null)
                    throw new LobbyNotFoundException("No se encontró un lobby donde seas el host.");

                if (lobby.IsGameInProgress)
                    throw new GameAlreadyRunningException("No se puede cambiar la configuración mientras el juego está en curso.");

                _logger.Info($"[UpdateGameSettings] Configuración actualizada por {hostUser.nickname}.");

                await Task.CompletedTask;

            }, "UpdateGameSettings");
        }

        public Task GetScoreboard()
        {
            _logger.Info("[GetScoreboard] Scoreboard solicitado.");
            return Task.CompletedTask;
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

        private void HandleException(Exception ex, string operationName)
        {
            var fault = ex as FaultException<ServiceFault>;
            if (fault != null)
                throw fault;

            string errorCode;

            if (ex is LobbyNotFoundException)
            {
                errorCode = "LOBBY_NOT_FOUND";
                _logger.Warn($"[{operationName}] {ex.Message}");
            }
            else if (ex is GameAlreadyRunningException)
            {
                errorCode = "GAME_ALREADY_RUNNING";
                _logger.Warn($"[{operationName}] {ex.Message}");
            }
            else if (ex is GameException)
            {
                errorCode = "GAME_ERROR";
                _logger.Warn($"[{operationName}] {ex.Message}");
            }
            else
            {
                errorCode = "GAME_500";
                _logger.Fatal($"[{operationName}] Error inesperado: {ex}", ex);
                throw new FaultException<ServiceFault>(
                    new ServiceFault
                    {
                        ErrorCode = errorCode,
                        Message = "Ha ocurrido un error interno en el servidor."
                    },
                    new FaultReason("Error interno")
                );
            }

            throw new FaultException<ServiceFault>(
                new ServiceFault
                {
                    ErrorCode = errorCode,
                    Message = ex.Message
                },
                new FaultReason(ex.Message)
            );
        }
    }
}