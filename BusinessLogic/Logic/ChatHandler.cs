using BusinessLogic.Models;
using DataAccess;
using System;
using System.Threading.Tasks;

namespace BusinessLogic.Logic
{
    public class ChatHandler
    {
        public void SendMessage(User currentUser, string message)
        {
            var client = GlobalSessionManager.Instance.GetClient(currentUser.id_user);

            if (client?.CurrentLobby == null)
            {
                throw new Exception("No estás en un lobby para chatear.");
            }

            if (string.IsNullOrWhiteSpace(message)) return;

            client.CurrentLobby.BroadcastChatMessage(client.Nickname, message);
        }
    }
}