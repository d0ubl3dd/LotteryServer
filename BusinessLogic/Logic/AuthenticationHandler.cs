using BusinessLogic.Exceptions;
using Contracts.DTOs;
using Contracts.Faults;
using DataAccess;
using DataAccess.DAOs;
using BusinessLogic.Validation;
using log4net;
using System;
using System.ServiceModel;
using System.Threading.Tasks;

namespace BusinessLogic.Handlers
{
    public class AuthenticationHandler
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(AuthenticationHandler));
        private readonly IUserDao _userDAO;

        public AuthenticationHandler(IUserDao userDAO)
        {
            _userDAO = userDAO ?? throw new ArgumentNullException(nameof(userDAO));
        }

        public async Task<User> LoginUser(string userName, string password)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                _logger.Info($"[LoginUser] Intento de login para: {userName}");

                User foundUser = await _userDAO.GetUserByNicknameAsync(userName);

                var validationResult = LoginValidator.ValidateLoginAttempt(userName, password, foundUser);

                if (validationResult == LoginValidationResult.Success)
                {
                    await HandleSuccessfulLogin(foundUser);
                    return foundUser;
                }
                else
                {
                    await HandleFailedLogin(foundUser, validationResult);
                    ThrowLoginException(validationResult);
                    return null;
                }

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

                var userInDb = await _userDAO.GetUserByIdAsync(userToLogout.id_user);

                if (userInDb == null)
                {
                    throw new UserNotFoundException("El usuario a desconectar no se encuentra en la BD.");
                }

                userInDb.status = "Offline";
                await _userDAO.SaveChangesAsync();

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
            switch (result)
            {
                case LoginValidationResult.UserNotFound:
                    throw new UserNotFoundException("El usuario no existe.");

                case LoginValidationResult.IncorrectPassword:
                    throw new IncorrectPasswordException("Contraseña incorrecta.");

                case LoginValidationResult.AccountLocked:
                    throw new AccountLockedException("Tu cuenta está bloqueada por demasiados intentos.");

                default:
                    throw new InvalidOperationException("Error de validación desconocido.");
            }
        }

        private async Task ExecuteFaultSafeAsync(Func<Task> action, string operationName)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                HandleException(ex, operationName);
            }
        }

        private async Task<T> ExecuteFaultSafeAsync<T>(Func<Task<T>> action, string operationName)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                HandleException(ex, operationName);
                return default;
            }
        }

        private void HandleException(Exception ex, string operationName)
        {
            if (ex is FaultException<ServiceFault>)
                throw ex;

            string errorCode;
            string clientMessage;

            switch (ex)
            {
                case UserNotFoundException _:
                    errorCode = "AUTH_USER_NOT_FOUND";
                    clientMessage = "Credenciales inválidas.";
                    _logger.Warn($"[{operationName}] Usuario no encontrado.");
                    break;

                case IncorrectPasswordException _:
                    errorCode = "AUTH_INVALID_CREDENTIALS";
                    clientMessage = "Credenciales inválidas.";
                    _logger.Warn($"[{operationName}] Contraseña incorrecta.");
                    break;

                case AccountLockedException _:
                    errorCode = "AUTH_ACCOUNT_LOCKED";
                    clientMessage = ex.Message;
                    _logger.Warn($"[{operationName}] Cuenta bloqueada.");
                    break;

                case ArgumentNullException _:
                    errorCode = "AUTH_BAD_REQUEST";
                    clientMessage = "Datos de solicitud incompletos.";
                    _logger.Error($"[{operationName}] Argumento nulo: {ex.Message}");
                    break;

                case System.Data.Entity.Core.EntityException _:
                case System.Data.SqlClient.SqlException _:
                    errorCode = "AUTH_DB_ERROR";
                    clientMessage = "Error de conexión con la base de datos.";
                    _logger.Fatal($"[{operationName}] Error de BD: {ex}", ex);
                    break;

                default:
                    errorCode = "AUTH_INTERNAL_500";
                    clientMessage = "Ocurrió un error inesperado en el servidor.";
                    _logger.Fatal($"[{operationName}] Error no controlado: {ex}", ex);
                    break;
            }

            throw new FaultException<ServiceFault>(
                new ServiceFault
                {
                    ErrorCode = errorCode,
                    Message = clientMessage
                },
                new FaultReason(clientMessage)
            );
        }
    }
}