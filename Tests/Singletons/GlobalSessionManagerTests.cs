using Xunit;
using Moq;
using System;
using System.ServiceModel;
using System.Collections.Generic;
using BusinessLogic.Logic;
using BusinessLogic.Models;
using Contracts.Callbacks;
using DataAccess;
using Tests.Builders;
using BusinessLogic.Exceptions;
using Contracts.Faults;

namespace Tests.Logic
{
    public class GlobalSessionManagerTests
    {
        private readonly Mock<ILotteryCallback> _mockCallback;
        private readonly GlobalSessionManager _manager;

        public GlobalSessionManagerTests()
        {
            _mockCallback = new Mock<ILotteryCallback>();
            _manager = GlobalSessionManager.Instance;

            foreach (var user in _manager.GetAllOnlineUsers())
            {
                _manager.UnregisterClient(user.UserId);
            }
        }

        [Fact]
        public void RegisterClient_WhenUserIsNull_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _manager.RegisterClient(null, _mockCallback.Object));
        }

        [Fact]
        public void RegisterClient_WhenCallbackIsNull_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _manager.RegisterClient(new User(), null));
        }

        [Theory]
        [InlineData(1, "User1")]
        [InlineData(2, "User2")]
        [InlineData(999, "UserMax")]
        public void RegisterClient_WhenValid_ShouldAddUserToOnlineList(int id, string nickname)
        {
            var user = new UserBuilder().WithId(id).WithNickname(nickname).Build();

            _manager.RegisterClient(user, _mockCallback.Object);

            Assert.True(_manager.IsUserOnline(id));
            var client = _manager.GetClient(id);
            Assert.Equal(nickname, client.Nickname);
        }

        [Fact]
        public void UnregisterClient_WhenUserOnline_ShouldRemoveFromList()
        {
            var user = new UserBuilder().WithId(10).Build();
            _manager.RegisterClient(user, _mockCallback.Object);

            var removedClient = _manager.UnregisterClient(10);

            Assert.NotNull(removedClient);
            Assert.False(_manager.IsUserOnline(10));
        }

        [Fact]
        public void UnregisterClient_WhenUserAlreadyOffline_ShouldReturnNull()
        {
            var result = _manager.UnregisterClient(555);
            Assert.Null(result);
        }

        [Fact]
        public void ReconnectUser_WhenNewCallbackIsNull_ShouldThrowArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => _manager.ReconnectUser(1, null));
        }

        [Fact]
        public void ReconnectUser_WhenUserOnline_ShouldUpdateCallback()
        {
            var user = new UserBuilder().WithId(20).Build();
            var oldCallback = new Mock<ILotteryCallback>();
            var newCallback = new Mock<ILotteryCallback>();

            _manager.RegisterClient(user, oldCallback.Object);

            _manager.ReconnectUser(20, newCallback.Object);

            var client = _manager.GetClient(20);
            Assert.Same(newCallback.Object, client.CallbackChannel);
        }

        [Fact]
        public void ReconnectUser_WhenUserOffline_ShouldRegisterAsNew()
        {
            var newCallback = new Mock<ILotteryCallback>();

            _manager.ReconnectUser(30, newCallback.Object);

            Assert.True(_manager.IsUserOnline(30));
            var client = _manager.GetClient(30);
            Assert.Equal("Unknown", client.Nickname);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void IsUserOnline_WhenRegistered_ShouldReturnTrue(int id)
        {
            var user = new UserBuilder().WithId(id).Build();
            _manager.RegisterClient(user, _mockCallback.Object);

            Assert.True(_manager.IsUserOnline(id));
        }

        [Theory]
        [InlineData(100)]
        [InlineData(200)]
        public void IsUserOnline_WhenNotRegistered_ShouldReturnFalse(int id)
        {
            Assert.False(_manager.IsUserOnline(id));
        }

        [Fact]
        public void RegisterClient_DoubleRegistration_ShouldUpdateExistingClient()
        {
            var user = new UserBuilder().WithId(50).WithNickname("OldName").Build();
            _manager.RegisterClient(user, _mockCallback.Object);

            var updatedUser = new UserBuilder().WithId(50).WithNickname("NewName").Build();
            _manager.RegisterClient(updatedUser, _mockCallback.Object);

            var client = _manager.GetClient(50);
            Assert.Equal("NewName", client.Nickname);
        }
    }
}