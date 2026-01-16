using BusinessLogic.Exceptions;
using BusinessLogic.Handlers;
using BusinessLogic.Logic.Base;
using BusinessLogic.Models;
using Contracts.Callbacks;
using DataAccess;
using DataAccess.DAOs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Timers;

namespace BusinessLogic.Logic
{
    public class GlobalSessionManager : BaseHandler, ISessionManager
    {
        private static readonly Lazy<GlobalSessionManager> _instance = new Lazy<GlobalSessionManager>(() => new GlobalSessionManager());

        public static GlobalSessionManager Instance
        {
            get { return _instance.Value; }
        }

        private readonly ConcurrentDictionary<int, PlayerClient> _onlineUsers = new ConcurrentDictionary<int, PlayerClient>();

        private readonly ConcurrentDictionary<int, Timer> _reconnectionTimers = new ConcurrentDictionary<int, Timer>();

        public ILobbyManager LobbyManagerService { get; set; }

        private const double RECONNECTION_TIMEOUT_MS = 30000;

        private GlobalSessionManager() : base(typeof(GlobalSessionManager))
        {
            _logger.Info("GlobalSessionManager inicializado.");
        }

        public void RegisterClient(User user, ILotteryCallback callback)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            ExecuteFaultSafe(() =>
            {
                if (_reconnectionTimers.TryRemove(user.id_user, out var timer))
                {
                    timer.Stop();
                    timer.Dispose();
                    _logger.InfoFormat("[RegisterClient] Timer de desconexión cancelado para usuario {0} por nuevo Login.", user.id_user);
                }

                var client = new PlayerClient(
                            user.id_user,
                            user.nickname,
                            user.id_avatar,
                            callback
                );

                _onlineUsers[user.id_user] = client;

                _logger.InfoFormat("[RegisterClient] Usuario registrado: {0} - {1}", user.id_user, user.nickname);

                SubscribeToChannelEvents(callback, user.id_user);

            }, "RegisterClient");
        }

        public PlayerClient GetClient(int userId)
        {
            return ExecuteFaultSafe(() =>
            {
                if (!_onlineUsers.TryGetValue(userId, out var client))
                {
                    throw new ClientNotFoundException(string.Format("El cliente con ID {0} no está conectado.", userId));
                }

                _logger.InfoFormat("[GetClient] Cliente recuperado: {0}", userId);
                return client;

            }, "GetClient");
        }

        public PlayerClient UnregisterClient(int userId)
        {
            return ExecuteFaultSafe(() =>
            {
                if (_reconnectionTimers.TryRemove(userId, out var timer))
                {
                    timer.Stop();
                    timer.Dispose();
                }

                if (!_onlineUsers.TryRemove(userId, out var client))
                {
                    _logger.WarnFormat("[UnregisterClient] Usuario {0} ya estaba desconectado. No se toma acción.", userId);
                    return null;
                }

                _logger.InfoFormat("[UnregisterClient] Usuario desconectado y marcado como Offline: {0}", userId);
                return client;

            }, "UnregisterClient");
        }

        private void HandleConnectionLost(int userId)
        {
            if (_reconnectionTimers.ContainsKey(userId))
            {
                return;
            }

            _logger.WarnFormat("[HandleConnectionLost] Conexión perdida para usuario {0}. Iniciando espera de {1}ms para reconexión...", userId, RECONNECTION_TIMEOUT_MS);

            var timer = new Timer(RECONNECTION_TIMEOUT_MS);
            timer.AutoReset = false;
            timer.Elapsed += (sender, e) => ExecuteTimeoutDisconnection(userId, timer);

            if (_reconnectionTimers.TryAdd(userId, timer))
            {
                timer.Start();
            }
        }

        private void ExecuteTimeoutDisconnection(int userId, Timer timer)
        {
            try
            {
                timer.Stop();
                timer.Dispose();
                _reconnectionTimers.TryRemove(userId, out _);

                _logger.WarnFormat("[ExecuteTimeoutDisconnection] Tiempo agotado para usuario {0}. Ejecutando limpieza completa.", userId);

                if (this.LobbyManagerService == null)
                {
                    _logger.ErrorFormat("FATAL: LobbyManagerService no ha sido asignado en GlobalSessionManager. No se pueden cerrar lobbies para el usuario {0}.", userId);
                    UnregisterClient(userId);
                    return;
                }

                var disconnectionHandler = new DisconnectionHandler(this, this.LobbyManagerService);

                Task.Run(async () =>
                {
                    await disconnectionHandler.HandleDisconnectionAsync(userId, "Timeout por falta de reconexión");
                });
            }
            catch (Exception ex)
            {
                _logger.ErrorFormat("[ExecuteTimeoutDisconnection] Error crítico limpiando usuario {0}: {1}", userId, ex);
            }
        }

        public int? GetUserIdFromContext()
        {
            return ExecuteFaultSafe(() =>
            {
                var context = OperationContext.Current;
                if (context == null)
                {
                    throw new SessionContextException("El OperationContext actual es nulo.");
                }

                var callback = context.GetCallbackChannel<ILotteryCallback>();
                if (callback == null)
                {
                    throw new SessionContextException("No se pudo obtener el canal de callback del contexto.");
                }

                var entry = _onlineUsers.FirstOrDefault(x => x.Value.CallbackChannel == callback);

                if (entry.Value == null)
                {
                    throw new SessionContextException("El callback del contexto no corresponde a ningún usuario registrado.");
                }

                _logger.InfoFormat("[GetUserIdFromContext] Usuario identificado: {0}", entry.Key);
                return (int?)entry.Key;

            }, "GetUserIdFromContext");
        }

        public void ReconnectUser(int userId, ILotteryCallback newCallback)
        {
            if (newCallback == null)
            {
                throw new ArgumentNullException(nameof(newCallback));
            }

            ExecuteFaultSafe(() =>
            {
                if (_reconnectionTimers.TryRemove(userId, out var timer))
                {
                    timer.Stop();
                    timer.Dispose();
                    _logger.InfoFormat("[ReconnectUser] Usuario {0} reconectado a tiempo. Desconexión cancelada.", userId);
                }

                if (_onlineUsers.TryGetValue(userId, out var existingClient))
                {
                    _logger.InfoFormat("[ReconnectUser] Actualizando canal de callback para usuario {0}.", userId);
                    existingClient.CallbackChannel = newCallback;
                }
                else
                {
                    _logger.WarnFormat("[ReconnectUser] Usuario {0} no encontrado en memoria (posiblemente expiró). Registrando como nuevo.", userId);

                    var client = new PlayerClient(
                        userId,
                        "Unknown",
                        0,
                        newCallback
                    );
                    _onlineUsers[userId] = client;
                }

                SubscribeToChannelEvents(newCallback, userId);

            }, "ReconnectUser");
        }

        public IEnumerable<PlayerClient> GetAllOnlineUsers()
        {
            return _onlineUsers.Values;
        }

        public bool IsUserOnline(int userId)
        {
            return _onlineUsers.ContainsKey(userId);
        }

        private void SubscribeToChannelEvents(ILotteryCallback callback, int userId)
        {
            if (callback is ICommunicationObject channel)
            {
                channel.Closed += (s, e) =>
                {
                    _logger.InfoFormat("Canal cerrado (Closed) para usuario {0}.", userId);
                    UnregisterClient(userId);
                };

                channel.Faulted += (s, e) =>
                {
                    _logger.WarnFormat("Canal falló (Faulted) para usuario {0}.", userId);
                    HandleConnectionLost(userId);
                };
            }
        }
    }
}