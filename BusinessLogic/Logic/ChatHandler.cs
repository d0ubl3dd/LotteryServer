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
        private readonly ILobbyManager _lobbyManager;

        private const string SPAM_KEYWORD = "Spam";

        public ChatHandler(ISessionManager sessionManager, ILobbyManager lobbyManager)
            : base(typeof(ChatHandler))
        {
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            _lobbyManager = lobbyManager ?? throw new ArgumentNullException(nameof(lobbyManager));
        }

        public async Task SendMessage(User currentUser, string message)
        {
            if (currentUser == null)
            {
                throw new ArgumentNullException(nameof(currentUser));
            }

            await ExecuteFaultSafeAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(message))
                {
                    _logger.InfoFormat("[SendMessage] Mensaje vacío ignorado para usuario {0}.", currentUser.nickname);
                    return;
                }

                _logger.InfoFormat("[SendMessage] Usuario {0} intenta enviar mensaje.", currentUser.nickname);

                var client = _sessionManager.GetClient(currentUser.id_user);

                if (client == null)
                {
                    throw new UserNotOnlineException("No se encontró sesión activa para este usuario.");
                }

                var lobby = client.CurrentLobby;

                if (lobby == null)
                {
                    throw new UserNotInLobbyException("No estás dentro de un lobby, no puedes enviar mensajes.");
                }

                try
                {
                    lobby.BroadcastChatMessage(client.Nickname, message);

                    _logger.InfoFormat("[SendMessage] Mensaje enviado en lobby '{0}'.", lobby.LobbyCode);
                }
                catch (ForbiddenWordException)
                {
                    _logger.WarnFormat("[SendMessage] Grosería detectada de {0}. Procediendo a expulsión.", client.Nickname);
                    _lobbyManager.KickPlayer(lobby.Host, client.UserId);
                }
                catch (ChatException ex) when (ex.Message.Contains(SPAM_KEYWORD))
                {
                    _logger.WarnFormat("[SendMessage] Spam detectado de {0}. Procediendo a expulsión.", client.Nickname);
                    _lobbyManager.KickPlayer(lobby.Host, client.UserId);
                }

            }, "SendMessage");
        }
    }
}