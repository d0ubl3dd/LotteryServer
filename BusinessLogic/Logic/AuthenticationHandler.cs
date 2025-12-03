using BusinessLogic.Exceptions;
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

                _logger.InfoFormat("[LoginUser] Intento de login para: {0}", userName);

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
            if (userToLogout == null)
            {
                throw new ArgumentNullException(nameof(userToLogout), "No se puede cerrar sesión de un usuario nulo.");
            }

            await ExecuteFaultSafeAsync(async () =>
            {
                _logger.InfoFormat("[LogoutUser] Cerrando sesión para {0}.", userToLogout.nickname);

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
                    _logger.InfoFormat("[LogoutUser] Usuario invitado {0} desconectado (limpieza en memoria).", userToLogout.nickname);
                }

                _logger.Info("[LogoutUser] Sesión cerrada correctamente.");

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

                _logger.InfoFormat("[LoginUser] Login exitoso y estado actualizado para: {0}", foundUser.nickname);
            }
        }

        private async Task HandleFailedLogin(User foundUser, LoginValidationResult reason)
        {
            _logger.InfoFormat("[LoginUser] Login fallido para {0}. Motivo: {1}",
                foundUser?.nickname ?? "Desconocido",
                reason);

            if (foundUser != null)
            {
                var userToUpdate = await _userDAO.GetUserByIdAsync(foundUser.id_user);
                if (userToUpdate != null)
                {
                    userToUpdate.failedLoginAttempts++;

                    if (userToUpdate.failedLoginAttempts >= 5)
                    {
                        userToUpdate.isLocked = true;
                        _logger.WarnFormat("[LoginUser] La cuenta de {0} ha sido BLOQUEADA.", userToUpdate.nickname);
                    }

                    await _userDAO.SaveChangesAsync();
                }
            }
        }

        private static void ThrowLoginException(LoginValidationResult result)
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