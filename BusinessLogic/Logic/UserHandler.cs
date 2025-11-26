using BusinessLogic.Exceptions;
using BusinessLogic.Logic;
using Contracts.DTOs;
using Contracts.Faults;
using DataAccess;
using DataAccess.DAOs;
using log4net;
using System;
using System.ServiceModel;
using System.Threading.Tasks;

namespace BusinessLogic.Handlers
{
    public partial class UserHandler
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(UserHandler));
        private readonly IUserDao _userRepository;
        private readonly VerificationHandler _verificationHandler;

        public UserHandler(IUserDao userDao, VerificationHandler verificationHandler)
        {
            _userRepository = userDao ?? throw new ArgumentNullException(nameof(userDao));
            _verificationHandler = verificationHandler ?? throw new ArgumentNullException(nameof(verificationHandler));
        }

        public async Task<int> RequestUserVerification(UserDto userData)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                _logger.Info($"[RequestVerification] Procesando solicitud para {userData.Email}.");

                if (string.IsNullOrEmpty(userData.Email) ||
                    string.IsNullOrEmpty(userData.Nickname) ||
                    string.IsNullOrEmpty(userData.Password))
                {
                    throw new ArgumentException("Los datos de registro están incompletos.");
                }

                if (await _userRepository.NicknameExistsAsync(userData.Nickname))
                {
                    throw new UserAlreadyExistsException($"El nickname '{userData.Nickname}' ya está en uso.");
                }

                if (await _userRepository.EmailExistsAsync(userData.Email))
                {
                    throw new UserAlreadyExistsException($"El correo '{userData.Email}' ya está registrado.");
                }

                bool codeSent = await _verificationHandler.SendVerificationCode(userData.Email);

                if (!codeSent)
                {
                    throw new VerificationException("No se pudo enviar el correo de verificación. Inténtalo más tarde.");
                }

                _logger.Info($"[RequestVerification] Código enviado a {userData.Email}");
                return 1;

            }, "RequestUserVerification");
        }

        public async Task<int> RegisterUser(UserDto userData)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
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
                return newUser.id_user;

            }, "RegisterUser");
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
                _logger.Info($"[ChangePassword] Solicitud para ID {userId}");

                var user = await GetUserOrThrow(userId);

                PasswordHasher.CreatePasswordHash(newPassword, out byte[] hash, out byte[] salt);
                user.passwordHash = hash;
                user.passwordSalt = salt;

                await _userRepository.SaveChangesAsync();

                _logger.Info($"[ChangePassword] Éxito para ID {userId}");
                return true;

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

                return true;

            }, "RequestEmailChange");
        }

        public async Task<bool> ConfirmEmailChange(int userId, string newEmail, string verificationCode)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
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
                return true;

            }, "ConfirmEmailChange");
        }

        public Task<int> RegisterGuest()
        {
            return ExecuteFaultSafeAsync(async () =>
            {
                _logger.Info("[RegisterGuest] Registrando invitado.");
                return await Task.FromResult(-1);

            }, "RegisterGuest");
        }

        public Task RecoverPassword(string email)
        {
            return ExecuteFaultSafeAsync(async () =>
            {
                _logger.Info($"[RecoverPassword] Solicitud para {email}.");
                await Task.CompletedTask;

            }, "RecoverPassword");
        }

        public async Task<FriendDto> FindUserByNickname(string nickname)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                var user = await _userRepository.GetUserByNicknameAsync(nickname);
                if (user == null)
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

        private async Task<User> GetUserOrThrow(int userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                throw new UserNotFoundException($"El usuario con ID {userId} no existe.");
            }
            return user;
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
                    errorCode = "USER_NOT_FOUND";
                    clientMessage = "Usuario no encontrado.";
                    _logger.Warn($"[{operationName}] {ex.Message}");
                    break;

                case UserAlreadyExistsException _:
                    errorCode = "USER_DUPLICATE";
                    clientMessage = ex.Message;
                    _logger.Warn($"[{operationName}] Conflicto de datos: {ex.Message}");
                    break;

                case VerificationException _:
                    errorCode = "USER_VERIFICATION_ERROR";
                    clientMessage = ex.Message;
                    _logger.Warn($"[{operationName}] Error de verificación: {ex.Message}");
                    break;

                case ArgumentException _:
                    errorCode = "USER_BAD_REQUEST";
                    clientMessage = "Datos de entrada inválidos o incompletos.";
                    _logger.Error($"[{operationName}] Validación fallida: {ex.Message}");
                    break;

                default:
                    errorCode = "USER_INTERNAL_ERROR";
                    clientMessage = "Error interno procesando la solicitud de usuario.";
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