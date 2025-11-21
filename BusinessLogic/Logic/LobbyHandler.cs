using BusinessLogic.Models;
using Contracts.DTOs;
using Contracts.Faults;
using DataAccess;
using log4net;
using System;
using System.ServiceModel;
using System.Threading.Tasks;

namespace BusinessLogic.Logic
{
    public class LobbyHandler
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(LobbyHandler));

        private readonly LobbyManager _lobbyManager;

        public LobbyHandler(LobbyManager lobbyManager)
        {
            _lobbyManager = lobbyManager;
        }

        private PlayerClient GetClient(User user)
        {
            try
            {
                _logger.Info($"Obteniendo cliente para el usuario {user.nickname} (ID {user.id_user}).");

                var client = GlobalSessionManager.Instance.GetClient(user.id_user);

                if (client == null)
                {
                    var reason = "Error de sesión. No se encontró el cliente.";
                    _logger.Error(reason);
                    throw new FaultException<ServiceFault>(
                        new ServiceFault 
                        {
                            Message = reason 
                        },
                        new FaultReason(reason)
                    );
                }

                return client;
            }
            catch (FaultException<ServiceFault> fault)
            {
                _logger.Error("Error controlado al obtener cliente: " + fault.Reason.ToString());
                throw;
            }
            catch (Exception ex)
            {
                var fatalReason = "Error inesperado obteniendo cliente para usuario.";
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

        public Task<LobbyStateDto> CreateLobby(User currentUser)
        {
            try
            {
                _logger.Info($"Intento de crear lobby por {currentUser.nickname}.");

                var hostClient = GetClient(currentUser);

                if (hostClient.CurrentLobby != null)
                {
                    var reason = "Ya estás en un lobby.";
                    _logger.Error(reason);
                    throw new FaultException<ServiceFault>(
                        new ServiceFault
                        {
                            Message = reason 
                        },
                        new FaultReason(reason)
                    );
                }

                var lobbyState = _lobbyManager.CreateLobby(hostClient);
                _logger.Info($"Lobby creado correctamente por {currentUser.nickname}. Código: {lobbyState.LobbyCode}");

                return Task.FromResult(lobbyState);
            }
            catch (FaultException<ServiceFault> fault)
            {
                _logger.Error("Error controlado al crear lobby: " + fault.Reason.ToString());
                throw;
            }
            catch (Exception ex)
            {
                var fatalReason = "Error inesperado al crear lobby.";
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

        public Task<LobbyStateDto> JoinLobby(User currentUser, string lobbyCode)
        {
            try
            {
                _logger.Info($"Usuario {currentUser.nickname} intentando unirse al lobby {lobbyCode}.");

                var playerClient = GetClient(currentUser);

                if (playerClient.CurrentLobby != null)
                {
                    var reason = "Ya estás en un lobby.";
                    _logger.Error(reason);
                    throw new FaultException<ServiceFault>(
                        new ServiceFault 
                        {
                            Message = reason 
                        },
                        new FaultReason(reason)
                    );
                }

                var lobbyState = _lobbyManager.JoinLobby(playerClient, lobbyCode);
                _logger.Info($"Usuario {currentUser.nickname} se unió correctamente al lobby {lobbyCode}.");

                return Task.FromResult(lobbyState);
            }
            catch (FaultException<ServiceFault> fault)
            {
                _logger.Error($"Error controlado al unirse al lobby {lobbyCode}: " + fault.Reason.ToString());
                throw;
            }
            catch (Exception ex)
            {
                var fatalReason = $"Error inesperado al unirse al lobby {lobbyCode}.";
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

        public Task LeaveLobby(User currentUser)
        {
            try
            {
                _logger.Info($"Usuario {currentUser.nickname} solicitó salir de su lobby actual.");

                var client = GlobalSessionManager.Instance.GetClient(currentUser.id_user);

                if (client != null)
                {
                    _lobbyManager.LeaveLobby(client);
                    _logger.Info($"Usuario {currentUser.nickname} salió correctamente del lobby.");
                }
                else
                {
                    var reason = $"No se encontró cliente activo para {currentUser.nickname} al intentar salir del lobby.";
                    _logger.Error(reason);
                    throw new FaultException<ServiceFault>(
                        new ServiceFault 
                        {
                            Message = reason 
                        },
                        new FaultReason(reason)
                    );
                }

                return Task.CompletedTask;
            }
            catch (FaultException<ServiceFault> fault)
            {
                _logger.Error("Error controlado al salir del lobby: " + fault.Reason.ToString());
                throw;
            }
            catch (Exception ex)
            {
                var fatalReason = "Error inesperado al salir del lobby.";
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

        public Task KickPlayer(User currentUser, int targetPlayerId)
        {
            try
            {
                _logger.Info($"Usuario {currentUser.nickname} intentando expulsar al jugador con ID {targetPlayerId}.");

                var hostClient = GetClient(currentUser);

                _lobbyManager.KickPlayer(hostClient, targetPlayerId);

                _logger.Info($"Jugador {targetPlayerId} expulsado correctamente por {currentUser.nickname}.");

                return Task.CompletedTask;
            }
            catch (FaultException<ServiceFault> fault)
            {
                _logger.Error($"Error controlado al expulsar jugador {targetPlayerId}: " + fault.Reason.ToString());
                throw;
            }
            catch (Exception ex)
            {
                var fatalReason = $"Error inesperado al expulsar jugador {targetPlayerId}.";
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
    }
}