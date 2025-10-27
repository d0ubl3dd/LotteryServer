using BusinessLogic.Validation;
using DataAccess;
using DataAccess.DAOs;
using System;
using System.Threading.Tasks;

namespace BusinessLogic.Handlers
{
    public class AuthenticationHandler
    {
        private readonly IUserDAO userDAO;

        public AuthenticationHandler()
        {
            userDAO = new UserDAO();
        }

        public async Task<User> LoginUser(string userName, string password)
        {
            User foundUser = await userDAO.GetUserByNicknameAsync(userName);

            var validationResult = LoginValidator.ValidateLoginAttempt(userName, password, foundUser);

            if (validationResult == LoginValidationResult.Success)
            {
                var userToUpdate = await userDAO.GetUserByIdAsync(foundUser.id_user);
                if (userToUpdate != null)
                {
                    userToUpdate.status = "Online";
                    userToUpdate.failedLoginAttempts = 0;
                    userToUpdate.lastLoginDate = DateTime.UtcNow;
                    await userDAO.SaveChangesAsync();
                }
                return userToUpdate;
            }
            else
            {
                if (foundUser != null)
                {
                    var userToUpdate = await userDAO.GetUserByIdAsync(foundUser.id_user);
                    if (userToUpdate != null)
                    {
                        userToUpdate.failedLoginAttempts++;
                        if (userToUpdate.failedLoginAttempts >= 5)
                        {
                            userToUpdate.isLocked = true;
                        }
                        await userDAO.SaveChangesAsync();
                    }
                }

                string errorMessage = ObtenerMensajeDeError(validationResult);

                throw new Exception(errorMessage);
            }
        }

        private string ObtenerMensajeDeError(LoginValidationResult result)
        {
            switch (result)
            {
                case LoginValidationResult.UserNotFound:
                    return "El usuario no existe.";

                case LoginValidationResult.IncorrectPassword:
                    return "Contraseña incorrecta.";

                case LoginValidationResult.AccountLocked:
                    return "Tu cuenta está bloqueada por demasiados intentos fallidos.";

                default:
                    return "Error de inicio de sesión desconocido.";
            }
        }


        public async Task LogoutUser(User userToLogout)
        {
            if (userToLogout == null) return;

            try
            {
                var userInDb = await userDAO.GetUserByIdAsync(userToLogout.id_user);
                if (userInDb != null)
                {
                    userInDb.status = "Offline";
                    await userDAO.SaveChangesAsync();
                    Console.WriteLine($"User {userInDb.nickname} has logged out successfully.");
                }
            }
            catch (Exception ex)
            { 
                Console.WriteLine($"[ERROR] Failed to update user status on logout: {ex.Message}");
            }
        }
    }
}