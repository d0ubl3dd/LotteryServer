using Xunit;
using Moq;
using System;
using System.Threading.Tasks;
using System.ServiceModel;
using BusinessLogic.Handlers;
using DataAccess.DAOs;
using DataAccess;
using Contracts.Faults;
using Tests.Builders;

namespace Tests.Handlers
{
    public class AuthenticationHandlerTests
    {
        private readonly Mock<IUserDao> _mockUserDao;
        private readonly AuthenticationHandler _handler;

        public AuthenticationHandlerTests()
        {
            _mockUserDao = new Mock<IUserDao>();
            _handler = new AuthenticationHandler(_mockUserDao.Object);
        }

        [Fact]
        public async Task LoginUser_WhenCredentialsAreValid_ShouldReturnUserAndSetOnline()
        {
            string password = "Pass123";
            var validUser = new UserBuilder()
                .WithNickname("JugadorPro")
                .WithPassword(password)
                .WithFailedAttempts(2)
                .Build();

            _mockUserDao.Setup(dao => dao.GetUserByNicknameAsync(validUser.nickname)).ReturnsAsync(validUser);
            _mockUserDao.Setup(dao => dao.GetUserByIdAsync(validUser.id_user)).ReturnsAsync(validUser);

            User result = await _handler.LoginUser(validUser.nickname, password);

            Assert.NotNull(result);
            Assert.Equal("Online", validUser.status);
            Assert.Equal(0, validUser.failedLoginAttempts);
            _mockUserDao.Verify(dao => dao.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task LoginUser_WhenUserDoesNotExist_ShouldThrowFault_UserNotFound()
        {
            string username = "Fantasma";
            _mockUserDao.Setup(dao => dao.GetUserByNicknameAsync(username)).ReturnsAsync((User)null);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.LoginUser(username, "pass"));

            Assert.Equal("AUTH_USER_NOT_FOUND", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task LoginUser_WhenPasswordIsIncorrect_ShouldThrowFault_InvalidCredentials()
        {
            var user = new UserBuilder().WithNickname("Test").WithPassword("RealPass").Build();

            _mockUserDao.Setup(dao => dao.GetUserByNicknameAsync(user.nickname)).ReturnsAsync(user);
            _mockUserDao.Setup(dao => dao.GetUserByIdAsync(user.id_user)).ReturnsAsync(user);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.LoginUser(user.nickname, "WrongPass"));

            Assert.Equal("AUTH_INVALID_CREDENTIALS", ex.Detail.ErrorCode);
            Assert.Equal(1, user.failedLoginAttempts); // Efecto secundario
            _mockUserDao.Verify(dao => dao.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task LoginUser_WhenAccountIsLocked_ShouldThrowFault_AccountLocked()
        {
            var lockedUser = new UserBuilder().Locked().Build();
            _mockUserDao.Setup(dao => dao.GetUserByNicknameAsync(lockedUser.nickname)).ReturnsAsync(lockedUser);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.LoginUser(lockedUser.nickname, "anyPass"));

            Assert.Equal("AUTH_ACCOUNT_LOCKED", ex.Detail.ErrorCode);
            _mockUserDao.Verify(dao => dao.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task LogoutUser_WhenUserIsMissingInDb_ShouldThrowFault_UserNotFound()
        {
            var user = new UserBuilder().WithId(99).Build();
            _mockUserDao.Setup(dao => dao.GetUserByIdAsync(99)).ReturnsAsync((User)null);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.LogoutUser(user));

            Assert.Equal("AUTH_USER_NOT_FOUND", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task LogoutUser_WhenArgumentIsNull_ShouldThrowFault_BadRequest()
        {
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.LogoutUser(null));

            Assert.Equal("GLOBAL_BAD_REQUEST", ex.Detail.ErrorCode);
        }
    }
}