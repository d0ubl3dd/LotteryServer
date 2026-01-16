using BusinessLogic.Logic;
using BusinessLogic.Models;
using log4net;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace BusinessLogic.Handlers
{
    public class DisconnectionHandler
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(DisconnectionHandler));

        private readonly ISessionManager _sessionManager;
        private readonly ILobbyManager _lobbyManager;

        private static readonly ConcurrentDictionary<int, bool> _processingDisconnections = new ConcurrentDictionary<int, bool>();

        public DisconnectionHandler(ISessionManager sessionManager, ILobbyManager lobbyManager)
        {
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            _lobbyManager = lobbyManager ?? throw new ArgumentNullException(nameof(lobbyManager));
        }

        public async Task HandleDisconnectionAsync(int userId, string reason)
        {
            if (!_processingDisconnections.TryAdd(userId, true))
            {
                _logger.Warn($"[DisconnectionHandler] Disconnection already in progress for {userId}.");
                return;
            }

            try
            {
                _logger.Info($"[DisconnectionHandler] STARTING cleanup for user {userId}. Reason: {reason}");

                PlayerClient client = null;

                try
                {
                    if (_sessionManager.IsUserOnline(userId))
                    {
                        client = _sessionManager.GetClient(userId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn($"[DisconnectionHandler] Could not retrieve client {userId} from SessionManager: {ex.Message}");
                }

                if (client == null)
                {
                    _logger.Warn($"[DisconnectionHandler] User {userId} not found in global session. Searching in active Lobbies...");

                    Lobby lobby = _lobbyManager.FindLobbyByPlayerId(userId);

                    if (lobby != null)
                    {
                        _logger.Info($"[DisconnectionHandler] User {userId} found in Lobby {lobby.LobbyCode} " +
                            $"(Ghost Mode). Reconstructing temporary client object.");

                        client = new PlayerClient(userId, "DisconnectingUser", 0, null)
                        {
                            CurrentLobby = lobby
                        };
                    }
                }

                if (client != null && client.CurrentLobby != null)
                {
                    _logger.Info($"[DisconnectionHandler] Executing LeaveLobby for user {userId} in lobby {client.CurrentLobby.LobbyCode}...");

                    await Task.Run(() =>
                    {
                        try
                        {
                            _lobbyManager.LeaveLobby(client);
                        }
                        catch (Exception ex)
                        {
                            _logger.Error($"Error executing LeaveLobby for {userId}", ex);
                        }
                    });
                }
                else
                {
                    _logger.Info($"[DisconnectionHandler] User {userId} was not in any lobby or could not be determined.");
                }

                _sessionManager.UnregisterClient(userId);

                _logger.Info($"[DisconnectionHandler] Cleanup COMPLETED for {userId}.");
            }
            catch (Exception ex)
            {
                _logger.Error($"[DisconnectionHandler] Critical error disconnecting user {userId}", ex);
            }
            finally
            {
                _processingDisconnections.TryRemove(userId, out _);
            }
        }
    }
}