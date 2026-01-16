using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ServiceModel;
using BusinessLogic.Handlers;
using BusinessLogic.Exceptions;
using BusinessLogic.Logic;
using Contracts.DTOs;
using Contracts.Faults;
using Contracts.Services.Users;
using DataAccess;
using DataAccess.DAOs;
using Tests.Builders;
using System.Linq;

namespace Tests.Handlers
{
    public class UserHandlerTests
    {
        private readonly Mock<IUserDao> _mockUserDao;
        private readonly Mock<IVerificationService> _mockVerificationService;
        private readonly UserHandler _handler;

        public UserHandlerTests()
        {
            _mockUserDao = new Mock<IUserDao>();
            _mockVerificationService = new Mock<IVerificationService>();
            _handler = new UserHandler(_mockUserDao.Object, _mockVerificationService.Object);
        }

        [Fact]
        public void Constructor_WhenUserDaoIsNull_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new UserHandler(null, _mockVerificationService.Object));
        }

        [Fact]
        public void Constructor_WhenVerificationServiceIsNull_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new UserHandler(_mockUserDao.Object, null));
        }

        [Fact]
        public async Task RequestUserVerification_WhenDtoIsNull_ShouldThrowArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.RequestUserVerification(null));
        }

        [Fact]
        public async Task RegisterUserWithCode_WhenDtoIsNull_ShouldThrowArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.RegisterUserWithCode(null, "123"));
        }

        [Fact]
        public async Task VerifyPassword_WhenCorrect_ShouldReturnTrue()
        {
            var pass = "Pass1!";
            PasswordHasher.CreatePasswordHash(pass, out byte[] hash, out byte[] salt);
            var user = new UserBuilder().WithId(1).Build();
            user.passwordHash = hash;
            user.passwordSalt = salt;

            _mockUserDao.Setup(d => d.GetUserByIdAsync(1)).ReturnsAsync(user);

            var result = await _handler.VerifyPassword(1, pass);
            Assert.True(result);
        }

        [Fact]
        public async Task ChangePassword_WhenValid_ShouldUpdateAndSave()
        {
            var user = new UserBuilder().WithId(1).Build();
            _mockUserDao.Setup(d => d.GetUserByIdAsync(1)).ReturnsAsync(user);

            await _handler.ChangePassword(1, "NewPass1!");

            _mockUserDao.Verify(d => d.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateProfile_WhenDtoIsNull_ShouldThrowArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.UpdateProfile(1, null));
        }

        [Fact]
        public async Task UpdateProfile_WhenValid_ShouldUpdateFieldsAndReturnSuccess()
        {
            var user = new UserBuilder().WithId(1).WithNickname("Old").Build();
            var dto = new UserDto { Nickname = "New", FirstName = "F", PaternalLastName = "P", AvatarId = 5 };

            _mockUserDao.Setup(d => d.GetUserByIdAsync(1)).ReturnsAsync(user);
            _mockUserDao.Setup(d => d.NicknameExistsAsync("New")).ReturnsAsync(false);

            var result = await _handler.UpdateProfile(1, dto);

            Assert.True(result.Success);
            Assert.Equal("New", user.nickname);
            Assert.Equal("F", user.first_name);
            Assert.Equal(5, user.id_avatar);
            _mockUserDao.Verify(d => d.SaveChangesAsync(), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task RequestEmailChangeVerification_WhenEmailEmpty_ShouldThrowArgumentException(string email)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _handler.RequestEmailChangeVerification(email));
        }

        [Fact]
        public async Task RequestEmailChangeVerification_WhenValid_ShouldReturnTrue()
        {
            _mockUserDao.Setup(d => d.EmailExistsAsync("new@a.com")).ReturnsAsync(false);
            _mockVerificationService.Setup(v => v.SendVerificationCode("new@a.com")).ReturnsAsync(true);

            var result = await _handler.RequestEmailChangeVerification("new@a.com");
            Assert.True(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task ChangeEmailWithCodeAsync_WhenEmailEmpty_ShouldThrowArgumentException(string email)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _handler.ChangeEmailWithCodeAsync(1, email, "123"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task ChangeEmailWithCodeAsync_WhenCodeEmpty_ShouldThrowArgumentException(string code)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _handler.ChangeEmailWithCodeAsync(1, "a@a.com", code));
        }

        [Fact]
        public async Task ChangeEmailWithCodeAsync_WhenValid_ShouldUpdateEmail()
        {
            var user = new UserBuilder().WithId(1).Build();
            _mockUserDao.Setup(d => d.GetUserByIdAsync(1)).ReturnsAsync(user);
            _mockVerificationService.Setup(v => v.VerifyCode("new@a.com", "123")).ReturnsAsync(true);
            _mockUserDao.Setup(d => d.EmailExistsAsync("new@a.com")).ReturnsAsync(false);

            await _handler.ChangeEmailWithCodeAsync(1, "new@a.com", "123");

            Assert.Equal("new@a.com", user.email);
            _mockUserDao.Verify(d => d.SaveChangesAsync(), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task RecoverPasswordRequest_WhenEmailEmpty_ShouldThrowArgumentException(string email)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _handler.RecoverPasswordRequest(email));
        }

        [Fact]
        public async Task RecoverPasswordRequest_WhenValid_ShouldReturnTrue()
        {
            _mockUserDao.Setup(d => d.EmailExistsAsync("a@a.com")).ReturnsAsync(true);
            _mockVerificationService.Setup(v => v.SendVerificationCode("a@a.com")).ReturnsAsync(true);

            Assert.True(await _handler.RecoverPasswordRequest("a@a.com"));
        }

        [Fact]
        public async Task RegisterGuest_ShouldReturnNegativeOne()
        {
            Assert.Equal(-1, await _handler.RegisterGuest());
        }

        [Theory]
        [InlineData("", "pass")]
        [InlineData(null, "pass")]
        [InlineData("a@a.com", "")]
        [InlineData("a@a.com", null)]
        public async Task RecoverPassword_WhenArgsEmpty_ShouldThrowArgumentException(string email, string pass)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _handler.RecoverPassword(email, pass));
        }

        [Fact]
        public async Task RecoverPassword_WhenValid_ShouldUpdatePass()
        {
            var user = new UserBuilder().Build();
            _mockUserDao.Setup(d => d.GetUserByEmailAsync("a@a.com")).ReturnsAsync(user);

            await _handler.RecoverPassword("a@a.com", "NewPass123!");
            _mockUserDao.Verify(d => d.SaveChangesAsync(), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task FindUserByNickname_WhenEmpty_ShouldThrowArgumentException(string nick)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _handler.FindUserByNickname(nick));
        }

        [Fact]
        public async Task FindUserByNickname_WhenFound_ShouldReturnDto()
        {
            var user = new UserBuilder().WithId(1).WithNickname("Found").Build();
            _mockUserDao.Setup(d => d.GetUserByNicknameAsync("Found")).ReturnsAsync(user);

            var result = await _handler.FindUserByNickname("Found");
            Assert.Equal("Found", result.Nickname);
        }

        [Fact]
        public async Task GetUserProfile_WhenFound_ShouldReturnDto()
        {
            var user = new UserBuilder().WithId(1).WithNickname("Nick").Build();
            _mockUserDao.Setup(d => d.GetUserByIdAsync(1)).ReturnsAsync(user);

            var result = await _handler.GetUserProfile(1);
            Assert.Equal("Nick", result.Nickname);
        }

        [Fact]
        public async Task GetLeaderboard_ShouldReturnList()
        {
            var list = new List<User>
            {
                new UserBuilder().WithId(1).Build(),
                new UserBuilder().WithId(2).Build()
            };
            _mockUserDao.Setup(d => d.GetLeaderboard()).ReturnsAsync(list);

            var result = await _handler.GetLeaderboard();
            Assert.Equal(2, result.Count);
        }
    }
}