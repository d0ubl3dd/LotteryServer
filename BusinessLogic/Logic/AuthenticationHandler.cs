using BusinessLogic.Exceptions;
using BusinessLogic.Logic;
using BusinessLogic.Logic.Base;
using BusinessLogic.Validation;
using DataAccess;
using DataAccess.DAOs;
using System;
using System.Threading.Tasks;

namespace BusinessLogic.Handlers
{
    public class AuthenticationHandler : BaseHandler
    {
        private readonly IUserDao _userDAO;

        public AuthenticationHandler(IUserDao userDAO) : base(typeof(AuthenticationHandler))
        {
            if (userDAO == null)
            {
                throw new ArgumentNullException(nameof(userDAO));
            }
            _userDAO = userDAO;
        }

        public async Task<User> LoginUser(string userName, string password)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                User userResult = null;

                _logger.Info($"[LoginUser] Intento de login para: {userName}");

                User foundUser = await _userDAO.GetUserByNicknameAsync(userName);

                var validationResult = LoginValidator.ValidateLoginAttempt(userName, password, foundUser);

                if (validationResult == LoginValidationResult.Success)
                {
                    await HandleSuccessfulLogin(foundUser);
                    userResult = foundUser;
                }
                else
                {
                    await HandleFailedLogin(foundUser, validationResult);
                    ThrowLoginException(validationResult);
                }

                return userResult;

            }, "LoginUser");
        }

        public async Task LogoutUser(User userToLogout)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                if (userToLogout == null)
                {
                    throw new ArgumentNullException(nameof(userToLogout), "No se puede cerrar sesión de un usuario nulo.");
                }

                _logger.Info($"[LogoutUser] Cerrando sesión para {userToLogout.nickname}.");

                if (userToLogout.id_user > 0)
                {
                    var userInDb = await _userDAO.GetUserByIdAsync(userToLogout.id_user);

                    if (userInDb == null)
                    {
                        throw new UserNotFoundException("El usuario a desconectar no se encuentra en la BD.");
                    }

                    userInDb.status = "Offline";
                    await _userDAO.SaveChangesAsync();
                }
                else
                {
                    _logger.Info($"[LogoutUser] Usuario invitado {userToLogout.nickname} desconectado (limpieza en memoria).");
                }

                _logger.Info($"[LogoutUser] Sesión cerrada correctamente.");

            }, "LogoutUser");
        }

        private async Task HandleSuccessfulLogin(User foundUser)
        {
            var userToUpdate = await _userDAO.GetUserByIdAsync(foundUser.id_user);
            if (userToUpdate != null)
            {
                userToUpdate.status = "Online";
                userToUpdate.failedLoginAttempts = 0;
                userToUpdate.lastLoginDate = DateTime.UtcNow;
                await _userDAO.SaveChangesAsync();
                _logger.Info($"[LoginUser] Login exitoso y estado actualizado para: {foundUser.nickname}");
            }
        }

        private async Task HandleFailedLogin(User foundUser, LoginValidationResult reason)
        {
            _logger.Info($"[LoginUser] Login fallido para {foundUser?.nickname ?? "Desconocido"}. Motivo: {reason}");

            if (foundUser != null)
            {
                var userToUpdate = await _userDAO.GetUserByIdAsync(foundUser.id_user);
                if (userToUpdate != null)
                {
                    userToUpdate.failedLoginAttempts++;

                    if (userToUpdate.failedLoginAttempts >= 5)
                    {
                        userToUpdate.isLocked = true;
                        _logger.Warn($"[LoginUser] La cuenta de {userToUpdate.nickname} ha sido BLOQUEADA.");
                    }

                    await _userDAO.SaveChangesAsync();
                }
            }
        }

        private void ThrowLoginException(LoginValidationResult result)
        {
            Exception exceptionToThrow;

            switch (result)
            {
                case LoginValidationResult.UserNotFound:
                    exceptionToThrow = new UserNotFoundException("El usuario no existe.");
                    break;

                case LoginValidationResult.IncorrectPassword:
                    exceptionToThrow = new IncorrectPasswordException("Contraseña incorrecta.");
                    break;

                case LoginValidationResult.AccountLocked:
                    exceptionToThrow = new AccountLockedException("Tu cuenta está bloqueada por demasiados intentos.");
                    break;

                default:
                    exceptionToThrow = new InvalidOperationException("Error de validación desconocido.");
                    break;
            }

            throw exceptionToThrow;
        }
    }
}