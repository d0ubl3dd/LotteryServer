using Xunit;
using BusinessLogic.Validation;
using BusinessLogic.Logic;
using DataAccess;
using System.Text;

namespace Tests.Validation
{
    public class LoginValidatorTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        [InlineData("\t")]
        [InlineData("\n")]
        public void ValidateLoginAttempt_WhenUserNameIsEmpty_ShouldReturnEmptyUserName(string userName)
        {
            var result = LoginValidator.ValidateLoginAttempt(userName, "ValidPass", new User());
            Assert.Equal(LoginValidationResult.EmptyUserName, result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        [InlineData("\t")]
        public void ValidateLoginAttempt_WhenPasswordIsEmpty_ShouldReturnEmptyPassword(string password)
        {
            var result = LoginValidator.ValidateLoginAttempt("ValidUser", password, new User());
            Assert.Equal(LoginValidationResult.EmptyPassword, result);
        }

        [Theory]
        [InlineData("a")]
        [InlineData("ab")]
        [InlineData("abc")]
        [InlineData("123")]
        [InlineData("A")]
        public void ValidateLoginAttempt_WhenNicknameTooShort_ShouldReturnNicknameTooShort(string shortName)
        {
            var result = LoginValidator.ValidateLoginAttempt(shortName, "ValidPass", new User());
            Assert.Equal(LoginValidationResult.NicknameTooShort, result);
        }

        [Theory]
        [InlineData("ValidUser")]
        [InlineData("PlayerOne")]
        public void ValidateLoginAttempt_WhenUserIsNull_ShouldReturnUserNotFound(string userName)
        {
            var result = LoginValidator.ValidateLoginAttempt(userName, "ValidPass", null);
            Assert.Equal(LoginValidationResult.UserNotFound, result);
        }

        [Fact]
        public void ValidateLoginAttempt_WhenUserIsLocked_ShouldReturnAccountLocked()
        {
            var user = new User { isLocked = true, nickname = "LockedUser" };

            var result = LoginValidator.ValidateLoginAttempt("LockedUser", "AnyPass", user);

            Assert.Equal(LoginValidationResult.AccountLocked, result);
        }

        [Theory]
        [InlineData("MyPassword", "WrongPassword")]
        [InlineData("123456", "1234567")]
        [InlineData("Admin123", "admin123")]
        public void ValidateLoginAttempt_WhenPasswordHashDoesNotMatch_ShouldReturnIncorrectPassword(string actualPass, string wrongInput)
        {
            PasswordHasher.CreatePasswordHash(actualPass, out byte[] hash, out byte[] salt);

            var user = new User
            {
                isLocked = false,
                passwordHash = hash,
                passwordSalt = salt
            };

            var result = LoginValidator.ValidateLoginAttempt("ValidUser", wrongInput, user);

            Assert.Equal(LoginValidationResult.IncorrectPassword, result);
        }

        [Theory]
        [InlineData("SuperSecretPass!")]
        [InlineData("123456")]
        [InlineData("complex-pass-word")]
        public void ValidateLoginAttempt_WhenCredentialsCorrect_ShouldReturnSuccess(string password)
        {
            PasswordHasher.CreatePasswordHash(password, out byte[] hash, out byte[] salt);

            var user = new User
            {
                isLocked = false,
                passwordHash = hash,
                passwordSalt = salt
            };

            var result = LoginValidator.ValidateLoginAttempt("ValidUser", password, user);

            Assert.Equal(LoginValidationResult.Success, result);
        }

        [Fact]
        public void ValidateLoginAttempt_WhenUserIsLocked_EvenWithCorrectPassword_ShouldReturnAccountLocked()
        {
            string password = "CorrectPassword";
            PasswordHasher.CreatePasswordHash(password, out byte[] hash, out byte[] salt);

            var user = new User
            {
                isLocked = true,
                passwordHash = hash,
                passwordSalt = salt
            };

            var result = LoginValidator.ValidateLoginAttempt("ValidUser", password, user);

            Assert.Equal(LoginValidationResult.AccountLocked, result);
        }
    }
}