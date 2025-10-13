using BusinessLogic.Handlers; // Suponiendo que tus handlers están en este namespace
using BusinessLogic.Logic;
using Contracts;
using System.Threading.Tasks;

namespace BusinessLogic
{
    public class LotteryService : ILotteryService
    {
        // Instancia de cada especialista
        private readonly AuthenticationHandler _authHandler = new AuthenticationHandler();
        private readonly UserHandler _userHandler = new UserHandler();
        private readonly FriendHandler _friendHandler = new FriendHandler();
        private readonly LobbyHandler _lobbyHandler = new LobbyHandler();
        private readonly GameHandler _gameHandler = new GameHandler();
        private readonly ChatHandler _chatHandler = new ChatHandler();

        // --- IAuthenticationService ---
        public Task<bool> UserLogin(string username, string password)
        {
            return _authHandler.UserLogin(username, password);
        }

        public Task UserLogout()
        {
            return _authHandler.UserLogout();
        }

        // --- IUserService ---
        public Task<int> RegisterUser(/* UserRegisterDTO userData */)
        {
            return _userHandler.RegisterUser(/* userData */);
        }

        public Task<int> RegisterGuest()
        {
            return _userHandler.RegisterGuest();
        }

        public Task ChangePassword(string oldPassword, string newPassword)
        {
            return _userHandler.ChangePassword(oldPassword, newPassword);
        }

        public Task RecoverPassword(string email)
        {
            return _userHandler.RecoverPassword(email);
        }

        public Task UpdateProfile(/* UserProfileDTO profileData */)
        {
            return _userHandler.UpdateProfile(/* profileData */);
        }

        // --- IFriendService ---
        public Task SendRequestFriendship(int targetUserId)
        {
            return _friendHandler.SendRequestFriendship(targetUserId);
        }

        public Task AddFriend(int friendshipRequestId)
        {
            return _friendHandler.AddFriend(friendshipRequestId);
        }

        public Task RemoveFriend(int friendUserId)
        {
            return _friendHandler.RemoveFriend(friendUserId);
        }

        // --- ILobbyService ---
        public Task<string> CreateLobby()
        {
            return _lobbyHandler.CreateLobby();
        }

        public Task JoinLobby(string lobbyCode)
        {
            return _lobbyHandler.JoinLobby(lobbyCode);
        }

        // --- IGameService ---
        public Task StartGame()
        {
            return _gameHandler.StartGame();
        }

        public Task UpdateGameSettings(/* DTO con settings */)
        {
            return _gameHandler.UpdateGameSettings(/* settings */);
        }

        public Task GetScoreboard()
        {
            return _gameHandler.GetScoreboard();
        }

        // --- IChatService ---
        public void SendMessage(string message)
        {
            _chatHandler.SendMessage(message);
        }
    }
}