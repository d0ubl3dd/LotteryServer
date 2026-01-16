using Xunit;
using Moq;
using System;
using System.Threading.Tasks;
using BusinessLogic.Handlers;
using BusinessLogic.Logic;
using BusinessLogic.Models;
using Contracts.Callbacks;
using DataAccess;
using DataAccess.DAOs;
using Tests.Builders;

namespace Tests.Handlers
{
    public class DisconnectionHandlerTests
    {
        private readonly Mock<ISessionManager> _mockSessionManager;
        private readonly Mock<ILobbyManager> _mockLobbyManager;
        private readonly Mock<ILotteryCallback> _mockCallback;
        private readonly Mock<IUserDao> _mockUserDao;
        private readonly DisconnectionHandler _handler;

        public DisconnectionHandlerTests()
        {
            _mockSessionManager = new Mock<ISessionManager>();
            _mockLobbyManager = new Mock<ILobbyManager>();
            _mockCallback = new Mock<ILotteryCallback>();
            _mockUserDao = new Mock<IUserDao>();

            _handler = new DisconnectionHandler(_mockSessionManager.Object, _mockLobbyManager.Object);
        }

        [Fact]
        public void Constructor_WhenSessionManagerIsNull_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new DisconnectionHandler(null, _mockLobbyManager.Object));
        }

        [Fact]
        public void Constructor_WhenLobbyManagerIsNull_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new DisconnectionHandler(_mockSessionManager.Object, null));
        }

        [Theory]
        [InlineData(1, "Connection Lost")]
        [InlineData(2, "Browser Closed")]
        [InlineData(3, "Manual Logout")]
        [InlineData(4, "Timeout")]
        [InlineData(5, "Server Error")]
        public async Task HandleDisconnection_WhenUserOnlineAndInLobby_ShouldLeaveLobbyAndUnregister(int userId, string reason)
        {
            var user = new UserBuilder().WithId(userId).Build();
            var client = new PlayerClient(userId, "User", 1, _mockCallback.Object);
            var lobby = new Lobby("LOBBY1", client, _mockUserDao.Object);
            client.CurrentLobby = lobby;

            _mockSessionManager.Setup(sm => sm.IsUserOnline(userId)).Returns(true);
            _mockSessionManager.Setup(sm => sm.GetClient(userId)).Returns(client);

            await _handler.HandleDisconnectionAsync(userId, reason);

            _mockLobbyManager.Verify(lm => lm.LeaveLobby(client), Times.Once);
            _mockSessionManager.Verify(sm => sm.UnregisterClient(userId), Times.Once);
        }

        [Theory]
        [InlineData(10, "Reason A")]
        [InlineData(11, "Reason B")]
        [InlineData(12, "Reason C")]
        [InlineData(13, "Reason D")]
        public async Task HandleDisconnection_WhenUserOnlineButNotInLobby_ShouldOnlyUnregister(int userId, string reason)
        {
            var client = new PlayerClient(userId, "User", 1, _mockCallback.Object);
            client.CurrentLobby = null;

            _mockSessionManager.Setup(sm => sm.IsUserOnline(userId)).Returns(true);
            _mockSessionManager.Setup(sm => sm.GetClient(userId)).Returns(client);

            await _handler.HandleDisconnectionAsync(userId, reason);

            _mockLobbyManager.Verify(lm => lm.LeaveLobby(It.IsAny<PlayerClient>()), Times.Never);
            _mockSessionManager.Verify(sm => sm.UnregisterClient(userId), Times.Once);
        }

        [Theory]
        [InlineData(20)]
        [InlineData(21)]
        [InlineData(22)]
        [InlineData(23)]
        [InlineData(24)]
        public async Task HandleDisconnection_WhenUserOfflineInSession_ShouldUseFallbackAndLeaveLobby(int userId)
        {
            _mockSessionManager.Setup(sm => sm.IsUserOnline(userId)).Returns(false);

            var dummyHost = new PlayerClient(999, "Host", 1, _mockCallback.Object);
            var lobby = new Lobby("GHOST", dummyHost, _mockUserDao.Object);

            _mockLobbyManager.Setup(lm => lm.FindLobbyByPlayerId(userId)).Returns(lobby);

            await _handler.HandleDisconnectionAsync(userId, "Ghost Mode");

            _mockLobbyManager.Verify(lm => lm.LeaveLobby(It.Is<PlayerClient>(p => p.UserId == userId && p.CurrentLobby == lobby)), Times.Once);
            _mockSessionManager.Verify(sm => sm.UnregisterClient(userId), Times.Once);
        }

        [Theory]
        [InlineData(30)]
        [InlineData(31)]
        [InlineData(32)]
        public async Task HandleDisconnection_WhenUserOfflineAndNoLobbyFound_ShouldOnlyUnregister(int userId)
        {
            _mockSessionManager.Setup(sm => sm.IsUserOnline(userId)).Returns(false);
            _mockLobbyManager.Setup(lm => lm.FindLobbyByPlayerId(userId)).Returns((Lobby)null);

            await _handler.HandleDisconnectionAsync(userId, "Clean Cleanup");

            _mockLobbyManager.Verify(lm => lm.LeaveLobby(It.IsAny<PlayerClient>()), Times.Never);
            _mockSessionManager.Verify(sm => sm.UnregisterClient(userId), Times.Once);
        }

        [Theory]
        [InlineData(40, typeof(InvalidOperationException))]
        [InlineData(41, typeof(NullReferenceException))]
        [InlineData(42, typeof(ArgumentException))]
        public async Task HandleDisconnection_WhenGetClientThrows_ShouldContinueToFallback(int userId, Type exceptionType)
        {
            var exception = (Exception)Activator.CreateInstance(exceptionType);
            _mockSessionManager.Setup(sm => sm.IsUserOnline(userId)).Throws(exception);

            _mockLobbyManager.Setup(lm => lm.FindLobbyByPlayerId(userId)).Returns((Lobby)null);

            await _handler.HandleDisconnectionAsync(userId, "Error Recovery");

            _mockSessionManager.Verify(sm => sm.UnregisterClient(userId), Times.Once);
        }

        [Theory]
        [InlineData(50, typeof(InvalidOperationException))]
        [InlineData(51, typeof(Exception))]
        public async Task HandleDisconnection_WhenLeaveLobbyThrows_ShouldContinueToUnregister(int userId, Type exceptionType)
        {
            var client = new PlayerClient(userId, "User", 1, _mockCallback.Object);
            var lobby = new Lobby("FAIL", client, _mockUserDao.Object);
            client.CurrentLobby = lobby;

            _mockSessionManager.Setup(sm => sm.IsUserOnline(userId)).Returns(true);
            _mockSessionManager.Setup(sm => sm.GetClient(userId)).Returns(client);

            var exception = (Exception)Activator.CreateInstance(exceptionType);
            _mockLobbyManager.Setup(lm => lm.LeaveLobby(client)).Throws(exception);

            await _handler.HandleDisconnectionAsync(userId, "Leave Fail");

            _mockSessionManager.Verify(sm => sm.UnregisterClient(userId), Times.Once);
        }

        [Theory]
        [InlineData(60)]
        [InlineData(61)]
        public async Task HandleDisconnection_WhenUnregisterThrows_ShouldNotPropagateException(int userId)
        {
            _mockSessionManager.Setup(sm => sm.IsUserOnline(userId)).Returns(false);
            _mockLobbyManager.Setup(lm => lm.FindLobbyByPlayerId(userId)).Returns((Lobby)null);

            _mockSessionManager.Setup(sm => sm.UnregisterClient(userId)).Throws(new Exception("Final cleanup error"));

            var exception = await Record.ExceptionAsync(() => _handler.HandleDisconnectionAsync(userId, "Final Error"));

            Assert.Null(exception);
        }
    }
}