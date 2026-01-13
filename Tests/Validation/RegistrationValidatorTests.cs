using Xunit;
using BusinessLogic.Validation;
using DataAccess;
using Tests.Builders;

namespace Tests.Validation
{
    public class RegistrationValidatorTests
    {
        private User CreateValidUser()
        {
            return new User
            {
                nickname = "ValidNick",
                email = "valid@email.com",
                first_name = "Juan",
                paternal_last_name = "Perez",
                maternal_last_name = "Lopez"
            };
        }

        private const string VALID_PASS = "Password123!";

        [Fact]
        public void Validate_WhenNicknameEmpty_ShouldReturnEmptyNickname()
        {
            var user = CreateValidUser();
            user.nickname = "";
            var result = RegistrationValidator.Validate(user, VALID_PASS, false, false);
            Assert.Equal(RegistrationValidationResult.EmptyNickname, result);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("thisnicknameiswaytoolongtobevalid")]
        public void Validate_WhenNicknameLengthInvalid_ShouldReturnInvalidLength(string badNick)
        {
            var user = CreateValidUser();
            user.nickname = badNick;
            var result = RegistrationValidator.Validate(user, VALID_PASS, false, false);
            Assert.Equal(RegistrationValidationResult.InvalidNicknameLength, result);
        }

        [Theory]
        [InlineData("Nick$Name")]
        [InlineData("Nick Name")]
        public void Validate_WhenNicknameFormatInvalid_ShouldReturnInvalidFormat(string badNick)
        {
            var user = CreateValidUser();
            user.nickname = badNick;
            var result = RegistrationValidator.Validate(user, VALID_PASS, false, false);
            Assert.Equal(RegistrationValidationResult.InvalidNicknameFormat, result);
        }

        [Theory]
        [InlineData("invalid-email")]
        [InlineData("user@domain")]
        [InlineData("@domain.com")]
        public void Validate_WhenEmailInvalid_ShouldReturnInvalidEmailFormat(string badEmail)
        {
            var user = CreateValidUser();
            user.email = badEmail;
            var result = RegistrationValidator.Validate(user, VALID_PASS, false, false);
            Assert.Equal(RegistrationValidationResult.InvalidEmailFormat, result);
        }

        [Fact]
        public void Validate_WhenPasswordTooShort_ShouldReturnPasswordTooShort()
        {
            var result = RegistrationValidator.Validate(CreateValidUser(), "Pass1!", false, false);
            Assert.Equal(RegistrationValidationResult.PasswordTooShort, result);
        }

        [Fact]
        public void Validate_WhenPasswordNoUpper_ShouldReturnNoUpperCase()
        {
            var result = RegistrationValidator.Validate(CreateValidUser(), "password123!", false, false);
            Assert.Equal(RegistrationValidationResult.PasswordNoUpperCase, result);
        }

        [Fact]
        public void Validate_WhenPasswordNoLower_ShouldReturnNoLowerCase()
        {
            var result = RegistrationValidator.Validate(CreateValidUser(), "PASSWORD123!", false, false);
            Assert.Equal(RegistrationValidationResult.PasswordNoLowerCase, result);
        }

        [Fact]
        public void Validate_WhenPasswordNoDigit_ShouldReturnNoNumber()
        {
            var result = RegistrationValidator.Validate(CreateValidUser(), "Password!", false, false);
            Assert.Equal(RegistrationValidationResult.PasswordNoNumber, result);
        }

        [Fact]
        public void Validate_WhenPasswordNoSpecial_ShouldReturnNoSpecialChar()
        {
            var result = RegistrationValidator.Validate(CreateValidUser(), "Password123", false, false);
            Assert.Equal(RegistrationValidationResult.PasswordNoSpecialCharacter, result);
        }

        [Fact]
        public void Validate_WhenNicknameExists_ShouldReturnNicknameAlreadyExists()
        {
            var result = RegistrationValidator.Validate(CreateValidUser(), VALID_PASS, nicknameExists: true, emailExists: false);
            Assert.Equal(RegistrationValidationResult.NicknameAlreadyExists, result);
        }

        [Fact]
        public void Validate_WhenEmailExists_ShouldReturnEmailAlreadyExists()
        {
            var result = RegistrationValidator.Validate(CreateValidUser(), VALID_PASS, nicknameExists: false, emailExists: true);
            Assert.Equal(RegistrationValidationResult.EmailAlreadyExists, result);
        }

        [Fact]
        public void Validate_WhenEverythingValid_ShouldReturnSuccess()
        {
            var result = RegistrationValidator.Validate(CreateValidUser(), VALID_PASS, false, false);
            Assert.Equal(RegistrationValidationResult.Success, result);
        }

        [Fact]
        public void ValidateGuest_WhenValid_ShouldReturnSuccess()
        {
            var result = RegistrationValidator.ValidateGuestNickname("GuestUser");
            Assert.Equal(RegistrationValidationResult.Success, result);
        }

        [Fact]
        public void ValidateGuest_WhenEmpty_ShouldReturnEmptyNickname()
        {
            var result = RegistrationValidator.ValidateGuestNickname("");
            Assert.Equal(RegistrationValidationResult.EmptyNickname, result);
        }
    }
}