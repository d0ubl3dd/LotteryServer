using Xunit;
using BusinessLogic.Validation;
using BusinessLogic.Logic;
using DataAccess;

namespace LotteryServer.Tests.Validation
{
    public class LoginValidatorTests
    {
        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public void ValidateLoginAttempt_EmptyUserName_ReturnsEmptyUserName(string invalidUser)
        {
            var result = LoginValidator.ValidateLoginAttempt(invalidUser, "pass123", new User());

            Assert.Equal(LoginValidationResult.EmptyUserName, result);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public void ValidateLoginAttempt_EmptyPassword_ReturnsEmptyPassword(string invalidPass)
        {
            var result = LoginValidator.ValidateLoginAttempt("ValidUser", invalidPass, new User());

            Assert.Equal(LoginValidationResult.EmptyPassword, result);
        }

        [Theory]
        [InlineData("a")]
        [InlineData("ab")]
        [InlineData("abc")]
        public void ValidateLoginAttempt_ShortUserName_ReturnsNicknameTooShort(string shortUser)
        {
            var result = LoginValidator.ValidateLoginAttempt(shortUser, "pass123", new User());

            Assert.Equal(LoginValidationResult.NicknameTooShort, result);
        }

        [Fact]
        public void ValidateLoginAttempt_UserNull_ReturnsUserNotFound()
        {
            string user = "validUser";
            string pass = "password123";
            User dbUser = null;

            var result = LoginValidator.ValidateLoginAttempt(user, pass, dbUser);

            Assert.Equal(LoginValidationResult.UserNotFound, result);
        }

        [Fact]
        public void ValidateLoginAttempt_UserLocked_ReturnsAccountLocked()
        {
            string user = "lockedUser";
            string pass = "password123";
            User dbUser = new User { isLocked = true };

            var result = LoginValidator.ValidateLoginAttempt(user, pass, dbUser);

            Assert.Equal(LoginValidationResult.AccountLocked, result);
        }

        [Fact]
        public void ValidateLoginAttempt_ValidCredentials_ReturnsSuccess()
        {
            string password = "MySecretPassword";

            byte[] passwordHash, passwordSalt;
            PasswordHasher.CreatePasswordHash(password, out passwordHash, out passwordSalt);

            User dbUser = new User
            {
                isLocked = false,
                passwordHash = passwordHash,
                passwordSalt = passwordSalt
            };

            var result = LoginValidator.ValidateLoginAttempt("validUser", password, dbUser);

            Assert.Equal(LoginValidationResult.Success, result);
        }

        [Fact]
        public void ValidateLoginAttempt_IncorrectPassword_ReturnsIncorrectPassword()
        {
            string correctPassword = "MySecretPassword";
            string wrongPassword = "WrongPassword123";

            byte[] passwordHash, passwordSalt;
            PasswordHasher.CreatePasswordHash(correctPassword, out passwordHash, out passwordSalt);

            User dbUser = new User
            {
                isLocked = false,
                passwordHash = passwordHash,
                passwordSalt = passwordSalt
            };

            var result = LoginValidator.ValidateLoginAttempt("validUser", wrongPassword, dbUser);

            Assert.Equal(LoginValidationResult.IncorrectPassword, result);
        }
    }
}