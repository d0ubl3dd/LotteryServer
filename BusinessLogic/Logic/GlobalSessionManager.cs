using BusinessLogic.Exceptions;
using BusinessLogic.Models;
using Contracts.Callbacks;
using Contracts.Faults;
using DataAccess;
using DataAccess.DAOs;
using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace BusinessLogic.Logic
{
    public class GlobalSessionManager : ISessionManager
    {
        private static readonly Lazy<GlobalSessionManager> _instance =
            new Lazy<GlobalSessionManager>(() => new GlobalSessionManager());
        public static GlobalSessionManager Instance => _instance.Value;

        private static readonly ILog _logger = LogManager.GetLogger(typeof(GlobalSessionManager));

        private readonly ConcurrentDictionary<int, PlayerClient> _onlineUsers =
            new ConcurrentDictionary<int, PlayerClient>();

        private readonly IUserDao _userDao;

        private GlobalSessionManager()
        {
            _userDao = new UserDao();
            _logger.Info("GlobalSessionManager inicializado.");
        }

        public void RegisterClient(User user, ILotteryCallback callback)
        {
            ExecuteFaultSafe(() =>
            {
                if (user == null) throw new ArgumentNullException(nameof(user));
                if (callback == null) throw new ArgumentNullException(nameof(callback));

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
                if (!_onlineUsers.TryGetValue(userId, out var client))
                {
                    throw new ClientNotFoundException($"El cliente con ID {userId} no está conectado.");
                }

                _logger.Info($"[GetClient] Cliente recuperado: {userId}");
                return client;

            }, "GetClient");
        }

        public PlayerClient UnregisterClient(int userId)
        {
            return ExecuteFaultSafe(() =>
            {
                if (!_onlineUsers.TryRemove(userId, out var client))
                {
                    _logger.Warn($"[UnregisterClient] Usuario {userId} ya estaba desconectado. No se toma acción.");
                    return null;
                }

                _logger.Info($"[UnregisterClient] Usuario desconectado y marcado como Offline: {userId}");
                return client;

            }, "UnregisterClient");
        }

        private void AutoDisconnect(int userId)
        {
            try
            {
                if (!_onlineUsers.ContainsKey(userId))
                {
                    _logger.Warn($"[AutoDisconnect] Usuario {userId} ya estaba desconectado. Ignorando evento duplicado.");
                    return;
                }

                _logger.Warn($"[AutoDisconnect] Detectada desconexión para userId={userId}. Procediendo a limpiar sesión.");
                UnregisterClient(userId);
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
                return (int?)entry.Key;

            }, "GetUserIdFromContext");
        }

        public IEnumerable<PlayerClient> GetAllOnlineUsers()
        {
            return _onlineUsers.Values;
        }

        private void ExecuteFaultSafe(Action action, string operationName)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                HandleException(ex, operationName);
            }
        }

        private T ExecuteFaultSafe<T>(Func<T> action, string operationName)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                HandleException(ex, operationName);
                return default;
            }
        }

        private void HandleException(Exception ex, string operationName)
        {
            if (ex is FaultException<ServiceFault>)
                throw ex;

            string errorCode;
            string clientMessage;

            switch (ex)
            {
                case ClientNotFoundException _:
                    errorCode = "SESSION_CLIENT_NOT_FOUND";
                    clientMessage = ex.Message;
                    _logger.Warn($"[{operationName}] Cliente no encontrado.");
                    break;

                case SessionContextException _:
                    errorCode = "SESSION_CONTEXT_ERROR";
                    clientMessage = "Error de comunicación o sesión perdida.";
                    _logger.Error($"[{operationName}] Error de contexto WCF: {ex.Message}");
                    break;

                case ArgumentNullException _:
                    errorCode = "SESSION_BAD_REQUEST";
                    clientMessage = "Datos de sesión inválidos.";
                    _logger.Error($"[{operationName}] Argumento nulo: {ex.Message}");
                    break;

                default:
                    errorCode = "SESSION_INTERNAL_ERROR";
                    clientMessage = "Error interno en el gestor de sesiones.";
                    _logger.Fatal($"[{operationName}] Error crítico inesperado: {ex}", ex);
                    break;
            }

            throw new FaultException<ServiceFault>(
                new ServiceFault
                {
                    ErrorCode = errorCode,
                    Message = clientMessage
                },
                new FaultReason(clientMessage)
            );
        }
    }
}
