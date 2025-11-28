using Xunit;
using Moq;
using BusinessLogic.Handlers;
using DataAccess.DAOs;
using DataAccess;
using System.Threading.Tasks;
using System.ServiceModel;
using Contracts.Faults;
using BusinessLogic.Logic;

namespace LotteryServer.Tests.Handlers
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
        public async Task LoginUser_Success_ReturnsUser_And_UpdatesStatus()
        {
            string username = "JugadorPro";
            string password = "PasswordSeguro123";

            byte[] hash, salt;
            PasswordHasher.CreatePasswordHash(password, out hash, out salt);

            var dbUser = new User
            {
                id_user = 1,
                nickname = username,
                passwordHash = hash,
                passwordSalt = salt,
                status = "Offline",
                isLocked = false,
                failedLoginAttempts = 2
            };

            _mockUserDao.Setup(dao => dao.GetUserByNicknameAsync(username)).ReturnsAsync(dbUser);
            _mockUserDao.Setup(dao => dao.IsUserBannedAsync(dbUser.id_user)).ReturnsAsync(false);
            _mockUserDao.Setup(dao => dao.GetUserByIdAsync(dbUser.id_user)).ReturnsAsync(dbUser);

            User result = await _handler.LoginUser(username, password);

            Assert.NotNull(result);
            Assert.Equal("Online", dbUser.status);
            Assert.Equal(0, dbUser.failedLoginAttempts);
            _mockUserDao.Verify(dao => dao.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task LoginUser_UserNotFound_ThrowsFault_UserNotFound()
        {
            string username = "NoExisto";
            _mockUserDao.Setup(dao => dao.GetUserByNicknameAsync(username))
                        .ReturnsAsync((User)null);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.LoginUser(username, "pass"));

            Assert.Equal("AUTH_USER_NOT_FOUND", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task LoginUser_IncorrectPassword_IncrementsAttempts_And_ThrowsFault()
        {
            string username = "JugadorPro";
            string correctPass = "RealPass";
            string wrongPass = "FakePass";

            byte[] hash, salt;
            PasswordHasher.CreatePasswordHash(correctPass, out hash, out salt);

            var dbUser = new User
            {
                id_user = 1,
                nickname = username,
                passwordHash = hash,
                passwordSalt = salt,
                failedLoginAttempts = 0
            };

            _mockUserDao.Setup(dao => dao.GetUserByNicknameAsync(username)).ReturnsAsync(dbUser);
            _mockUserDao.Setup(dao => dao.IsUserBannedAsync(dbUser.id_user)).ReturnsAsync(false);
            _mockUserDao.Setup(dao => dao.GetUserByIdAsync(dbUser.id_user)).ReturnsAsync(dbUser);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.LoginUser(username, wrongPass));

            Assert.Equal("AUTH_INVALID_CREDENTIALS", ex.Detail.ErrorCode);
            Assert.Equal(1, dbUser.failedLoginAttempts);
            _mockUserDao.Verify(dao => dao.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task LoginUser_AccountLocked_ThrowsFault_AccountLocked()
        {
            string username = "Bloqueado";
            var dbUser = new User
            {
                id_user = 2,
                nickname = username,
                isLocked = true
            };

            _mockUserDao.Setup(dao => dao.GetUserByNicknameAsync(username)).ReturnsAsync(dbUser);
            _mockUserDao.Setup(dao => dao.IsUserBannedAsync(dbUser.id_user)).ReturnsAsync(false);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.LoginUser(username, "anyPass"));

            Assert.Equal("AUTH_ACCOUNT_LOCKED", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task LoginUser_AccountBanned_ThrowsFault_AccountBanned()
        {
            string username = "Baneado";
            var dbUser = new User { id_user = 3, nickname = username };

            _mockUserDao.Setup(dao => dao.GetUserByNicknameAsync(username)).ReturnsAsync(dbUser);

            _mockUserDao.Setup(dao => dao.IsUserBannedAsync(dbUser.id_user)).ReturnsAsync(true);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.LoginUser(username, "anyPass"));

            Assert.Equal("AUTH_ACCOUNT_BANNED", ex.Detail.ErrorCode);
        }
    }
}