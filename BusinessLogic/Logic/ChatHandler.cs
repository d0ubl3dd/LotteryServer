using BusinessLogic.Exceptions;
using BusinessLogic.Logic;
using BusinessLogic.Logic.Base;
using BusinessLogic.Models;
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
                    _logger.InfoFormat("[SendMessage] Empty message ignored for user {0}.", currentUser.nickname);
                    return;
                }

                _logger.InfoFormat("[SendMessage] User {0} attempting to send message.", currentUser.nickname);

                PlayerClient client = _sessionManager.GetClient(currentUser.id_user);

                if (client == null)
                {
                    throw new UserNotOnlineException("No active session found for this user.");
                }

                Lobby lobby = client.CurrentLobby;

                if (lobby == null)
                {
                    throw new UserNotInLobbyException("You are not inside a lobby, you cannot send messages.");
                }

                try
                {
                    lobby.BroadcastChatMessage(client.Nickname, message);

                    _logger.InfoFormat("[SendMessage] Message sent in lobby '{0}'.", lobby.LobbyCode);
                }
                catch (ForbiddenWordException)
                {
                    _logger.WarnFormat("[SendMessage] Profanity detected from {0}. Proceeding to kick.", client.Nickname);
                    _lobbyManager.KickPlayer(lobby.Host, client.UserId);
                }
                catch (ChatException ex) when (ex.Message.Contains(SPAM_KEYWORD))
                {
                    _logger.WarnFormat("[SendMessage] Spam detected from {0}. Proceeding to kick.", client.Nickname);
                    _lobbyManager.KickPlayer(lobby.Host, client.UserId);
                }

            }, "SendMessage");
        }
    }
}