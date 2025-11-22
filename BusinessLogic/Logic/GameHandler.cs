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
                    throw new GameAlreadyRunningException("El juego ya está en curso en este lobby.");

                lobby.StartLobbyGame(settings);

                _logger.Info($"[StartGame] Juego iniciado por {hostUser.nickname}.");

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

            }, "UpdateGameSettings");
        }

        public Task GetScoreboard()
        {
            _logger.Info("[GetScoreboard] Scoreboard solicitado.");
            return Task.CompletedTask;
        }

        public async Task DeclareWin(int userId)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                _logger.Info($"[DeclareWin] Usuario {userId} reclama victoria.");

                // 1. Buscar el lobby del usuario
                // Lógica para validar si realmente ganó (revisar cartón vs cartas salidas)
                // Si ganó -> Notificar a todos
                // Si no ganó -> throw new InvalidDeclarationException(...)

                // Ejemplo simplificado:
                Lobby lobby = _lobbyManager.FindLobbyByHostId(userId);
                if (lobby == null) throw new LobbyNotFoundException("No estás en un lobby.");

                // ... Lógica de validación ...

                // Si es válido, avisar a los clientes:
                lobby.NotifyGameWin(userId);

            }, "DeclareWin");
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
            if (ex is FaultException<ServiceFault>)
                throw ex;

            string errorCode;
            string clientMessage;

            switch (ex)
            {
                case LobbyNotFoundException _:
                    errorCode = "LOBBY_NOT_FOUND";
                    clientMessage = ex.Message;
                    _logger.Warn($"[{operationName}] {clientMessage}");
                    break;

                case GameAlreadyRunningException _:
                    errorCode = "GAME_ALREADY_RUNNING";
                    clientMessage = ex.Message;
                    _logger.Warn($"[{operationName}] {clientMessage}");
                    break;

                case GameException _:
                    errorCode = "GAME_ERROR";
                    clientMessage = ex.Message;
                    _logger.Warn($"[{operationName}] {clientMessage}");
                    break;

                default:
                    errorCode = "GAME_500";
                    clientMessage = "Ha ocurrido un error interno en el servidor.";

                    _logger.Fatal($"[{operationName}] Error inesperado: {ex}", ex);
                    break;
            }

            var fault = new ServiceFault
            {
                ErrorCode = errorCode,
                Message = clientMessage
            };

            throw new FaultException<ServiceFault>(fault, new FaultReason(clientMessage));
        }
    }
}