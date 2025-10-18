using Contracts.DTOs;
using DataAccess;
using System;
using System.Threading.Tasks;

namespace BusinessLogic.Handlers
{
    public class GameHandler
    {
        public Task StartGame(User hostUser)
        {
            Console.WriteLine($"Game started by {hostUser.nickname}.");
            // Find the lobby where this user is the host.
            // Change the lobby state to "InProgress".
            // Notify all players in the lobby via callbacks that the game has started.
            return Task.CompletedTask;
        }

        public Task UpdateGameSettings(User hostUser, GameSettingsDTO settings)
        {
            Console.WriteLine($"Game settings updated by {hostUser.nickname}.");
            // Find the lobby, verify the user is the host, and update settings.
            // Notify players of the changes via callbacks.
            return Task.CompletedTask;
        }

        public Task GetScoreboard()
        {
            Console.WriteLine("Scoreboard requested.");
            // This would query the database for users ordered by score/wins.
            return Task.CompletedTask;
        }
    }
}