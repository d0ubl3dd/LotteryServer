using BusinessLogic.Exceptions;
using Contracts.DTOs;
using Contracts.Faults;
using DataAccess;
using log4net;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

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
                    throw new InvalidFriendshipRequestException("No puedes agregarte a ti mismo.");

                using (var context = new lottery_databaseEntities())
                {
                    bool exists = await context.Friendship.AnyAsync(f =>
                        (f.id_user_sender == currentUserId && f.id_user_receiver == targetUserId) ||
                        (f.id_user_sender == targetUserId && f.id_user_receiver == currentUserId));

                    if (exists)
                        throw new FriendshipDuplicateException("Ya existe una solicitud o amistad previa.");

                    context.Friendship.Add(new Friendship
                    {
                        id_user_sender = currentUserId,
                        id_user_receiver = targetUserId,
                        status = "Pending"
                    });

                    await context.SaveChangesAsync();
                    _logger.Info($"[SendRequestFriendship] Solicitud enviada.");
                }

            }, "SendRequestFriendship");
        }

        public async Task AcceptFriendRequest(int currentUserId, int requesterId)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                _logger.Info($"[AcceptFriendRequest] Aceptando solicitud de {requesterId}.");

                using (var context = new lottery_databaseEntities())
                {
                    var request = await context.Friendship.FirstOrDefaultAsync(f =>
                        f.id_user_sender == requesterId &&
                        f.id_user_receiver == currentUserId &&
                        f.status == "Pending");

                    if (request == null)
                        throw new FriendshipNotFoundException("No existe la solicitud.");

                    request.status = "Accepted";
                    await context.SaveChangesAsync();

                    _logger.Info($"[AcceptFriendRequest] Solicitud aceptada.");
                }

            }, "AcceptFriendRequest");
        }

        public async Task RejectFriendRequest(int currentUserId, int requesterId)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                _logger.Info($"[RejectFriendRequest] Rechazando solicitud de {requesterId}.");

                using (var context = new lottery_databaseEntities())
                {
                    var request = await context.Friendship.FirstOrDefaultAsync(f =>
                        f.id_user_sender == requesterId &&
                        f.id_user_receiver == currentUserId &&
                        f.status == "Pending");

                    if (request == null)
                        throw new FriendshipNotFoundException("La solicitud no existe.");

                    context.Friendship.Remove(request);
                    await context.SaveChangesAsync();

                    _logger.Info($"[RejectFriendRequest] Solicitud rechazada.");
                }

            }, "RejectFriendRequest");
        }

        public async Task CancelFriendRequest(int currentUserId, int targetUserId)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                _logger.Info($"[CancelFriendRequest] Cancelando solicitud a {targetUserId}.");

                using (var context = new lottery_databaseEntities())
                {
                    var request = await context.Friendship.FirstOrDefaultAsync(f =>
                        f.id_user_sender == currentUserId &&
                        f.id_user_receiver == targetUserId &&
                        f.status == "Pending");

                    if (request == null)
                        throw new FriendshipNotFoundException("No existe solicitud pendiente.");

                    context.Friendship.Remove(request);
                    await context.SaveChangesAsync();

                    _logger.Info($"[CancelFriendRequest] Solicitud cancelada.");
                }

            }, "CancelFriendRequest");
        }

        public async Task RemoveFriend(int currentUserId, int friendUserId)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                _logger.Info($"[RemoveFriend] Eliminando amistad.");

                using (var context = new lottery_databaseEntities())
                {
                    var friendship = await context.Friendship.FirstOrDefaultAsync(f =>
                        ((f.id_user_sender == currentUserId && f.id_user_receiver == friendUserId) ||
                         (f.id_user_sender == friendUserId && f.id_user_receiver == currentUserId)) &&
                        f.status == "Accepted");

                    if (friendship == null)
                        throw new FriendshipNotFoundException("No existe una amistad con ese usuario.");

                    context.Friendship.Remove(friendship);
                    await context.SaveChangesAsync();

                    _logger.Info("[RemoveFriend] Amistad eliminada.");
                }

            }, "RemoveFriend");
        }

        public async Task<List<FriendDto>> GetFriends(int currentUserId)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                _logger.Info($"[GetFriends] Lista de amigos para {currentUserId}.");

                using (var context = new lottery_databaseEntities())
                {
                    var friends = await context.Friendship
                        .Where(f => (f.id_user_sender == currentUserId || f.id_user_receiver == currentUserId)
                                    && f.status == "Accepted")
                        .ToListAsync();

                    var friendIds = friends.Select(f =>
                        f.id_user_sender == currentUserId ? f.id_user_receiver : f.id_user_sender).ToList();

                    var friendDetails = await context.User
                        .Where(u => friendIds.Contains(u.id_user))
                        .Select(u => new FriendDto
                        {
                            FriendId = u.id_user,
                            Nickname = u.nickname,
                            Status = u.status
                        })
                        .ToListAsync();

                    _logger.Info($"[GetFriends] {friendDetails.Count} amigos encontrados.");
                    return friendDetails;
                }

            }, "GetFriends");
        }

        public async Task<List<FriendDto>> GetPendingRequests(int currentUserId)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                _logger.Info($"[GetPendingRequests] Solicitudes recibidas para {currentUserId}.");

                using (var context = new lottery_databaseEntities())
                {
                    return await context.Friendship
                        .Where(f => f.id_user_receiver == currentUserId && f.status == "Pending")
                        .Join(context.User,
                            f => f.id_user_sender,
                            u => u.id_user,
                            (f, u) => new FriendDto
                            {
                                FriendId = u.id_user,
                                Nickname = u.nickname
                            })
                        .ToListAsync();
                }

            }, "GetPendingRequests");
        }

        public async Task<List<FriendDto>> GetSentRequests(int currentUserId)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                _logger.Info($"[GetSentRequests] Solicitudes enviadas por {currentUserId}.");

                using (var context = new lottery_databaseEntities())
                {
                    return await context.Friendship
                        .Where(f => f.id_user_sender == currentUserId && f.status == "Pending")
                        .Join(context.User,
                            f => f.id_user_receiver,
                            u => u.id_user,
                            (f, u) => new FriendDto
                            {
                                FriendId = currentUserId,
                                UserId = u.id_user,
                                Nickname = u.nickname
                            })
                        .ToListAsync();
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

                var inviter = GlobalSessionManager.Instance.GetClient(currentUserId.Value);
                if (inviter == null || inviter.CurrentLobby == null)
                    throw new LobbyException("No tienes un lobby activo.");

                if (inviter.CurrentLobby.LobbyCode != lobbyCode)
                    throw new LobbyException("Intento de invitación desde un lobby diferente.");

                var target = GlobalSessionManager.Instance.GetClient(targetFriendId);
                if (target == null)
                    throw new UserNotConnectedException("El amigo no está conectado.");

                if (target.CurrentLobby != null)
                {
                    if (target.CurrentLobby == inviter.CurrentLobby)
                        throw new UserAlreadyInLobbyException("El usuario ya está en este lobby.");
                    else
                        throw new UserAlreadyInLobbyException("El usuario está en otro lobby.");
                }

                target.CallbackChannel.ReceiveLobbyInvite(inviter.Nickname, lobbyCode);
                _logger.Info("[InviteFriendToLobby] Invitación enviada.");

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
                case FriendshipNotFoundException _:
                    errorCode = "FRIEND_NOT_FOUND";
                    clientMessage = ex.Message;
                    _logger.Warn($"[{operationName}] {clientMessage}");
                    break;

                case InvalidFriendshipRequestException _:
                    errorCode = "FRIEND_INVALID";
                    clientMessage = ex.Message;
                    _logger.Warn($"[{operationName}] {clientMessage}");
                    break;

                case FriendshipDuplicateException _:
                    errorCode = "FRIEND_DUPLICATE";
                    clientMessage = ex.Message;
                    _logger.Warn($"[{operationName}] {clientMessage}");
                    break;

                case UserNotConnectedException _:
                    errorCode = "USER_OFFLINE";
                    clientMessage = ex.Message;
                    _logger.Warn($"[{operationName}] {clientMessage}");
                    break;

                case UserAlreadyInLobbyException _:
                    errorCode = "USER_IN_LOBBY";
                    clientMessage = ex.Message;
                    _logger.Warn($"[{operationName}] {clientMessage}");
                    break;

                case LobbyException _:
                    errorCode = "LOBBY_ERROR";
                    clientMessage = ex.Message;
                    _logger.Warn($"[{operationName}] {clientMessage}");
                    break;

                default:
                    errorCode = "FR-500";
                    clientMessage = "Error interno del servidor.";
                    _logger.Fatal($"[{operationName}] Error inesperado: {ex}", ex);
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