using BusinessLogic.Logic;
using Contracts.DTOs;
using DataAccess;
using System;
using System.Linq;
using System.Data.Entity;
using System.Threading.Tasks;
using Contracts.Services.Users;
using Microsoft.SqlServer.Server;
using DataAccess.DAOs;

namespace BusinessLogic.Handlers
{
    public partial class UserHandler
    {
        private readonly IUserDAO _userRepository;
        private readonly VerificationHandler _verificationHandler;

        public UserHandler()
        {
            _userRepository = new UserDAO();
            _verificationHandler = new VerificationHandler();
        }
        public async Task<int> RequestUserVerification(UserDto userData)
        {
            if (string.IsNullOrEmpty(userData.Email) || string.IsNullOrEmpty(userData.Nickname) || 
                string.IsNullOrEmpty(userData.Password))
            {
                return -3;
            }
            if (await _userRepository.NicknameExistsAsync(userData.Nickname))
            {
                Console.WriteLine("Registration failed: Nickname already exists.");
                return -1;
            }

            if (await _userRepository.EmailExistsAsync(userData.Email))
            {
                Console.WriteLine("Registration failed: Email already exists.");
                return -2;
            }
            bool codeSent = await _verificationHandler.SendVerificationCode(userData.Email);
            if (!codeSent)
            {
                Console.WriteLine($"No se pudo enviar el código de verificación a {userData.Email}");
            }
            return codeSent ? 1 : 0;
        }

        public async Task<int> RegisterUser(UserDto userData)
        {
            try
            {
                PasswordHasher.CreatePasswordHash(userData.Password, out byte[] passwordHash, out byte[] passwordSalt);

                var newUser = new User
                {
                    nickname = userData.Nickname,
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
                    id_avatar = 1
                };

                _userRepository.AddUser(newUser);
                await _userRepository.SaveChangesAsync();

                return newUser.id_user;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during user registration: {ex.Message}");
                return 0;
            }
        }

        public async Task<bool> VerifyPassword(int userId, string password)
        {
            try
            {
                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user == null) return false;

                return PasswordHasher.VerifyPasswordHash(password, user.passwordHash, user.passwordSalt);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verifying password for user ID {userId}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ChangePassword(int userId, string newPassword)
        {
            try
            {
                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user == null) return false;

                PasswordHasher.CreatePasswordHash(newPassword, out byte[] hash, out byte[] salt);
                user.passwordHash = hash;
                user.passwordSalt = salt;

                await _userRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error changing password for user ID {userId}: {ex.Message}");
                return false;
            }
        }

        public async Task<(bool Success, string Message)> UpdateProfile(int currentUserId, UserDto userData)
        {
            try
            {
                var userInDb = await _userRepository.GetUserByIdAsync(currentUserId);
                if (userInDb == null)
                    return (false, "Usuario no encontrado.");

                if (!string.Equals(userInDb.nickname, userData.Nickname, StringComparison.OrdinalIgnoreCase))
                {
                    if (await _userRepository.NicknameExistsAsync(userData.Nickname))
                        return (false, "El nickname ya está en uso.");
                }

                userInDb.first_name = userData.FirstName;
                userInDb.paternal_last_name = userData.PaternalLastName;
                userInDb.maternal_last_name = userData.MaternalLastName;
                userInDb.nickname = userData.Nickname;
                userInDb.id_avatar = userData.AvatarId;                

                await _userRepository.SaveChangesAsync();
                return (true, "Perfil actualizado correctamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating profile for user ID {currentUserId}: {ex.Message}");
                return (false, "Ocurrió un error al actualizar el perfil.");
            }
        }

        public Task<int> RegisterGuest()
        {
            Console.WriteLine("RegisterGuest handled by UserHandler.");
            return Task.FromResult(-1);
        }

        public Task RecoverPassword(string email)
        {
            Console.WriteLine("RecoverPassword handled by UserHandler.");
            return Task.CompletedTask;
        }

        public async Task<FriendDto> FindUserByNickname(string nickname)
        {
            using (var context = new lottery_databaseEntities())
            {
                var user = await context.User.Where(u => u.nickname == nickname).Select(u => new FriendDto
                {
                    UserId = u.id_user,
                    Nickname = u.nickname,
                    Status = u.status
                })
                .FirstOrDefaultAsync();

                return user;
            }
        }

        public async Task<UserDto> GetUserProfile(int userId)
        {
            using (var context = new lottery_databaseEntities())
            {
                var user = await context.User.Where(u => u.id_user == userId).Select(u => new UserDto
                {
                    UserId =  u.id_user,
                    Nickname = u.nickname,
                    Email = u.email,
                    FirstName = u.first_name,
                    PaternalLastName = u.paternal_last_name,
                    MaternalLastName = u.maternal_last_name,
                    AvatarId = u.id_avatar,
                    AvatarUrl = u.Avatar != null ? u.Avatar.path : null
                })
                .FirstOrDefaultAsync();
                return user;
            }
        }

        public async Task<bool> RequestEmailChange(int userId, string newEmail)
        {
            try
            {
                var userInDb = await _userRepository.GetUserByIdAsync(userId);
                if (userInDb == null)
                {
                    return false;
                }

                if (await _userRepository.EmailExistsAsync(newEmail))
                {
                    return false;
                }

                bool sent = await _verificationHandler.SendVerificationCode(newEmail);
                if (!sent)
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al solicitar cambio de correo para el usuario {userId}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ConfirmEmailChange(int userId, string newEmail, string verificationCode)
        {
            try
            {
                var userInDb = await _userRepository.GetUserByIdAsync(userId);
                if (userInDb == null)
                {
                    return false;
                }

                bool isValid = await _verificationHandler.VerifyCode(newEmail, verificationCode);
                if (!isValid)
                {
                    return false;
                }

                userInDb.email = newEmail;
                await _userRepository.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al confirmar cambio de correo para el usuario {userId}: {ex.Message}");
                return false;
            }
        }
    }
}