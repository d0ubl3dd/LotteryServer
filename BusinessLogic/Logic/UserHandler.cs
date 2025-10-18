using BusinessLogic.Logic;
using Contracts.DTOs;
using DataAccess;
using System;
using System.Data.Entity;
using System.Threading.Tasks;

namespace BusinessLogic.Handlers
{
    public class UserHandler
    {
        public async Task<int> RegisterUser(UserRegisterDTO userData)
        {
            // Basic validation for new user data
            if (string.IsNullOrEmpty(userData.Username) || string.IsNullOrEmpty(userData.Password))
            {
                return 0; // Indicates failure
            }

            PasswordHasher.CreatePasswordHash(userData.Password, out byte[] passwordHash, out byte[] passwordSalt);

            var newUser = new User
            {
                nickname = userData.Username,
                email = userData.Email,
                registration_date = DateTime.UtcNow,
                first_name = userData.FirstName,
                paternal_last_name = userData.PaternalLastName,
                maternal_last_name = userData.MaternalLastName,
                passwordHash = passwordHash,
                passwordSalt = passwordSalt,
                isLocked = false,
                failedLoginAttempts = 0,
                status = "Offline",
                lastLoginDate = DateTime.UtcNow
            };

            try
            {
                using (var context = new base_pruebaEntities())
                {
                    context.User.Add(newUser);
                    await context.SaveChangesAsync();
                    return newUser.id_user;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during user registration: {ex.Message}");
                return 0;
            }
        }

        public async Task<bool> ChangePassword(User currentUser, string oldPassword, string newPassword)
        {
            try
            {
                using (var context = new base_pruebaEntities())
                {
                    var userInDb = await context.User.FindAsync(currentUser.id_user);
                    if (userInDb == null) return false;

                    // Verify the old password first
                    if (!PasswordHasher.VerifyPasswordHash(oldPassword, userInDb.passwordHash, userInDb.passwordSalt))
                    {
                        return false; // Old password does not match
                    }

                    // Create hash for the new password
                    PasswordHasher.CreatePasswordHash(newPassword, out byte[] passwordHash, out byte[] passwordSalt);

                    userInDb.passwordHash = passwordHash;
                    userInDb.passwordSalt = passwordSalt;

                    await context.SaveChangesAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error changing password for {currentUser.nickname}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateProfile(User currentUser, UserProfileDTO profileData)
        {
            try
            {
                using (var context = new base_pruebaEntities())
                {
                    var userInDb = await context.User.FindAsync(currentUser.id_user);
                    if (userInDb == null) return false;

                    // Update properties from the DTO
                    userInDb.email = profileData.Email;
                    // Add any other properties from UserProfileDTO you want to update

                    await context.SaveChangesAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating profile for {currentUser.nickname}: {ex.Message}");
                return false;
            }
        }

        public Task<int> RegisterGuest()
        {
            Console.WriteLine("RegisterGuest handled by UserHandler.");
            // This would create a temporary user in the database
            return Task.FromResult(-1);
        }

        public Task RecoverPassword(string email)
        {
            Console.WriteLine("RecoverPassword handled by UserHandler.");
            // This would involve generating a token and sending an email, which is more complex.
            return Task.CompletedTask;
        }
    }
}