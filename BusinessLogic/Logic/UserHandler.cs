using BusinessLogic.Exceptions;
using BusinessLogic.Logic;
using BusinessLogic.Logic.Base;
using BusinessLogic.Validation;
using Contracts.DTOs;
using Contracts.Faults;
using Contracts.Services.Users;
using DataAccess;
using DataAccess.DAOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

namespace BusinessLogic.Handlers
{
    public partial class UserHandler : BaseHandler
    {
        private const string MANDATORY_FIELD_MESSAGE = "Todos los campos son obligatorios.";
        private const string DEFAULT_STATUS = "Offline";

        private readonly IUserDao _userRepository;
        private readonly IVerificationService _verificationHandler;

        private static readonly Dictionary<RegistrationValidationResult, Func<Exception>> _validationExceptions =
            new Dictionary<RegistrationValidationResult, Func<Exception>>
            {
                { RegistrationValidationResult.EmptyNickname, () => new ArgumentException(MANDATORY_FIELD_MESSAGE) },
                { RegistrationValidationResult.EmptyEmail, () => new ArgumentException(MANDATORY_FIELD_MESSAGE) },
                { RegistrationValidationResult.EmptyPassword, () => new ArgumentException(MANDATORY_FIELD_MESSAGE) },
                { RegistrationValidationResult.EmptyName, () => new ArgumentException(MANDATORY_FIELD_MESSAGE) },
                { RegistrationValidationResult.InvalidNicknameLength, () => new ArgumentException("El nickname debe tener al menos 4 caracteres.") },
                { RegistrationValidationResult.InvalidEmailFormat, () => new ArgumentException("El formato del correo electrónico no es válido.") },
                { RegistrationValidationResult.PasswordTooShort, () => new ArgumentException("La contraseña debe tener al menos 8 caracteres.") },
                { RegistrationValidationResult.PasswordNoUpperCase, () => new ArgumentException("La contraseña debe contener al menos una letra mayúscula.") },
                { RegistrationValidationResult.PasswordNoSpecialCharacter, () => new ArgumentException("La contraseña debe contener al menos un carácter especial.") },
                { RegistrationValidationResult.NicknameAlreadyExists, () => new UserAlreadyExistsException("El nombre de usuario ya está en uso.") },
                { RegistrationValidationResult.EmailAlreadyExists, () => new UserAlreadyExistsException("El correo electrónico ya está registrado.") }
            };

        public UserHandler(IUserDao userDao, IVerificationService verificationHandler) : base(typeof(UserHandler))
        {
            _userRepository = userDao ?? throw new ArgumentNullException(nameof(userDao));
            _verificationHandler = verificationHandler ?? throw new ArgumentNullException(nameof(verificationHandler));
        }

        public async Task<int> RequestUserVerification(UserDto userData)
        {
            if (userData == null)
            {
                throw new ArgumentNullException(nameof(userData));
            }

            return await ExecuteFaultSafeAsync(async () =>
            {
                _logger.InfoFormat("[RequestVerification] Procesando solicitud para {0}.", userData.Email);

                var tempUser = new User
                {
                    nickname = userData.Nickname,
                    email = userData.Email,
                    first_name = userData.FirstName,
                    paternal_last_name = userData.PaternalLastName
                };

                bool nickExists = await _userRepository.NicknameExistsAsync(userData.Nickname);
                bool emailExists = await _userRepository.EmailExistsAsync(userData.Email);

                var validationResult = RegistrationValidator.Validate(tempUser, userData.Password, nickExists, emailExists);

                if (validationResult != RegistrationValidationResult.Success)
                {
                    ThrowRegistrationException(validationResult);
                }

                bool codeSent = await _verificationHandler.SendVerificationCode(userData.Email);

                if (!codeSent)
                {
                    throw new VerificationException("No se pudo enviar el correo de verificación. Inténtalo más tarde.");
                }

                _logger.InfoFormat("[RequestVerification] Código enviado a {0}", userData.Email);

                return 1;

            }, "RequestUserVerification");
        }

        public async Task<int> RegisterUserWithCode(UserDto userData, string code)
        {
            if (userData == null)
            {
                throw new ArgumentNullException(nameof(userData));
            }

            return await ExecuteFaultSafeAsync(async () =>
            {
                bool isValidCode = await _verificationHandler.VerifyCode(userData.Email, code);
                if (!isValidCode)
                {
                    throw new VerificationException("El código de verificación es incorrecto o ha expirado.");
                }

                bool nickExists = await _userRepository.NicknameExistsAsync(userData.Nickname);
                bool emailExists = await _userRepository.EmailExistsAsync(userData.Email);

                var validationResult = RegistrationValidator.Validate(new User
                {
                    nickname = userData.Nickname,
                    email = userData.Email,
                    first_name = userData.FirstName,
                    paternal_last_name = userData.PaternalLastName
                }, userData.Password, nickExists, emailExists);

                if (validationResult != RegistrationValidationResult.Success)
                {
                    ThrowRegistrationException(validationResult);
                }

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
                    status = DEFAULT_STATUS,
                    id_avatar = 1
                };

                _userRepository.AddUser(newUser);
                await _userRepository.SaveChangesAsync();

                await _verificationHandler.ConsumeVerificationCode(userData.Email);

                _logger.InfoFormat("[RegisterUserWithCode] Registro exitoso ID: {0}", newUser.id_user);
                return newUser.id_user;

            }, "RegisterUserWithCode");
        }

