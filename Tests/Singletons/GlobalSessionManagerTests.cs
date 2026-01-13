using Xunit;
using Moq;
using System;
using System.Reflection;
using System.ServiceModel;
using BusinessLogic.Logic;
using BusinessLogic.Models;
using BusinessLogic.Exceptions;
using Contracts.Callbacks;
using DataAccess;
using Contracts.Faults;
using Tests.Builders;

namespace Tests.Logic
{
    public class GlobalSessionManagerTests
    {
        private GlobalSessionManager _sut;
        private readonly Mock<ILotteryCallback> _mockCallback;

        public GlobalSessionManagerTests()
        {
            _mockCallback = new Mock<ILotteryCallback>();
            _sut = CreateIsolatedInstance();
        }

        private GlobalSessionManager CreateIsolatedInstance()
        {
            var constructor = typeof(GlobalSessionManager).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null, new Type[0], null);

            return (GlobalSessionManager)constructor.Invoke(null);
        }

        [Fact]
        public void RegisterClient_WhenUserIsValid_ShouldAddUserToDictionary()
        {
            var user = new UserBuilder().WithId(10).WithNickname("Gamer").Build();

            _sut.RegisterClient(user, _mockCallback.Object);

            var client = _sut.GetClient(user.id_user);
            Assert.NotNull(client);
            Assert.Equal("Gamer", client.Nickname);
        }

        [Fact]
        public void RegisterClient_WhenArgumentsNull_ShouldThrowFault_BadRequest()
        {
            Assert.Throws<FaultException<ServiceFault>>(() =>
                _sut.RegisterClient(null, _mockCallback.Object));
        }

        [Fact]
        public void GetClient_WhenUserNotRegistered_ShouldThrowFault_ClientNotFound()
        {
            var ex = Assert.Throws<FaultException<ServiceFault>>(() =>
                _sut.GetClient(999));

            Assert.Equal("SESSION_CLIENT_NOT_FOUND", ex.Detail.ErrorCode);
        }

        [Fact]
        public void UnregisterClient_WhenUserExists_ShouldRemoveAndReturnClient()
        {
            var user = new UserBuilder().WithId(5).Build();
            _sut.RegisterClient(user, _mockCallback.Object);

            var removedClient = _sut.UnregisterClient(user.id_user);

            Assert.NotNull(removedClient);
            Assert.Equal(5, removedClient.UserId);

            Assert.Throws<FaultException<ServiceFault>>(() => _sut.GetClient(5));
        }

        [Fact]
        public void UnregisterClient_WhenUserDoesNotExist_ShouldReturnNull()
        {
            var result = _sut.UnregisterClient(999);

            Assert.Null(result);
        }

        [Fact]
        public void AutoDisconnect_WhenChannelCloses_ShouldRemoveUser()
        {
            var user = new UserBuilder().WithId(20).Build();

            var mockChannel = new Mock<ILotteryCallback>();
            var mockComm = mockChannel.As<ICommunicationObject>();

            _sut.RegisterClient(user, mockChannel.Object);

            mockComm.Raise(c => c.Closed += null, EventArgs.Empty);

            Assert.Throws<FaultException<ServiceFault>>(() => _sut.GetClient(20));
        }
    }
}