using BusinessLogic.Logic;
using DataAccess;
using System;
using System.Data.Entity;
using System.Threading.Tasks;

namespace BusinessLogic.Handlers
{
    public class AuthenticationHandler
    {
        public async Task<User> LoginUser(string userName, string password)
        {
            using (var context = new base_pruebaEntities())
            {
                var foundUser = await context.User.FirstOrDefaultAsync(u => u.nickname == userName);

                if (foundUser == null)
                {
                    return null;
                }

                if (!PasswordHasher.VerifyPasswordHash(password, foundUser.passwordHash, foundUser.passwordSalt))
                {
                    return null;
                }

                return foundUser;
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
                    Console.WriteLine($"User {userToLogout.nickname} has logged out successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during logout: {ex.Message}");
            }
        }
    }
}