        public async Task<bool> VerifyPassword(int userId, string password)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                var user = await GetUserOrThrow(userId);
                return PasswordHasher.VerifyPasswordHash(password, user.passwordHash, user.passwordSalt);

            }, "VerifyPassword");
        }

        public async Task<bool> ChangePassword(int userId, string newPassword)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                _logger.InfoFormat("[ChangePassword] Solicitud para ID {0}", userId);

                var user = await GetUserOrThrow(userId);

                PasswordHasher.CreatePasswordHash(newPassword, out byte[] hash, out byte[] salt);
                user.passwordHash = hash;
                user.passwordSalt = salt;

                await _userRepository.SaveChangesAsync();

                _logger.InfoFormat("[ChangePassword] Éxito para ID {0}", userId);
                return true;

            }, "ChangePassword");
        }

        public async Task<(bool Success, string Message)> UpdateProfile(int currentUserId, UserDto userData)
        {
            if (userData == null)
            {
                throw new ArgumentNullException(nameof(userData));
            }

            return await ExecuteFaultSafeAsync(async () =>
            {
                _logger.InfoFormat("[UpdateProfile] ID {0}", currentUserId);

                var userInDb = await GetUserOrThrow(currentUserId);

                if (!string.Equals(userInDb.nickname, userData.Nickname, StringComparison.OrdinalIgnoreCase) &&
                    await _userRepository.NicknameExistsAsync(userData.Nickname))
                {
                    throw new UserAlreadyExistsException($"El nickname '{userData.Nickname}' ya está ocupado.");
                }

                userInDb.first_name = userData.FirstName;
                userInDb.paternal_last_name = userData.PaternalLastName;
                userInDb.maternal_last_name = userData.MaternalLastName;
                userInDb.nickname = userData.Nickname;
                userInDb.id_avatar = userData.AvatarId;

                await _userRepository.SaveChangesAsync();

                _logger.InfoFormat("[UpdateProfile] Perfil actualizado para ID {0}", currentUserId);

                return (true, "Perfil actualizado correctamente.");

            }, "UpdateProfile");
        }

        public async Task<bool> RequestEmailChangeVerification(string newEmail)
        {
            if (string.IsNullOrWhiteSpace(newEmail))
            {
                throw new ArgumentException("Email vacío", nameof(newEmail));
            }

            return await ExecuteFaultSafeAsync(async () =>
            {
                bool emailExists = false;

                try
                {
                    emailExists = await _userRepository.EmailExistsAsync(newEmail);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error BD: {ex.Message}", ex);
                    throw new FaultException<ServiceFault>(
                        new ServiceFault { ErrorCode = "DB_ERROR", Message = "Error de base de datos" },
                        new FaultReason("Database Error"));
                }

                if (emailExists)
                {
                    ThrowRegistrationException(RegistrationValidationResult.EmailAlreadyExists);
                }

                bool codeSent = await _verificationHandler.SendVerificationCode(newEmail);

                if (!codeSent)
                {
                    throw new VerificationException("No se pudo enviar el código de verificación.");
                }

                return true;
            }, "RequestEmailChangeVerification");
        }

        public async Task<bool> ChangeEmailWithCodeAsync(int userId, string newEmail, string code)
        {
            if (string.IsNullOrWhiteSpace(newEmail))
            {
                throw new ArgumentException("El correo no puede estar vacío.", nameof(newEmail));
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException("El código de verificación no puede estar vacío.", nameof(code));
            }

            return await ExecuteFaultSafeAsync(async () =>
            {
                _logger.InfoFormat("[ChangeEmailWithCode] Usuario {0} intenta cambiar correo a {1}", userId, newEmail);

                var user = await GetUserOrThrow(userId);

                bool isValid = await _verificationHandler.VerifyCode(newEmail, code);
                if (!isValid)
                {
                    _logger.WarnFormat("[ChangeEmailWithCode] Código inválido para {0}", newEmail);
                    throw new VerificationException("El código de verificación es incorrecto o ha expirado.");
                }

                bool emailExists = await _userRepository.EmailExistsAsync(newEmail);
                if (emailExists)
                {
                    _logger.WarnFormat("[ChangeEmailWithCode] El correo {0} ya fue registrado por otro usuario", newEmail);
                    throw new UserAlreadyExistsException("El correo electrónico ya está registrado.");
                }

                user.email = newEmail;
                await _userRepository.SaveChangesAsync();

                await _verificationHandler.ConsumeVerificationCode(newEmail);

                _logger.InfoFormat("[ChangeEmailWithCode] Correo actualizado correctamente para ID {0}", userId);

                return true;

            }, "ChangeEmailWithCode");
        }

        public async Task<bool> RecoverPasswordRequest(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("El correo electrónico no puede estar vacío.", nameof(email));
            }

            return await ExecuteFaultSafeAsync(async () =>
            {
                _logger.InfoFormat("[RecoverPasswordRequest] Procesando solicitud para el correo: {0}", email);

                bool emailExists = await _userRepository.EmailExistsAsync(email);

                if (!emailExists)
                {
                    _logger.WarnFormat("[RecoverPasswordRequest] El correo {0} no está registrado.", email);
                    throw new UserNotFoundException("No existe ninguna cuenta asociada a este correo electrónico.");
                }

                bool codeSent = await _verificationHandler.SendVerificationCode(email);

                if (!codeSent)
                {
                    _logger.ErrorFormat("[RecoverPasswordRequest] Error al enviar correo a {0}.", email);
                    throw new VerificationException("No se pudo enviar el código de recuperación. Inténtalo más tarde.");
                }

                _logger.InfoFormat("[RecoverPasswordRequest] Código de recuperación enviado con éxito a {0}.", email);

                return true;

            }, "RecoverPasswordRequest");
        }

        public async Task<int> RegisterGuest()
        {
            return await ExecuteFaultSafeAsync(() =>
            {
                _logger.Info("[RegisterGuest] Registrando invitado.");
                return Task.FromResult(-1);

            }, "RegisterGuest");
        }

        public async Task<bool> RecoverPassword(string email, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException(MANDATORY_FIELD_MESSAGE, nameof(email));
            }

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                throw new ArgumentException(MANDATORY_FIELD_MESSAGE, nameof(newPassword));
            }

            return await ExecuteFaultSafeAsync(async () =>
            {
                _logger.InfoFormat("[ResetPasswordByEmail] Solicitud de cambio para el correo: {0}", email);
                var user = await _userRepository.GetUserByEmailAsync(email);

                if (user == null)
                {
                    _logger.WarnFormat("[ResetPasswordByEmail] No se encontró usuario con el correo: {0}", email);
                    throw new UserNotFoundException("No existe ninguna cuenta asociada a este correo electrónico.");
                }

                PasswordHasher.CreatePasswordHash(newPassword, out byte[] hash, out byte[] salt);
                user.passwordHash = hash;
                user.passwordSalt = salt;

                await _userRepository.SaveChangesAsync();

                _logger.InfoFormat("[ResetPasswordByEmail] Contraseña actualizada exitosamente para: {0}", email);

                return true;

            }, "ResetPasswordByEmail");
        }

        public async Task<FriendDto> FindUserByNickname(string nickname)
        {
            if (string.IsNullOrWhiteSpace(nickname))
            {
                throw new ArgumentException("El nickname a buscar no puede estar vacío.", nameof(nickname));
            }

            return await ExecuteFaultSafeAsync(async () =>
            {
                var user = await _userRepository.GetUserByNicknameAsync(nickname);

                if (user == null || !string.Equals(user.nickname, nickname, StringComparison.OrdinalIgnoreCase))
                {
                    throw new UserNotFoundException($"No se encontró usuario con nickname: {nickname}");
                }

                return new FriendDto
                {
                    UserId = user.id_user,
                    Nickname = user.nickname,
                    Status = user.status
                };

            }, "FindUserByNickname");
        }

        public async Task<UserDto> GetUserProfile(int userId)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                var user = await GetUserOrThrow(userId);

                return new UserDto
                {
                    UserId = user.id_user,
                    Nickname = user.nickname,
                    Email = user.email,
                    FirstName = user.first_name,
                    PaternalLastName = user.paternal_last_name,
                    MaternalLastName = user.maternal_last_name,
                    AvatarId = user.id_avatar,
                    AvatarUrl = user.Avatar?.path
                };

            }, "GetUserProfile");
        }

        public async Task<List<LeaderboardPlayerDto>> GetLeaderboard()
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                var users = await _userRepository.GetLeaderboard();

                return users.Select(user => new LeaderboardPlayerDto
                {
                    UserId = user.id_user,
                    Nickname = user.nickname,
                    Score = user.score
                }).ToList();

            }, "GetLeaderboard");
        }

        private async Task<User> GetUserOrThrow(int userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                throw new UserNotFoundException($"El usuario con ID {userId} no existe.");
            }
            return user;
        }

        private static void ThrowRegistrationException(RegistrationValidationResult result)
        {
            if (_validationExceptions.TryGetValue(result, out var exceptionFunc))
            {
                throw exceptionFunc();
            }

            throw new InvalidOperationException("Error de validación desconocido.");
        }
    }
}