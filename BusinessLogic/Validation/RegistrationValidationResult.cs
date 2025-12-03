namespace BusinessLogic.Validation
{
    public enum RegistrationValidationResult
    {
        Success,
        EmptyNickname,
        EmptyEmail,
        EmptyPassword,
        EmptyName,

        InvalidNicknameLength,
        InvalidNicknameFormat,

        InvalidEmailFormat,

        NameTooLong,
        InvalidNameFormat,

        PasswordTooShort,
        PasswordNoUpperCase,
        PasswordNoLowerCase,
        PasswordNoNumber,
        PasswordNoSpecialCharacter,
        PasswordInvalidCharacters,

        NicknameAlreadyExists,
        EmailAlreadyExists
    }
}