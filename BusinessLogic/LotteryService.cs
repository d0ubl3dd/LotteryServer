using BusinessLogic.Handlers;
using BusinessLogic.Logic;
using BusinessLogic.Services;
using Contracts;
using Contracts.Callbacks;
using Contracts.DTOs;
using Contracts.Faults;
using Contracts.Services.Email;
using DataAccess;
using DataAccess.DAOs;
using log4net;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;

namespace BusinessLogic
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class LotteryService : ILotteryService
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(LotteryService));

        private User _currentUser;
        private readonly AuthenticationHandler _authHandler;
        private readonly UserHandler _userHandler;
        private readonly FriendHandler _friendHandler;
        private readonly LobbyHandler _lobbyHandler;
        private readonly GameHandler _gameHandler;
        private readonly ChatHandler _chatHandler;
        private readonly VerificationHandler _verificationHandler;
        private readonly GuestHandler _guestHandler;
        private readonly SocialMediaHandler _socialMediaHandler;

        public LotteryService()
        {
            IUserDao userDao = new UserDao();
            IFriendshipDao friendshipDao = new FriendshipDao();
            IEmailService emailService = new EmailService();

            var lobbyManagerInstance = LobbyManager.Instance;
            var sessionManagerInstance = GlobalSessionManager.Instance;

            _verificationHandler = new VerificationHandler(emailService);
            _lobbyHandler = new LobbyHandler(lobbyManagerInstance);
            _gameHandler = new GameHandler(lobbyManagerInstance);
            _chatHandler = new ChatHandler(sessionManagerInstance);
            _authHandler = new AuthenticationHandler(userDao);
            _friendHandler = new FriendHandler(sessionManagerInstance, friendshipDao);
            _userHandler = new UserHandler(userDao, _verificationHandler);
            _guestHandler = new GuestHandler();
            _socialMediaHandler = new SocialMediaHandler(new SocialMediaDao(), new UserDao());

            _logger.Info("LotteryService instanciado.");
        }

        // --- IAuthenticationService ---

        public async Task<UserDto> LoginUser(string username, string password)
        {
            var operationContext = OperationContext.Current;
            if (operationContext == null)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = "Error de conexión dúplex: El contexto del servidor es nulo." },
                    new FaultReason("Error de conexión")
                );
            }

            var channel = operationContext.Channel;
            var callback = operationContext.GetCallbackChannel<ILotteryCallback>();

            _currentUser = await _authHandler.LoginUser(username, password);

            if (_currentUser == null)
            {
                return null;
            }

            GlobalSessionManager.Instance.RegisterClient(_currentUser, callback);
            channel.Faulted += OnChannelFaulted;
            channel.Closing += OnChannelFaulted;

            return new UserDto
            {
                UserId = _currentUser.id_user,
                Nickname = _currentUser.nickname,
                AvatarId = _currentUser.id_avatar
            };
        }

        public async Task<UserDto> LoginGuest(string nickname)
        {
            User guestUser = await _guestHandler.LoginGuest(nickname);

            if (guestUser != null)
            {
                var callback = OperationContext.Current.GetCallbackChannel<ILotteryCallback>();
                _currentUser = guestUser;

                GlobalSessionManager.Instance.RegisterClient(guestUser, callback);

                var channel = OperationContext.Current.Channel;
                channel.Faulted += OnChannelFaulted;
                channel.Closing += OnChannelFaulted;

                return new UserDto
                {
                    UserId = guestUser.id_user,
                    Nickname = guestUser.nickname,
                    AvatarId = guestUser.id_avatar,
                    IsHost = false,
                    Score = 0,
                    Email = "Invitado"
                };
            }
            return null;
        }

        public Task LogoutUser()
        {
            var userToLogout = _currentUser;
            _currentUser = null;

            if (userToLogout != null)
            {
                _lobbyHandler.LeaveLobby(userToLogout);
                GlobalSessionManager.Instance.UnregisterClient(userToLogout.id_user);
            }

            return _authHandler.LogoutUser(userToLogout);
        }

        private void OnChannelFaulted(object sender, EventArgs e)
        {
            if (_currentUser != null)
            {
                _lobbyHandler.LeaveLobby(_currentUser);
                GlobalSessionManager.Instance.UnregisterClient(_currentUser.id_user);
                _currentUser = null;
            }
        }

        // --- IUserService ---

        public async Task<int> RequestUserVerification(UserDto userData)
        {
            return await _userHandler.RequestUserVerification(userData);
        }

        public async Task<int> RegisterUser(UserDto userData)
        {
            return await _userHandler.RegisterUser(userData);
        }

        public Task<int> RegisterGuest()
        {
            return _userHandler.RegisterGuest();
        }

        public Task RecoverPassword(string email)
        {
            return _userHandler.RecoverPassword(email);
        }

        public Task<bool> VerifyPassword(int userId, string password)
        {
            return _userHandler.VerifyPassword(userId, password);
        }

        public Task<bool> ChangePassword(int currentUserId, string newPassword)
        {
            if (_currentUser == null || _currentUser.id_user != currentUserId)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = "Sesión de usuario no válida para esta operación." },
                    new FaultReason("Sesión inválida")
                );
            }

            return _userHandler.ChangePassword(currentUserId, newPassword);
        }

        public Task<(bool Success, string Message)> UpdateProfile(int currentUserId, UserDto profileData)
        {
            if (_currentUser == null || _currentUser.id_user != currentUserId)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = "Sesión de usuario no válida para esta operación." },
                    new FaultReason("Sesión inválida")
                );
            }

            return _userHandler.UpdateProfile(currentUserId, profileData);
        }

        public Task<FriendDto> FindUserByNickname(string nickname)
        {
            return _userHandler.FindUserByNickname(nickname);
        }

        public Task<UserDto> GetUserProfile(int userId)
        {
            return _userHandler.GetUserProfile(userId);
        }

        public Task<bool> RequestEmailChange(int userId, string newEmail)
        {
            if (_currentUser == null || _currentUser.id_user != userId)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = "Sesión de usuario no válida para esta operación." },
                    new FaultReason("Sesión inválida")
                );
            }

            return _userHandler.RequestEmailChange(userId, newEmail);
        }

        public Task<bool> ConfirmEmailChange(int userId, string newEmail, string verificationCode)
        {
            if (_currentUser == null || _currentUser.id_user != userId)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = "Sesión de usuario no válida para esta operación." },
                    new FaultReason("Sesión inválida")
                );
            }

            return _userHandler.ConfirmEmailChange(userId, newEmail, verificationCode);
        }

        // --- IFriendService ---

        public Task SendRequestFriendship(int currentUserId, int targetUserId)
        {
            if (_currentUser == null || _currentUser.id_user != currentUserId)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = "Sesión de usuario no válida para esta operación." },
                    new FaultReason("Sesión inválida")
                );
            }

            return _friendHandler.SendRequestFriendship(currentUserId, targetUserId);
        }

        public Task AcceptFriendRequest(int currentUserId, int requesterId)
        {
            if (_currentUser == null || _currentUser.id_user != currentUserId)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = "Sesión de usuario no válida para esta operación." },
                    new FaultReason("Sesión inválida")
                );
            }

            return _friendHandler.AcceptFriendRequest(currentUserId, requesterId);
        }

        public Task RejectFriendRequest(int currentUserId, int requesterId)
        {
            if (_currentUser == null || _currentUser.id_user != currentUserId)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = "Sesión de usuario no válida para esta operación." },
                    new FaultReason("Sesión inválida")
                );
            }

            return _friendHandler.RejectFriendRequest(currentUserId, requesterId);
        }

        public Task CancelFriendRequest(int currentUserId, int requesterId)
        {
            if (_currentUser == null || _currentUser.id_user != currentUserId)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = "Sesión de usuario no válida para esta operación." },
                    new FaultReason("Sesión inválida")
                );
            }

            return _friendHandler.CancelFriendRequest(currentUserId, requesterId);
        }

        public Task RemoveFriend(int currentUserId, int friendUserId)
        {
            if (_currentUser == null || _currentUser.id_user != currentUserId)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = "Sesión de usuario no válida para esta operación." },
                    new FaultReason("Sesión inválida")
                );
            }

            return _friendHandler.RemoveFriend(currentUserId, friendUserId);
        }

        public Task<List<FriendDto>> GetFriends(int currentUserId)
        {
            if (_currentUser == null || _currentUser.id_user != currentUserId)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = "Sesión de usuario no válida para esta operación." },
                    new FaultReason("Sesión inválida")
                );
            }

            return _friendHandler.GetFriends(currentUserId);
        }

        public Task<List<FriendDto>> GetPendingRequests(int currentUserId)
        {
            if (_currentUser == null || _currentUser.id_user != currentUserId)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = "Sesión de usuario no válida para esta operación." },
                    new FaultReason("Sesión inválida")
                );
            }

            return _friendHandler.GetPendingRequests(currentUserId);
        }

        public Task<List<FriendDto>> GetSentRequests(int currentUserId)
        {
            if (_currentUser == null || _currentUser.id_user != currentUserId)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = "Sesión de usuario no válida para esta operación." },
                    new FaultReason("Sesión inválida")
                );
            }

            return _friendHandler.GetSentRequests(currentUserId);
        }

        public Task InviteFriendToLobby(string lobbyCode, int targetFriendId)
        {
            if (_currentUser == null)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = "Usuario no conectado." },
                    new FaultReason("Sesión inválida")
                );
            }

            return _friendHandler.InviteFriendToLobby(lobbyCode, targetFriendId);
        }

        // --- ILobbyService ---

        public Task<LobbyStateDto> CreateLobby()
        {
            if (_currentUser == null)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = "Usuario no conectado." },
                    new FaultReason("Sesión inválida")
                );
            }

            return _lobbyHandler.CreateLobby(_currentUser);
        }

        public Task<LobbyStateDto> JoinLobby(UserDto currentUserDto, string lobbyCode)
        {
            if (_currentUser == null)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = "Usuario no conectado." },
                    new FaultReason("Sesión inválida")
                );
            }

            return _lobbyHandler.JoinLobby(_currentUser, lobbyCode);
        }

        public void LeaveLobby()
        {
            if (_currentUser == null) return;
            _lobbyHandler.LeaveLobby(_currentUser);
        }

        public Task KickPlayer(int targetPlayerId)
        {
            if (_currentUser == null)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = "Usuario no conectado." },
                    new FaultReason("Sesión inválida")
                );
            }

            return _lobbyHandler.KickPlayer(_currentUser, targetPlayerId);
        }

        // --- IGameService ---

        public Task StartGame(GameSettingsDto settings)
        {
            if (_currentUser == null)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = "Usuario no conectado." },
                    new FaultReason("Sesión inválida")
                );
            }

            return _gameHandler.StartGame(_currentUser, settings);
        }

        public Task UpdateGameSettings(GameSettingsDto settings)
        {
            if (_currentUser == null)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = "Usuario no conectado." },
                    new FaultReason("Sesión inválida")
                );
            }

            return _gameHandler.UpdateGameSettings(_currentUser, settings);
        }

        public Task GetScoreboard()
        {
            return _gameHandler.GetScoreboard();
        }

        public async Task DeclareWin(int userId)
        {
            await Task.CompletedTask;
        }

        // --- IChatService ---

        public void SendMessage(string message)
        {
            if (_currentUser == null)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = "Usuario no conectado." },
                    new FaultReason("Sesión inválida")
                );
            }

            _chatHandler.SendMessage(_currentUser, message);
        }

        // --- IVerificationService ---

        public Task<bool> SendVerificationCode(string email)
        {
            return _verificationHandler.SendVerificationCode(email);
        }

        public Task<bool> VerifyCode(string email, string code)
        {
            return _verificationHandler.VerifyCode(email, code);
        }

        // --- ISocialMediaService ---

        public async Task<SocialMediaDto> GetSocialMediaAsync(int userId)
        {
            return await _socialMediaHandler.GetSocialMedia(userId);
        }

        public async Task<bool> SaveOrUpdateSocialMediaAsync(SocialMediaDto media)
        {
            return await _socialMediaHandler.UpdateSocialMedia(media);
        }
    }
}