using Contracts.DTOs;
using DataAccess;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.ServiceModel;
using Contracts.Faults;
using log4net;

namespace BusinessLogic.Logic
{
    public class FriendHandler
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(FriendHandler));

        public async Task SendRequestFriendship(int currentUserId, int targetUserId)
        {
            _logger.Info($"[SendRequestFriendship] Usuario {currentUserId} intenta agregar a {targetUserId}.");

            try
            {
                if (currentUserId == targetUserId)
                {
                    _logger.Warn($"[SendRequestFriendship] Usuario {currentUserId} intentó agregarse a sí mismo.");
                    throw new FaultException<ServiceFault>(
                        new ServiceFault
                        {
                            ErrorCode = "FR-001",
                            Message = "No puedes agregarte a ti mismo como amigo."
                        },
                        new FaultReason("Solicitud inválida")
                    );
                }

                using (var context = new lottery_databaseEntities())
                {
                    bool exists = await context.Friendship.AnyAsync(f =>
                        (f.id_user_sender == currentUserId && f.id_user_receiver == targetUserId) ||
                        (f.id_user_sender == targetUserId && f.id_user_receiver == currentUserId));

                    if (exists)
                    {
                        _logger.Warn($"[SendRequestFriendship] Ya existe amistad o solicitud entre {currentUserId} y {targetUserId}.");
                        throw new FaultException<ServiceFault>(
                            new ServiceFault
                            {
                                ErrorCode = "FR-002",
                                Message = "Ya existe una solicitud de amistad o ya son amigos."
                            },
                            new FaultReason("Solicitud duplicada")
                        );
                    }

                    var newRequest = new Friendship
                    {
                        id_user_sender = currentUserId,
                        id_user_receiver = targetUserId,
                        status = "Pending"
                    };

                    context.Friendship.Add(newRequest);
                    await context.SaveChangesAsync();

                    _logger.Info($"[SendRequestFriendship] Solicitud enviada de {currentUserId} a {targetUserId}.");
                }
            }
            catch (FaultException<ServiceFault>)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.Fatal($"[SendRequestFriendship] Error inesperado: {ex.Message}", ex);

                throw new FaultException<ServiceFault>(
                    new ServiceFault
                    {
                        ErrorCode = "FR-500",
                        Message = $"Error inesperado en la base de datos: {ex.Message}"
                    },
                    new FaultReason("Error interno del servidor")
                );
            }
        }

        public async Task AcceptFriendRequest(int currentUserId, int requesterId)
        {
            _logger.Info($"[AcceptFriendRequest] Usuario {currentUserId} aceptando solicitud de {requesterId}.");

            using (var context = new lottery_databaseEntities())
            {
                var request = await context.Friendship.FirstOrDefaultAsync(f =>
                    f.id_user_sender == requesterId &&
                    f.id_user_receiver == currentUserId &&
                    f.status == "Pending");

                if (request != null)
                {
                    request.status = "Accepted";
                    await context.SaveChangesAsync();

                    _logger.Info($"[AcceptFriendRequest] Solicitud entre {requesterId} → {currentUserId} aceptada.");
                }
                else
                {
                    _logger.Warn($"[AcceptFriendRequest] No existe solicitud entre {requesterId} y {currentUserId}.");
                    throw new FaultException<ServiceFault>(new ServiceFault
                    {
                        Message = "No se encontró la solicitud de amistad. Es posible que el usuario la haya cancelado."
                    });
                }
            }
        }

        public async Task RejectFriendRequest(int currentUserId, int requesterId)
        {
            _logger.Info($"[RejectFriendRequest] Usuario {currentUserId} rechazando solicitud de {requesterId}.");

            using (var context = new lottery_databaseEntities())
            {
                var request = await context.Friendship.FirstOrDefaultAsync(f =>
                    f.id_user_sender == requesterId &&
                    f.id_user_receiver == currentUserId &&
                    f.status == "Pending");

                if (request != null)
                {
                    context.Friendship.Remove(request);
                    await context.SaveChangesAsync();

                    _logger.Info($"[RejectFriendRequest] Solicitud de {requesterId} → {currentUserId} rechazada.");
                }
                else
                {
                    _logger.Warn($"[RejectFriendRequest] No existe solicitud de {requesterId} para {currentUserId}.");
                    throw new FaultException<ServiceFault>(new ServiceFault
                    {
                        Message = "No se encontró la solicitud de amistad."
                    });
                }
            }
        }

        public async Task CancelFriendRequest(int currentUserId, int targetUserId)
        {
            _logger.Info($"[CancelFriendRequest] Usuario {currentUserId} cancelando solicitud enviada a {targetUserId}.");

            using (var context = new lottery_databaseEntities())
            {
                var request = await context.Friendship.FirstOrDefaultAsync(f =>
                    f.id_user_sender == currentUserId &&
                    f.id_user_receiver == targetUserId &&
                    f.status == "Pending");

                if (request != null)
                {
                    context.Friendship.Remove(request);
                    await context.SaveChangesAsync();

                    _logger.Info($"[CancelFriendRequest] Solicitud cancelada {currentUserId} → {targetUserId}.");
                }
                else
                {
                    _logger.Warn($"[CancelFriendRequest] No existe solicitud pendiente de {currentUserId} a {targetUserId}.");
                    throw new FaultException<ServiceFault>(new ServiceFault
                    {
                        Message = "No se ha encontrado ninguna solicitud enviada a ese jugador."
                    });
                }
            }
        }

        public async Task RemoveFriend(int currentUserId, int friendUserId)
        {
            _logger.Info($"[RemoveFriend] Usuario {currentUserId} eliminando amistad con {friendUserId}.");

            using (var context = new lottery_databaseEntities())
            {
                var friendship = await context.Friendship.FirstOrDefaultAsync(f =>
                    ((f.id_user_sender == currentUserId && f.id_user_receiver == friendUserId) ||
                     (f.id_user_sender == friendUserId && f.id_user_receiver == currentUserId)) &&
                    f.status == "Accepted");

                if (friendship != null)
                {
                    context.Friendship.Remove(friendship);
                    await context.SaveChangesAsync();

                    _logger.Info($"[RemoveFriend] Amistad entre {currentUserId} y {friendUserId} eliminada.");
                }
                else
                {
                    _logger.Warn($"[RemoveFriend] No existe amistad entre {currentUserId} y {friendUserId}.");
                    throw new FaultException<ServiceFault>(new ServiceFault
                    {
                        Message = "No se encontró la amistad. Es posible que ya la hayas eliminado."
                    });
                }
            }
        }

        public async Task<List<FriendDto>> GetFriends(int currentUserId)
        {
            _logger.Info($"[GetFriends] Obteniendo lista de amigos para el usuario {currentUserId}.");

            using (var context = new lottery_databaseEntities())
            {
                var friends = await context.Friendship
                    .Where(f => (f.id_user_sender == currentUserId || f.id_user_receiver == currentUserId)
                                && f.status == "Accepted")
                    .Select(f => new
                    {
                        FriendUserId = f.id_user_sender == currentUserId ? f.id_user_receiver : f.id_user_sender
                    })
                    .ToListAsync();

                var friendIds = friends.Select(f => f.FriendUserId).ToList();
                var friendDetails = await context.User
                    .Where(u => friendIds.Contains(u.id_user))
                    .Select(u => new FriendDto
                    {
                        FriendId = u.id_user,
                        Nickname = u.nickname,
                        Status = u.status
                    })
                    .ToListAsync();

                _logger.Info($"[GetFriends] Usuario {currentUserId} tiene {friendDetails.Count} amigos.");

                return friendDetails;
            }
        }

        public async Task<List<FriendDto>> GetPendingRequests(int currentUserId)
        {
            _logger.Info($"[GetPendingRequests] Obteniendo solicitudes pendientes para {currentUserId}.");

            using (var context = new lottery_databaseEntities())
            {
                var requests = await context.Friendship
                    .Where(f => f.id_user_receiver == currentUserId && f.status == "Pending")
                    .Join(context.User,
                        friendship => friendship.id_user_sender,
                        user => user.id_user,
                        (friendship, user) => new FriendDto
                        {
                            FriendId = user.id_user,
                            Nickname = user.nickname
                        })
                    .ToListAsync();

                _logger.Info($"[GetPendingRequests] Usuario {currentUserId} tiene {requests.Count} solicitudes pendientes.");

                return requests;
            }
        }

        public async Task<List<FriendDto>> GetSentRequests(int currentUserId)
        {
            _logger.Info($"[GetSentRequests] Obteniendo solicitudes enviadas por {currentUserId}.");

            using (var context = new lottery_databaseEntities())
            {
                var requests = await context.Friendship
                    .Where(f => f.id_user_sender == currentUserId && f.status == "Pending")
                    .Join(context.User,
                        friendship => friendship.id_user_receiver,
                        user => user.id_user,
                        (friendship, user) => new FriendDto
                        {
                            FriendId = currentUserId,
                            UserId = user.id_user,
                            Nickname = user.nickname
                        })
                    .ToListAsync();

                _logger.Info($"[GetSentRequests] Usuario {currentUserId} ha enviado {requests.Count} solicitudes.");

                return requests;
            }
        }

        public Task InviteFriendToLobby(string lobbyCode, int targetFriendId)
        {
            _logger.Info($"[InviteFriendToLobby] Invitando a {targetFriendId} al lobby {lobbyCode}.");

            int? currentUserId = GlobalSessionManager.Instance.GetUserIdFromContext();

            if (currentUserId == null)
            {
                _logger.Error("[InviteFriendToLobby] No se pudo identificar al usuario actual.");
                throw new FaultException<ServiceFault>(
                    new ServiceFault
                    {
                        Message = "Error de sesión: No se pudo identificar al usuario actual."
                    });
            }

            if (string.IsNullOrEmpty(lobbyCode))
            {
                _logger.Error("[InviteFriendToLobby] Código de lobby nulo.");
                throw new FaultException<ServiceFault>(
                    new ServiceFault
                    {
                        Message = "Error de invitación: El código del lobby es nulo."
                    });
            }

            var inviter = GlobalSessionManager.Instance.GetClient(currentUserId.Value);
            if (inviter == null || inviter.CurrentLobby == null)
            {
                _logger.Error("[InviteFriendToLobby] El usuario no tiene lobby.");
                throw new FaultException<ServiceFault>(
                    new ServiceFault
                    {
                        Message = "Error de sesión: No se encontró tu lobby."
                    });
            }

            if (inviter.CurrentLobby.LobbyCode != lobbyCode)
            {
                _logger.Warn("[InviteFriendToLobby] Intento de invitación desde un lobby diferente.");
                throw new FaultException<ServiceFault>(
                    new ServiceFault
                    {
                        Message = "Error de invitación: No estás en el lobby correcto."
                    });
            }

            var target = GlobalSessionManager.Instance.GetClient(targetFriendId);
            if (target == null)
            {
                _logger.Warn($"[InviteFriendToLobby] Usuario {targetFriendId} no está conectado.");
                throw new FaultException<ServiceFault>(
                    new ServiceFault
                    {
                        Message = "Tu amigo no está conectado.",
                        ErrorCode = "FRIEND_NOT_CONNECTED"
                    },
                    new FaultReason("El amigo no está conectado.")
                );
            }

            if (target.CurrentLobby != null)
            {
                if (target.CurrentLobby == inviter.CurrentLobby)
                {
                    _logger.Info($"[InviteFriendToLobby] {targetFriendId} ya está en el lobby.");
                    throw new FaultException<ServiceFault>(
                        new ServiceFault
                        {
                            Message = "El jugador ya se encuentra en el lobby."
                        });
                }
                else
                {
                    _logger.Warn($"[InviteFriendToLobby] {targetFriendId} ya está en otro lobby.");
                    throw new FaultException<ServiceFault>(
                        new ServiceFault
                        {
                            Message = "Tu amigo ya está en otro lobby."
                        });
                }
            }

            target.CallbackChannel.ReceiveLobbyInvite(inviter.Nickname, lobbyCode);

            _logger.Info($"[InviteFriendToLobby] Invitación enviada a {targetFriendId}.");

            return Task.CompletedTask;
        }
    }
}