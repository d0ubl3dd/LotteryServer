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
        private readonly ChatHandler _handler;
        private readonly Mock<ILotteryCallback> _mockCallback;

        private readonly Mock<IUserDao> _mockUserDao;

        public ChatHandlerTests()
        {
            _mockSessionManager = new Mock<ISessionManager>();
            _mockLobbyManager = new Mock<ILobbyManager>();
            _mockCallback = new Mock<ILotteryCallback>();
            _mockUserDao = new Mock<IUserDao>();

            _handler = new ChatHandler(_mockSessionManager.Object, _mockLobbyManager.Object);
        }

        [Fact]
        public async Task SendMessage_WhenUserAndLobbyAreValid_ShouldBroadcastMessage()
        {
            string message = "Hola Mundo";
            var user = new UserBuilder().WithNickname("Chatter").Build();

            var client = new PlayerClient(
                user.id_user,
                user.nickname,
                user.id_avatar,
                _mockCallback.Object
            );

            var realLobby = new Lobby("CODE1", client, _mockUserDao.Object);

            client.CurrentLobby = realLobby;
            _mockSessionManager.Setup(sm => sm.GetClient(user.id_user)).Returns(client);

            await _handler.SendMessage(user, message);

            _mockCallback.Verify(cb => cb.ReceiveChatMessage("Chatter", message), Times.Once);
        }

        [Fact]
        public async Task SendMessage_WhenMessageIsEmpty_ShouldDoNothing()
        {
            var user = new UserBuilder().Build();

            await _handler.SendMessage(user, "   ");

            _mockSessionManager.Verify(sm => sm.GetClient(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task SendMessage_WhenForbiddenWord_ShouldKickPlayer()
        {
            var user = new UserBuilder().WithNickname("Grosero").Build();
            var client = new PlayerClient(user.id_user, user.nickname, 1, _mockCallback.Object);

            var mockLobby = new Mock<Lobby>("LOBBY1", client, _mockUserDao.Object);

            mockLobby.Setup(l => l.BroadcastChatMessage(It.IsAny<string>(), It.IsAny<string>()))
                     .Throws(new ForbiddenWordException("Palabra prohibida"));

            client.CurrentLobby = mockLobby.Object;

            _mockSessionManager.Setup(sm => sm.GetClient(user.id_user)).Returns(client);

            await _handler.SendMessage(user, "tonto");

            _mockLobbyManager.Verify(m => m.KickPlayer(client.CurrentLobby.Host, client.UserId), Times.Once);
        }

        [Fact]
        public async Task SendMessage_WhenUserIsNull_ShouldThrowFault_BadRequest()
        {
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.SendMessage(null, "Hola"));

            Assert.Equal("GLOBAL_BAD_REQUEST", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task SendMessage_WhenUserIsNotOnline_ShouldThrowFault_UserOffline()
        {
            var user = new UserBuilder().WithId(99).Build();
            _mockSessionManager.Setup(sm => sm.GetClient(user.id_user)).Returns((PlayerClient)null);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.SendMessage(user, "Hola"));

            Assert.Equal("USER_OFFLINE", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task SendMessage_WhenUserIsNotInLobby_ShouldThrowFault_UserNotInLobby()
        {
            var user = new UserBuilder().Build();
            var client = new PlayerClient(user.id_user, user.nickname, user.id_avatar, _mockCallback.Object);

            client.CurrentLobby = null;

            _mockSessionManager.Setup(sm => sm.GetClient(user.id_user)).Returns(client);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.SendMessage(user, "Hola"));

            Assert.Equal("CHAT_USER_NOT_IN_LOBBY", ex.Detail.ErrorCode);
        }
    }
}