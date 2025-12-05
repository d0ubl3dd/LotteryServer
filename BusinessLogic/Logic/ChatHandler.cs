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

        public ChatHandler(ISessionManager sessionManager, ILobbyManager lobbyManager)
            : base(typeof(ChatHandler))
        {
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            _lobbyManager = lobbyManager ?? throw new ArgumentNullException(nameof(lobbyManager));
        }

        public async Task SendMessage(User currentUser, string message)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                if (currentUser == null) throw new ArgumentNullException(nameof(currentUser));

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

                    try
                    {
                        client.CurrentLobby.BroadcastChatMessage(client.Nickname, message);

                        _logger.InfoFormat("[SendMessage] Mensaje enviado en lobby '{0}'.", client.CurrentLobby.LobbyCode);
                    }
                    catch (ForbiddenWordException)
                    {
                        _logger.WarnFormat("[SendMessage] Grosería detectada de {0}. Procediendo a expulsión.", client.Nickname);

                        _lobbyManager.KickPlayer(client.CurrentLobby.Host, client.UserId);
                    }
                    catch (ChatException ex) when (ex.Message.Contains("Spam"))
                    {
                        _logger.WarnFormat("[SendMessage] Spam detectado de {0}. Procediendo a expulsión.", client.Nickname);

                        _lobbyManager.KickPlayer(client.CurrentLobby.Host, client.UserId);
                    }
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