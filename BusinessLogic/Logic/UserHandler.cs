using BusinessLogic.Exceptions;
using BusinessLogic.Logic;
using BusinessLogic.Logic.Base;
using BusinessLogic.Validation;
using Contracts.DTOs;
using DataAccess;
using DataAccess.DAOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessLogic.Handlers
{
    public partial class UserHandler : BaseHandler
    {
        private readonly IUserDao _userRepository;
        private readonly VerificationHandler _verificationHandler;

        private static readonly Dictionary<RegistrationValidationResult, Func<Exception>> _validationExceptions =
            new Dictionary<RegistrationValidationResult, Func<Exception>>
            {
                { RegistrationValidationResult.EmptyNickname, () => new ArgumentException("Todos los campos son obligatorios.") },
                { RegistrationValidationResult.EmptyEmail, () => new ArgumentException("Todos los campos son obligatorios.") },
                { RegistrationValidationResult.EmptyPassword, () => new ArgumentException("Todos los campos son obligatorios.") },
                { RegistrationValidationResult.EmptyName, () => new ArgumentException("Todos los campos son obligatorios.") },
                { RegistrationValidationResult.InvalidNicknameLength, () => new ArgumentException("El nickname debe tener al menos 4 caracteres.") },
                { RegistrationValidationResult.InvalidEmailFormat, () => new ArgumentException("El formato del correo electrónico no es válido.") },
                { RegistrationValidationResult.PasswordTooShort, () => new ArgumentException("La contraseña debe tener al menos 8 caracteres.") },
                { RegistrationValidationResult.PasswordNoUpperCase, () => new ArgumentException("La contraseña debe contener al menos una letra mayúscula.") },
                { RegistrationValidationResult.PasswordNoSpecialCharacter, () => new ArgumentException("La contraseña debe contener al menos un carácter especial.") },
                { RegistrationValidationResult.NicknameAlreadyExists, () => new UserAlreadyExistsException("El nombre de usuario ya está en uso.") },
                { RegistrationValidationResult.EmailAlreadyExists, () => new UserAlreadyExistsException("El correo electrónico ya está registrado.") }
            };

        public UserHandler(IUserDao userDao, VerificationHandler verificationHandler) : base(typeof(UserHandler))
        {
            if (userDao == null)
            {
                throw new ArgumentNullException(nameof(userDao));
            }
            if (verificationHandler == null)
            {
                throw new ArgumentNullException(nameof(verificationHandler));
            }

            _userRepository = userDao;
            _verificationHandler = verificationHandler;
        }

        public async Task<int> RequestUserVerification(UserDto userData)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                int result;

                _logger.Info($"[RequestVerification] Procesando solicitud para {userData.Email}.");

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

                _logger.Info($"[RequestVerification] Código enviado a {userData.Email}");
                result = 1;

                return result;

            }, "RequestUserVerification");
        }

        public async Task<int> RegisterUser(UserDto userData)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                int newUserId;

                _logger.Info($"[RegisterUser] Registrando: {userData.Nickname}");

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

                _logger.Info($"[RegisterUser] Registro exitoso. ID: {newUser.id_user}");
                newUserId = newUser.id_user;

                return newUserId;

            }, "RegisterUser");
        }

        public async Task<bool> VerifyPassword(int userId, string password)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                bool isValid;
                var user = await GetUserOrThrow(userId);
                isValid = PasswordHasher.VerifyPasswordHash(password, user.passwordHash, user.passwordSalt);
                return isValid;

            }, "VerifyPassword");
        }

        public async Task<bool> ChangePassword(int userId, string newPassword)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                bool success = false;

                _logger.Info($"[ChangePassword] Solicitud para ID {userId}");

                var user = await GetUserOrThrow(userId);

                PasswordHasher.CreatePasswordHash(newPassword, out byte[] hash, out byte[] salt);
                user.passwordHash = hash;
                user.passwordSalt = salt;

                await _userRepository.SaveChangesAsync();

                _logger.Info($"[ChangePassword] Éxito para ID {userId}");
                success = true;

                return success;

            }, "ChangePassword");
        }

        public async Task<(bool Success, string Message)> UpdateProfile(int currentUserId, UserDto userData)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                _logger.Info($"[UpdateProfile] ID {currentUserId}");

                var userInDb = await GetUserOrThrow(currentUserId);

                if (!string.Equals(userInDb.nickname, userData.Nickname, StringComparison.OrdinalIgnoreCase))
                {
                    if (await _userRepository.NicknameExistsAsync(userData.Nickname))
                    {
                        throw new UserAlreadyExistsException($"El nickname '{userData.Nickname}' ya está ocupado.");
                    }
                }

                userInDb.first_name = userData.FirstName;
                userInDb.paternal_last_name = userData.PaternalLastName;
                userInDb.maternal_last_name = userData.MaternalLastName;
                userInDb.nickname = userData.Nickname;
                userInDb.id_avatar = userData.AvatarId;

                await _userRepository.SaveChangesAsync();

                _logger.Info($"[UpdateProfile] Perfil actualizado para ID {currentUserId}");

                return (true, "Perfil actualizado correctamente.");

            }, "UpdateProfile");
        }

        public async Task<bool> RequestEmailChange(int userId, string newEmail)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                bool success = false;

                _logger.Info($"[RequestEmailChange] Usuario {userId} solicita cambio a {newEmail}");

                await GetUserOrThrow(userId);

                if (await _userRepository.EmailExistsAsync(newEmail))
                {
                    throw new UserAlreadyExistsException("Ese correo ya está asociado a otra cuenta.");
                }

                bool sent = await _verificationHandler.SendVerificationCode(newEmail);
                if (!sent)
                {
                    throw new VerificationException("Error al enviar el código de confirmación al nuevo correo.");
                }

                success = true;
                return success;

            }, "RequestEmailChange");
        }

        public async Task<bool> ConfirmEmailChange(int userId, string newEmail, string verificationCode)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                bool success = false;

                _logger.Info($"[ConfirmEmailChange] Usuario {userId}");

                var userInDb = await GetUserOrThrow(userId);

                bool isValid = await _verificationHandler.VerifyCode(newEmail, verificationCode);
                if (!isValid)
                {
                    throw new VerificationException("El código de verificación es incorrecto o ha expirado.");
                }

                userInDb.email = newEmail;
                await _userRepository.SaveChangesAsync();

                _logger.Info($"[ConfirmEmailChange] Correo actualizado para ID {userId}");
                success = true;

                return success;

            }, "ConfirmEmailChange");
        }

        public async Task<int> RegisterGuest()
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                int result;
                _logger.Info("[RegisterGuest] Registrando invitado.");
                result = await Task.FromResult(-1);
                return result;

            }, "RegisterGuest");
        }

        public async Task RecoverPassword(string email)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                _logger.Info($"[RecoverPassword] Solicitud para {email}.");
                await Task.CompletedTask;

            }, "RecoverPassword");
        }

        public async Task<FriendDto> FindUserByNickname(string nickname)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                FriendDto friendDto;

                if (string.IsNullOrWhiteSpace(nickname))
                {
                    throw new ArgumentException("El nickname a buscar no puede estar vacío.");
                }

                var user = await _userRepository.GetUserByNicknameAsync(nickname);

                if (user == null || !string.Equals(user.nickname, nickname, StringComparison.OrdinalIgnoreCase))
                {
                    throw new UserNotFoundException($"No se encontró usuario con nickname: {nickname}");
                }

                friendDto = new FriendDto
                {
                    UserId = user.id_user,
                    Nickname = user.nickname,
                    Status = user.status
                };

                return friendDto;

            }, "FindUserByNickname");
        }

        public async Task<UserDto> GetUserProfile(int userId)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                UserDto profile;
                var user = await GetUserOrThrow(userId);

                profile = new UserDto
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

                return profile;

            }, "GetUserProfile");
        }

        private async Task<User> GetUserOrThrow(int userId)
        {
            User user;
            user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                throw new UserNotFoundException($"El usuario con ID {userId} no existe.");
            }
            return user;
        }

        private void ThrowRegistrationException(RegistrationValidationResult result)
        {
            if (_validationExceptions.TryGetValue(result, out var exceptionFunc))
            {
                throw exceptionFunc();
            }

            throw new InvalidOperationException("Error de validación desconocido.");
        }
    }
}