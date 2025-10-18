using BusinessLogic.Validation;
using DataAccess;
using System;
using System.Data.Entity;
using System.Threading.Tasks;

namespace BusinessLogic.Handlers
{
    public class AuthenticationHandler
    {
        private const int MaxFailedAttempts = 5;

        public async Task<User> LoginUser(string userName, string password)
        {
            User foundUser = null;
            try
            {
                using (var context = new base_pruebaEntities())
                {
                    foundUser = await context.User.FirstOrDefaultAsync(u => u.nickname == userName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error during login: {ex.Message}");
                return null;
            }

            var validationResult = LoginValidator.ValidateLoginAttempt(userName, password, foundUser);

            using (var context = new base_pruebaEntities())
            {
                var userToUpdate = foundUser != null ? await context.User.FindAsync(foundUser.id_user) : null;

                switch (validationResult)
                {
                    case LoginValidationResult.Success:
                        if (userToUpdate != null)
                        {
                            userToUpdate.failedLoginAttempts = 0;
                            userToUpdate.status = "Online";
                            userToUpdate.lastLoginDate = DateTime.UtcNow;
                            await context.SaveChangesAsync();
                        }
                        Console.WriteLine($"Login successful for: {userName}");
                        return userToUpdate;

                    case LoginValidationResult.IncorrectPassword:
                        if (userToUpdate != null)
                        {
                            userToUpdate.failedLoginAttempts++;
                            if (userToUpdate.failedLoginAttempts >= MaxFailedAttempts)
                            {
                                userToUpdate.isLocked = true;
                                Console.WriteLine($"Account for '{userName}' has been locked due to too many failed attempts.");
                            }
                            await context.SaveChangesAsync();
                        }
                        Console.WriteLine($"Login failed: Incorrect password for user '{userName}'.");
                        return null;

                    default:
                        Console.WriteLine($"Login failed for '{userName}' with reason: {validationResult}");
                        return null;
                }
            }
        }

        public async Task LogoutUser(User userToLogout)
        {
            if (userToLogout == null)
            {
                return;
            }

            Console.WriteLine($"User {userToLogout.nickname} has requested to log out.");
            try
            {
                using (var context = new base_pruebaEntities())
                {
                    var userInDb = await context.User.FindAsync(userToLogout.id_user);
                    if (userInDb != null)
                    {
                        userInDb.status = "Offline";
                        await context.SaveChangesAsync();
                        Console.WriteLine($"User {userToLogout.nickname} has logged out successfully.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during logout: {ex.Message}");
            }
        }
    }
}