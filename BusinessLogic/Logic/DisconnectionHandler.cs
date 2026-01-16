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

        // Lock para asegurar que no desconectamos al mismo usuario dos veces simultáneamente
        private static readonly ConcurrentDictionary<int, bool> _processingDisconnections = new ConcurrentDictionary<int, bool>();

        public DisconnectionHandler(ISessionManager sessionManager, ILobbyManager lobbyManager)
        {
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            _lobbyManager = lobbyManager ?? throw new ArgumentNullException(nameof(lobbyManager));
        }

        public async Task HandleDisconnectionAsync(int userId, string reason)
        {
            // Evitar re-entrada para el mismo usuario
            if (!_processingDisconnections.TryAdd(userId, true))
            {
                _logger.Warn($"[DisconnectionHandler] Ya se está procesando la desconexión para {userId}.");
                return;
            }

            try
            {
                _logger.Info($"[DisconnectionHandler] INICIANDO limpieza para usuario {userId}. Razón: {reason}");

                PlayerClient client = null;

                // 1. Intentamos obtener el cliente de la memoria global
                try
                {
                    if (_sessionManager.IsUserOnline(userId))
                    {
                        client = _sessionManager.GetClient(userId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn($"[DisconnectionHandler] No se pudo recuperar cliente {userId} de SessionManager: {ex.Message}");
                }

                // 2. RECUPERACIÓN DE EMERGENCIA (FALLBACK)
                // Si el usuario ya fue borrado de SessionManager por una race condition,
                // buscamos si existe en algún lobby activo directamente en el LobbyManager.
                if (client == null)
                {
                    _logger.Warn($"[DisconnectionHandler] Usuario {userId} no encontrado en sesión global. Buscando en Lobbies activos...");

                    // Necesitamos un método en LobbyManager para buscar lobbies por ID de usuario
                    // Si no existe, usamos la referencia que tengamos
                    Lobby lobby = _lobbyManager.FindLobbyByPlayerId(userId); // Asumo que tienes este método en LobbyManager

                    if (lobby != null)
                    {
                        _logger.Info($"[DisconnectionHandler] Usuario {userId} encontrado en Lobby {lobby.LobbyCode} (Modo Fantasma). Reconstruyendo objeto cliente temporal.");

                        // Creamos un cliente temporal solo para ejecutar la lógica de salida
                        // Usamos reflexión o cambiamos el modelo si UserId es solo get, 
                        // pero tu modelo tiene constructor:
                        client = new PlayerClient(userId, "DisconnectingUser", 0, null)
                        {
                            CurrentLobby = lobby
                        };
                    }
                }

                // 3. Ejecutar la salida del Lobby
                if (client != null && client.CurrentLobby != null)
                {
                    _logger.Info($"[DisconnectionHandler] Ejecutando LeaveLobby para usuario {userId} en lobby {client.CurrentLobby.LobbyCode}...");

                    // Ejecutamos de forma síncrona dentro del Task.Run para asegurar que termine antes de limpiar sesión
                    await Task.Run(() =>
                    {
                        try
                        {
                            _lobbyManager.LeaveLobby(client);
                        }
                        catch (Exception ex)
                        {
                            _logger.Error($"Error al ejecutar LeaveLobby para {userId}", ex);
                        }
                    });
                }
                else
                {
                    _logger.Info($"[DisconnectionHandler] El usuario {userId} no estaba en ningún lobby o no se pudo determinar.");
                }

                // 4. Limpieza final de Sesión Global
                _sessionManager.UnregisterClient(userId);

                _logger.Info($"[DisconnectionHandler] Limpieza COMPLETADA para {userId}.");
            }
            catch (Exception ex)
            {
                _logger.Error($"[DisconnectionHandler] Error crítico desconectando usuario {userId}", ex);
            }
            finally
            {
                _processingDisconnections.TryRemove(userId, out _);
            }
        }
    }
}