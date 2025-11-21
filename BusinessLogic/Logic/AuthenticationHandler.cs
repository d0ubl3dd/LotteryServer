using BusinessLogic.Validation;
using Contracts.Faults;
using DataAccess;
using DataAccess.DAOs;
using log4net;
using System;
using System.ServiceModel;
using System.Threading.Tasks;

namespace BusinessLogic.Handlers
{
    public class AuthenticationHandler
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(AuthenticationHandler));
        private readonly IUserDAO userDAO;

        public AuthenticationHandler()
        {
            userDAO = new UserDAO();
        }

        public async Task<User> LoginUser(string userName, string password)
        {
            _logger.Info($"Intento de login para el usuario: {userName}");

            User foundUser = await userDAO.GetUserByNicknameAsync(userName);

            var validationResult = LoginValidator.ValidateLoginAttempt(userName, password, foundUser);

            if (validationResult == LoginValidationResult.Success)
            {
                _logger.Info($"Login exitoso para el usuario: {userName}");

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
                _logger.Info($"Login fallido para el usuario: {userName}. Motivo: {validationResult}");

                if (foundUser != null)
                {
                    var userToUpdate = await userDAO.GetUserByIdAsync(foundUser.id_user);
                    if (userToUpdate != null)
                    {
                        userToUpdate.failedLoginAttempts++;

                        if (userToUpdate.failedLoginAttempts >= 5)
                        {
                            userToUpdate.isLocked = true;
                            _logger.Warn($"La cuenta del usuario {userName} ha sido BLOQUEADA por demasiados intentos fallidos.");
                        }

                        try
                        {
                            await userDAO.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            _logger.Fatal($"Error FATAL guardando cambios en intentos fallidos del usuario {userName}.", ex);
                        }
                    }
                }

                string errorMessage = GetErrorMessage(validationResult);
                throw new FaultException<ServiceFault>(
                    new ServiceFault
                    {
                        Message = errorMessage
                    },
                    new FaultReason(errorMessage)
                );
            }
        }

        private string GetErrorMessage(LoginValidationResult result)
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
            if (userToLogout == null)
            {
                _logger.Warn("Se intentó cerrar sesión pero el parámetro userToLogout es null.");
                return;
            }

            try
            {
                var userInDb = await userDAO.GetUserByIdAsync(userToLogout.id_user);
                if (userInDb != null)
                {
                    userInDb.status = "Offline";
                    await userDAO.SaveChangesAsync();

                    _logger.Info($"Usuario {userInDb.nickname} cerró sesión correctamente.");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error al actualizar el estado del usuario {userToLogout.nickname} durante el logout.", ex);
            }
        }
    }
}
