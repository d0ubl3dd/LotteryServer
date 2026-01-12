using BusinessLogic.Exceptions;
using BusinessLogic.Logic;
using BusinessLogic.Logic.Base;
using BusinessLogic.Models;
using Contracts.DTOs;
using DataAccess;
using System;
using System.Threading.Tasks;

namespace BusinessLogic.Handlers
{
    public class GameHandler : BaseHandler
    {
        private readonly ILobbyManager _lobbyManager;

        public GameHandler(ILobbyManager lobbyManager) : base(typeof(GameHandler))
        {
            _lobbyManager = lobbyManager ?? throw new ArgumentNullException(nameof(lobbyManager));
        }

        public async Task StartGame(User hostUser, GameSettingsDto settings)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                if (hostUser == null)
                {
                    throw new ArgumentNullException(nameof(hostUser));
                }
                if (settings == null)
                {
                    throw new ArgumentNullException(nameof(settings));
                }

                _logger.InfoFormat("[StartGame] Host {0} intenta iniciar partida.", hostUser.nickname);

                Lobby lobby = _lobbyManager.FindLobbyByHostId(hostUser.id_user);

                if (lobby == null)
                {
                    throw new LobbyNotFoundException("No se encontró un lobby donde seas el host.");
                }

                if (lobby.IsGameInProgress)
                {
                    throw new GameAlreadyRunningException("El juego ya está en curso en este lobby.");
                }

                lobby.StartLobbyGame(settings);

                _logger.InfoFormat("[StartGame] Juego iniciado exitosamente por {0}.", hostUser.nickname);

                await Task.CompletedTask;

            }, "StartGame");
        }

        public async Task UpdateGameSettings(User hostUser, GameSettingsDto settings)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                if (hostUser == null)
                {
                    throw new ArgumentNullException(nameof(hostUser));
                }

                _logger.InfoFormat("[UpdateGameSettings] Host {0} intenta actualizar configuración.", hostUser.nickname);

                Lobby lobby = _lobbyManager.FindLobbyByHostId(hostUser.id_user);

                if (lobby == null)
                {
                    throw new LobbyNotFoundException("No se encontró el lobby para editar.");
                }

                if (lobby.IsGameInProgress)
                {
                    throw new GameAlreadyRunningException("No se puede cambiar la configuración durante una partida.");
                }

                _logger.Info("[UpdateGameSettings] Configuración actualizada.");

                await Task.CompletedTask;

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
        public async Task DeclareWin(PlayerBoardDto playerBoard)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                var lobby = _lobbyManager.FindLobbyByPlayerId(playerBoard.PlayerId);
                if (lobby == null)
                    throw new LobbyNotFoundException("Lobby no encontrado.");

                await lobby.DeclareWinAsync(playerBoard);

            }, "DeclareWin");
        }

        public async Task<bool> ValidateFalseLoteriaAsync(int challengerUserId)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                var lobby = _lobbyManager.FindLobbyByPlayerId(challengerUserId);
                if (lobby == null)
                {
                    throw new LobbyNotFoundException("No se encontró el lobby del jugador.");
                }

                return await lobby.ValidateFalseLoteriaAsync(challengerUserId);

            }, "ValidateFalseLoteriaAsync");
        }
    }
}