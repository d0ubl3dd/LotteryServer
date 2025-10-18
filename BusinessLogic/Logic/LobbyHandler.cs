using DataAccess;
using System;
using System.Threading.Tasks;

namespace BusinessLogic.Handlers
{
    public class LobbyHandler
    {
        public Task<string> CreateLobby(User hostUser)
        {
            string lobbyCode = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
            Console.WriteLine($"Lobby created by {hostUser.nickname} with code {lobbyCode}.");

            // In a real scenario, you would save this lobby to the database
            // and add the host to an in-memory list of players for this lobby.

            return Task.FromResult(lobbyCode);
        }

        public Task JoinLobby(User joiningUser, string lobbyCode)
        {
            Console.WriteLine($"{joiningUser.nickname} is joining lobby {lobbyCode}.");

            // In a real scenario, you would find the lobby, validate it's open,
            // add the user to the in-memory player list, and notify other players
            // via their WCF callbacks.

            return Task.CompletedTask;
        }
    }
}