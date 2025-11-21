using BusinessLogic.Models;
using Contracts.Callbacks;
using DataAccess;
using Contracts.Faults;
using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace BusinessLogic.Logic
{
    public class GlobalSessionManager
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
            try
            {
                if (user == null || callback == null)
                {
                    var reason = "Parámetros de registro inválidos.";
                    _logger.Error(reason);
                    throw new FaultException<ServiceFault>(
                        new ServiceFault 
                        { 
                            Message = reason 
                        },
                        new FaultReason(reason)
                    );
                }

                var client = new PlayerClient(user, callback);
                _onlineUsers[user.id_user] = client;
                _logger.Info($"Usuario registrado: {user.id_user} - {user.nickname}");
            }
            catch (FaultException<ServiceFault> fault)
            {
                _logger.Error("Error controlado al registrar cliente: " + fault.Reason.ToString());
                throw;
            }
            catch (Exception ex)
            {
                var fatalReason = "Error inesperado al registrar cliente.";
                _logger.Fatal(fatalReason, ex);
                throw new FaultException<ServiceFault>(
                    new ServiceFault
                    {
                        Message = fatalReason 
                    },
                    new FaultReason(fatalReason)
                );
            }
        }

        public PlayerClient GetClient(int userId)
        {
            try
            {
                if (!_onlineUsers.TryGetValue(userId, out var client))
                {
                    var reason = $"Cliente con UserId {userId} no encontrado.";
                    _logger.Error(reason);
                    throw new FaultException<ServiceFault>(
                        new ServiceFault 
                        {
                            Message = reason 
                        },
                        new FaultReason(reason)
                    );
                }

                _logger.Info($"Cliente obtenido: {userId}");
                return client;
            }
            catch (FaultException<ServiceFault> fault)
            {
                _logger.Error("Error controlado al obtener cliente: " + fault.Reason.ToString());
                throw;
            }
            catch (Exception ex)
            {
                var fatalReason = $"Error inesperado al obtener cliente {userId}.";
                _logger.Fatal(fatalReason, ex);
                throw new FaultException<ServiceFault>(
                    new ServiceFault 
                    {
                        Message = fatalReason
                    },
                    new FaultReason(fatalReason)
                );
            }
        }

        public PlayerClient UnregisterClient(int userId)
        {
            try
            {
                _onlineUsers.TryRemove(userId, out var client);
                if (client != null)
                {
                    _logger.Info($"Usuario desconectado: {userId}");
                    return client;
                }
                else
                {
                    var reason = $"Usuario {userId} no estaba registrado.";
                    _logger.Error(reason);
                    throw new FaultException<ServiceFault>(
                        new ServiceFault 
                        {
                            Message = reason 
                        },
                        new FaultReason(reason)
                    );
                }
            }
            catch (FaultException<ServiceFault> fault)
            {
                _logger.Error("Error controlado al desconectar cliente: " + fault.Reason.ToString());
                throw;
            }
            catch (Exception ex)
            {
                var fatalReason = $"Error inesperado al desconectar cliente {userId}.";
                _logger.Fatal(fatalReason, ex);
                throw new FaultException<ServiceFault>(
                    new ServiceFault 
                    {
                        Message = fatalReason 
                    },
                    new FaultReason(fatalReason)
                );
            }
        }

        public int? GetUserIdFromContext()
        {
            try
            {
                var callback = OperationContext.Current?.GetCallbackChannel<ILotteryCallback>();
                if (callback == null)
                {
                    var reason = "No se pudo obtener el usuario desde el contexto.";
                    _logger.Error(reason);
                    throw new FaultException<ServiceFault>(
                        new ServiceFault
                        { 
                            Message = reason 
                        },
                        new FaultReason(reason)
                    );
                }

                var entry = _onlineUsers.FirstOrDefault(x => x.Value.CallbackChannel == callback);
                if (!entry.Equals(default(KeyValuePair<int, PlayerClient>)))
                {
                    _logger.Info($"Usuario obtenido desde contexto: {entry.Key}");
                    return entry.Key;
                }
                else
                {
                    var reason = "Callback no registrado en GlobalSessionManager.";
                    _logger.Error(reason);
                    throw new FaultException<ServiceFault>(
                        new ServiceFault 
                        {
                            Message = reason
                        },
                        new FaultReason(reason)
                    );
                }
            }
            catch (FaultException<ServiceFault> fault)
            {
                _logger.Error("Error controlado al obtener usuario desde contexto: " + fault.Reason.ToString());
                throw;
            }
            catch (Exception ex)
            {
                var fatalReason = "Error inesperado al obtener usuario desde contexto.";
                _logger.Fatal(fatalReason, ex);
                throw new FaultException<ServiceFault>(
                    new ServiceFault 
                    {
                        Message = fatalReason 
                    },
                    new FaultReason(fatalReason)
                );
            }
        }

        public IEnumerable<PlayerClient> GetAllOnlineUsers()
        {
            return _onlineUsers.Values;
        }
    }
}
