using BusinessLogic.Exceptions;
using BusinessLogic.Logic;
using BusinessLogic.Logic.Base;
using DataAccess;
using System;
using System.Threading.Tasks;

namespace BusinessLogic.Handlers
{
    public class ChatHandler : BaseHandler
    {
        private readonly ISessionManager _sessionManager;

        public ChatHandler(ISessionManager sessionManager) : base(typeof(ChatHandler))
        {
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        }

        public async Task SendMessage(User currentUser, string message)
        {
            if (currentUser == null)
            {
                throw new ArgumentNullException(nameof(currentUser), "El usuario actual no puede ser nulo.");
            }

            await ExecuteFaultSafeAsync(async () =>
            {
                if (!string.IsNullOrWhiteSpace(message))
                {
                    _logger.InfoFormat("[SendMessage] Usuario {0} intenta enviar mensaje.", currentUser.nickname);

                    var client = _sessionManager.GetClient(currentUser.id_user);

                    if (client == null)
                    {
                        throw new UserNotOnlineException("No se encontró sesión activa para este usuario.");
                    }

                    if (client.CurrentLobby == null)
                    {
                        throw new UserNotInLobbyException("No estás dentro de un lobby, no puedes enviar mensajes.");
                    }

                    _logger.InfoFormat("[SendMessage] Enviando mensaje en lobby '{0}' desde {1}.",
                        client.CurrentLobby.LobbyCode,
                        client.Nickname);

                    client.CurrentLobby.BroadcastChatMessage(client.Nickname, message);
                }
                else
                {
                    _logger.InfoFormat("[SendMessage] Mensaje vacío ignorado para usuario {0}.", currentUser.nickname);
                }

                await Task.CompletedTask;

            }, "SendMessage");
        }
    }
}