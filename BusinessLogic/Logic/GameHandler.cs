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
        private readonly LobbyManager _lobbyManager;
        public GameHandler(LobbyManager lobbyManager)
        {
            _lobbyManager = lobbyManager;
        }

        public Task StartGame(User hostUser, GameSettingsDto settings)
        {
            Console.WriteLine($"Intento de iniciar juego por {hostUser.nickname}.");

            Lobby lobby = _lobbyManager.FindLobbyByHostId(hostUser.id_user);

            if (lobby == null)
            {
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
                throw new FaultException<ServiceFault>(
                    new ServiceFault
                    {
                        Message = "El juego en este lobby ya está en curso."
                    },

                    new FaultReason("Juego en curso")
                );
            }

            lobby.StartLobbyGame(settings);

            return Task.CompletedTask;
        }

        public Task UpdateGameSettings(User hostUser, GameSettingsDto settings)
        {
            Console.WriteLine($"Configuración actualizada por {hostUser.nickname}.");

            Lobby lobby = _lobbyManager.FindLobbyByHostId(hostUser.id_user);

            if (lobby == null)
            {
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
                throw new FaultException<ServiceFault>(
                    new ServiceFault
                    {
                        Message = "No se puede cambiar la configuración mientras el juego está en curso."
                    },

                    new FaultReason("Juego en curso")
                );
            }

            return Task.CompletedTask;
        }

        public Task GetScoreboard()
        {
            Console.WriteLine("Scoreboard requested.");
            return Task.CompletedTask;
        }
    }
}