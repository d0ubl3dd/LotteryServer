using BusinessLogic.Exceptions;
using BusinessLogic.Models;
using Contracts.Callbacks;
using Contracts.Faults;
using DataAccess;
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

        private GlobalSessionManager()
        {
            _logger.Info("GlobalSessionManager inicializado.");
        }

        public void RegisterClient(User user, ILotteryCallback callback)
        {
            ExecuteFaultSafe(() =>
            {
                if (user == null) throw new ArgumentNullException(nameof(user), "El usuario es nulo.");
                if (callback == null) throw new ArgumentNullException(nameof(callback), "El callback es nulo.");

                var client = new PlayerClient(user, callback);

                _onlineUsers[user.id_user] = client;

                _logger.Info($"[RegisterClient] Usuario registrado: {user.id_user} - {user.nickname}");

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
                    throw new ClientNotFoundException($"No se pudo desconectar al usuario {userId} porque no estaba registrado.");
                }

                _logger.Info($"[UnregisterClient] Usuario eliminado de memoria: {userId}");
                return client;

            }, "UnregisterClient");
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
                    _logger.Warn($"[{operationName}] Cliente no encontrado en memoria.");
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