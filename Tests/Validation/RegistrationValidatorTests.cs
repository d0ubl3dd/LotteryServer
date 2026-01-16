using Xunit;
using BusinessLogic.Validation;
using DataAccess;

namespace Tests.Validation
{
    public class RegistrationValidatorTests
    {
        private const string VALID_PASS = "ValidPass1!";
        private const string VALID_EMAIL = "test@test.com";

        private User CreateValidUser()
        {
            return new User
            {
                nickname = "ValidNick",
                email = VALID_EMAIL,
                first_name = "ValidName",
                paternal_last_name = "ValidLast"
            };
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_NicknameEmpty_ShouldReturnEmptyNickname(string nickname)
        {
            var user = CreateValidUser();
            user.nickname = nickname;
            var result = RegistrationValidator.Validate(user, VALID_PASS, false, false);
            Assert.Equal(RegistrationValidationResult.EmptyNickname, result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_EmailEmpty_ShouldReturnEmptyEmail(string email)
        {
            var user = CreateValidUser();
            user.email = email;
            var result = RegistrationValidator.Validate(user, VALID_PASS, false, false);
            Assert.Equal(RegistrationValidationResult.EmptyEmail, result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_PasswordEmpty_ShouldReturnEmptyPassword(string password)
        {
            var result = RegistrationValidator.Validate(CreateValidUser(), password, false, false);
            Assert.Equal(RegistrationValidationResult.EmptyPassword, result);
        }

        [Theory]
        [InlineData("", "Valid")]
        [InlineData("Valid", "")]
        [InlineData("   ", "Valid")]
        [InlineData("Valid", "   ")]
        public void Validate_NameEmpty_ShouldReturnEmptyName(string first, string last)
        {
            var user = CreateValidUser();
            user.first_name = first;
            user.paternal_last_name = last;
            var result = RegistrationValidator.Validate(user, VALID_PASS, false, false);
            Assert.Equal(RegistrationValidationResult.EmptyName, result);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("ab")]
        [InlineData("a")]
        public void Validate_NicknameTooShort_ShouldReturnInvalidLength(string nickname)
        {
            var user = CreateValidUser();
            user.nickname = nickname;
            var result = RegistrationValidator.Validate(user, VALID_PASS, false, false);
            Assert.Equal(RegistrationValidationResult.InvalidNicknameLength, result);
        }

        [Theory]
        [InlineData("UserWithTwentyOneCharsX")]
        [InlineData("ThisIsWayTooLongForANickname")]
        public void Validate_NicknameTooLong_ShouldReturnInvalidLength(string nickname)
        {
            var user = CreateValidUser();
            user.nickname = nickname;
            var result = RegistrationValidator.Validate(user, VALID_PASS, false, false);
            Assert.Equal(RegistrationValidationResult.InvalidNicknameLength, result);
        }

        [Theory]
        [InlineData("Nick$Name")]
        [InlineData("Nick Name")]
        [InlineData("Nick#Name")]
        [InlineData("Nick/Name")]
        public void Validate_NicknameFormat_ShouldReturnInvalidFormat(string nickname)
        {
            var user = CreateValidUser();
            user.nickname = nickname;
            var result = RegistrationValidator.Validate(user, VALID_PASS, false, false);
            Assert.Equal(RegistrationValidationResult.InvalidNicknameFormat, result);
        }

        [Theory]
        [InlineData("plainaddress")]
        [InlineData("#@%^%#$@#$@#.com")]
        [InlineData("@example.com")]
        [InlineData("Joe Smith <email@example.com>")]
        [InlineData("email.example.com")]
        [InlineData("email@example@example.com")]
        public void Validate_EmailFormat_ShouldReturnInvalidEmailFormat(string email)
        {
            var user = CreateValidUser();
            user.email = email;
            var result = RegistrationValidator.Validate(user, VALID_PASS, false, false);
            Assert.Equal(RegistrationValidationResult.InvalidEmailFormat, result);
        }

        [Fact]
        public void Validate_NameTooLong_ShouldReturnNameTooLong()
        {
            var user = CreateValidUser();
            user.first_name = new string('A', 31);
            var result = RegistrationValidator.Validate(user, VALID_PASS, false, false);
            Assert.Equal(RegistrationValidationResult.NameTooLong, result);
        }

        [Theory]
        [InlineData("Name123")]
        [InlineData("Name!")]
        [InlineData("Name@")]
        public void Validate_NameFormat_ShouldReturnInvalidNameFormat(string name)
        {
            var user = CreateValidUser();
            user.first_name = name;
            var result = RegistrationValidator.Validate(user, VALID_PASS, false, false);
            Assert.Equal(RegistrationValidationResult.InvalidNameFormat, result);
        }

        [Fact]
        public void Validate_PasswordTooShort_ShouldReturnPasswordTooShort()
        {
            var result = RegistrationValidator.Validate(CreateValidUser(), "Short1!", false, false);
            Assert.Equal(RegistrationValidationResult.PasswordTooShort, result);
        }

        [Fact]
        public void Validate_PasswordNoUpper_ShouldReturnPasswordNoUpperCase()
        {
            var result = RegistrationValidator.Validate(CreateValidUser(), "lower123!", false, false);
            Assert.Equal(RegistrationValidationResult.PasswordNoUpperCase, result);
        }

        [Fact]
        public void Validate_PasswordNoLower_ShouldReturnPasswordNoLowerCase()
        {
            var result = RegistrationValidator.Validate(CreateValidUser(), "UPPER123!", false, false);
            Assert.Equal(RegistrationValidationResult.PasswordNoLowerCase, result);
        }

        [Fact]
        public void Validate_PasswordNoDigit_ShouldReturnPasswordNoNumber()
        {
            var result = RegistrationValidator.Validate(CreateValidUser(), "NoNumber!", false, false);
            Assert.Equal(RegistrationValidationResult.PasswordNoNumber, result);
        }

        [Fact]
        public void Validate_PasswordNoSpecial_ShouldReturnPasswordNoSpecialCharacter()
        {
            var result = RegistrationValidator.Validate(CreateValidUser(), "NoSpecial1", false, false);
            Assert.Equal(RegistrationValidationResult.PasswordNoSpecialCharacter, result);
        }

        [Fact]
        public void Validate_NicknameExists_ShouldReturnNicknameAlreadyExists()
        {
            var result = RegistrationValidator.Validate(CreateValidUser(), VALID_PASS, true, false);
            Assert.Equal(RegistrationValidationResult.NicknameAlreadyExists, result);
        }

        [Fact]
        public void Validate_EmailExists_ShouldReturnEmailAlreadyExists()
        {
            var result = RegistrationValidator.Validate(CreateValidUser(), VALID_PASS, false, true);
            Assert.Equal(RegistrationValidationResult.EmailAlreadyExists, result);
        }

        [Fact]
        public void Validate_AllValid_ShouldReturnSuccess()
        {
            var result = RegistrationValidator.Validate(CreateValidUser(), VALID_PASS, false, false);
            Assert.Equal(RegistrationValidationResult.Success, result);
        }

        [Theory]
        [InlineData("ValidGuest")]
        [InlineData("Player1")]
        public void ValidateGuestNickname_Valid_ShouldReturnSuccess(string nick)
        {
            var result = RegistrationValidator.ValidateGuestNickname(nick);
            Assert.Equal(RegistrationValidationResult.Success, result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidateGuestNickname_Empty_ShouldReturnEmptyNickname(string nick)
        {
            var result = RegistrationValidator.ValidateGuestNickname(nick);
            Assert.Equal(RegistrationValidationResult.EmptyNickname, result);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("TooLongGuestName12345")]
        public void ValidateGuestNickname_InvalidLength_ShouldReturnInvalidLength(string nick)
        {
            var result = RegistrationValidator.ValidateGuestNickname(nick);
            Assert.Equal(RegistrationValidationResult.InvalidNicknameLength, result);
        }
    }
}