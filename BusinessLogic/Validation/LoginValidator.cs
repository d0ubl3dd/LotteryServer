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
        private const int MinInputLength = 4;

        /// <summary>
        /// Performs a series of validations on a login attempt.
        /// </summary>
        /// <param name="userName">The username provided by the user.</param>
        /// <param name="password">The password provided by the user.</param>
        /// <param name="foundUser">The user entity retrieved from the database.</param>
        /// <returns>A LoginValidationResult enum indicating the outcome of the validation.</returns>
        public static LoginValidationResult ValidateLoginAttempt(string userName, string password, User foundUser)
        {
            // Validation 1: Check for null, empty, or short inputs.
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password) || userName.Length < MinInputLength)
            {
                return LoginValidationResult.InvalidInput;
            }

            // Validation 2: Check if the user exists in the database.
            if (foundUser == null)
            {
                return LoginValidationResult.UserNotFound;
            }

            // Validation 3: Check if the account is banned.
            // This assumes your 'User' entity has an 'isBanned' property of type bool.
            using (var context = new base_pruebaEntities())
            {
                // Check if there is any active ban for this user
                bool isCurrentlyBanned = context.Banned.Any(b => b.id_user == foundUser.id_user && b.unbanned_at == null);
                if (isCurrentlyBanned)
                {
                    return LoginValidationResult.AccountBanned;
                }
            }

            // Validation 4: Check if the account is locked (e.g., due to too many failed attempts).
            // This assumes your 'User' entity has an 'isLocked' property of type bool.
            if (foundUser.isLocked == true)
            {
                return LoginValidationResult.AccountLocked;
            }

            // Validation 5: Verify the password hash.
            // This is the final and most resource-intensive check.
            if (!PasswordHasher.VerifyPasswordHash(password, foundUser.passwordHash, foundUser.passwordSalt))
            {
                // The action of incrementing failed attempts should be handled in AuthenticationHandler,
                // after this method returns IncorrectPassword.
                return LoginValidationResult.IncorrectPassword;
            }

            // If all validations pass, the login is successful.
            return LoginValidationResult.Success;
        }
    }
}