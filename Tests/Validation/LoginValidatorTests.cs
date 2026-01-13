using Xunit;
using BusinessLogic.Validation;
using DataAccess;
using Tests.Builders;

namespace Tests.Validation
{
    public class LoginValidatorTests
    {
        [Fact]
        public void Validate_WhenUsernameIsEmpty_ShouldReturnEmptyUserName()
        {
            var result = LoginValidator.ValidateLoginAttempt("", "pass", new User());
            Assert.Equal(LoginValidationResult.EmptyUserName, result);
        }

        [Fact]
        public void Validate_WhenPasswordIsEmpty_ShouldReturnEmptyPassword()
        {
            var result = LoginValidator.ValidateLoginAttempt("User", "", new User());
            Assert.Equal(LoginValidationResult.EmptyPassword, result);
        }

        [Fact]
        public void Validate_WhenNicknameTooShort_ShouldReturnNicknameTooShort()
        {
            var result = LoginValidator.ValidateLoginAttempt("Bob", "pass", new User());
            Assert.Equal(LoginValidationResult.NicknameTooShort, result);
        }

        [Fact]
        public void Validate_WhenUserIsNull_ShouldReturnUserNotFound()
        {
            var result = LoginValidator.ValidateLoginAttempt("ValidUser", "pass", null);
            Assert.Equal(LoginValidationResult.UserNotFound, result);
        }

        [Fact]
        public void Validate_WhenUserIsLocked_ShouldReturnAccountLocked()
        {
            var lockedUser = new UserBuilder().Locked().Build();
            var result = LoginValidator.ValidateLoginAttempt("ValidUser", "pass", lockedUser);

            Assert.Equal(LoginValidationResult.AccountLocked, result);
        }

        [Fact]
        public void Validate_WhenPasswordIncorrect_ShouldReturnIncorrectPassword()
        {
            var user = new UserBuilder().WithPassword("PasswordSeguro123").Build();

            var result = LoginValidator.ValidateLoginAttempt("ValidUser", "WrongPass", user);

            Assert.Equal(LoginValidationResult.IncorrectPassword, result);
        }

        [Fact]
        public void Validate_WhenCredentialsCorrect_ShouldReturnSuccess()
        {
            string pass = "MySecretPass";
            var user = new UserBuilder().WithPassword(pass).Build();

            var result = LoginValidator.ValidateLoginAttempt("ValidUser", pass, user);

            Assert.Equal(LoginValidationResult.Success, result);
        }
    }
}