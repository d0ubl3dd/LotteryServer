using Contracts.DTOs;
using DataAccess;
using System;
using System.Threading.Tasks;
using BusinessLogic.Logic;
using BusinessLogic.Models;
using System.ServiceModel;
using Contracts.Faults;

namespace BusinessLogic.Handlers
{
    public class GameHandler
    {
        private static readonly log4net.ILog _logger = log4net.LogManager.GetLogger(typeof(GameHandler));

        private readonly LobbyManager _lobbyManager;

        public GameHandler(LobbyManager lobbyManager)
        {
            _lobbyManager = lobbyManager;
        }

        public Task StartGame(User hostUser, GameSettingsDto settings)
        {
            try
            {
                _logger.Info($"Intento de iniciar juego por {hostUser.nickname}.");

                Lobby lobby = _lobbyManager.FindLobbyByHostId(hostUser.id_user);

                if (lobby == null)
                {
                    _logger.Warn($"Lobby no encontrado para host {hostUser.nickname}.");

                    throw new FaultException<ServiceFault>(
                        new ServiceFault
                        {
                            Message = "No se encontró un lobby donde seas el host."
                        },
                        new FaultReason("Lobby no encontrado")
                    );
                }

                if (lobby.IsGameInProgress)
                {
                    _logger.Warn($"El juego ya está en curso. Host: {hostUser.nickname}");

                    throw new FaultException<ServiceFault>(
                        new ServiceFault
                        {
                            Message = "El juego en este lobby ya está en curso."
                        },
                        new FaultReason("Juego en curso")
                    );
                }

                lobby.StartLobbyGame(settings);

                _logger.Info($"Juego iniciado correctamente por {hostUser.nickname}.");

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.Error("Error inesperado al iniciar juego.", ex);
                throw;
            }
        }

        public Task UpdateGameSettings(User hostUser, GameSettingsDto settings)
        {
            try
            {
                _logger.Info($"Configuración de juego actualizada por {hostUser.nickname}.");

                Lobby lobby = _lobbyManager.FindLobbyByHostId(hostUser.id_user);

                if (lobby == null)
                {
                    _logger.Warn($"Lobby no encontrado para host {hostUser.nickname}.");

                    throw new FaultException<ServiceFault>(
                        new ServiceFault
                        {
                            Message = "No se encontró un lobby donde seas el host."
                        },
                        new FaultReason("Lobby no encontrado")
                    );
                }

                if (lobby.IsGameInProgress)
                {
                    _logger.Warn($"Intento de cambiar configuración durante partida. Host: {hostUser.nickname}");

                    throw new FaultException<ServiceFault>(
                        new ServiceFault
                        {
                            Message = "No se puede cambiar la configuración mientras el juego está en curso."
                        },
                        new FaultReason("Juego en curso")
                    );
                }

                _logger.Info($"Configuración de juego actualizada correctamente por {hostUser.nickname}.");

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.Error("Error inesperado al actualizar configuración de juego.", ex);
                throw;
            }
        }

        public Task GetScoreboard()
        {
            _logger.Info("Scoreboard solicitado.");
            return Task.CompletedTask;
        }
    }
}
