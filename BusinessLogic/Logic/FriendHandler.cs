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
using BusinessLogic.Exceptions;

namespace BusinessLogic.Logic
{
    public class FriendHandler
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(FriendHandler));

        public async Task SendRequestFriendship(int currentUserId, int targetUserId)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                _logger.Info($"[SendRequestFriendship] Usuario {currentUserId} intenta agregar a {targetUserId}.");

                if (currentUserId == targetUserId)
                    throw new InvalidFriendshipRequestException("No puedes agregarte a ti mismo como amigo.");

                using (var context = new lottery_databaseEntities())
                {
                    bool exists = await context.Friendship.AnyAsync(f =>
                        (f.id_user_sender == currentUserId && f.id_user_receiver == targetUserId) ||
                        (f.id_user_sender == targetUserId && f.id_user_receiver == currentUserId));

                    if (exists)
                        throw new FriendshipDuplicateException($"Ya existe amistad o solicitud entre {currentUserId} y {targetUserId}.");

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
            }, "SendRequestFriendship");
        }

        public async Task AcceptFriendRequest(int currentUserId, int requesterId)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                _logger.Info($"[AcceptFriendRequest] Usuario {currentUserId} aceptando solicitud de {requesterId}.");

                using (var context = new lottery_databaseEntities())
                {
                    var request = await context.Friendship.FirstOrDefaultAsync(f =>
                        f.id_user_sender == requesterId &&
                        f.id_user_receiver == currentUserId &&
                        f.status == "Pending");

                    if (request == null)
                        throw new FriendshipNotFoundException($"No se encontró la solicitud de amistad.");

                    request.status = "Accepted";
                    await context.SaveChangesAsync();

                    _logger.Info($"[AcceptFriendRequest] Solicitud entre {requesterId} -> {currentUserId} aceptada.");
                }
            }, "AcceptFriendRequest");
        }

        public async Task RejectFriendRequest(int currentUserId, int requesterId)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                _logger.Info($"[RejectFriendRequest] Usuario {currentUserId} rechazando solicitud de {requesterId}.");

                using (var context = new lottery_databaseEntities())
                {
                    var request = await context.Friendship.FirstOrDefaultAsync(f =>
                        f.id_user_sender == requesterId &&
                        f.id_user_receiver == currentUserId &&
                        f.status == "Pending");

                    if (request == null)
                        throw new FriendshipNotFoundException($"No se encontró la solicitud de amistad.");

                    context.Friendship.Remove(request);
                    await context.SaveChangesAsync();

                    _logger.Info($"[RejectFriendRequest] Solicitud de {requesterId} -> {currentUserId} rechazada.");
                }
            }, "RejectFriendRequest");
        }

        public async Task CancelFriendRequest(int currentUserId, int targetUserId)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                _logger.Info($"[CancelFriendRequest] Usuario {currentUserId} cancelando solicitud enviada a {targetUserId}.");

                using (var context = new lottery_databaseEntities())
                {
                    var request = await context.Friendship.FirstOrDefaultAsync(f =>
                        f.id_user_sender == currentUserId &&
                        f.id_user_receiver == targetUserId &&
                        f.status == "Pending");

                    if (request == null)
                        throw new FriendshipNotFoundException($"No se encontró solicitud pendiente.");

                    context.Friendship.Remove(request);
                    await context.SaveChangesAsync();

                    _logger.Info($"[CancelFriendRequest] Solicitud cancelada {currentUserId} -> {targetUserId}.");
                }
            }, "CancelFriendRequest");
        }

        public async Task RemoveFriend(int currentUserId, int friendUserId)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                _logger.Info($"[RemoveFriend] Usuario {currentUserId} eliminando amistad con {friendUserId}.");

                using (var context = new lottery_databaseEntities())
                {
                    var friendship = await context.Friendship.FirstOrDefaultAsync(f =>
                        ((f.id_user_sender == currentUserId && f.id_user_receiver == friendUserId) ||
                         (f.id_user_sender == friendUserId && f.id_user_receiver == currentUserId)) &&
                        f.status == "Accepted");

                    if (friendship == null)
                        throw new FriendshipNotFoundException($"No existe amistad entre {currentUserId} y {friendUserId}");

                    context.Friendship.Remove(friendship);
                    await context.SaveChangesAsync();

                    _logger.Info($"[RemoveFriend] Amistad entre {currentUserId} y {friendUserId} eliminada.");
                }
            }, "RemoveFriend");
        }

        public async Task<List<FriendDto>> GetFriends(int currentUserId)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                _logger.Info($"[GetFriends] Obteniendo lista de amigos para el usuario {currentUserId}.");

                using (var context = new lottery_databaseEntities())
                {
                    var friends = await context.Friendship
                        .Where(f => (f.id_user_sender == currentUserId || f.id_user_receiver == currentUserId)
                                    && f.status == "Accepted")
                        .Select(f => new { FriendUserId = f.id_user_sender == currentUserId ? f.id_user_receiver : f.id_user_sender })
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
            }, "GetFriends");
        }

        public async Task<List<FriendDto>> GetPendingRequests(int currentUserId)
        {
            return await ExecuteFaultSafeAsync(async () =>
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
            }, "GetPendingRequests");
        }

        public async Task<List<FriendDto>> GetSentRequests(int currentUserId)
        {
            return await ExecuteFaultSafeAsync(async () =>
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
            }, "GetSentRequests");
        }

        public async Task InviteFriendToLobby(string lobbyCode, int targetFriendId)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                _logger.Info($"[InviteFriendToLobby] Invitando a {targetFriendId} al lobby {lobbyCode}.");

                int? currentUserId = GlobalSessionManager.Instance.GetUserIdFromContext();
                if (currentUserId == null)
                    throw new UserNotConnectedException("Usuario no autenticado.");

                try
                {
                    var inviter = GlobalSessionManager.Instance.GetClient(currentUserId.Value);

                    if (inviter == null || inviter.CurrentLobby == null)
                        throw new LobbyException("El usuario no tiene lobby.");

                    if (inviter.CurrentLobby.LobbyCode != lobbyCode)
                        throw new LobbyException("Intento de invitación desde otro lobby.");

                    try
                    {
                        var target = GlobalSessionManager.Instance.GetClient(targetFriendId);

                        if (target.CurrentLobby != null)
                        {
                            if (target.CurrentLobby == inviter.CurrentLobby)
                                throw new UserAlreadyInLobbyException($"El jugador {targetFriendId} ya está en el lobby.");
                            else
                                throw new UserAlreadyInLobbyException($"El jugador {targetFriendId} está en otro lobby.");
                        }

                        target.CallbackChannel.ReceiveLobbyInvite(inviter.Nickname, lobbyCode);
                        _logger.Info($"[InviteFriendToLobby] Invitación enviada a {targetFriendId}.");
                    }
                    catch (Exception)
                    {
                        throw new UserNotConnectedException($"El amigo {targetFriendId} no está conectado.");
                    }
                }
                catch (Exception ex) when (!(ex is UserNotConnectedException))
                {
                    throw new LobbyException("Error de sesión: No se encontró tu conexión activa.");
                }

            }, "InviteFriendToLobby");
        }

        private async Task ExecuteFaultSafeAsync(Func<Task> action, string operationName)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                HandleException(ex, operationName);
            }
        }

        private async Task<T> ExecuteFaultSafeAsync<T>(Func<Task<T>> action, string operationName)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                HandleException(ex, operationName);
                return default(T);
            }
        }

        private void HandleException(Exception ex, string operationName)
        {
            if (ex is FaultException<ServiceFault>)
            {
                throw ex;
            }

            if (ex is FriendshipNotFoundException)
            {
                _logger.Warn($"[{operationName}] No encontrado: {ex.Message}");
                throw new FaultException<ServiceFault>(
                    new ServiceFault 
                    { 
                        ErrorCode = "FR-404",
                        Message = ex.Message 
                    },
                    new FaultReason(ex.Message)
                );
            }

            if (ex is InvalidFriendshipRequestException)
            {
                _logger.Warn($"[{operationName}] Solicitud inválida: {ex.Message}");
                throw new FaultException<ServiceFault>(
                    new ServiceFault { ErrorCode = "FR-001", Message = ex.Message },
                    new FaultReason(ex.Message)
                );
            }

            if (ex is FriendshipDuplicateException)
            {
                _logger.Warn($"[{operationName}] Duplicado: {ex.Message}");
                throw new FaultException<ServiceFault>(
                    new ServiceFault { ErrorCode = "FR-002", Message = ex.Message },
                    new FaultReason(ex.Message)
                );
            }

            if (ex is UserNotConnectedException)
            {
                _logger.Warn($"[{operationName}] Usuario desconectado: {ex.Message}");
                throw new FaultException<ServiceFault>(
                    new ServiceFault { ErrorCode = "LOBBY-001", Message = ex.Message },
                    new FaultReason("El usuario no está disponible.")
                );
            }

            if (ex is UserAlreadyInLobbyException)
            {
                _logger.Warn($"[{operationName}] Conflicto de Lobby: {ex.Message}");
                throw new FaultException<ServiceFault>(
                    new ServiceFault { ErrorCode = "LOBBY-002", Message = ex.Message },
                    new FaultReason(ex.Message)
                );
            }

            if (ex is LobbyException)
            {
                _logger.Warn($"[{operationName}] Error de Lobby: {ex.Message}");
                throw new FaultException<ServiceFault>(
                    new ServiceFault { ErrorCode = "LOBBY-GENERIC", Message = ex.Message },
                    new FaultReason(ex.Message)
                );
            }

            _logger.Fatal($"[{operationName}] Error INESPERADO: {ex}", ex);

            throw new FaultException<ServiceFault>(
                new ServiceFault
                {
                    ErrorCode = "FR-500",
                    Message = "Ha ocurrido un error interno en el servidor."
                },
                new FaultReason("Error interno del servidor.")
            );
        }
    }
}