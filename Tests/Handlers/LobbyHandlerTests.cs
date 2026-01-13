using BusinessLogic.Exceptions;
using BusinessLogic.Logic;
using BusinessLogic.Models;
using Contracts.Callbacks;
using Contracts.DTOs;
using Contracts.Faults;
using DataAccess.DAOs;
using Moq;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.ServiceModel;
using System.Threading.Tasks;
using Tests.Builders;
using Xunit;

namespace Tests.Handlers
{
    public class LobbyHandlerTests
    {
        private readonly Mock<ILobbyManager> _mockLobbyManager;
        private readonly Mock<ISessionManager> _mockSessionManager;
        private readonly Mock<ILotteryCallback> _mockCallback;
        private readonly Mock<IUserDao> _mockUserDao;
        private readonly LobbyHandler _handler;

        public LobbyHandlerTests()
        {
            _mockLobbyManager = new Mock<ILobbyManager>();
            _mockSessionManager = new Mock<ISessionManager>();
            _mockCallback = new Mock<ILotteryCallback>();
            _mockUserDao = new Mock<IUserDao>();

            _handler = new LobbyHandler(_mockLobbyManager.Object, _mockSessionManager.Object);
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
        public async Task CreateLobby_WhenUserIsValidAndNotInLobby_ShouldReturnLobbyState()
        {
            var user = new UserBuilder().WithId(1).Build();
            var client = new PlayerClient(user.id_user, user.nickname, user.id_avatar, _mockCallback.Object);
            client.CurrentLobby = null;

            var expectedDto = new LobbyStateDto
            {
                LobbyCode = "ABC1234",
                Players = new List<UserDto> { new UserDto { UserId = user.id_user } }
            };

            _mockSessionManager.Setup(sm => sm.GetClient(user.id_user)).Returns(client);
            _mockLobbyManager.Setup(lm => lm.CreateLobby(client)).Returns(expectedDto);

            var result = await _handler.CreateLobby(user);

            Assert.NotNull(result);
            Assert.Equal("ABC1234", result.LobbyCode);
            Assert.Equal(1, client.SelectedBoardId);
            _mockLobbyManager.Verify(lm => lm.CreateLobby(client), Times.Once);
        }

        [Fact]
        public async Task CreateLobby_WhenUserAlreadyInLobby_ShouldThrowFault_AlreadyInLobby()
        {
            var user = new UserBuilder().Build();
            var client = new PlayerClient(user.id_user, user.nickname, user.id_avatar, _mockCallback.Object);

            var existingLobby = new Lobby("EXIST", client, _mockUserDao.Object);
            client.CurrentLobby = existingLobby;

            _mockSessionManager.Setup(sm => sm.GetClient(user.id_user)).Returns(client);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.CreateLobby(user));

            Assert.Equal("LOBBY_USER_ALREADY_IN", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task CreateLobby_WhenSessionNotFound_ShouldThrowFault_UserOffline()
        {
            var user = new UserBuilder().Build();
            _mockSessionManager.Setup(sm => sm.GetClient(user.id_user)).Returns((PlayerClient)null);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.CreateLobby(user));

            Assert.Equal("USER_OFFLINE", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task JoinLobby_WhenCodeIsValid_ShouldReturnLobbyState()
        {
            var user = new UserBuilder().WithId(2).Build();
            var client = new PlayerClient(user.id_user, user.nickname, user.id_avatar, _mockCallback.Object);
            client.CurrentLobby = null;

            string code = "CODE12";

            var hostClient = new PlayerClient(1, "Host", 1, _mockCallback.Object);
            var lobby = new Lobby(code, hostClient, _mockUserDao.Object);

            var expectedDto = new LobbyStateDto
            {
                LobbyCode = code,
                Players = new List<UserDto>
                {
                    new UserDto { UserId = 1 },
                    new UserDto { UserId = user.id_user }
                }
            };

            _mockSessionManager.Setup(sm => sm.GetClient(user.id_user)).Returns(client);

            _mockLobbyManager.Setup(lm => lm.JoinLobby(client, code))
                .Callback(() =>
                {
                    lobby.AddPlayer(client);
                })
                .Returns(expectedDto);

            var result = await _handler.JoinLobby(user, code);

            Assert.Equal(code, result.LobbyCode);
            Assert.NotEqual(0, client.SelectedBoardId);
        }

        [Fact]
        public async Task JoinLobby_WhenCodeIsEmpty_ShouldThrowFault_BadRequest()
        {
            var user = new UserBuilder().Build();
            var client = new PlayerClient(user.id_user, user.nickname, user.id_avatar, _mockCallback.Object);

            _mockSessionManager.Setup(sm => sm.GetClient(user.id_user)).Returns(client);

            _mockLobbyManager.Setup(lm => lm.JoinLobby(client, ""))
                             .Throws(new ArgumentException("El código no puede ser vacío"));

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.JoinLobby(user, ""));

            Assert.Equal("GLOBAL_BAD_REQUEST", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task KickPlayer_WhenHostIsInLobby_ShouldCallManagerKick()
        {
            var hostUser = new UserBuilder().WithId(1).Build();
            var hostClient = new PlayerClient(hostUser.id_user, hostUser.nickname, hostUser.id_avatar, _mockCallback.Object);

            var realLobby = new Lobby("MYLOBBY", hostClient, _mockUserDao.Object);
            hostClient.CurrentLobby = realLobby;

            _mockSessionManager.Setup(sm => sm.GetClient(hostUser.id_user)).Returns(hostClient);

            await _handler.KickPlayer(hostUser, 50);

            _mockLobbyManager.Verify(lm => lm.KickPlayer(hostClient, 50), Times.Once);
        }

        [Fact]
        public async Task KickPlayer_WhenHostNotInLobby_ShouldThrowFault_LobbyError()
        {
            var user = new UserBuilder().Build();
            var client = new PlayerClient(user.id_user, user.nickname, user.id_avatar, _mockCallback.Object);
            client.CurrentLobby = null;

            _mockSessionManager.Setup(sm => sm.GetClient(user.id_user)).Returns(client);

            _mockLobbyManager.Setup(lm => lm.KickPlayer(client, 50))
                             .Throws(new LobbyException("No estás en un lobby."));

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.KickPlayer(user, 50));

            Assert.Equal("LOBBY_ERROR", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task ChooseBoard_WhenBoardIsValid_ShouldAssignToClient()
        {
            var user = new UserBuilder().WithId(10).Build();
            var client = new PlayerClient(user.id_user, user.nickname, user.id_avatar, _mockCallback.Object);

            var realLobby = new Lobby("LOBBY1", client, _mockUserDao.Object);
            client.CurrentLobby = realLobby;

            _mockSessionManager.Setup(sm => sm.GetClient(user.id_user)).Returns(client);

            await _handler.ChooseBoard(user, 1);

            Assert.Equal(1, client.SelectedBoardId);
            Assert.NotNull(client.WinningCards);
            Assert.NotEmpty(client.WinningCards);
        }

        [Fact]
        public async Task ChooseBoard_WhenNotInLobby_ShouldThrowFault_LobbyError()
        {
            var user = new UserBuilder().Build();
            var client = new PlayerClient(user.id_user, user.nickname, user.id_avatar, _mockCallback.Object);
            client.CurrentLobby = null;

            _mockSessionManager.Setup(sm => sm.GetClient(user.id_user)).Returns(client);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.ChooseBoard(user, 1));

            Assert.Equal("LOBBY_ERROR", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task ChooseBoard_WhenGameInProgress_ShouldThrowFault_GameError()
        {
            var user = new UserBuilder().Build();
            var client = new PlayerClient(user.id_user, user.nickname, user.id_avatar, _mockCallback.Object);

            var realLobby = new Lobby("LOBBY1", client, _mockUserDao.Object);

            ForceSetGameInProgress(realLobby, true);

            client.CurrentLobby = realLobby;

            _mockSessionManager.Setup(sm => sm.GetClient(user.id_user)).Returns(client);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.ChooseBoard(user, 1));

            Assert.Equal("GAME_ERROR", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task ChooseBoard_WhenBoardIdInvalid_ShouldThrowFault_BadRequest()
        {
            var user = new UserBuilder().Build();
            var client = new PlayerClient(user.id_user, user.nickname, user.id_avatar, _mockCallback.Object);

            var realLobby = new Lobby("LOBBY1", client, _mockUserDao.Object);

            client.CurrentLobby = realLobby;

            _mockSessionManager.Setup(sm => sm.GetClient(user.id_user)).Returns(client);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.ChooseBoard(user, -99));

            Assert.Equal("GLOBAL_BAD_REQUEST", ex.Detail.ErrorCode);
        }
    }
}