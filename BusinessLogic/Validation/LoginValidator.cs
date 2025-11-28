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
        private const int MIN_INPUT_LENGTH = 4;

        /// <summary>
        /// </summary>
        /// <param name="userName">The username provided by the user.</param>
        /// <param name="password">The password provided by the user.</param>
        /// <param name="foundUser">The user entity retrieved from the database.</param>
        /// <returns>A LoginValidationResult enum indicating the outcome of the validation.</returns>
        public static LoginValidationResult ValidateLoginAttempt(string userName, string password, User foundUser, bool isUserBanned)
        {
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password) || userName.Length < MIN_INPUT_LENGTH)
            {
                return LoginValidationResult.InvalidInput;
            }

            if (foundUser == null)
            {
                return LoginValidationResult.UserNotFound;
            }

            if (isUserBanned)
            {
                return LoginValidationResult.AccountBanned;
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