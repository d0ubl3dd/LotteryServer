using DataAccess;
using System;

namespace BusinessLogic.Handlers
{
    public class ChatHandler
    {
        public void SendMessage(User sendingUser, string message)
        {
            Console.WriteLine($"Message from {sendingUser.nickname}: {message}");
            // Find the lobby/game the user is in.
            // Get the list of all other players in that session.
            // Loop through the list and invoke the 'OnMessageReceived' callback for each player.
        }
    }
}