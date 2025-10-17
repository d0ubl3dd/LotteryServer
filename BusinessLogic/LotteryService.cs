using BusinessLogic.Handlers; // Suponiendo que tus handlers están en este namespace
using BusinessLogic.Logic;
using Contracts;
using Contracts.DTOs;
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
        public void RegisterUser(UserRegisterDTO user)
        {
            
        }

        public void RegisterGuest(string nickname)
        {
            
        }

        public void ChangePassword(string password)
        {
            
        }

        public void RecoverPassword(string email)
        {
            
        }

        public void UpdateProfile(/* UserProfileDTO profileData */)
        {
            
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