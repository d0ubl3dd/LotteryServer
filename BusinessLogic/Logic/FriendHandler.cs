using BusinessLogic.Exceptions;
using Contracts.DTOs;
using Contracts.Faults;
using DataAccess.DAOs;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

namespace BusinessLogic.Logic
{
    public class FriendHandler
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(FriendHandler));

        private readonly IFriendshipDao _friendshipDao;
        private readonly GlobalSessionManager _sessionManager;

        public FriendHandler(GlobalSessionManager sessionManager, IFriendshipDao friendshipDao)
        {
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            _friendshipDao = friendshipDao ?? throw new ArgumentNullException(nameof(friendshipDao));
        }

        // =====================================================================================
        // FRIENDSHIP ACTIONS
        // =====================================================================================

        public Task SendRequestFriendship(int currentUserId, int targetUserId)
            => ExecuteFaultSafeAsync(async () =>
            {
                // VALIDACIÓN DE INVITADO (Guard Clause)
                if (currentUserId < 0) throw new GuestActionException("Los invitados no pueden enviar solicitudes de amistad.");

                _logger.Info($"[SendRequestFriendship] {currentUserId} → {targetUserId}");

                if (currentUserId == targetUserId)
                    throw new InvalidFriendshipRequestException("No puedes agregarte a ti mismo.");

                if (await _friendshipDao.FriendshipExistsAsync(currentUserId, targetUserId))
                    throw new FriendshipDuplicateException("Ya existe una amistad o solicitud.");

                await _friendshipDao.RequestFriendshipAsync(currentUserId, targetUserId);

                _logger.Info("[SendRequestFriendship] Solicitud enviada.");
            }, "SendRequestFriendship");


        public Task AcceptFriendRequest(int currentUserId, int requesterId)
            => ExecuteFaultSafeAsync(async () =>
            {
                if (currentUserId < 0) throw new GuestActionException("Los invitados no pueden aceptar solicitudes.");

                _logger.Info($"[AcceptFriendRequest] {currentUserId} acepta de {requesterId}");

                var request = await _friendshipDao.GetPendingRequestAsync(requesterId, currentUserId)
                    ?? throw new FriendshipNotFoundException("La solicitud no existe.");

                await _friendshipDao.AcceptRequestAsync(request);

                _logger.Info("[AcceptFriendRequest] Solicitud aceptada.");
            }, "AcceptFriendRequest");


        public Task RejectFriendRequest(int currentUserId, int requesterId)
            => ExecuteFaultSafeAsync(async () =>
            {
                if (currentUserId < 0) throw new GuestActionException("Los invitados no pueden rechazar solicitudes.");

                _logger.Info($"[RejectFriendRequest] {currentUserId} rechaza a {requesterId}");

                var request = await _friendshipDao.GetPendingRequestAsync(requesterId, currentUserId)
                    ?? throw new FriendshipNotFoundException("La solicitud no existe.");

                await _friendshipDao.RemoveFriendshipAsync(request);

                _logger.Info("[RejectFriendRequest] Solicitud rechazado.");
            }, "RejectFriendRequest");


        public Task CancelFriendRequest(int currentUserId, int targetUserId)
            => ExecuteFaultSafeAsync(async () =>
            {
                if (currentUserId < 0) throw new GuestActionException("Los invitados no pueden cancelar solicitudes.");

                _logger.Info($"[CancelFriendRequest] {currentUserId} cancela solicitud a {targetUserId}");

                var request = await _friendshipDao.GetPendingRequestAsync(currentUserId, targetUserId)
                    ?? throw new FriendshipNotFoundException("No existe solicitud pendiente.");

                await _friendshipDao.RemoveFriendshipAsync(request);

                _logger.Info("[CancelFriendRequest] Solicitud cancelada.");
            }, "CancelFriendRequest");


        public Task RemoveFriend(int currentUserId, int friendUserId)
            => ExecuteFaultSafeAsync(async () =>
            {
                if (currentUserId < 0) throw new GuestActionException("Los invitados no pueden eliminar amigos.");

                _logger.Info($"[RemoveFriend] {currentUserId} elimina amistad con {friendUserId}");

                var friendship = await _friendshipDao.GetAcceptedFriendshipAsync(currentUserId, friendUserId)
                    ?? throw new FriendshipNotFoundException("No existe una amistad con ese usuario.");

                await _friendshipDao.RemoveFriendshipAsync(friendship);

                _logger.Info("[RemoveFriend] Amistad eliminada.");
            }, "RemoveFriend");


        // =====================================================================================
        // GET DATA
        // =====================================================================================

        public Task<List<FriendDto>> GetFriends(int currentUserId)
            => ExecuteFaultSafeAsync(async () =>
            {
                // Si es invitado, simplemente retornamos lista vacía o lanzamos error.
                // Lanzar error es más seguro para que el cliente sepa que no debe llamar esto.
                if (currentUserId < 0) throw new GuestActionException("Los invitados no tienen lista de amigos.");

                _logger.Info($"[GetFriends] Solicitando amigos de {currentUserId}");

                var users = await _friendshipDao.GetAcceptedFriendsAsync(currentUserId);

                return users.Select(u => new FriendDto
                {
                    FriendId = u.id_user,
                    Nickname = u.nickname,
                    Status = u.status
                }).ToList();
            }, "GetFriends");


        public Task<List<FriendDto>> GetPendingRequests(int currentUserId)
            => ExecuteFaultSafeAsync(async () =>
            {
                if (currentUserId < 0) throw new GuestActionException("Los invitados no tienen solicitudes.");

                _logger.Info($"[GetPendingRequests] {currentUserId}");

                var users = await _friendshipDao.GetPendingRequestsAsync(currentUserId);

                return users.Select(u => new FriendDto
                {
                    FriendId = u.id_user,
                    Nickname = u.nickname
                }).ToList();
            }, "GetPendingRequests");


        public Task<List<FriendDto>> GetSentRequests(int currentUserId)
            => ExecuteFaultSafeAsync(async () =>
            {
                if (currentUserId < 0) throw new GuestActionException("Los invitados no tienen solicitudes enviadas.");

                _logger.Info($"[GetSentRequests] {currentUserId}");

                var users = await _friendshipDao.GetSentRequestsAsync(currentUserId);

                return users.Select(u => new FriendDto
                {
                    FriendId = currentUserId,
                    UserId = u.id_user,
                    Nickname = u.nickname
                }).ToList();
            }, "GetSentRequests");


        // =====================================================================================
        // INVITES
        // =====================================================================================

        public Task InviteFriendToLobby(string lobbyCode, int targetFriendId)
            => ExecuteFaultSafeAsync(async () =>
            {
                _logger.Info($"[InviteFriendToLobby] Invitando a {targetFriendId} al lobby {lobbyCode}");

                int? currentUserId = _sessionManager.GetUserIdFromContext()
                    ?? throw new UserNotConnectedException("Usuario no autenticado.");

                // VALIDACIÓN DE INVITADO
                if (currentUserId.Value < 0)
                {
                    throw new GuestActionException("Los invitados no pueden invitar amigos.");
                }

                var inviter = _sessionManager.GetClient(currentUserId.Value);

                if (inviter?.CurrentLobby == null)
                    throw new LobbyException("No tienes un lobby activo.");

                if (inviter.CurrentLobby.LobbyCode != lobbyCode)
                    throw new LobbyException("Intento de invitación desde un lobby distinto.");

                var target = _sessionManager.GetClient(targetFriendId)
                    ?? throw new UserNotConnectedException("El amigo no está conectado.");

                if (target.CurrentLobby != null)
                    throw new UserAlreadyInLobbyException(
                        target.CurrentLobby == inviter.CurrentLobby
                        ? "El usuario ya está en este lobby."
                        : "El usuario está en otro lobby."
                    );

                target.CallbackChannel.ReceiveLobbyInvite(inviter.Nickname, lobbyCode);

                _logger.Info("[InviteFriendToLobby] Invitación enviada.");
            }, "InviteFriendToLobby");



        // =====================================================================================
        // ERROR HANDLING
        // =====================================================================================

        private async Task ExecuteFaultSafeAsync(Func<Task> action, string operationName)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                throw BuildFaultException(ex, operationName);
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
                throw BuildFaultException(ex, operationName);
            }
        }


        private FaultException<ServiceFault> BuildFaultException(Exception ex, string operationName)
        {
            if (ex is FaultException<ServiceFault> fault)
                return fault;

            string errorCode;
            string clientMessage;

            switch (ex)
            {
                case GuestActionException _:
                    errorCode = "FRIEND_GUEST_RESTRICTED";
                    clientMessage = ex.Message;
                    _logger.Warn($"[{operationName}] Acceso denegado a invitado.");
                    break;

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
                    errorCode = "FRIEND_INTERNAL";
                    clientMessage = "Error interno del servidor.";
                    _logger.Fatal($"[{operationName}] Error inesperado: {ex}", ex);
                    break;
            }

            return new FaultException<ServiceFault>(
                new ServiceFault
                {
                    ErrorCode = errorCode,
                    Message = clientMessage
                },
                new FaultReason(clientMessage)
            );
        }
    }

    // --- Excepción Personalizada ---
    [Serializable]
    public class GuestActionException : Exception
    {
        public GuestActionException(string message) : base(message) { }
    }
}