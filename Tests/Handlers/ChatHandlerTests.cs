using Xunit;
using Moq;
using BusinessLogic.Logic;
using BusinessLogic.Models;
using DataAccess;
using Contracts.Callbacks;
using System.ServiceModel;
using Contracts.Faults;
using System;
using BusinessLogic.Exceptions;

namespace LotteryServer.Tests.Handlers
{
    public class ChatHandlerTests
    {
        private readonly Mock<ISessionManager> _mockSessionManager;
        private readonly ChatHandler _handler;
        private readonly Mock<ILotteryCallback> _mockCallback;

        public ChatHandlerTests()
        {
            _mockSessionManager = new Mock<ISessionManager>();
            _mockCallback = new Mock<ILotteryCallback>();
            _handler = new ChatHandler(_mockSessionManager.Object);
        }

        [Fact]
        public void SendMessage_UserNull_ThrowsFault_BadRequest()
        {
            var ex = Assert.Throws<FaultException<ServiceFault>>(() =>
                _handler.SendMessage(null, "Hola"));

            Assert.Equal("CHAT_BAD_REQUEST", ex.Detail.ErrorCode);
        }

        [Fact]
        public void SendMessage_EmptyMessage_DoesNotBroadcast()
        {
            var user = new User { id_user = 1, nickname = "SilentBob" };

            _handler.SendMessage(user, "");
            _handler.SendMessage(user, "   ");

            _mockSessionManager.Verify(sm => sm.GetClient(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void SendMessage_UserNotOnline_ThrowsFault_UserOffline()
        {
            var user = new User { id_user = 99, nickname = "Ghost" };

            _mockSessionManager.Setup(sm => sm.GetClient(user.id_user))
                               .Returns((PlayerClient)null);

            var ex = Assert.Throws<FaultException<ServiceFault>>(() =>
                _handler.SendMessage(user, "Hola mundo"));

            Assert.Equal("CHAT_USER_OFFLINE", ex.Detail.ErrorCode);
        }

        [Fact]
        public void SendMessage_UserNotInLobby_ThrowsFault_UserNotInLobby()
        {
            var user = new User { id_user = 1, nickname = "LobbyLess" };

            var client = new PlayerClient(user, _mockCallback.Object);

            _mockSessionManager.Setup(sm => sm.GetClient(user.id_user))
                               .Returns(client);

            var ex = Assert.Throws<FaultException<ServiceFault>>(() =>
                _handler.SendMessage(user, "Hola??"));

            Assert.Equal("CHAT_USER_NOT_IN_LOBBY", ex.Detail.ErrorCode);
        }

        [Fact]
        public void SendMessage_Success_BroadcastsToLobby()
        {
            var user = new User { id_user = 1, nickname = "ChatterBox", id_avatar = 5 };
            string message = "¡Buena suerte a todos!";

            var client = new PlayerClient(user, _mockCallback.Object);

            var lobby = new Lobby("CODE123", client);

            Assert.NotNull(client.CurrentLobby);

            _mockSessionManager.Setup(sm => sm.GetClient(user.id_user))
                               .Returns(client);

            _handler.SendMessage(user, message);

            _mockCallback.Verify(cb => cb.ReceiveChatMessage(user.nickname, message), Times.Once);
        }
    }
}