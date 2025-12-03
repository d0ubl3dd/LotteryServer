using DataAccess;
using System.Text.RegularExpressions;
using System.Linq;

namespace BusinessLogic.Validation
{
    public static class RegistrationValidator
    {
        private const int MIN_NICKNAME_LENGTH = 4;
        private const int MAX_NICKNAME_LENGTH = 20;
        private const int MIN_PASSWORD_LENGTH = 8;
        private const int MAX_NAME_LENGTH = 30;

        private const string NICKNAME_PATTERN = @"^[a-zA-Z0-9._\-@]+$";
        private const string NAME_PATTERN = @"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s]+$";
        private const string PASSWORD_SPECIAL_CHARS = "!@#$%&*_-+=";
        private const string EMAIL_PATTERN = @"^[a-zA-Z0-9._\-]+@[a-zA-Z0-9._\-]+(\.[a-zA-Z]{2,})+$";
        private const string MATERNAL_NAME_PATTERN = @"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s]*$";
        private const string PASSWORD_PATTERN = @"^[a-zA-Z0-9!@#$%&*_\-+=]+$";

        public static RegistrationValidationResult Validate(User user, string password, bool nicknameExists, bool emailExists)
        {
            var requiredResult = ValidateRequiredFields(user, password);
            if (requiredResult != RegistrationValidationResult.Success)
            {
                return requiredResult;
            }

            var nicknameResult = ValidateNicknameRules(user.nickname);
            if (nicknameResult != RegistrationValidationResult.Success)
            {
                return nicknameResult;
            }

            var emailResult = ValidateEmailRules(user.email);
            if (emailResult != RegistrationValidationResult.Success)
            {
                return emailResult;
            }

            var nameResult = ValidateNameRules(user);
            if (nameResult != RegistrationValidationResult.Success)
            {
                return nameResult;
            }

            var passwordResult = ValidatePasswordRules(password);
            if (passwordResult != RegistrationValidationResult.Success)
            {
                return passwordResult;
            }

            return ValidateUniqueness(nicknameExists, emailExists);
        }

        public static RegistrationValidationResult ValidateGuestNickname(string nickname)
        {
            if (string.IsNullOrWhiteSpace(nickname))
            {
                return RegistrationValidationResult.EmptyNickname;
            }

            return ValidateNicknameRules(nickname);
        }

        private static RegistrationValidationResult ValidateRequiredFields(User user, string password)
        {
            if (string.IsNullOrWhiteSpace(user.nickname))
            {
                return RegistrationValidationResult.EmptyNickname;
            }

            if (string.IsNullOrWhiteSpace(user.email))
            {
                return RegistrationValidationResult.EmptyEmail;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                return RegistrationValidationResult.EmptyPassword;
            }

            if (string.IsNullOrWhiteSpace(user.first_name) || string.IsNullOrWhiteSpace(user.paternal_last_name))
            {
                return RegistrationValidationResult.EmptyName;
            }

            return RegistrationValidationResult.Success;
        }

        private static RegistrationValidationResult ValidateNicknameRules(string nickname)
        {
            if (nickname.Length < MIN_NICKNAME_LENGTH || nickname.Length > MAX_NICKNAME_LENGTH)
            {
                return RegistrationValidationResult.InvalidNicknameLength;
            }

            if (!Regex.IsMatch(nickname, NICKNAME_PATTERN))
            {
                return RegistrationValidationResult.InvalidNicknameFormat;
            }

            return RegistrationValidationResult.Success;
        }

        private static RegistrationValidationResult ValidateEmailRules(string email)
        {
            if (!Regex.IsMatch(email, EMAIL_PATTERN))
            {
                return RegistrationValidationResult.InvalidEmailFormat;
            }

            return RegistrationValidationResult.Success;
        }

        private static RegistrationValidationResult ValidateNameRules(User user)
        {
            if (user.first_name.Length > MAX_NAME_LENGTH || user.paternal_last_name.Length > MAX_NAME_LENGTH)
            {
                return RegistrationValidationResult.NameTooLong;
            }

            if (!Regex.IsMatch(user.first_name, NAME_PATTERN) || !Regex.IsMatch(user.paternal_last_name, NAME_PATTERN))
            {
                return RegistrationValidationResult.InvalidNameFormat;
            }

            if (!string.IsNullOrEmpty(user.maternal_last_name))
            {
                if (user.maternal_last_name.Length > MAX_NAME_LENGTH)
                {
                    return RegistrationValidationResult.NameTooLong;
                }

                if (!Regex.IsMatch(user.maternal_last_name, MATERNAL_NAME_PATTERN))
                {
                    return RegistrationValidationResult.InvalidNameFormat;
                }
            }

            return RegistrationValidationResult.Success;
        }

        private static RegistrationValidationResult ValidatePasswordRules(string password)
        {
            if (password.Length < MIN_PASSWORD_LENGTH)
            {
                return RegistrationValidationResult.PasswordTooShort;
            }

            if (!password.Any(char.IsUpper))
            {
                return RegistrationValidationResult.PasswordNoUpperCase;
            }

            if (!password.Any(char.IsLower))
            {
                return RegistrationValidationResult.PasswordNoLowerCase;
            }

            if (!password.Any(char.IsDigit))
            {
                return RegistrationValidationResult.PasswordNoNumber;
            }

            if (!password.Any(ch => PASSWORD_SPECIAL_CHARS.Contains(ch)))
            {
                return RegistrationValidationResult.PasswordNoSpecialCharacter;
            }

            if (!Regex.IsMatch(password, PASSWORD_PATTERN))
            {
                return RegistrationValidationResult.PasswordInvalidCharacters;
            }

            return RegistrationValidationResult.Success;
        }

        private static RegistrationValidationResult ValidateUniqueness(bool nicknameExists, bool emailExists)
        {
            if (nicknameExists)
            {
                return RegistrationValidationResult.NicknameAlreadyExists;
            }

            if (emailExists)
            {
                return RegistrationValidationResult.EmailAlreadyExists;
            }

            return RegistrationValidationResult.Success;
        }
    }
}