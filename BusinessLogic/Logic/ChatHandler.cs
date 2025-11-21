using BusinessLogic.Models;
using DataAccess;
using System;
using System.Threading.Tasks;
using System.ServiceModel;
using Contracts.Faults;
using log4net;

namespace BusinessLogic.Logic
{
    public class ChatHandler
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ChatHandler));
        private readonly GlobalSessionManager _sessionManager;

        public ChatHandler(GlobalSessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public void SendMessage(User currentUser, string message)
        {
            _logger.Info($"Usuario {currentUser?.nickname ?? "desconocido"} intenta enviar mensaje.");

            var client = _sessionManager.GetClient(currentUser.id_user);

            if (client?.CurrentLobby == null)
            {
                _logger.Error($"El usuario {currentUser.nickname} intentó enviar mensaje pero NO está en un lobby.");

                throw new FaultException<ServiceFault>(
                    new ServiceFault 
                    {
                        Message = "No estás en un lobby para chatear." 
                    },
                    new FaultReason("No estás en un lobby")
                );
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                _logger.Info($"Mensaje vacío o whitespace ignorado para usuario {currentUser.nickname}.");
                return;
            }

            try
            {
                _logger.Info($"Enviando mensaje en lobby '{client.CurrentLobby.LobbyCode}' desde {client.Nickname}.");
                client.CurrentLobby.BroadcastChatMessage(client.Nickname, message);
            }
            catch (Exception ex)
            {
                _logger.Fatal($"Error FATAL al intentar enviar un mensaje en lobby por el usuario {client.Nickname}.", ex);

                throw new FaultException<ServiceFault>(
                    new ServiceFault 
                    {
                        Message = "Ocurrió un error al enviar tu mensaje." 
                    },
                    new FaultReason("Error al enviar mensaje")
                );
            }
        }
    }
}
