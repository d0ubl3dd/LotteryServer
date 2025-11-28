using Xunit;
using Moq;
using BusinessLogic.Logic;
using DataAccess.DAOs;
using DataAccess;
using System.Threading.Tasks;
using System.ServiceModel;
using Contracts.Faults;
using System;

namespace LotteryServer.Tests.Handlers
{
    public class AdminHandlerTests
    {
        private readonly Mock<IUserDao> _mockUserDao;
        private readonly AdminHandler _handler;

        public AdminHandlerTests()
        {
            _mockUserDao = new Mock<IUserDao>();
            _handler = new AdminHandler(_mockUserDao.Object);
        }

        [Fact]
        public async Task BanUser_Success_AddsBanRecord_And_LogsOutUser()
        {
            int adminId = 1;
            int targetId = 2;
            string reason = "Hacks";

            var admin = new User { id_user = adminId, nickname = "Admin" };
            var target = new User { id_user = targetId, nickname = "Cheater", status = "Online" };

            _mockUserDao.Setup(dao => dao.GetUserByIdAsync(adminId)).ReturnsAsync(admin);
            _mockUserDao.Setup(dao => dao.GetUserByIdAsync(targetId)).ReturnsAsync(target);
            _mockUserDao.Setup(dao => dao.IsUserBannedAsync(targetId)).ReturnsAsync(false);

            await _handler.BanUser(adminId, targetId, reason);

            _mockUserDao.Verify(dao => dao.BanUserAsync(It.Is<Banned>(b =>
                b.id_user == targetId &&
                b.reason == reason
            )), Times.Once);

            Assert.Equal("Offline", target.status);
            _mockUserDao.Verify(dao => dao.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task BanUser_AdminNotFound_ThrowsFault()
        {
            int adminId = 99;
            _mockUserDao.Setup(dao => dao.GetUserByIdAsync(adminId)).ReturnsAsync((User)null);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.BanUser(adminId, 2, "reason"));

            Assert.Equal("ADMIN_USER_NOT_FOUND", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task BanUser_TargetUserNotFound_ThrowsFault()
        {
            int adminId = 1;
            int targetId = 99;

            _mockUserDao.Setup(dao => dao.GetUserByIdAsync(adminId)).ReturnsAsync(new User());
            _mockUserDao.Setup(dao => dao.GetUserByIdAsync(targetId)).ReturnsAsync((User)null);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.BanUser(adminId, targetId, "reason"));

            Assert.Equal("ADMIN_USER_NOT_FOUND", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task BanUser_AlreadyBanned_ThrowsFault_InvalidOperation()
        {
            int targetId = 2;

            _mockUserDao.Setup(dao => dao.GetUserByIdAsync(It.IsAny<int>())).ReturnsAsync(new User());

            _mockUserDao.Setup(dao => dao.IsUserBannedAsync(targetId)).ReturnsAsync(true);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.BanUser(1, targetId, "reason"));

            Assert.Equal("ADMIN_INVALID_OPERATION", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task BanUser_DbError_ThrowsFault_DbError()
        {
            int adminId = 1;

            _mockUserDao.Setup(dao => dao.GetUserByIdAsync(adminId))
                        .ThrowsAsync(new System.Data.Entity.Core.EntityException("Connection failed"));

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.BanUser(adminId, 2, "reason"));

            Assert.Equal("ADMIN_DB_ERROR", ex.Detail.ErrorCode);
        }
    }
}