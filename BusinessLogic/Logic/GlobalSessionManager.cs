using BusinessLogic.Exceptions;
using BusinessLogic.Logic.Base;
using BusinessLogic.Models;
using Contracts.Callbacks;
using DataAccess;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace BusinessLogic.Logic
{
    public class GlobalSessionManager : BaseHandler, ISessionManager
    {
        private static readonly Lazy<GlobalSessionManager> _instance =
            new Lazy<GlobalSessionManager>(() => new GlobalSessionManager());
        public static GlobalSessionManager Instance => _instance.Value;

        private readonly ConcurrentDictionary<int, PlayerClient> _onlineUsers =
            new ConcurrentDictionary<int, PlayerClient>();

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
                if (!_onlineUsers.TryRemove(userId, out var client))
                {
                    _logger.WarnFormat("[UnregisterClient] Usuario {0} ya estaba desconectado. No se toma acción.", userId);
                    return null;
                }

                _logger.InfoFormat("[UnregisterClient] Usuario desconectado y marcado como Offline: {0}", userId);
                return client;

            }, "UnregisterClient");
        }

        private void AutoDisconnect(int userId)
        {
            try
            {
                var disconnectedClient = UnregisterClient(userId);

                if (disconnectedClient == null)
                {
                    _logger.WarnFormat("[AutoDisconnect] Usuario {0} ya estaba desconectado. Ignorando evento duplicado.", userId);
                }
                else
                {
                    _logger.WarnFormat("[AutoDisconnect] Detectada desconexión para userId={0}. Procediendo a limpiar sesión.", userId);
                }
            }
            catch (Exception exception)
            {
                _logger.WarnFormat("[AutoDisconnect] Error al desconectar automáticamente userId={0}. Detalle: {1}", userId, exception);
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
                if (_onlineUsers.TryGetValue(userId, out var existingClient))
                {
                    _logger.InfoFormat("[ReconnectUser] Usuario {0} reconectando. Reemplazando callback.", userId);
                    existingClient.CallbackChannel = newCallback;
                }
                else
                {
                    _logger.WarnFormat("[ReconnectUser] Usuario {0} no estaba registrado. Se registra como nuevo.", userId);

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
                channel.Closed += (s, e) => AutoDisconnect(userId);
                channel.Faulted += (s, e) => AutoDisconnect(userId);
            }
        }
    }
}