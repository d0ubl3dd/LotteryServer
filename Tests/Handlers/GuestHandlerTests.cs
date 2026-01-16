using Xunit;
using System;
using System.Threading.Tasks;
using System.ServiceModel;
using BusinessLogic.Handlers;
using BusinessLogic.Exceptions;
using Contracts.Faults;
using DataAccess;

namespace Tests.Handlers
{
    public class GuestHandlerTests
    {
        private readonly GuestHandler _handler;

        public GuestHandlerTests()
        {
            _handler = new GuestHandler();
        }

        [Theory]
        [InlineData("GuestUser")]
        [InlineData("Player1")]
        [InlineData("ValidNick")]
        [InlineData("User1234")]
        public async Task LoginGuest_WhenNicknameIsValid_ShouldReturnUserWithNegativeId(string nickname)
        {
            User result = await _handler.LoginGuest(nickname);

            Assert.NotNull(result);
            Assert.True(result.id_user < 0);
            Assert.Equal(nickname, result.nickname);
            Assert.Equal("Online", result.status);
            Assert.Equal(1, result.id_avatar);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task LoginGuest_WhenNicknameIsEmpty_ShouldThrowEmptyNicknameException(string nickname)
        {
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.LoginGuest(nickname));

            Assert.Equal("AUTH_EMPTY_NICKNAME", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task LoginGuest_SequentialCalls_ShouldDecrementIds()
        {
            var user1 = await _handler.LoginGuest("Guest1");
            var user2 = await _handler.LoginGuest("Guest2");

            Assert.True(user1.id_user < 0);
            Assert.True(user2.id_user < 0);
            Assert.NotEqual(user1.id_user, user2.id_user);
            Assert.True(user2.id_user < user1.id_user); // -2 < -1
        }
    }
}