using BusinessLogic.Models;
using Contracts.DTOs;
using Contracts.Faults;
using log4net;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.ServiceModel;

namespace BusinessLogic.Logic
{
    public class LobbyManager
    {
        private static readonly Lazy<LobbyManager> _instance = new Lazy<LobbyManager>(() => new LobbyManager());
        public static LobbyManager Instance => _instance.Value;

        private static readonly ILog _logger = LogManager.GetLogger(typeof(LobbyManager));

        private readonly ConcurrentDictionary<string, Lobby> _lobbies = new ConcurrentDictionary<string, Lobby>();

        private LobbyManager() { }

        public LobbyStateDto CreateLobby(PlayerClient host)
        {
            try
            {
                var lobbyCode = GenerateLobbyCode();
                var lobby = new Lobby(lobbyCode, host);
                _lobbies[lobbyCode] = lobby;

                _logger.Info($"Lobby creado: {lobbyCode} por host {host.UserId}");

                return new LobbyStateDto
                {
                    LobbyCode = lobbyCode,
                    Players = lobby.GetPlayerDTOs()
                };
            }
            catch (Exception ex)
            {
                var fatalReason = "Error inesperado creando un lobby.";
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

        public LobbyStateDto JoinLobby(PlayerClient player, string lobbyCode)
        {
            try
            {
                if (string.IsNullOrEmpty(lobbyCode))
                {
                    var reason = "El código del lobby no puede ser nulo o vacío.";
                    _logger.Error($"Jugador {player.UserId} intentó unirse con código vacío.");
                    throw new FaultException<ServiceFault>(
                        new ServiceFault 
                        { 
                            Message = reason 
                        },
                        new FaultReason(reason)
                    );
                }

                if (!_lobbies.TryGetValue(lobbyCode, out var lobby))
                {
                    var reason = "El lobby no existe.";
                    _logger.Error($"Jugador {player.UserId} intentó unirse a lobby inexistente {lobbyCode}.");
                    throw new FaultException<ServiceFault>(
                        new ServiceFault 
                        { 
                            Message = reason 
                        },
                        new FaultReason(reason)
                    );
                }

                if (!lobby.AddPlayer(player))
                {
                    var reason = "El lobby está lleno o ya estás en él.";
                    _logger.Error($"Jugador {player.UserId} no pudo unirse al lobby {lobbyCode}: lleno o ya presente.");
                    throw new FaultException<ServiceFault>(
                        new ServiceFault 
                        { 
                            Message = reason 
                        },
                        new FaultReason(reason)
                    );
                }

                lobby.BroadcastPlayerJoined(player);
                _logger.Info($"Jugador {player.UserId} se unió al lobby {lobbyCode}");

                return new LobbyStateDto
                {
                    LobbyCode = lobbyCode,
                    Players = lobby.GetPlayerDTOs()
                };
            }
            catch (FaultException<ServiceFault> fault)
            {
                _logger.Error($"Error controlado al jugador {player?.UserId} unirse al lobby {lobbyCode}: {fault.Reason}", fault);
                throw;
            }
            catch (Exception ex)
            {
                var fatalReason = $"Error inesperado al jugador {player?.UserId} unirse al lobby {lobbyCode}.";
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

        public void LeaveLobby(PlayerClient player)
        {
            try
            {
                if (player?.CurrentLobby == null) return;

                var lobby = player.CurrentLobby;
                lobby.RemovePlayer(player);

                if (player.UserId == lobby.Host.UserId)
                {
                    lobby.BroadcastLobbyClosed();
                    _lobbies.TryRemove(lobby.LobbyCode, out _);
                    _logger.Info($"Host {player.UserId} cerró el lobby {lobby.LobbyCode}");
                }
                else
                {
                    lobby.BroadcastPlayerLeft(player.UserId);
                    _logger.Info($"Jugador {player.UserId} salió del lobby {lobby.LobbyCode}");
                }
            }
            catch (Exception ex)
            {
                var fatalReason = $"Error inesperado al jugador {player?.UserId} salir del lobby.";
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

        public void KickPlayer(PlayerClient host, int targetPlayerId)
        {
            try
            {
                var lobby = host.CurrentLobby;
                if (lobby == null || lobby.Host.UserId != host.UserId)
                {
                    var reason = "No tienes permiso para expulsar jugadores.";
                    _logger.Error($"Host {host.UserId} intentó expulsar sin permiso.");
                    throw new FaultException<ServiceFault>(
                        new ServiceFault 
                        { 
                            Message = reason 
                        },
                        new FaultReason(reason)
                    );
                }

                var playerToKick = GlobalSessionManager.Instance.GetClient(targetPlayerId);
                if (playerToKick == null || playerToKick.CurrentLobby != lobby)
                {
                    var reason = "El jugador no está en tu lobby.";
                    _logger.Error($"Intento inválido de expulsión: jugador {targetPlayerId} no está en el lobby {lobby.LobbyCode}");
                    throw new FaultException<ServiceFault>(
                        new ServiceFault 
                        { 
                            Message = reason 
                        },
                        new FaultReason(reason)
                    );
                }

                if (playerToKick.UserId == host.UserId)
                {
                    var reason = "No puedes expulsarte a ti mismo.";
                    _logger.Error($"Host {host.UserId} intentó expulsarse a sí mismo en el lobby {lobby.LobbyCode}");
                    throw new FaultException<ServiceFault>(
                        new ServiceFault 
                        { 
                            Message = reason 
                        },
                        new FaultReason(reason)
                    );
                }

                lobby.RemovePlayer(playerToKick);
                lobby.BroadcastKicked(targetPlayerId);
                playerToKick.CallbackChannel.YouWereKicked();

                _logger.Info($"Jugador {targetPlayerId} expulsado del lobby {lobby.LobbyCode} por host {host.UserId}");
            }
            catch (FaultException<ServiceFault> fault)
            {
                _logger.Error($"Error controlado al expulsar jugador {targetPlayerId} por host {host.UserId}: {fault.Reason}", fault);
                throw;
            }
            catch (Exception ex)
            {
                var fatalReason = $"Error inesperado al expulsar jugador {targetPlayerId} por host {host.UserId}.";
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

        public Lobby FindLobbyByHostId(int hostUserId)
        {
            return _lobbies.Values.FirstOrDefault(lobby => lobby.Host.UserId == hostUserId);
        }

        private string GenerateLobbyCode()
        {
            var chars = "ABCDEFGHIJKLMNPQRSTUVWXYZ123456789";
            var random = new Random();
            string code;
            do
            {
                code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
            } 
            while (_lobbies.ContainsKey(code));
            
            return code;
        }
    }
}
