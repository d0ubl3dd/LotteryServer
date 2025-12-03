using BusinessLogic.Exceptions;
using BusinessLogic.Logic;
using BusinessLogic.Logic.Base;
using BusinessLogic.Validation;
using DataAccess;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessLogic.Handlers
{
    public class GuestHandler : BaseHandler
    {
        private static int _guestIdCounter = 0;

        public GuestHandler() : base(typeof(GuestHandler))
        {
        }

        public async Task<User> LoginGuest(string nickname)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                User guestUser = null;

                _logger.InfoFormat("[LoginGuest] Procesando solicitud para: {0}", nickname);

                var validationResult = RegistrationValidator.ValidateGuestNickname(nickname);

                if (validationResult != RegistrationValidationResult.Success)
                {
                    ThrowGuestValidationException(validationResult);
                }
                else
                {
                    int guestId = Interlocked.Decrement(ref _guestIdCounter);

                    guestUser = new User
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

                    _logger.InfoFormat("[LoginGuest] Invitado creado exitosamente: ID {0}", guestId);
                }

                await Task.CompletedTask;

                return guestUser;

            }, "LoginGuest");
        }

        private static void ThrowGuestValidationException(RegistrationValidationResult result)
        {
            Exception exceptionToThrow;

            switch (result)
            {
                case RegistrationValidationResult.EmptyNickname:
                    exceptionToThrow = new EmptyNicknameException("El nickname es obligatorio.");
                    break;

                case RegistrationValidationResult.InvalidNicknameLength:
                    exceptionToThrow = new InvalidNicknameLengthException("El nickname debe tener entre 4 y 20 caracteres.");
                    break;

                case RegistrationValidationResult.InvalidNicknameFormat:
                    exceptionToThrow = new InvalidNicknameFormatException("El nickname contiene caracteres inválidos.");
                    break;

                default:
                    exceptionToThrow = new ArgumentException("El nickname no es válido.");
                    break;
            }

            throw exceptionToThrow;
        }
    }
}