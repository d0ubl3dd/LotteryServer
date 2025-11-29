using BusinessLogic.Logic;
using DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Validation
{
    public static class LoginValidator
    {
        private const int MIN_NICKNAME_LENGTH = 4;

        public static LoginValidationResult ValidateLoginAttempt(string userName, string password, User foundUser)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                return LoginValidationResult.EmptyUserName;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                return LoginValidationResult.EmptyPassword;
            }

            if (userName.Length < MIN_NICKNAME_LENGTH)
            {
                return LoginValidationResult.NicknameTooShort;
            }

            if (foundUser == null)
            {
                return LoginValidationResult.UserNotFound;
            }

            if (foundUser.isLocked == true)
            {
                return LoginValidationResult.AccountLocked;
            }

            if (!PasswordHasher.VerifyPasswordHash(password, foundUser.passwordHash, foundUser.passwordSalt))
            {
                return LoginValidationResult.IncorrectPassword;
            }

            return LoginValidationResult.Success;
        }
    }
}