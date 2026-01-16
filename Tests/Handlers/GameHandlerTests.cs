using Xunit;
using Moq;
using System;
using System.Threading.Tasks;
using System.ServiceModel;
using BusinessLogic.Handlers;
using BusinessLogic.Logic;
using BusinessLogic.Models;
using BusinessLogic.Exceptions;
using Contracts.Faults;
using DataAccess;
using DataAccess.DAOs;
using Contracts.DTOs;
using Contracts.Callbacks;
using Tests.Builders;
using System.Collections.Generic;
using System.Reflection;

namespace Tests.Handlers
{
    public class GameHandlerTests
    {
        private readonly Mock<ILobbyManager> _mockLobbyManager;
        private readonly Mock<IUserDao> _mockUserDao;
        private readonly Mock<ILotteryCallback> _mockCallback;
        private readonly GameHandler _handler;

        public GameHandlerTests()
        {
            _mockLobbyManager = new Mock<ILobbyManager>();
            _mockUserDao = new Mock<IUserDao>();
            _mockCallback = new Mock<ILotteryCallback>();
            _handler = new GameHandler(_mockLobbyManager.Object);
        }

        [Fact]
        public void Constructor_WhenLobbyManagerIsNull_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new GameHandler(null));
        }

        [Fact]
        public async Task StartGame_WhenUserIsNull_ShouldThrowArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.StartGame(null, new GameSettingsDto()));
        }

        [Fact]
        public async Task StartGame_WhenSettingsAreNull_ShouldThrowArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.StartGame(new UserBuilder().Build(), null));
        }

        [Fact]
        public async Task StartGame_WhenLobbyNotFound_ShouldThrowLobbyNotFound()
        {
            var user = new UserBuilder().WithId(1).Build();
            _mockLobbyManager.Setup(lm => lm.FindLobbyByHostId(1)).Returns((Lobby)null);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.StartGame(user, new GameSettingsDto()));

            Assert.Equal("LOBBY_NOT_FOUND", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task StartGame_WhenNotEnoughPlayers_ShouldThrowNotEnoughPlayers()
        {
            var user = new UserBuilder().WithId(1).Build();
            var hostClient = new PlayerClient(1, "Host", 1, _mockCallback.Object);
            var lobby = new Lobby("CODE", hostClient, _mockUserDao.Object);

            _mockLobbyManager.Setup(lm => lm.FindLobbyByHostId(1)).Returns(lobby);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.StartGame(user, new GameSettingsDto()));

            Assert.Equal("GAME_NOT_ENOUGH_PLAYERS", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task StartGame_WhenValid_ShouldStartGame()
        {
            var user = new UserBuilder().WithId(1).Build();
            var hostClient = new PlayerClient(1, "Host", 1, _mockCallback.Object);
            var lobby = new Lobby("CODE", hostClient, _mockUserDao.Object);

            lobby.AddPlayer(new PlayerClient(2, "P2", 1, _mockCallback.Object));

            _mockLobbyManager.Setup(lm => lm.FindLobbyByHostId(1)).Returns(lobby);

            await _handler.StartGame(user, new GameSettingsDto());

            Assert.True(lobby.IsGameInProgress);
        }

        [Fact]
        public async Task UpdateGameSettings_WhenHostNull_ShouldThrowArgumentNull()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.UpdateGameSettings(null, new GameSettingsDto()));
        }

        [Fact]
        public async Task UpdateGameSettings_WhenLobbyNotFound_ShouldThrowLobbyNotFound()
        {
            var user = new UserBuilder().WithId(1).Build();
            _mockLobbyManager.Setup(lm => lm.FindLobbyByHostId(1)).Returns((Lobby)null);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.UpdateGameSettings(user, new GameSettingsDto()));

            Assert.Equal("LOBBY_NOT_FOUND", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task UpdateGameSettings_WhenValid_ShouldSucceed()
        {
            var user = new UserBuilder().WithId(1).Build();
            var hostClient = new PlayerClient(1, "Host", 1, _mockCallback.Object);
            var lobby = new Lobby("CODE", hostClient, _mockUserDao.Object);

            _mockLobbyManager.Setup(lm => lm.FindLobbyByHostId(1)).Returns(lobby);

            await _handler.UpdateGameSettings(user, new GameSettingsDto());
        }

        [Fact]
        public async Task GetScoreboard_WhenLobbyNotFound_ShouldReturnEmptyArray()
        {
            var user = new UserBuilder().WithId(1).Build();
            _mockLobbyManager.Setup(lm => lm.FindLobbyByPlayerId(1)).Returns((Lobby)null);

            var result = await _handler.GetScoreboard(user);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetScoreboard_WhenLobbyExists_ShouldReturnCards()
        {
            var user = new UserBuilder().WithId(1).Build();
            var hostClient = new PlayerClient(1, "Host", 1, _mockCallback.Object);
            var lobby = new Lobby("CODE", hostClient, _mockUserDao.Object);

            _mockLobbyManager.Setup(lm => lm.FindLobbyByPlayerId(1)).Returns(lobby);

            var result = await _handler.GetScoreboard(user);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task DeclareWin_WhenLobbyNotFound_ShouldThrowLobbyNotFound()
        {
            var dto = new PlayerBoardDto { PlayerId = 1 };
            _mockLobbyManager.Setup(lm => lm.FindLobbyByPlayerId(1)).Returns((Lobby)null);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.DeclareWin(dto));

            Assert.Equal("LOBBY_NOT_FOUND", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task ValidateFalseLoteria_WhenLobbyNotFound_ShouldThrowLobbyNotFound()
        {
            _mockLobbyManager.Setup(lm => lm.FindLobbyByPlayerId(1)).Returns((Lobby)null);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.ValidateFalseLoteriaAsync(1));

            Assert.Equal("LOBBY_NOT_FOUND", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task ConfirmGameEnd_WhenLobbyNotFound_ShouldLogAndReturn()
        {
            var user = new UserBuilder().WithId(1).Build();
            _mockLobbyManager.Setup(lm => lm.FindLobbyByPlayerId(1)).Returns((Lobby)null);

            await _handler.ConfirmGameEnd(user, 2);
        }

        private void SetGameInProgress(Lobby lobby, bool value)
        {
            var prop = typeof(Lobby).GetProperty("IsGameInProgress");
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(lobby, value);
            }
            else
            {
                var field = typeof(Lobby).GetField("<IsGameInProgress>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null) field.SetValue(lobby, value);
            }
        }
    }
}