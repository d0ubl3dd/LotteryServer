using BusinessLogic.Logic;
using Contracts.DTOs;
using Contracts.Faults;
using DataAccess;
using System;
using System.Threading.Tasks;
using DataAccess.DAOs;
using System.ServiceModel;
using log4net;

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
            try
            {
                _logger.Info($"Solicitud de verificación para correo {userData.Email}.");

                if (string.IsNullOrEmpty(userData.Email) ||
                    string.IsNullOrEmpty(userData.Nickname) ||
                    string.IsNullOrEmpty(userData.Password))
                {
                    var reason = "Datos incompletos para verificación.";
                    _logger.Error(reason);
                    throw new FaultException<ServiceFault>(
                        new ServiceFault
                        {
                            Message = reason
                        },
                        new FaultReason(reason)
                    );
                }

                if (await _userRepository.NicknameExistsAsync(userData.Nickname))
                {
                    var reason = $"Nickname ya existe: {userData.Nickname}";
                    throw new FaultException<ServiceFault>(
                        new ServiceFault
                        {
                            Message = reason
                        },
                        new FaultReason(reason)
                    );
                }

                if (await _userRepository.EmailExistsAsync(userData.Email))
                {
                    var reason = $"Email ya existe: {userData.Email}";
                    throw new FaultException<ServiceFault>(
                        new ServiceFault
                        {
                            Message = reason
                        },
                        new FaultReason(reason)
                    );
                }

                bool codeSent = await _verificationHandler.SendVerificationCode(userData.Email);

                if (!codeSent)
                {
                    var reason = $"No se pudo enviar código de verificación a {userData.Email}";
                    _logger.Error(reason);
                    throw new FaultException<ServiceFault>(
                        new ServiceFault
                        {
                            Message = reason
                        },
                        new FaultReason(reason)
                    );
                }

                _logger.Info($"Código de verificación enviado a {userData.Email}");
                return 1;
            }
            catch (FaultException<ServiceFault>)
            {
                throw;
            }
            catch (Exception ex)
            {
                var fatalReason = "Error inesperado en RequestUserVerification.";
                _logger.Fatal(fatalReason, ex);
                throw new FaultException<ServiceFault>(
                    new ServiceFault
                    {
                        Message = fatalReason
                    },
                    new FaultReason(fatalReason)
                );
            }
        }

        public async Task<int> RegisterUser(UserDto userData)
        {
            try
            {
                _logger.Info($"Intentando registrar usuario {userData.Nickname}");

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

                _logger.Info($"Usuario registrado correctamente: {newUser.nickname} (ID {newUser.id_user})");
                return newUser.id_user;
            }
            catch (Exception ex)
            {
                var fatalReason = "Error inesperado durante el registro de usuario.";
                _logger.Fatal(fatalReason, ex);
                throw new FaultException<ServiceFault>(
                    new ServiceFault
                    {
                        Message = fatalReason
                    },
                    new FaultReason(fatalReason)
                );
            }
        }

        public async Task<bool> VerifyPassword(int userId, string password)
        {
            try
            {
                _logger.Info($"Verificando contraseña para userId {userId}");

                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user == null)
                {
                    var reason = $"Usuario no encontrado en VerifyPassword: {userId}";
                    throw new FaultException<ServiceFault>(
                        new ServiceFault
                        {
                            Message = reason
                        },
                        new FaultReason(reason)
                    );
                }

                return PasswordHasher.VerifyPasswordHash(password, user.passwordHash, user.passwordSalt);
            }
            catch (FaultException<ServiceFault>)
            {
                throw;
            }
            catch (Exception ex)
            {
                var fatalReason = $"Error inesperado verificando contraseña para userId {userId}.";
                _logger.Fatal(fatalReason, ex);
                throw new FaultException<ServiceFault>(
                    new ServiceFault
                    {
                        Message = fatalReason
                    },
                    new FaultReason(fatalReason)
                );
            }
        }

        public async Task<bool> ChangePassword(int userId, string newPassword)
        {
            try
            {
                _logger.Info($"Solicitud de cambio de contraseña para userId {userId}");

                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user == null)
                {
                    var reason = $"Usuario no encontrado en ChangePassword: {userId}";
                    throw new FaultException<ServiceFault>(
                        new ServiceFault
                        {
                            Message = reason
                        },
                        new FaultReason(reason)
                    );
                }

                PasswordHasher.CreatePasswordHash(newPassword, out byte[] hash, out byte[] salt);
                user.passwordHash = hash;
                user.passwordSalt = salt;

                await _userRepository.SaveChangesAsync();
                _logger.Info($"Contraseña actualizada correctamente para userId {userId}");

                return true;
            }
            catch (FaultException<ServiceFault>)
            {
                throw;
            }
            catch (Exception ex)
            {
                var fatalReason = $"Error inesperado cambiando contraseña para userId {userId}.";
                _logger.Fatal(fatalReason, ex);
                throw new FaultException<ServiceFault>(
                    new ServiceFault
                    {
                        Message = fatalReason
                    },
                    new FaultReason(fatalReason)
                );
            }
        }

        public async Task<(bool Success, string Message)> UpdateProfile(int currentUserId, UserDto userData)
        {
            try
            {
                _logger.Info($"Actualizando perfil del usuario ID {currentUserId}");

                var userInDb = await _userRepository.GetUserByIdAsync(currentUserId);
                if (userInDb == null)
                {
                    var reason = $"Usuario no encontrado en UpdateProfile: {currentUserId}";
                    throw new FaultException<ServiceFault>(
                        new ServiceFault
                        {
                            Message = reason
                        },
                        new FaultReason(reason)
                    );
                }

                if (!string.Equals(userInDb.nickname, userData.Nickname, StringComparison.OrdinalIgnoreCase))
                {
                    if (await _userRepository.NicknameExistsAsync(userData.Nickname))
                    {
                        var reason = $"El nickname ya está en uso: {userData.Nickname}";
                        throw new FaultException<ServiceFault>(
                            new ServiceFault
                            {
                                Message = reason
                            },
                            new FaultReason(reason)
                        );
                    }
                }

                userInDb.first_name = userData.FirstName;
                userInDb.paternal_last_name = userData.PaternalLastName;
                userInDb.maternal_last_name = userData.MaternalLastName;
                userInDb.nickname = userData.Nickname;
                userInDb.id_avatar = userData.AvatarId;

                await _userRepository.SaveChangesAsync();

                _logger.Info($"Perfil actualizado correctamente para userId {currentUserId}");
                return (true, "Perfil actualizado correctamente.");
            }
            catch (FaultException<ServiceFault>)
            {
                throw;
            }
            catch (Exception ex)
            {
                var fatalReason = $"Error inesperado en UpdateProfile para userId {currentUserId}.";
                _logger.Fatal(fatalReason, ex);
                throw new FaultException<ServiceFault>(
                    new ServiceFault
                    {
                        Message = fatalReason
                    },
                    new FaultReason(fatalReason)
                );
            }
        }

        public async Task<bool> RequestEmailChange(int userId, string newEmail)
        {
            try
            {
                _logger.Info($"Solicitud de cambio de correo para userId {userId}.");

                var userInDb = await _userRepository.GetUserByIdAsync(userId);
                if (userInDb == null)
                {
                    var reason = $"Usuario no encontrado en RequestEmailChange: {userId}";
                    throw new FaultException<ServiceFault>(
                        new ServiceFault
                        {
                            Message = reason
                        },
                        new FaultReason(reason)
                    );
                }

                if (await _userRepository.EmailExistsAsync(newEmail))
                {
                    var reason = $"Email ya existe: {newEmail}";
                    throw new FaultException<ServiceFault>(
                        new ServiceFault
                        {
                            Message = reason
                        },
                        new FaultReason(reason)
                    );
                }

                bool sent = await _verificationHandler.SendVerificationCode(newEmail);
                if (!sent)
                {
                    var reason = $"Error enviando código a {newEmail}";
                    _logger.Error(reason);
                    throw new FaultException<ServiceFault>(
                        new ServiceFault
                        {
                            Message = reason
                        },
                        new FaultReason(reason)
                    );
                }

                _logger.Info($"Código enviado exitosamente a {newEmail}");
                return sent;
            }
            catch (FaultException<ServiceFault>)
            {
                throw;
            }
            catch (Exception ex)
            {
                var fatalReason = $"Error inesperado en RequestEmailChange para userId {userId}.";
                _logger.Fatal(fatalReason, ex);
                throw new FaultException<ServiceFault>(
                    new ServiceFault
                    {
                        Message = fatalReason
                    },
                    new FaultReason(fatalReason)
                );
            }
        }

        public async Task<bool> ConfirmEmailChange(int userId, string newEmail, string verificationCode)
        {
            try
            {
                _logger.Info($"Confirmando cambio de correo para userId {userId}");

                var userInDb = await _userRepository.GetUserByIdAsync(userId);
                if (userInDb == null)
                {
                    var reason = $"Usuario no encontrado en ConfirmEmailChange: {userId}";
                    throw new FaultException<ServiceFault>(
                        new ServiceFault
                        {
                            Message = reason
                        },
                        new FaultReason(reason)
                    );
                }

                bool isValid = await _verificationHandler.VerifyCode(newEmail, verificationCode);
                if (!isValid)
                {
                    var reason = $"Código de verificación incorrecto para {newEmail}";
                    throw new FaultException<ServiceFault>(
                        new ServiceFault
                        {
                            Message = reason
                        },
                        new FaultReason(reason)
                    );
                }

                userInDb.email = newEmail;
                await _userRepository.SaveChangesAsync();

                _logger.Info($"Correo actualizado correctamente para userId {userId}");
                return true;
            }
            catch (FaultException<ServiceFault>)
            {
                throw;
            }
            catch (Exception ex)
            {
                var fatalReason = $"Error inesperado en ConfirmEmailChange para userId {userId}.";
                _logger.Fatal(fatalReason, ex);
                throw new FaultException<ServiceFault>(
                    new ServiceFault
                    {
                        Message = fatalReason
                    },
                    new FaultReason(fatalReason)
                );
            }
        }

        public Task<int> RegisterGuest()
        {
            _logger.Info("RegisterGuest invocado.");
            return Task.FromResult(-1);
        }

        public Task RecoverPassword(string email)
        {
            _logger.Info($"RecoverPassword solicitado para {email}.");
            return Task.CompletedTask;
        }

        public async Task<FriendDto> FindUserByNickname(string nickname)
        {
            try
            {
                _logger.Info($"Buscando usuario por nickname: {nickname}");

                var user = await _userRepository.GetUserByNicknameAsync(nickname);

                if (user == null)
                {
                    var reason = $"Usuario no encontrado con nickname: {nickname}";
                    throw new FaultException<ServiceFault>(
                        new ServiceFault
                        {
                            Message = reason
                        },
                        new FaultReason(reason)
                    );
                }

                return new FriendDto
                {
                    UserId = user.id_user,
                    Nickname = user.nickname,
                    Status = user.status
                };
            }
            catch (FaultException<ServiceFault>)
            {
                throw;
            }
            catch (Exception ex)
            {
                var fatalReason = $"Error inesperado buscando usuario por nickname: {nickname}";
                _logger.Fatal(fatalReason, ex);
                throw new FaultException<ServiceFault>(
                    new ServiceFault
                    {
                        Message = fatalReason
                    },
                    new FaultReason(fatalReason)
                );
            }
        }

        public async Task<UserDto> GetUserProfile(int userId)
        {
            try
            {
                _logger.Info($"Obteniendo perfil para userId {userId}");

                var user = await _userRepository.GetUserByIdAsync(userId);

                if (user == null)
                {
                    var reason = $"Usuario no encontrado para userId: {userId}";
                    throw new FaultException<ServiceFault>(
                        new ServiceFault
                        {
                            Message = reason
                        },
                        new FaultReason(reason)
                    );
                }

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
            }
            catch (FaultException<ServiceFault>)
            {
                throw;
            }
            catch (Exception ex)
            {
                var fatalReason = $"Error inesperado obteniendo perfil para userId: {userId}";
                _logger.Fatal(fatalReason, ex);
                throw new FaultException<ServiceFault>(
                    new ServiceFault
                    {
                        Message = fatalReason
                    },
                    new FaultReason(fatalReason)
                );
            }
        }
    }
}