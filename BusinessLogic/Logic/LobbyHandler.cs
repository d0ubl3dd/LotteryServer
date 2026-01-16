using BusinessLogic.Exceptions;
using BusinessLogic.Logic.Base;
using BusinessLogic.Models;
using Contracts.DTOs;
using Contracts.GameData;
using DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
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
            if (currentUser == null)
            {
                throw new ArgumentNullException(nameof(currentUser));
            }

            await ExecuteFaultSafeAsync(() =>
            {
                var client = GetClientOrThrow(currentUser);

                if (client.CurrentLobby == null)
                {
                    throw new LobbyException("No estás en ningún lobby.");
                }

                if (client.CurrentLobby.IsGameInProgress)
                {
                    throw new GameException("La partida ya comenzó.");
                }

                bool taken = client.CurrentLobby.Players
                    .Any(p => p.SelectedBoardId == boardId && p.UserId != currentUser.id_user);

                if (taken)
                {
                    throw new GameException("Ese tablero ya está ocupado.");
                }

                var cards = BoardConfigurations.GetBoardById(boardId);
                if (cards == null || cards.Count == 0)
                {
                    throw new ArgumentException("Tablero inválido.");
                }

                client.SelectedBoardId = boardId;
                client.WinningCards = new HashSet<int>(cards);

                BroadcastLobbyState(client);

                return Task.CompletedTask;

            }, "ChooseBoard");
        }

        public async Task<LobbyStateDto> CreateLobby(User currentUser)
        {
            if (currentUser == null)
            {
                throw new ArgumentNullException(nameof(currentUser));
            }

            return await ExecuteFaultSafeAsync(() =>
            {
                var host = GetClientOrThrow(currentUser);

                if (host.CurrentLobby != null)
                {
                    throw new UserAlreadyInLobbyException("Ya estás en un lobby.");
                }

                var lobbyState = _lobbyManager.CreateLobby(host);

                host.SelectedBoardId = 1;
                host.WinningCards = new HashSet<int>(BoardConfigurations.GetBoardById(1));

                var hostDto = lobbyState.Players.First(p => p.UserId == currentUser.id_user);
                hostDto.SelectedBoardId = 1;

                BroadcastLobbyState(host);

                return Task.FromResult(lobbyState);

            }, "CreateLobby");
        }

        public async Task<LobbyStateDto> JoinLobby(User currentUser, string lobbyCode)
        {
            if (currentUser == null)
            {
                throw new ArgumentNullException(nameof(currentUser));
            }

            return await ExecuteFaultSafeAsync(() =>
            {
                var client = GetClientOrThrow(currentUser);

                if (client.CurrentLobby != null)
                {
                    throw new UserAlreadyInLobbyException("Ya estás en un lobby.");
                }

                var lobbyState = _lobbyManager.JoinLobby(client, lobbyCode);

                var occupied = client.CurrentLobby.Players
                    .Where(p => p.UserId != currentUser.id_user)
                    .Select(p => p.SelectedBoardId)
                    .ToList();

                int board = 1;
                while (occupied.Contains(board))
                {
                    board++;
                }

                client.SelectedBoardId = board;
                client.WinningCards = new HashSet<int>(BoardConfigurations.GetBoardById(board));

                BroadcastLobbyState(client);

                foreach (var dto in lobbyState.Players)
                {
                    var internalClient = client.CurrentLobby.Players
                        .First(p => p.UserId == dto.UserId);

                    dto.SelectedBoardId = internalClient.SelectedBoardId;
                }
                lobbyState.ChatHistory = client.CurrentLobby.GetChatHistory();

                return Task.FromResult(lobbyState);

            }, "JoinLobby");
        }

        public async Task LeaveLobby(User currentUser)
        {
            if (currentUser == null)
            {
                throw new ArgumentNullException(nameof(currentUser));
            }

            await ExecuteFaultSafeAsync(() =>
            {
                var client = GetClientOrThrow(currentUser);
                _lobbyManager.LeaveLobby(client);

                return Task.CompletedTask;

            }, "LeaveLobby");
        }

        public async Task KickPlayer(User currentUser, int targetPlayerId)
        {
            if (currentUser == null)
            {
                throw new ArgumentNullException(nameof(currentUser));
            }

            await ExecuteFaultSafeAsync(() =>
            {
                var host = GetClientOrThrow(currentUser);
                _lobbyManager.KickPlayer(host, targetPlayerId);

                return Task.CompletedTask;

            }, "KickPlayer");
        }

        public async Task<LobbyStateDto> GetLobbyState(User currentUser, string lobbyCode)
        {
            if (currentUser == null)
            {
                throw new ArgumentNullException(nameof(currentUser));
            }

            return await ExecuteFaultSafeAsync(() =>
            {
                var client = GetClientOrThrow(currentUser);
                var lobby = client.CurrentLobby;

                if (lobby == null || lobby.LobbyCode != lobbyCode)
                {
                    throw new LobbyException("No perteneces a este lobby.");
                }

                var state = new LobbyStateDto
                {
                    LobbyCode = lobby.LobbyCode,
                    Players = lobby.GetPlayerDTOs(),
                    ChatHistory = lobby.GetChatHistory()
                };

                return Task.FromResult(state);

            }, "GetLobbyState");
        }

        private void BroadcastLobbyState(PlayerClient source)
        {
            var lobby = source.CurrentLobby;
            if (lobby == null)
            {
                return;
            }

            var dto = new LobbyStateDto
            {
                LobbyCode = lobby.LobbyCode,
                Players = lobby.Players.Select(p => new UserDto
                {
                    UserId = p.UserId,
                    Nickname = p.Nickname,
                    SelectedBoardId = p.SelectedBoardId,
                    IsHost = p.UserId == lobby.HostUserId
                }).ToList()
            };

            foreach (var player in lobby.Players)
            {
                player.CallbackChannel.LobbyStateUpdated(dto);
            }
        }

        private PlayerClient GetClientOrThrow(User user)
        {
            var client = _sessionManager.GetClient(user.id_user);
            if (client == null)
            {
                throw new UserNotConnectedException("Sesión no encontrada.");
            }

            return client;
        }
    }
}