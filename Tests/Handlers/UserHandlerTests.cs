using BusinessLogic.Handlers;
using Contracts.DTOs;
using Contracts.Faults;
using Contracts.Services.Users;
using DataAccess;
using DataAccess.DAOs;
using Moq;
using System.ServiceModel;
using System.Threading.Tasks;
using Tests.Builders;
using Xunit;

namespace Tests.Logic
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
        public async Task RegisterUserWithCode_WhenDtoAndCodeAreValid_ShouldAddUserAndReturnId()
        {
            var dto = new UserDto
            {
                Nickname = "NewUser",
                Email = "new@test.com",
                Password = "Password123!",
                FirstName = "Test",
                PaternalLastName = "Test"
            };

            string verificationCode = "123456";

            _mockVerificationService
                .Setup(v => v.VerifyCode(dto.Email, verificationCode))
                .ReturnsAsync(true);

            _mockVerificationService
                .Setup(v => v.ConsumeVerificationCode(dto.Email))
                .Returns(Task.FromResult(true));

            _mockUserDao
                .Setup(d => d.AddUser(It.IsAny<User>()))
                .Callback<User>(u => u.id_user = 10);

            int result = await _handler.RegisterUserWithCode(dto, verificationCode);

            _mockUserDao.Verify(d => d.AddUser(It.Is<User>(u => u.nickname == dto.Nickname && u.email == dto.Email)), Times.Once);
            _mockUserDao.Verify(d => d.SaveChangesAsync(), Times.Once);
            _mockVerificationService.Verify(v => v.ConsumeVerificationCode(dto.Email), Times.Once);

            Assert.True(result > 0);
        }

        [Fact]
        public async Task RequestVerification_WhenNicknameExists_ShouldThrowFault_UserDuplicate()
        {
            var dto = new UserDto
            {
                Nickname = "TakenNick",
                Email = "a@a.com",
                Password = "PasswordValid1!",
                FirstName = "A",
                PaternalLastName = "B"
            };

            _mockUserDao.Setup(d => d.NicknameExistsAsync("TakenNick")).ReturnsAsync(true);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.RequestUserVerification(dto));

            Assert.Equal("USER_DUPLICATE", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task VerifyPassword_WhenPasswordIsCorrect_ShouldReturnTrue()
        {
            string pass = "MySecretPass";
            var user = new UserBuilder().WithId(1).WithPassword(pass).Build();

            _mockUserDao.Setup(d => d.GetUserByIdAsync(1)).ReturnsAsync(user);

            bool isValid = await _handler.VerifyPassword(1, pass);

            Assert.True(isValid);
        }

        [Fact]
        public async Task VerifyPassword_WhenPasswordIsIncorrect_ShouldReturnFalse()
        {
            var user = new UserBuilder().WithId(1).WithPassword("CorrectPass").Build();
            _mockUserDao.Setup(d => d.GetUserByIdAsync(1)).ReturnsAsync(user);

            bool isValid = await _handler.VerifyPassword(1, "WrongPass");

            Assert.False(isValid);
        }

        [Fact]
        public async Task ChangePassword_WhenUserExists_ShouldUpdateHashAndSave()
        {
            var user = new UserBuilder().WithId(1).Build();
            _mockUserDao.Setup(d => d.GetUserByIdAsync(1)).ReturnsAsync(user);

            await _handler.ChangePassword(1, "NewPass123");

            _mockUserDao.Verify(d => d.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateProfile_WhenNicknameChangedToDuplicate_ShouldThrowFault()
        {
            var user = new UserBuilder().WithId(1).WithNickname("OldNick").Build();
            var dto = new UserDto { Nickname = "TakenNick", FirstName = "A", PaternalLastName = "B" };

            _mockUserDao.Setup(d => d.GetUserByIdAsync(1)).ReturnsAsync(user);
            _mockUserDao.Setup(d => d.NicknameExistsAsync("TakenNick")).ReturnsAsync(true);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.UpdateProfile(1, dto));

            Assert.Equal("USER_DUPLICATE", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task UpdateProfile_WhenValid_ShouldUpdatePropertiesAndSave()
        {
            var user = new UserBuilder().WithId(1).WithNickname("OldNick").Build();
            var dto = new UserDto
            {
                Nickname = "NewNick",
                FirstName = "NewName",
                PaternalLastName = "NewLast",
                AvatarId = 5
            };

            _mockUserDao.Setup(d => d.GetUserByIdAsync(1)).ReturnsAsync(user);
            _mockUserDao.Setup(d => d.NicknameExistsAsync("NewNick")).ReturnsAsync(false);

            await _handler.UpdateProfile(1, dto);

            Assert.Equal("NewNick", user.nickname);
            Assert.Equal("NewName", user.first_name);
            Assert.Equal(5, user.id_avatar);
            _mockUserDao.Verify(d => d.SaveChangesAsync(), Times.Once);
        }
    }
}