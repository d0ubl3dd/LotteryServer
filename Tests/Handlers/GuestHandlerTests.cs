using Xunit;
using System.Threading.Tasks;
using System.ServiceModel;
using BusinessLogic.Handlers;
using DataAccess;
using Contracts.Faults;

namespace Tests.Handlers
{
    public class GuestHandlerTests
    {
        private readonly GuestHandler _handler;

        public GuestHandlerTests()
        {
            _handler = new GuestHandler();
        }

        [Fact]
        public async Task LoginGuest_WhenNicknameIsValid_ShouldReturnUserWithNegativeId()
        {
            string nickname = "Visitante";

            User result = await _handler.LoginGuest(nickname);

            Assert.NotNull(result);
            Assert.Equal(nickname, result.nickname);
            Assert.True(result.id_user < 0, "El ID del invitado debe ser negativo.");
            Assert.Equal("Online", result.status);
            Assert.Equal("guest@temp.com", result.email);
        }

        [Fact]
        public async Task LoginGuest_WhenCalledMultipleTimes_ShouldGenerateUniqueIds()
        {
            User guest1 = await _handler.LoginGuest("GuestOne");
            User guest2 = await _handler.LoginGuest("GuestTwo");

            Assert.NotEqual(guest1.id_user, guest2.id_user);
            Assert.True(guest2.id_user < guest1.id_user, "Los IDs deben ser decrecientes (-1, -2, etc).");
        }

        [Fact]
        public async Task LoginGuest_WhenNicknameIsEmpty_ShouldThrowFault_EmptyNickname()
        {
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.LoginGuest(""));

            Assert.Equal("AUTH_EMPTY_NICKNAME", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task LoginGuest_WhenNicknameIsTooShort_ShouldThrowFault_InvalidLength()
        {
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.LoginGuest("Ab"));

            Assert.Equal("AUTH_INVALID_LENGTH", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task LoginGuest_WhenNicknameHasSpecialChars_ShouldThrowFault_InvalidFormat()
        {
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.LoginGuest("Guest$#"));

            Assert.Equal("AUTH_INVALID_FORMAT", ex.Detail.ErrorCode);
        }
    }
}