using BusinessLogic.Models;
using DataAccess;
using System;
using System.Threading.Tasks;
using System.ServiceModel;
using Contracts.Faults;

namespace BusinessLogic.Logic
{
    public class ChatHandler
    {
        private readonly GlobalSessionManager _sessionManager;
        public ChatHandler(GlobalSessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public void SendMessage(User currentUser, string message)
        {
            var client = _sessionManager.GetClient(currentUser.id_user);

            if (client?.CurrentLobby == null)
            {
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = "No estás en un lobby para chatear." },
                    new FaultReason("No estás en un lobby")
                );
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            client.CurrentLobby.BroadcastChatMessage(client.Nickname, message);
        }
    }
}