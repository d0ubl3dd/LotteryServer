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
using Contracts.Callbacks;
using DataAccess;
using DataAccess.DAOs;
using Tests.Builders;

namespace Tests.Handlers
{
    public class ChatHandlerTests
    {
        private readonly Mock<ISessionManager> _mockSessionManager;
        private readonly Mock<ILobbyManager> _mockLobbyManager;
        private readonly Mock<IUserDao> _mockUserDao;
        private readonly Mock<ILotteryCallback> _mockCallback;
        private readonly ChatHandler _handler;

        public ChatHandlerTests()
        {
            _mockSessionManager = new Mock<ISessionManager>();
            _mockLobbyManager = new Mock<ILobbyManager>();
            _mockUserDao = new Mock<IUserDao>();
            _mockCallback = new Mock<ILotteryCallback>();

            _handler = new ChatHandler(_mockSessionManager.Object, _mockLobbyManager.Object);
        }

        [Fact]
        public void Constructor_WhenSessionManagerIsNull_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ChatHandler(null, _mockLobbyManager.Object));
        }

        [Fact]
        public void Constructor_WhenLobbyManagerIsNull_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ChatHandler(_mockSessionManager.Object, null));
        }

        [Fact]
        public async Task SendMessage_WhenUserIsNull_ShouldThrowArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.SendMessage(null, "hello"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        [InlineData("\t")]
        [InlineData("\n")]
        [InlineData("\r\n")]
        public async Task SendMessage_WhenMessageIsWhitespace_ShouldReturnEarly(string emptyMessage)
        {
            var user = new UserBuilder().Build();

            await _handler.SendMessage(user, emptyMessage);

            _mockSessionManager.Verify(sm => sm.GetClient(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task SendMessage_WhenUserNotOnline_ShouldThrowFault_UserOffline()
        {
            var user = new UserBuilder().WithId(10).Build();
            _mockSessionManager.Setup(sm => sm.GetClient(10)).Returns((PlayerClient)null);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.SendMessage(user, "Hello"));

            Assert.Equal("USER_OFFLINE", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task SendMessage_WhenUserNotInLobby_ShouldThrowFault_UserNotInLobby()
        {
            var user = new UserBuilder().WithId(10).Build();
            var client = new PlayerClient(user.id_user, user.nickname, user.id_avatar, _mockCallback.Object);
            client.CurrentLobby = null;

            _mockSessionManager.Setup(sm => sm.GetClient(10)).Returns(client);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.SendMessage(user, "Hello"));

            Assert.Equal("CHAT_USER_NOT_IN_LOBBY", ex.Detail.ErrorCode);
        }

        [Theory]
        [InlineData("Hello")]
        [InlineData("Testing")]
        [InlineData("12345")]
        [InlineData("Special!#$")]
        public async Task SendMessage_WhenValid_ShouldBroadcastToLobby(string message)
        {
            var user = new UserBuilder().WithId(10).WithNickname("Sender").Build();
            var client = new PlayerClient(user.id_user, user.nickname, user.id_avatar, _mockCallback.Object);

            var mockLobby = new Mock<Lobby>("CODE", client, _mockUserDao.Object);
            client.CurrentLobby = mockLobby.Object;

            _mockSessionManager.Setup(sm => sm.GetClient(10)).Returns(client);

            await _handler.SendMessage(user, message);

            mockLobby.Verify(l => l.BroadcastChatMessage("Sender", message), Times.Once);
        }

        [Fact]
        public async Task SendMessage_WhenForbiddenWord_ShouldKickPlayer()
        {
            var user = new UserBuilder().WithId(10).Build();
            var client = new PlayerClient(user.id_user, user.nickname, user.id_avatar, _mockCallback.Object);

            var mockLobby = new Mock<Lobby>("CODE", client, _mockUserDao.Object);
            client.CurrentLobby = mockLobby.Object;

            _mockSessionManager.Setup(sm => sm.GetClient(10)).Returns(client);

            mockLobby.Setup(l => l.BroadcastChatMessage(It.IsAny<string>(), It.IsAny<string>()))
                     .Throws(new ForbiddenWordException("Bad word"));

            await _handler.SendMessage(user, "badword");

            _mockLobbyManager.Verify(lm => lm.KickPlayer(client, client.UserId), Times.Once);
        }

        [Fact]
        public async Task SendMessage_WhenSpamDetected_ShouldKickPlayer()
        {
            var user = new UserBuilder().WithId(10).Build();
            var client = new PlayerClient(user.id_user, user.nickname, user.id_avatar, _mockCallback.Object);

            var mockLobby = new Mock<Lobby>("CODE", client, _mockUserDao.Object);
            client.CurrentLobby = mockLobby.Object;

            _mockSessionManager.Setup(sm => sm.GetClient(10)).Returns(client);

            mockLobby.Setup(l => l.BroadcastChatMessage(It.IsAny<string>(), It.IsAny<string>()))
                     .Throws(new ChatException("Spam detected"));

            await _handler.SendMessage(user, "Spam");

            _mockLobbyManager.Verify(lm => lm.KickPlayer(client, client.UserId), Times.Once);
        }

        [Fact]
        public async Task SendMessage_WhenGenericChatException_ShouldNotKickButThrowFault()
        {
            var user = new UserBuilder().WithId(10).Build();
            var client = new PlayerClient(user.id_user, user.nickname, user.id_avatar, _mockCallback.Object);

            var mockLobby = new Mock<Lobby>("CODE", client, _mockUserDao.Object);
            client.CurrentLobby = mockLobby.Object;

            _mockSessionManager.Setup(sm => sm.GetClient(10)).Returns(client);

            mockLobby.Setup(l => l.BroadcastChatMessage(It.IsAny<string>(), It.IsAny<string>()))
                     .Throws(new ChatException("Generic Error"));

            await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.SendMessage(user, "Text"));

            _mockLobbyManager.Verify(lm => lm.KickPlayer(It.IsAny<PlayerClient>(), It.IsAny<int>()), Times.Never);
        }
    }
}