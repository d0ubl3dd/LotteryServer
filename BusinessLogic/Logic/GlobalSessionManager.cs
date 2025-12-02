using BusinessLogic.Exceptions;
using BusinessLogic.Logic;
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

namespace BusinessLogic.Logic
{
    public class GlobalSessionManager : BaseHandler, ISessionManager
    {
        private static readonly Lazy<GlobalSessionManager> _instance =
            new Lazy<GlobalSessionManager>(() => new GlobalSessionManager());
        public static GlobalSessionManager Instance => _instance.Value;

        private readonly ConcurrentDictionary<int, PlayerClient> _onlineUsers =
            new ConcurrentDictionary<int, PlayerClient>();

        private readonly IUserDao _userDao;

        private GlobalSessionManager() : base(typeof(GlobalSessionManager))
        {
            _userDao = new UserDao();
            _logger.Info("GlobalSessionManager inicializado.");
        }

        public void RegisterClient(User user, ILotteryCallback callback)
        {
            ExecuteFaultSafe(() =>
            {
                if (user == null)
                {
                    throw new ArgumentNullException(nameof(user));
                }
                if (callback == null)
                {
                    throw new ArgumentNullException(nameof(callback));
                }

                var client = new PlayerClient(user, callback);
                _onlineUsers[user.id_user] = client;

                _logger.Info($"[RegisterClient] Usuario registrado: {user.id_user} - {user.nickname}");

                if (callback is ICommunicationObject channel)
                {
                    channel.Closed += (s, e) => AutoDisconnect(user.id_user);
                    channel.Faulted += (s, e) => AutoDisconnect(user.id_user);
                }
            }, "RegisterClient");
        }

        public PlayerClient GetClient(int userId)
        {
            return ExecuteFaultSafe(() =>
            {
                PlayerClient clientResult;

                if (!_onlineUsers.TryGetValue(userId, out var client))
                {
                    throw new ClientNotFoundException($"El cliente con ID {userId} no está conectado.");
                }

                _logger.Info($"[GetClient] Cliente recuperado: {userId}");
                clientResult = client;

                return clientResult;

            }, "GetClient");
        }

        public PlayerClient UnregisterClient(int userId)
        {
            return ExecuteFaultSafe(() =>
            {
                PlayerClient clientResult = null;

                if (!_onlineUsers.TryRemove(userId, out var client))
                {
                    _logger.Warn($"[UnregisterClient] Usuario {userId} ya estaba desconectado. No se toma acción.");
                }
                else
                {
                    _logger.Info($"[UnregisterClient] Usuario desconectado y marcado como Offline: {userId}");
                    clientResult = client;
                }

                return clientResult;

            }, "UnregisterClient");
        }

        private void AutoDisconnect(int userId)
        {
            try
            {
                if (!_onlineUsers.ContainsKey(userId))
                {
                    _logger.Warn($"[AutoDisconnect] Usuario {userId} ya estaba desconectado. Ignorando evento duplicado.");
                }
                else
                {
                    _logger.Warn($"[AutoDisconnect] Detectada desconexión para userId={userId}. Procediendo a limpiar sesión.");
                    UnregisterClient(userId);
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"[AutoDisconnect] Error al desconectar automáticamente userId={userId}: {ex.Message}");
            }
        }

        public int? GetUserIdFromContext()
        {
            return ExecuteFaultSafe(() =>
            {
                int? resultId;

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

                _logger.Info($"[GetUserIdFromContext] Usuario identificado: {entry.Key}");
                resultId = (int?)entry.Key;

                return resultId;

            }, "GetUserIdFromContext");
        }

        public IEnumerable<PlayerClient> GetAllOnlineUsers()
        {
            return _onlineUsers.Values;
        }
    }
}