using BusinessLogic.Validation;
using Contracts.Faults;
using DataAccess;
using log4net;
using System;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using BusinessLogic.Exceptions;

namespace BusinessLogic.Handlers
{
    public class GuestHandler
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(GuestHandler));
        private static int _guestIdCounter = 0;

        public async Task<User> LoginGuest(string nickname)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                _logger.Info($"[LoginGuest] Procesando solicitud para: {nickname}");

                var validationResult = RegistrationValidator.ValidateGuestNickname(nickname);

                if (validationResult != RegistrationValidationResult.Success)
                {
                    ThrowGuestValidationException(validationResult);
                }

                int guestId = Interlocked.Decrement(ref _guestIdCounter);

                User guestUser = new User
                {
                    id_user = guestId,
                    nickname = nickname,
                    email = "guest@temp.com",
                    passwordHash = new byte[0],
                    passwordSalt = new byte[0],
                    status = "Online",
                    isLocked = false,
                    registration_date = DateTime.UtcNow,
                    id_avatar = 1,
                    first_name = "Guest",
                    paternal_last_name = "",
                    maternal_last_name = "",
                    score = 0,
                    failedLoginAttempts = 0
                };

                _logger.Info($"[LoginGuest] Invitado creado exitosamente: ID {guestId}");
                return guestUser;

            }, "LoginGuest");
        }

        private void ThrowGuestValidationException(RegistrationValidationResult result)
        {
            switch (result)
            {
                case RegistrationValidationResult.EmptyNickname:
                    throw new EmptyNicknameException("El nickname es obligatorio.");

                case RegistrationValidationResult.InvalidNicknameLength:
                    throw new InvalidNicknameLengthException("El nickname debe tener entre 4 y 20 caracteres.");

                case RegistrationValidationResult.InvalidNicknameFormat:
                    throw new InvalidNicknameFormatException("El nickname contiene caracteres inválidos.");

                default:
                    throw new ArgumentException("El nickname no es válido.");
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
                case EmptyNicknameException _:
                    errorCode = "AUTH_EMPTY_NICKNAME";
                    clientMessage = ex.Message;
                    _logger.Warn($"[{operationName}] Validación: Nickname vacío.");
                    break;

                case InvalidNicknameLengthException _:
                    errorCode = "AUTH_INVALID_LENGTH";
                    clientMessage = ex.Message;
                    _logger.Warn($"[{operationName}] Validación: Longitud incorrecta.");
                    break;

                case InvalidNicknameFormatException _:
                    errorCode = "AUTH_INVALID_FORMAT";
                    clientMessage = ex.Message;
                    _logger.Warn($"[{operationName}] Validación: Formato inválido.");
                    break;

                case ArgumentException _:
                    errorCode = "AUTH_BAD_REQUEST";
                    clientMessage = ex.Message;
                    _logger.Warn($"[{operationName}] Argumento inválido: {ex.Message}");
                    break;

                default:
                    errorCode = "AUTH_INTERNAL_500";
                    clientMessage = "Ocurrió un error inesperado al iniciar como invitado.";
                    _logger.Fatal($"[{operationName}] Error crítico: {ex}", ex);
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