using Xunit;
using Moq;
using System;
using System.Threading.Tasks;
using System.ServiceModel;
using BusinessLogic.Handlers;
using DataAccess.DAOs;
using DataAccess;
using Contracts.DTOs;
using Contracts.Faults;
using Tests.Builders;

namespace Tests.Handlers
{
    public class SocialMediaHandlerTests
    {
        private readonly Mock<ISocialMediaDao> _mockSocialDao;
        private readonly Mock<IUserDao> _mockUserDao;
        private readonly SocialMediaHandler _handler;

        public SocialMediaHandlerTests()
        {
            _mockSocialDao = new Mock<ISocialMediaDao>();
            _mockUserDao = new Mock<IUserDao>();
            _handler = new SocialMediaHandler(_mockSocialDao.Object, _mockUserDao.Object);
        }

        [Fact]
        public async Task GetSocialMedia_WhenUserExistsAndHasData_ShouldReturnDto()
        {
            var user = new UserBuilder().WithId(1).Build();
            var socialData = new SocialMedia { id_user = 1, facebook = "fb_user" };

            _mockUserDao.Setup(d => d.GetUserByIdAsync(1)).ReturnsAsync(user);
            _mockSocialDao.Setup(d => d.GetSocialMediaByUserIdAsync(1)).ReturnsAsync(socialData);

            var result = await _handler.GetSocialMedia(1);

            Assert.NotNull(result);
            Assert.Equal("fb_user", result.Facebook);
        }

        [Fact]
        public async Task GetSocialMedia_WhenUserExistsButNoData_ShouldReturnEmptyDto()
        {
            var user = new UserBuilder().WithId(1).Build();
            _mockUserDao.Setup(d => d.GetUserByIdAsync(1)).ReturnsAsync(user);
            _mockSocialDao.Setup(d => d.GetSocialMediaByUserIdAsync(1)).ReturnsAsync((SocialMedia)null);

            var result = await _handler.GetSocialMedia(1);

            Assert.NotNull(result);
            Assert.Null(result.Facebook);
        }

        [Fact]
        public async Task GetSocialMedia_WhenUserNotFound_ShouldThrowFault_UserNotFound()
        {
            _mockUserDao.Setup(d => d.GetUserByIdAsync(99)).ReturnsAsync((User)null);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.GetSocialMedia(99));

            Assert.Equal("AUTH_USER_NOT_FOUND", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task UpdateSocialMedia_WhenNewRecord_ShouldAddAndSave()
        {
            var user = new UserBuilder().WithId(1).Build();
            var dto = new SocialMediaDto { IdUser = 1, Twitter = "new_tw" };

            _mockUserDao.Setup(d => d.GetUserByIdAsync(1)).ReturnsAsync(user);
            _mockSocialDao.Setup(d => d.GetSocialMediaByUserIdAsync(1)).ReturnsAsync((SocialMedia)null);

            var success = await _handler.UpdateSocialMedia(dto);

            Assert.True(success);
            _mockSocialDao.Verify(d => d.AddSocialMediaAsync(It.Is<SocialMedia>(s => s.twitter == "new_tw")), Times.Once);
            _mockSocialDao.Verify(d => d.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateSocialMedia_WhenExistingRecord_ShouldUpdateAndSave()
        {
            var user = new UserBuilder().WithId(1).Build();
            var existingSocial = new SocialMedia { id_user = 1, twitter = "old_tw" };
            var dto = new SocialMediaDto { IdUser = 1, Twitter = "updated_tw" };

            _mockUserDao.Setup(d => d.GetUserByIdAsync(1)).ReturnsAsync(user);
            _mockSocialDao.Setup(d => d.GetSocialMediaByUserIdAsync(1)).ReturnsAsync(existingSocial);

            var success = await _handler.UpdateSocialMedia(dto);

            Assert.True(success);
            Assert.Equal("updated_tw", existingSocial.twitter);
            _mockSocialDao.Verify(d => d.UpdateSocialMediaAsync(existingSocial), Times.Once);
            _mockSocialDao.Verify(d => d.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateSocialMedia_WhenTwitterDuplicate_ShouldThrowFault_UserDuplicate()
        {
            var user = new UserBuilder().WithId(1).Build();
            var dto = new SocialMediaDto { IdUser = 1, Twitter = "taken_handle" };

            _mockUserDao.Setup(d => d.GetUserByIdAsync(1)).ReturnsAsync(user);
            _mockSocialDao.Setup(d => d.ExistsTwitterUsernameExcludingUserAsync(1, "taken_handle"))
                          .ReturnsAsync(true);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.UpdateSocialMedia(dto));

            Assert.Equal("USER_DUPLICATE", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task UpdateSocialMedia_WhenDtoIsNull_ShouldThrowFault_BadRequest()
        {
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.UpdateSocialMedia(null));

            Assert.Equal("GLOBAL_BAD_REQUEST", ex.Detail.ErrorCode);
        }
    }
}