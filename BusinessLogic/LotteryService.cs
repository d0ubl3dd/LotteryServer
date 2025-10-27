using BusinessLogic.Handlers;
using BusinessLogic.Logic;
using Contracts;
using Contracts.DTOs;
using DataAccess;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;
using Contracts.Callbacks;

namespace BusinessLogic
{
    [ServiceBehavior(
    InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class LotteryService : ILotteryService
    {
        private User currentUser;
        private readonly AuthenticationHandler authHandler = new AuthenticationHandler();
        private readonly UserHandler userHandler = new UserHandler();
        private readonly FriendHandler friendHandler = new FriendHandler();
        private readonly LobbyHandler lobbyHandler = new LobbyHandler();
        private readonly GameHandler gameHandler = new GameHandler();
        private readonly ChatHandler chatHandler = new ChatHandler();
        private readonly VerificationHandler verificationHandler = new VerificationHandler();

        // --- IAuthenticationService ---
        public async Task<UserSessionDTO> LoginUser(string username, string password)
        {
            var operationContext = OperationContext.Current;
            if (operationContext == null)
            {
                throw new Exception("Error de conexión dúplex: El contexto del servidor es nulo.");
            }

            var channel = operationContext.Channel;
            var callback = operationContext.GetCallbackChannel<ILotteryCallback>();

            this.currentUser = await authHandler.LoginUser(username, password);

            if (this.currentUser == null)
            {
                return null;
            }

            GlobalSessionManager.Instance.RegisterClient(this.currentUser, callback);
            channel.Faulted += OnChannelFaulted;
            channel.Closing += OnChannelFaulted;

            return new UserSessionDTO
            {
                UserId = this.currentUser.id_user,
                Nickname = this.currentUser.nickname,
                AvatarId = this.currentUser.id_avatar
            };
        }

        public Task LogoutUser()
        {
            var userToLogout = this.currentUser;
            this.currentUser = null;

            if (userToLogout != null)
            {
                lobbyHandler.LeaveLobby(userToLogout);
                GlobalSessionManager.Instance.UnregisterClient(userToLogout.id_user);
            }

            return authHandler.LogoutUser(userToLogout);
        }

        private void OnChannelFaulted(object sender, EventArgs e)
        {
            if (this.currentUser != null)
            {
                lobbyHandler.LeaveLobby(this.currentUser);
                GlobalSessionManager.Instance.UnregisterClient(this.currentUser.id_user);
                this.currentUser = null;
            }
        }

        // --- IUserService ---
        public async Task<int> RequestUserVerification(UserRegisterDTO userData)
        {
            return await userHandler.RequestUserVerification(userData);
        }
        public async Task<int> RegisterUser(UserRegisterDTO userData)
        {            
            return await userHandler.RegisterUser(userData);
        }

        public Task<int> RegisterGuest()
        {
            return userHandler.RegisterGuest();
        }

        public Task RecoverPassword(string email)
        {
            return userHandler.RecoverPassword(email);
        }

        public Task<bool> ChangePassword(int currentUserId, string oldPassword, string newPassword)
        {
            if (currentUser == null || currentUser.id_user != currentUserId)
            {
                throw new InvalidOperationException("Invalid user session for this operation.");
            }

            return userHandler.ChangePassword(currentUserId, oldPassword, newPassword);
        }

        public Task<bool> UpdateProfile(int currentUserId, UserProfileDTO profileData)
        {
            if (currentUser == null || currentUser.id_user != currentUserId)
            {
                throw new InvalidOperationException("Invalid user session for this operation.");
            }

            return userHandler.UpdateProfile(currentUserId, profileData);
        }

        public Task<FriendDTO> FindUserByNickname(string nickname)
        {
            return userHandler.FindUserByNickname(nickname);
        }

        // --- IFriendService ---
        public Task SendRequestFriendship(int currentUserId, int targetUserId)
        {
            if (currentUser == null || currentUser.id_user != currentUserId)
            {
                throw new InvalidOperationException("Sesión de usuario no válida para esta operación.");
            }

            return friendHandler.SendRequestFriendship(currentUserId, targetUserId);
        }

        public Task AcceptFriendRequest(int currentUserId, int requesterId)
        {
            if (currentUser == null || currentUser.id_user != currentUserId)
            {
                throw new InvalidOperationException("Sesión de usuario no válida para esta operación.");
            }

            return friendHandler.AcceptFriendRequest(currentUserId, requesterId);
        }

        public Task RejectFriendRequest(int currentUserId, int requesterId)
        {
            if (currentUser == null || currentUser.id_user != currentUserId)
            {
                throw new InvalidOperationException("Sesión de usuario no válida para esta operación.");
            }

            return friendHandler.RejectFriendRequest(currentUserId, requesterId);
        }

        public Task RemoveFriend(int currentUserId,int friendUserId)
        {
            if (currentUser == null || currentUser.id_user != currentUserId)
            {
                throw new InvalidOperationException("Sesión de usuario no válida para esta operación.");
            }

            return friendHandler.RemoveFriend(currentUserId, friendUserId);
        }

        public Task<List<FriendDTO>> GetFriends(int currentUserId)
        {
            if (currentUser == null || currentUser.id_user != currentUserId)
            {
                throw new InvalidOperationException("Sesión de usuario no válida para esta operación.");
            }

            return friendHandler.GetFriends(currentUserId);
        }

        public Task<List<FriendRequestDTO>> GetPendingRequests(int currentUserId)
        {
            if (currentUser == null || currentUser.id_user != currentUserId)
            {
                throw new InvalidOperationException("Sesión de usuario no válida para esta operación.");
            }

            return friendHandler.GetPendingRequests(currentUserId);
        }

        public Task<List<FriendRequestDTO>> GetPendingRequests()
        {
            if (currentUser == null) throw new InvalidOperationException("El usuario debe estar conectado.");
            return friendHandler.GetPendingRequests(this.currentUser.id_user);
        }

        public Task InviteFriendToLobby(string lobbyCode, int targetFriendId)
        {
            if (currentUser == null) throw new InvalidOperationException("Usuario no conectado.");

            return friendHandler.InviteFriendToLobby(this.currentUser.id_user, targetFriendId, lobbyCode);
        }

        // --- ILobbyService ---
        public Task<LobbyStateDTO> CreateLobby()
        {
            if (currentUser == null) throw new InvalidOperationException("Usuario no conectado.");
            return lobbyHandler.CreateLobby(this.currentUser);
        }

        public Task<LobbyStateDTO> JoinLobby(UserSessionDTO currentUserDto, string lobbyCode)
        {
            if (currentUser == null) throw new InvalidOperationException("User not logged in.");
            var userEntity = new User
            {
                id_user = currentUserDto.UserId,
                nickname = currentUserDto.Nickname,
                id_avatar = currentUserDto.AvatarId
            };
            return lobbyHandler.JoinLobby(this.currentUser, lobbyCode);
        }

        public void LeaveLobby()
        {
            if (currentUser == null) return;
            lobbyHandler.LeaveLobby(this.currentUser);
        }

        public Task KickPlayer(int targetPlayerId)
        {
            if (currentUser == null) throw new InvalidOperationException("User not logged in.");
            return lobbyHandler.KickPlayer(this.currentUser, targetPlayerId);
        }

        // --- IGameService ---
        public Task StartGame()
        {
            if (currentUser == null) throw new InvalidOperationException("User must be logged in to start a game.");
            return gameHandler.StartGame(this.currentUser);
        }

        public Task UpdateGameSettings(GameSettingsDTO settings)
        {
            if (currentUser == null) throw new InvalidOperationException("User must be logged in to update game settings.");
            return gameHandler.UpdateGameSettings(this.currentUser, settings);
        }

        public Task GetScoreboard()
        {
            return gameHandler.GetScoreboard();
        }

        // --- IChatService ---
        public void SendMessage(string message)
        {
            if (currentUser == null) throw new InvalidOperationException("User must be logged in to send a message.");
            chatHandler.SendMessage(this.currentUser, message);
        }

        // --- IVerificationService ---
        public Task<bool> SendVerificationCode(string email)
        {
            return verificationHandler.SendVerificationCode(email);
        }
        public Task<bool> VerifyCode(string email, string code)
        {
            return verificationHandler.VerifyCode(email, code);
        }
    }
}