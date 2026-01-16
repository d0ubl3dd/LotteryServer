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
using System.Runtime.Serialization;
using System.Data.SqlClient;

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

        [Theory]
        [InlineData(null, "pass")]
        [InlineData("", "pass")]
        [InlineData("   ", "pass")]
        public async Task LoginUser_WhenUserNameIsInvalid_ShouldThrowException(string user, string pass)
        {
            await Assert.ThrowsAnyAsync<Exception>(() => _handler.LoginUser(user, pass));
        }

        [Theory]
        [InlineData("user", null)]
        [InlineData("user", "")]
        [InlineData("user", "   ")]
        public async Task LoginUser_WhenPasswordIsInvalid_ShouldThrowException(string user, string pass)
        {
            await Assert.ThrowsAnyAsync<Exception>(() => _handler.LoginUser(user, pass));
        }

        [Theory]
        [InlineData(0, 1, false)]
        [InlineData(1, 2, false)]
        [InlineData(2, 3, false)]
        [InlineData(3, 4, false)]
        [InlineData(4, 5, true)]
        [InlineData(5, 6, true)]
        [InlineData(10, 11, true)]
        public async Task LoginUser_WhenPasswordIncorrect_ShouldIncrementAttemptsAndLockIfThresholdReached(
            int initialAttempts, int expectedAttempts, bool expectedLockState)
        {
            string username = "TestUser";
            string correctPass = "CorrectPass123!";
            string wrongPass = "WrongPass";

            var user = new UserBuilder()
                .WithNickname(username)
                .WithPassword(correctPass)
                .WithFailedAttempts(initialAttempts)
                .Build();

            _mockUserDao.Setup(dao => dao.GetUserByNicknameAsync(username)).ReturnsAsync(user);
            _mockUserDao.Setup(dao => dao.GetUserByIdAsync(user.id_user)).ReturnsAsync(user);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.LoginUser(username, wrongPass));

            Assert.Equal("AUTH_INVALID_CREDENTIALS", ex.Detail.ErrorCode);
            Assert.Equal(expectedAttempts, user.failedLoginAttempts);
            Assert.Equal(expectedLockState, user.isLocked);
            _mockUserDao.Verify(d => d.SaveChangesAsync(), Times.Once);
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
        public async Task LoginUser_WhenAccountIsLocked_ShouldThrowFault_AccountLocked()
        {
            var lockedUser = new UserBuilder().Locked().Build();
            _mockUserDao.Setup(dao => dao.GetUserByNicknameAsync(lockedUser.nickname)).ReturnsAsync(lockedUser);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.LoginUser(lockedUser.nickname, "anyPass"));

            Assert.Equal("AUTH_ACCOUNT_LOCKED", ex.Detail.ErrorCode);
            _mockUserDao.Verify(dao => dao.SaveChangesAsync(), Times.Never);
        }

        [Theory]
        [InlineData(typeof(TimeoutException))]
        [InlineData(typeof(SqlException))]
        public async Task LoginUser_WhenDatabaseFailsOnSearch_ShouldThrowMappedFault(Type exceptionType)
        {
            Exception exception;

            if (exceptionType == typeof(SqlException))
            {
                exception = FormatterServices.GetUninitializedObject(typeof(SqlException)) as SqlException;
            }
            else
            {
                exception = (Exception)Activator.CreateInstance(exceptionType, "Error simulado", null);
            }

            _mockUserDao.Setup(d => d.GetUserByNicknameAsync(It.IsAny<string>())).ThrowsAsync(exception);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.LoginUser("user", "pass"));

            Assert.NotNull(ex.Detail.ErrorCode);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-99)]
        [InlineData(int.MinValue)]
        public async Task LogoutUser_WhenGuest_ShouldNotCallDatabase(int guestId)
        {
            var guest = new UserBuilder().WithId(guestId).Build();

            await _handler.LogoutUser(guest);

            _mockUserDao.Verify(d => d.GetUserByIdAsync(It.IsAny<int>()), Times.Never);
            _mockUserDao.Verify(d => d.SaveChangesAsync(), Times.Never);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(999)]
        [InlineData(int.MaxValue)]
        public async Task LogoutUser_WhenRegistered_ShouldSetOfflineAndSave(int userId)
        {
            var user = new UserBuilder().WithId(userId).Build();
            user.status = "Online";

            _mockUserDao.Setup(d => d.GetUserByIdAsync(userId)).ReturnsAsync(user);

            await _handler.LogoutUser(user);

            Assert.Equal("Offline", user.status);
            _mockUserDao.Verify(d => d.SaveChangesAsync(), Times.Once);
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

        [Theory]
        [InlineData(typeof(TimeoutException))]
        public async Task LogoutUser_WhenDbFails_ShouldThrowFault(Type exceptionType)
        {
            var user = new UserBuilder().WithId(1).Build();
            _mockUserDao.Setup(d => d.GetUserByIdAsync(1)).ReturnsAsync(user);

            var exception = (Exception)Activator.CreateInstance(exceptionType, "Error", null);
            _mockUserDao.Setup(d => d.SaveChangesAsync()).ThrowsAsync(exception);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.LogoutUser(user));

            Assert.NotNull(ex.Detail.ErrorCode);
        }
    }
}