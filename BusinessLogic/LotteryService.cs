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

        private static readonly IUserDao _userDao = new UserDao();
        private static readonly ILobbyManager _sharedLobbyManager = new LobbyManager(GlobalSessionManager.Instance, _userDao);


        private const string INVALID_SESSION_MSG = "Sesión de usuario no válida para esta operación.";
        private const string INVALID_SESSION_REASON = "Sesión inválida";
        private const string USER_NOT_CONNECTED_MSG = "Usuario no conectado.";

        private User _currentUser;
        private readonly AuthenticationHandler _authenticationHandler;
        private readonly UserHandler _userHandler;
        private readonly FriendHandler _friendHandler;
        private readonly LobbyHandler _lobbyHandler;
        private readonly GameHandler _gameHandler;
        private readonly ChatHandler _chatHandler;
        private readonly VerificationHandler _verificationHandler;
        private readonly GuestHandler _guestHandler;
        private readonly SocialMediaHandler _socialMediaHandler;

        static LotteryService()
        {
            _logger.Info("Inicializando tipo LotteryService...");
            try
            {
                using (var context = new lottery_databaseEntities())
                {
                    int rowsAffected = context.Database.ExecuteSqlCommand("UPDATE [User] SET status = 'Offline' WHERE status = 'Online'");

                    _logger.InfoFormat("Mantenimiento de BD completado. Usuarios corregidos a Offline: {0}", rowsAffected);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error crítico al intentar limpiar estados de usuarios en BD.", ex);
            }
        }

        public LotteryService()
        {
            IUserDao userDao = new UserDao();
            IFriendshipDao friendshipDao = new FriendshipDao();
            ISocialMediaDao socialMediaDao = new SocialMediaDao();
            IEmailService emailService = new EmailService();

            ISessionManager sessionManager = GlobalSessionManager.Instance;
            ILobbyManager lobbyManager = _sharedLobbyManager;

            _verificationHandler = new VerificationHandler(emailService);
            _authenticationHandler = new AuthenticationHandler(userDao);
            _guestHandler = new GuestHandler();
            _userHandler = new UserHandler(userDao, _verificationHandler);
            _socialMediaHandler = new SocialMediaHandler(socialMediaDao, userDao);

            _chatHandler = new ChatHandler(sessionManager, lobbyManager);
            _friendHandler = new FriendHandler(sessionManager, friendshipDao);
            _gameHandler = new GameHandler(lobbyManager);
            _lobbyHandler = new LobbyHandler(lobbyManager, sessionManager);

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

            _currentUser = await _authenticationHandler.LoginUser(username, password);

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

        public async Task LogoutUser()
        {
            var userToLogout = _currentUser;
            _currentUser = null;

            if (userToLogout != null)
            {
                await _lobbyHandler.LeaveLobby(userToLogout);
                GlobalSessionManager.Instance.UnregisterClient(userToLogout.id_user);
            }

            await _authenticationHandler.LogoutUser(userToLogout);
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

        public async Task<int> RegisterUserWithCode(UserDto userData, string code)
        {
            return await _userHandler.RegisterUserWithCode(userData, code);
        }

        public Task<int> RegisterGuest()
        {
            return _userHandler.RegisterGuest();
        }

        public Task<bool> RecoverPassword(string email, string newPassword)
        {
            return _userHandler.RecoverPassword(email, newPassword);
        }

        public Task<bool> VerifyPassword(int currentId, string password)
        {
            return _userHandler.VerifyPassword(currentId, password);
        }

        public Task<bool> ChangePassword(int currentUserId, string newPassword)
        {
            if (_currentUser == null || _currentUser.id_user != currentUserId)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = INVALID_SESSION_MSG },
                    new FaultReason(INVALID_SESSION_REASON)
                );
            }

            return _userHandler.ChangePassword(currentUserId, newPassword);
        }

        public Task<(bool Success, string Message)> UpdateProfile(int currentUserId, UserDto userData)
        {
            if (_currentUser == null || _currentUser.id_user != currentUserId)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = INVALID_SESSION_MSG },
                    new FaultReason(INVALID_SESSION_REASON)
                );
            }

            return _userHandler.UpdateProfile(currentUserId, userData);
        }

        public Task<FriendDto> FindUserByNickname(string nickname)
        {
            return _userHandler.FindUserByNickname(nickname);
        }

        public Task<UserDto> GetUserProfile(int currentId)
        {
            return _userHandler.GetUserProfile(currentId);
        }

        public Task<bool> ChangeEmailWithCodeAsync(int currentUserId, string newEmail, string code)
        {
            if (_currentUser == null || _currentUser.id_user != currentUserId)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = INVALID_SESSION_MSG },
                    new FaultReason(INVALID_SESSION_REASON)
                );
            }
            return _userHandler.ChangeEmailWithCodeAsync(currentUserId, newEmail, code);
        }

        public Task<bool> RecoverPasswordRequest(string email)
        {            
            return _userHandler.RecoverPasswordRequest(email);
        }

        public Task<List<LeaderboardPlayerDto>> GetLeaderboard()
        {
            return _userHandler.GetLeaderboard();
        }

        public Task<bool> RequestEmailChangeVerification(string newEmail)
        {
            if (_currentUser == null)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault
                    {
                        ErrorCode = "USER_NOT_CONNECTED",
                        Message = USER_NOT_CONNECTED_MSG
                    },
                    new FaultReason(INVALID_SESSION_REASON)
                );
            }

            return _userHandler.RequestEmailChangeVerification(newEmail);
        }

        // --- IFriendService ---

        public Task SendRequestFriendship(int currentUserId, int targetUserId)
        {
            if (_currentUser == null || _currentUser.id_user != currentUserId)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = INVALID_SESSION_MSG },
                    new FaultReason(INVALID_SESSION_REASON)
                );
            }

            return _friendHandler.SendRequestFriendship(currentUserId, targetUserId);
        }

        public Task AcceptFriendRequest(int currentUserId, int requesterId)
        {
            if (_currentUser == null || _currentUser.id_user != currentUserId)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = INVALID_SESSION_MSG },
                    new FaultReason(INVALID_SESSION_REASON)
                );
            }

            return _friendHandler.AcceptFriendRequest(currentUserId, requesterId);
        }

        public Task RejectFriendRequest(int currentUserId, int requesterId)
        {
            if (_currentUser == null || _currentUser.id_user != currentUserId)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = INVALID_SESSION_MSG },
                    new FaultReason(INVALID_SESSION_REASON)
                );
            }

            return _friendHandler.RejectFriendRequest(currentUserId, requesterId);
        }

        public Task CancelFriendRequest(int currentUserId, int targetUserId)
        {
            if (_currentUser == null || _currentUser.id_user != currentUserId)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = INVALID_SESSION_MSG },
                    new FaultReason(INVALID_SESSION_REASON)
                );
            }

            return _friendHandler.CancelFriendRequest(currentUserId, targetUserId);
        }

        public Task RemoveFriend(int currentUserId, int friendUserId)
        {
            if (_currentUser == null || _currentUser.id_user != currentUserId)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = INVALID_SESSION_MSG },
                    new FaultReason(INVALID_SESSION_REASON)
                );
            }

            return _friendHandler.RemoveFriend(currentUserId, friendUserId);
        }

        public Task<List<FriendDto>> GetFriends(int currentUserId)
        {
            if (_currentUser == null || _currentUser.id_user != currentUserId)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = INVALID_SESSION_MSG },
                    new FaultReason(INVALID_SESSION_REASON)
                );
            }

            return _friendHandler.GetFriends(currentUserId);
        }

        public Task<List<FriendDto>> GetPendingRequests(int currentUserId)
        {
            if (_currentUser == null || _currentUser.id_user != currentUserId)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = INVALID_SESSION_MSG },
                    new FaultReason(INVALID_SESSION_REASON)
                );
            }

            return _friendHandler.GetPendingRequests(currentUserId);
        }

        public Task<List<FriendDto>> GetSentRequests(int currentUserId)
        {
            if (_currentUser == null || _currentUser.id_user != currentUserId)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = INVALID_SESSION_MSG },
                    new FaultReason(INVALID_SESSION_REASON)
                );
            }

            return _friendHandler.GetSentRequests(currentUserId);
        }

        public Task InviteFriendToLobby(string lobbyCode, int targetFriendId)
        {
            if (_currentUser == null)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = USER_NOT_CONNECTED_MSG },
                    new FaultReason(INVALID_SESSION_REASON)
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
                    new ServiceFault { Message = USER_NOT_CONNECTED_MSG },
                    new FaultReason(INVALID_SESSION_REASON)
                );
            }

            return _lobbyHandler.CreateLobby(_currentUser);
        }

        public Task<LobbyStateDto> JoinLobby(UserDto currentUser, string lobbyCode)
        {
            if (_currentUser == null)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = USER_NOT_CONNECTED_MSG },
                    new FaultReason(INVALID_SESSION_REASON)
                );
            }

            return _lobbyHandler.JoinLobby(_currentUser, lobbyCode);
        }

        public async void LeaveLobby()
        {
            if (_currentUser == null)
            {
                return;
            }
            await _lobbyHandler.LeaveLobby(_currentUser);
        }

        public Task KickPlayer(int targetPlayerId)
        {
            if (_currentUser == null)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = USER_NOT_CONNECTED_MSG },
                    new FaultReason(INVALID_SESSION_REASON)
                );
            }

            return _lobbyHandler.KickPlayer(_currentUser, targetPlayerId);
        }

        public Task ChooseBoard(UserDto user, int boardId)
        {
            if (_currentUser == null)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = USER_NOT_CONNECTED_MSG },
                    new FaultReason(INVALID_SESSION_REASON)
                );
            }

            return _lobbyHandler.ChooseBoard(_currentUser, boardId);
        }

        // --- IGameService ---

        public Task StartGame(GameSettingsDto settings)
        {
            if (_currentUser == null)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = USER_NOT_CONNECTED_MSG },
                    new FaultReason(INVALID_SESSION_REASON)
                );
            }

            return _gameHandler.StartGame(_currentUser, settings);
        }

        public Task UpdateGameSettings(GameSettingsDto settings)
        {
            if (_currentUser == null)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = USER_NOT_CONNECTED_MSG },
                    new FaultReason(INVALID_SESSION_REASON)
                );
            }

            return _gameHandler.UpdateGameSettings(_currentUser, settings);
        }

        public Task<int[]> GetScoreboard()
        {
            if (_currentUser == null)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = USER_NOT_CONNECTED_MSG },
                    new FaultReason(INVALID_SESSION_REASON)
                );
            }
            return _gameHandler.GetScoreboard(_currentUser);
        }

        public Task DeclareWin(PlayerBoardDto playerBoard)
        {
            return _gameHandler.DeclareWin(playerBoard);
        }

        public Task<bool> ValidateFalseLoteriaAsync(int challengerUserId)
        {
            return _gameHandler.ValidateFalseLoteriaAsync(challengerUserId);
        }

        public Task ConfirmGameEnd(int winnerId)
        {        
            if (_currentUser == null)
            {
                throw new FaultException<ServiceFault>(
                   new ServiceFault { Message = USER_NOT_CONNECTED_MSG },
                   new FaultReason(INVALID_SESSION_REASON)
               );
            }          
            return _gameHandler.ConfirmGameEnd(_currentUser, winnerId);
        }

        // --- IChatService ---

        public async Task SendMessage(string message)
        {
            await _chatHandler.SendMessage(_currentUser, message);
        }

        public void Reconnect(int userId)
        {
            var callback =
                OperationContext.Current.GetCallbackChannel<ILotteryCallback>();

            GlobalSessionManager.Instance.ReconnectUser(userId, callback);
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

        public Task<bool> ConsumeVerificationCode(string email)
        {
            return _verificationHandler.ConsumeVerificationCode(email);
        }

        public Task<LobbyStateDto> GetLobbyState(string lobbyCode)
        {            
            if (_currentUser == null)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = USER_NOT_CONNECTED_MSG },
                    new FaultReason(INVALID_SESSION_REASON)
                );
            }            
            return _lobbyHandler.GetLobbyState(_currentUser, lobbyCode);
        }

        // --- ISocialMediaService ---

        public async Task<SocialMediaDto> GetSocialMediaAsync(int currentUserId)
        {
            return await _socialMediaHandler.GetSocialMedia(currentUserId);
        }

        public async Task<bool> SaveOrUpdateSocialMediaAsync(SocialMediaDto media)
        {
            return await _socialMediaHandler.UpdateSocialMedia(media);
        }
    }
}