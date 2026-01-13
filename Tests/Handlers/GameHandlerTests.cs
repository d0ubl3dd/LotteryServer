using Xunit;
using Moq;
using System;
using System.Threading.Tasks;
using System.ServiceModel;
using BusinessLogic.Handlers;
using BusinessLogic.Logic;
using BusinessLogic.Models;
using Contracts.DTOs;
using DataAccess;
using DataAccess.DAOs;
using Contracts.Faults;
using Tests.Builders;
using Contracts.Callbacks;
using System.Reflection;

namespace Tests.Handlers
{
    public class GameHandlerTests
    {
        private readonly Mock<ILobbyManager> _mockLobbyManager;
        private readonly GameHandler _handler;
        private readonly Mock<ILotteryCallback> _mockCallback;
        private readonly Mock<IUserDao> _mockUserDao;

        public GameHandlerTests()
        {
            _mockLobbyManager = new Mock<ILobbyManager>();
            _mockCallback = new Mock<ILotteryCallback>();
            _mockUserDao = new Mock<IUserDao>();
            _handler = new GameHandler(_mockLobbyManager.Object);
        }

        private void ForceSetGameInProgress(Lobby lobby, bool value)
        {
            var property = typeof(Lobby).GetProperty("IsGameInProgress",
                BindingFlags.Public | BindingFlags.Instance);

            if (property != null && property.CanWrite)
            {
                property.SetValue(lobby, value);
            }
            else
            {
                var field = typeof(Lobby).GetField("<IsGameInProgress>k__BackingField",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                if (field != null)
                {
                    field.SetValue(lobby, value);
                }
            }
        }

        [Fact]
        public async Task StartGame_WhenConditionsMet_ShouldStartLobbyGame()
        {
            var host = new UserBuilder().WithId(1).WithNickname("HostUser").Build();
            var settings = new GameSettingsDto();

            var hostClient = new PlayerClient(host.id_user, host.nickname, host.id_avatar, _mockCallback.Object);

            var mockLobby = new Mock<Lobby>("CODE1", hostClient, _mockUserDao.Object);

            mockLobby.Setup(l => l.StartLobbyGame(settings));
            mockLobby.Object.Players.Add(new PlayerClient(2, "P2", 1, _mockCallback.Object));

            _mockLobbyManager.Setup(m => m.FindLobbyByHostId(host.id_user))
                             .Returns(mockLobby.Object);

            await _handler.StartGame(host, settings);

            mockLobby.Verify(l => l.StartLobbyGame(settings), Times.Once);
        }

        [Fact]
        public async Task StartGame_WhenLobbyNotFound_ShouldThrowFault_LobbyNotFound()
        {
            var host = new UserBuilder().WithId(1).Build();
            _mockLobbyManager.Setup(m => m.FindLobbyByHostId(1)).Returns((Lobby)null);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.StartGame(host, new GameSettingsDto()));

            Assert.Equal("LOBBY_NOT_FOUND", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task StartGame_WhenGameAlreadyRunning_ShouldThrowFault_GameAlreadyActive()
        {
            var host = new UserBuilder().WithId(1).Build();
            var client = new PlayerClient(host.id_user, host.nickname, host.id_avatar, _mockCallback.Object);

            var realLobby = new Lobby("CODE", client, _mockUserDao.Object);
            ForceSetGameInProgress(realLobby, true);

            _mockLobbyManager.Setup(m => m.FindLobbyByHostId(1)).Returns(realLobby);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.StartGame(host, new GameSettingsDto()));

            Assert.Equal("GAME_ALREADY_ACTIVE", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task StartGame_WhenArgumentsNull_ShouldThrowFault_BadRequest()
        {
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.StartGame(null, new GameSettingsDto()));

            Assert.Equal("GLOBAL_BAD_REQUEST", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task UpdateSettings_WhenGameInProgress_ShouldThrowFault_GameAlreadyActive()
        {
            var host = new UserBuilder().WithId(1).Build();
            var client = new PlayerClient(host.id_user, host.nickname, host.id_avatar, _mockCallback.Object);

            var realLobby = new Lobby("CODE", client, _mockUserDao.Object);
            ForceSetGameInProgress(realLobby, true);

            _mockLobbyManager.Setup(m => m.FindLobbyByHostId(1)).Returns(realLobby);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.UpdateGameSettings(host, new GameSettingsDto()));

            Assert.Equal("GAME_ALREADY_ACTIVE", ex.Detail.ErrorCode);
        }
    }
}