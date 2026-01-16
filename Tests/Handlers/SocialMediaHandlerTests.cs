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
        public void Constructor_WhenSocialDaoIsNull_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SocialMediaHandler(null, _mockUserDao.Object));
        }

        [Fact]
        public void Constructor_WhenUserDaoIsNull_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SocialMediaHandler(_mockSocialDao.Object, null));
        }

        [Fact]
        public async Task GetSocialMedia_WhenUserNotFound_ShouldThrowUserNotFound()
        {
            _mockUserDao.Setup(d => d.GetUserByIdAsync(1)).ReturnsAsync((User)null);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.GetSocialMedia(1));

            Assert.Equal("AUTH_USER_NOT_FOUND", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task GetSocialMedia_WhenNoSocialData_ShouldReturnEmptyDto()
        {
            var user = new UserBuilder().WithId(1).Build();
            _mockUserDao.Setup(d => d.GetUserByIdAsync(1)).ReturnsAsync(user);
            _mockSocialDao.Setup(d => d.GetSocialMediaByUserIdAsync(1)).ReturnsAsync((SocialMedia)null);

            var result = await _handler.GetSocialMedia(1);

            Assert.NotNull(result);
            Assert.Null(result.Facebook);
            Assert.Null(result.Twitter);
        }

        [Fact]
        public async Task GetSocialMedia_WhenDataExists_ShouldReturnMappedDto()
        {
            var user = new UserBuilder().WithId(1).Build();
            var social = new SocialMedia { id_user = 1, facebook = "fb", twitter = "tw" };

            _mockUserDao.Setup(d => d.GetUserByIdAsync(1)).ReturnsAsync(user);
            _mockSocialDao.Setup(d => d.GetSocialMediaByUserIdAsync(1)).ReturnsAsync(social);

            var result = await _handler.GetSocialMedia(1);

            Assert.Equal("fb", result.Facebook);
            Assert.Equal("tw", result.Twitter);
        }

        [Fact]
        public async Task UpdateSocialMedia_WhenDtoIsNull_ShouldThrowArgumentNull()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.UpdateSocialMedia(null));
        }

        [Fact]
        public async Task UpdateSocialMedia_WhenUserNotFound_ShouldThrowUserNotFound()
        {
            var dto = new SocialMediaDto { IdUser = 1 };
            _mockUserDao.Setup(d => d.GetUserByIdAsync(1)).ReturnsAsync((User)null);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.UpdateSocialMedia(dto));

            Assert.Equal("AUTH_USER_NOT_FOUND", ex.Detail.ErrorCode);
        }

        [Theory]
        [InlineData("taken_tw")]
        [InlineData("another_tw")]
        public async Task UpdateSocialMedia_WhenTwitterDuplicate_ShouldThrowUserAlreadyExists(string twitter)
        {
            var user = new UserBuilder().WithId(1).Build();
            var dto = new SocialMediaDto { IdUser = 1, Twitter = twitter };

            _mockUserDao.Setup(d => d.GetUserByIdAsync(1)).ReturnsAsync(user);
            _mockSocialDao.Setup(d => d.ExistsTwitterUsernameExcludingUserAsync(1, twitter)).ReturnsAsync(true);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.UpdateSocialMedia(dto));

            Assert.Equal("USER_DUPLICATE", ex.Detail.ErrorCode);
        }

        [Theory]
        [InlineData("taken_insta")]
        [InlineData("insta_pro")]
        public async Task UpdateSocialMedia_WhenInstagramDuplicate_ShouldThrowUserAlreadyExists(string instagram)
        {
            var user = new UserBuilder().WithId(1).Build();
            var dto = new SocialMediaDto { IdUser = 1, Instagram = instagram };

            _mockUserDao.Setup(d => d.GetUserByIdAsync(1)).ReturnsAsync(user);
            _mockSocialDao.Setup(d => d.ExistsInstagramUsernameExcludingUserAsync(1, instagram)).ReturnsAsync(true);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.UpdateSocialMedia(dto));

            Assert.Equal("USER_DUPLICATE", ex.Detail.ErrorCode);
        }

        [Theory]
        [InlineData("taken_tiktok")]
        [InlineData("tiktok_star")]
        public async Task UpdateSocialMedia_WhenTikTokDuplicate_ShouldThrowUserAlreadyExists(string tiktok)
        {
            var user = new UserBuilder().WithId(1).Build();
            var dto = new SocialMediaDto { IdUser = 1, TikTok = tiktok };

            _mockUserDao.Setup(d => d.GetUserByIdAsync(1)).ReturnsAsync(user);
            _mockSocialDao.Setup(d => d.ExistsTikTokUsernameExcludingUserAsync(1, tiktok)).ReturnsAsync(true);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.UpdateSocialMedia(dto));

            Assert.Equal("USER_DUPLICATE", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task UpdateSocialMedia_WhenNewRecord_ShouldAdd()
        {
            var user = new UserBuilder().WithId(1).Build();
            var dto = new SocialMediaDto { IdUser = 1, Facebook = "new_fb" };

            _mockUserDao.Setup(d => d.GetUserByIdAsync(1)).ReturnsAsync(user);
            _mockSocialDao.Setup(d => d.GetSocialMediaByUserIdAsync(1)).ReturnsAsync((SocialMedia)null);

            await _handler.UpdateSocialMedia(dto);

            _mockSocialDao.Verify(d => d.AddSocialMediaAsync(It.Is<SocialMedia>(s => s.facebook == "new_fb")), Times.Once);
            _mockSocialDao.Verify(d => d.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateSocialMedia_WhenExistingRecord_ShouldUpdate()
        {
            var user = new UserBuilder().WithId(1).Build();
            var existing = new SocialMedia { id_user = 1, facebook = "old_fb" };
            var dto = new SocialMediaDto { IdUser = 1, Facebook = "new_fb" };

            _mockUserDao.Setup(d => d.GetUserByIdAsync(1)).ReturnsAsync(user);
            _mockSocialDao.Setup(d => d.GetSocialMediaByUserIdAsync(1)).ReturnsAsync(existing);

            await _handler.UpdateSocialMedia(dto);

            Assert.Equal("new_fb", existing.facebook);
            _mockSocialDao.Verify(d => d.UpdateSocialMediaAsync(existing), Times.Once);
            _mockSocialDao.Verify(d => d.SaveChangesAsync(), Times.Once);
        }
    }
}