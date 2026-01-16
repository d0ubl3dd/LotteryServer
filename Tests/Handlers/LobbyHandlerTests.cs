using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ServiceModel;
using BusinessLogic.Logic;
using BusinessLogic.Models;
using BusinessLogic.Exceptions;
using Contracts.Faults;
using DataAccess;
using DataAccess.DAOs;
using Contracts.DTOs;
using Contracts.Callbacks;
using Tests.Builders;
using System.Linq;
using System.Reflection;

namespace Tests.Handlers
{
    public class LobbyHandlerTests
    {
        private readonly Mock<ILobbyManager> _mockLobbyManager;
        private readonly Mock<ISessionManager> _mockSessionManager;
        private readonly Mock<IUserDao> _mockUserDao;
        private readonly Mock<ILotteryCallback> _mockCallback;
        private readonly LobbyHandler _handler;

        public LobbyHandlerTests()
        {
            _mockLobbyManager = new Mock<ILobbyManager>();
            _mockSessionManager = new Mock<ISessionManager>();
            _mockUserDao = new Mock<IUserDao>();
            _mockCallback = new Mock<ILotteryCallback>();

            _handler = new LobbyHandler(_mockLobbyManager.Object, _mockSessionManager.Object);
        }

        [Fact]
        public void Constructor_WhenLobbyManagerIsNull_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new LobbyHandler(null, _mockSessionManager.Object));
        }

        [Fact]
        public void Constructor_WhenSessionManagerIsNull_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new LobbyHandler(_mockLobbyManager.Object, null));
        }

        [Fact]
        public async Task ChooseBoard_WhenUserIsNull_ShouldThrowArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.ChooseBoard(null, 1));
        }

        [Fact]
        public async Task ChooseBoard_WhenUserNotConnected_ShouldThrowUserNotConnected()
        {
            var user = new UserBuilder().WithId(1).Build();
            _mockSessionManager.Setup(sm => sm.GetClient(1)).Returns((PlayerClient)null);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.ChooseBoard(user, 1));

            Assert.Equal("USER_OFFLINE", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task ChooseBoard_WhenUserNotInLobby_ShouldThrowLobbyException()
        {
            var user = new UserBuilder().WithId(1).Build();
            var client = new PlayerClient(1, "User", 1, _mockCallback.Object);
            client.CurrentLobby = null;

            _mockSessionManager.Setup(sm => sm.GetClient(1)).Returns(client);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.ChooseBoard(user, 1));

            Assert.Equal("LOBBY_ERROR", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task ChooseBoard_WhenGameInProgress_ShouldThrowGameException()
        {
            var user = new UserBuilder().WithId(1).Build();
            var client = new PlayerClient(1, "User", 1, _mockCallback.Object);
            var lobby = new Lobby("CODE", client, _mockUserDao.Object);
            SetGameInProgress(lobby, true);
            client.CurrentLobby = lobby;

            _mockSessionManager.Setup(sm => sm.GetClient(1)).Returns(client);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.ChooseBoard(user, 1));

            Assert.Equal("GAME_ERROR", ex.Detail.ErrorCode);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public async Task ChooseBoard_WhenBoardTakenByOther_ShouldThrowGameException(int takenBoardId)
        {
            var user = new UserBuilder().WithId(1).Build();
            var client = new PlayerClient(1, "User", 1, _mockCallback.Object);
            var otherClient = new PlayerClient(2, "Other", 1, _mockCallback.Object);
            otherClient.SelectedBoardId = takenBoardId;

            var lobby = new Lobby("CODE", client, _mockUserDao.Object);
            lobby.AddPlayer(otherClient);
            client.CurrentLobby = lobby;

            _mockSessionManager.Setup(sm => sm.GetClient(1)).Returns(client);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.ChooseBoard(user, takenBoardId));

            Assert.Equal("GAME_ERROR", ex.Detail.ErrorCode);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(9999)]
        public async Task ChooseBoard_WhenBoardInvalid_ShouldThrowArgumentException(int invalidId)
        {
            var user = new UserBuilder().WithId(1).Build();
            var client = new PlayerClient(1, "User", 1, _mockCallback.Object);
            var lobby = new Lobby("CODE", client, _mockUserDao.Object);
            client.CurrentLobby = lobby;

            _mockSessionManager.Setup(sm => sm.GetClient(1)).Returns(client);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.ChooseBoard(user, invalidId));

            Assert.Equal("GLOBAL_BAD_REQUEST", ex.Detail.ErrorCode);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public async Task ChooseBoard_WhenValid_ShouldUpdateClientAndBroadcast(int boardId)
        {
            var user = new UserBuilder().WithId(1).Build();
            var client = new PlayerClient(1, "User", 1, _mockCallback.Object);
            var lobby = new Lobby("CODE", client, _mockUserDao.Object);
            client.CurrentLobby = lobby;

            _mockSessionManager.Setup(sm => sm.GetClient(1)).Returns(client);

            await _handler.ChooseBoard(user, boardId);

            Assert.Equal(boardId, client.SelectedBoardId);
            Assert.NotEmpty(client.WinningCards);
            _mockCallback.Verify(cb => cb.LobbyStateUpdated(It.IsAny<LobbyStateDto>()), Times.Once);
        }

        [Fact]
        public async Task CreateLobby_WhenUserIsNull_ShouldThrowArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.CreateLobby(null));
        }

        [Fact]
        public async Task CreateLobby_WhenUserAlreadyInLobby_ShouldThrowUserAlreadyInLobby()
        {
            var user = new UserBuilder().WithId(1).Build();
            var client = new PlayerClient(1, "User", 1, _mockCallback.Object);
            client.CurrentLobby = new Lobby("EXIST", client, _mockUserDao.Object);

            _mockSessionManager.Setup(sm => sm.GetClient(1)).Returns(client);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.CreateLobby(user));

            Assert.Equal("LOBBY_USER_ALREADY_IN", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task CreateLobby_WhenValid_ShouldReturnState()
        {
            var user = new UserBuilder().WithId(1).Build();
            var client = new PlayerClient(1, "User", 1, _mockCallback.Object);
            client.CurrentLobby = null;

            _mockSessionManager.Setup(sm => sm.GetClient(1)).Returns(client);

            var lobbyDto = new LobbyStateDto
            {
                LobbyCode = "NEW123",
                Players = new List<UserDto> { new UserDto { UserId = 1 } }
            };

            _mockLobbyManager.Setup(lm => lm.CreateLobby(client)).Returns(lobbyDto)
                .Callback(() =>
                {
                    client.CurrentLobby = new Lobby("NEW123", client, _mockUserDao.Object);
                });

            var result = await _handler.CreateLobby(user);

            Assert.Equal("NEW123", result.LobbyCode);
            Assert.Equal(1, client.SelectedBoardId);
            _mockCallback.Verify(cb => cb.LobbyStateUpdated(It.IsAny<LobbyStateDto>()), Times.Once);
        }

        [Fact]
        public async Task JoinLobby_WhenUserIsNull_ShouldThrowArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.JoinLobby(null, "CODE"));
        }

        [Fact]
        public async Task JoinLobby_WhenUserAlreadyInLobby_ShouldThrowUserAlreadyInLobby()
        {
            var user = new UserBuilder().WithId(1).Build();
            var client = new PlayerClient(1, "User", 1, _mockCallback.Object);
            client.CurrentLobby = new Lobby("EXIST", client, _mockUserDao.Object);

            _mockSessionManager.Setup(sm => sm.GetClient(1)).Returns(client);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.JoinLobby(user, "CODE"));

            Assert.Equal("LOBBY_USER_ALREADY_IN", ex.Detail.ErrorCode);
        }

        [Theory]
        [InlineData("CODE1")]
        [InlineData("CODE2")]
        public async Task JoinLobby_WhenValid_ShouldAssignAvailableBoard(string code)
        {
            var user = new UserBuilder().WithId(2).Build();
            var client = new PlayerClient(2, "Joiner", 1, _mockCallback.Object);
            client.CurrentLobby = null;

            var host = new PlayerClient(1, "Host", 1, _mockCallback.Object);
            host.SelectedBoardId = 1;
            var lobby = new Lobby(code, host, _mockUserDao.Object);

            _mockSessionManager.Setup(sm => sm.GetClient(2)).Returns(client);

            var lobbyDto = new LobbyStateDto
            {
                LobbyCode = code,
                Players = new List<UserDto>
                {
                    new UserDto { UserId = 1, SelectedBoardId = 1 },
                    new UserDto { UserId = 2 }
                }
            };

            _mockLobbyManager.Setup(lm => lm.JoinLobby(client, code)).Returns(lobbyDto)
                .Callback(() =>
                {
                    lobby.AddPlayer(client);
                });

            var result = await _handler.JoinLobby(user, code);

            Assert.Equal(code, result.LobbyCode);
            Assert.Equal(2, client.SelectedBoardId);
            _mockCallback.Verify(cb => cb.LobbyStateUpdated(It.IsAny<LobbyStateDto>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task LeaveLobby_WhenUserIsNull_ShouldThrowArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.LeaveLobby(null));
        }

        [Fact]
        public async Task LeaveLobby_WhenValid_ShouldCallManager()
        {
            var user = new UserBuilder().WithId(1).Build();
            var client = new PlayerClient(1, "User", 1, _mockCallback.Object);

            _mockSessionManager.Setup(sm => sm.GetClient(1)).Returns(client);

            await _handler.LeaveLobby(user);

            _mockLobbyManager.Verify(lm => lm.LeaveLobby(client), Times.Once);
        }

        [Fact]
        public async Task KickPlayer_WhenUserIsNull_ShouldThrowArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.KickPlayer(null, 2));
        }

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        public async Task KickPlayer_WhenValid_ShouldCallManager(int targetId)
        {
            var user = new UserBuilder().WithId(1).Build();
            var client = new PlayerClient(1, "Host", 1, _mockCallback.Object);

            _mockSessionManager.Setup(sm => sm.GetClient(1)).Returns(client);

            await _handler.KickPlayer(user, targetId);

            _mockLobbyManager.Verify(lm => lm.KickPlayer(client, targetId), Times.Once);
        }

        [Fact]
        public async Task GetLobbyState_WhenUserIsNull_ShouldThrowArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.GetLobbyState(null, "CODE"));
        }

        [Fact]
        public async Task GetLobbyState_WhenNotInLobby_ShouldThrowLobbyException()
        {
            var user = new UserBuilder().WithId(1).Build();
            var client = new PlayerClient(1, "User", 1, _mockCallback.Object);
            client.CurrentLobby = null;

            _mockSessionManager.Setup(sm => sm.GetClient(1)).Returns(client);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.GetLobbyState(user, "CODE"));

            Assert.Equal("LOBBY_ERROR", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task GetLobbyState_WhenInDifferentLobby_ShouldThrowLobbyException()
        {
            var user = new UserBuilder().WithId(1).Build();
            var client = new PlayerClient(1, "User", 1, _mockCallback.Object);
            client.CurrentLobby = new Lobby("REALCODE", client, _mockUserDao.Object);

            _mockSessionManager.Setup(sm => sm.GetClient(1)).Returns(client);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.GetLobbyState(user, "FAKECODE"));

            Assert.Equal("LOBBY_ERROR", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task GetLobbyState_WhenValid_ShouldReturnDto()
        {
            var user = new UserBuilder().WithId(1).Build();
            var client = new PlayerClient(1, "User", 1, _mockCallback.Object);
            var lobby = new Lobby("CODE", client, _mockUserDao.Object);
            client.CurrentLobby = lobby;

            _mockSessionManager.Setup(sm => sm.GetClient(1)).Returns(client);

            var result = await _handler.GetLobbyState(user, "CODE");

            Assert.Equal("CODE", result.LobbyCode);
            Assert.Single(result.Players);
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