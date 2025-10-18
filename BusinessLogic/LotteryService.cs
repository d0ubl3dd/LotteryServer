using BusinessLogic.Handlers;
using BusinessLogic.Logic;
using Contracts;
using Contracts.DTOs;
using DataAccess;
using System;
using System.ServiceModel;
using System.Threading.Tasks;

namespace BusinessLogic
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
    public class LotteryService : ILotteryService
    {
        private User currentUser;
        private readonly AuthenticationHandler _authHandler = new AuthenticationHandler();
        private readonly UserHandler _userHandler = new UserHandler();
        private readonly FriendHandler _friendHandler = new FriendHandler();
        private readonly LobbyHandler _lobbyHandler = new LobbyHandler();
        private readonly GameHandler _gameHandler = new GameHandler();
        private readonly ChatHandler _chatHandler = new ChatHandler();

        // --- IAuthenticationService ---
        public async Task<bool> LoginUser(string username, string password)
        {
            this.currentUser = await _authHandler.LoginUser(username, password);
            return this.currentUser != null;
        }

        public Task LogoutUser()
        {
            var userToLogout = this.currentUser;
            this.currentUser = null; // Clear the session user
            return _authHandler.LogoutUser(userToLogout);
        }

        // --- IUserService ---
        // These methods do not require a logged-in user.
        public Task<int> RegisterUser(UserRegisterDTO userData)
        {
            return _userHandler.RegisterUser(userData);
        }

        public Task<int> RegisterGuest()
        {
            return _userHandler.RegisterGuest();
        }

        public Task RecoverPassword(string email)
        {
            return _userHandler.RecoverPassword(email);
        }

        // These methods DO require a logged-in user.
        public Task ChangePassword(string oldPassword, string newPassword)
        {
            if (currentUser == null) throw new InvalidOperationException("User must be logged in to change password.");
            return _userHandler.ChangePassword(this.currentUser, oldPassword, newPassword);
        }

        public Task UpdateProfile(UserProfileDTO profileData)
        {
            if (currentUser == null) throw new InvalidOperationException("User must be logged in to update profile.");
            return _userHandler.UpdateProfile(this.currentUser, profileData);
        }

        // --- IFriendService ---
        public Task SendRequestFriendship(int targetUserId)
        {
            if (currentUser == null) throw new InvalidOperationException("User must be logged in to send a friend request.");
            return _friendHandler.SendRequestFriendship(this.currentUser.id_user, targetUserId);
        }

        public Task AddFriend(int requesterId)
        {
            if (currentUser == null) throw new InvalidOperationException("User must be logged in to add a friend.");
            return _friendHandler.AddFriend(this.currentUser, requesterId);
        }

        public Task RemoveFriend(int friendUserId)
        {
            if (currentUser == null) throw new InvalidOperationException("User must be logged in to remove a friend.");
            return _friendHandler.RemoveFriend(this.currentUser.id_user, friendUserId);
        }

        // --- ILobbyService ---
        public Task<string> CreateLobby()
        {
            if (currentUser == null) throw new InvalidOperationException("User must be logged in to create a lobby.");
            return _lobbyHandler.CreateLobby(this.currentUser);
        }

        public Task JoinLobby(string lobbyCode)
        {
            if (currentUser == null) throw new InvalidOperationException("User must be logged in to join a lobby.");
            return _lobbyHandler.JoinLobby(this.currentUser, lobbyCode);
        }

        // --- IGameService ---
        public Task StartGame()
        {
            if (currentUser == null) throw new InvalidOperationException("User must be logged in to start a game.");
            return _gameHandler.StartGame(this.currentUser);
        }

        public Task UpdateGameSettings(GameSettingsDTO settings)
        {
            if (currentUser == null) throw new InvalidOperationException("User must be logged in to update game settings.");
            return _gameHandler.UpdateGameSettings(this.currentUser, settings);
        }

        public Task GetScoreboard()
        {
            // Getting the scoreboard might not require a logged-in user, depending on your rules.
            return _gameHandler.GetScoreboard();
        }

        // --- IChatService ---
        public void SendMessage(string message)
        {
            if (currentUser == null) throw new InvalidOperationException("User must be logged in to send a message.");
            _chatHandler.SendMessage(this.currentUser, message);
        }
    }
}