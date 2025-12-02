using BusinessLogic.Exceptions;
using BusinessLogic.Logic.Base;
using BusinessLogic.Utilities;
using Contracts.DTOs;
using DataAccess.DAOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessLogic.Logic
{
    public class FriendHandler : BaseHandler
    {
        private readonly IFriendshipDao _friendshipDao;
        private readonly GlobalSessionManager _sessionManager;

        public FriendHandler(GlobalSessionManager sessionManager, IFriendshipDao friendshipDao)
            : base(typeof(FriendHandler))
        {
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            _friendshipDao = friendshipDao ?? throw new ArgumentNullException(nameof(friendshipDao));
        }

        public async Task SendRequestFriendship(int currentUserId, int targetUserId)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                if (currentUserId < 0)
                {
                    throw new GuestActionException("Los invitados no pueden enviar solicitudes de amistad.");
                }

                _logger.Info($"[SendRequestFriendship] {currentUserId} → {targetUserId}");

                if (currentUserId == targetUserId)
                {
                    throw new InvalidFriendshipRequestException("No puedes agregarte a ti mismo.");
                }

                if (await _friendshipDao.FriendshipExistsAsync(currentUserId, targetUserId))
                {
                    throw new FriendshipDuplicateException("Ya existe una amistad o solicitud.");
                }

                await _friendshipDao.RequestFriendshipAsync(currentUserId, targetUserId);

                _logger.Info("[SendRequestFriendship] Solicitud enviada.");

            }, "SendRequestFriendship");
        }

        public async Task AcceptFriendRequest(int currentUserId, int requesterId)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                if (currentUserId < 0)
                {
                    throw new GuestActionException("Los invitados no pueden aceptar solicitudes.");
                }

                _logger.Info($"[AcceptFriendRequest] {currentUserId} acepta de {requesterId}");

                var request = await _friendshipDao.GetPendingRequestAsync(requesterId, currentUserId)
                    ?? throw new FriendshipNotFoundException("La solicitud no existe.");

                await _friendshipDao.AcceptRequestAsync(request);

                _logger.Info("[AcceptFriendRequest] Solicitud aceptada.");

            }, "AcceptFriendRequest");
        }

        public async Task RejectFriendRequest(int currentUserId, int requesterId)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                if (currentUserId < 0)
                {
                    throw new GuestActionException("Los invitados no pueden rechazar solicitudes.");
                }

                _logger.Info($"[RejectFriendRequest] {currentUserId} rechaza a {requesterId}");

                var request = await _friendshipDao.GetPendingRequestAsync(requesterId, currentUserId)
                    ?? throw new FriendshipNotFoundException("La solicitud no existe.");

                await _friendshipDao.RemoveFriendshipAsync(request);

                _logger.Info("[RejectFriendRequest] Solicitud rechazado.");

            }, "RejectFriendRequest");
        }

        public async Task CancelFriendRequest(int currentUserId, int targetUserId)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                if (currentUserId < 0)
                {
                    throw new GuestActionException("Los invitados no pueden cancelar solicitudes.");
                }

                _logger.Info($"[CancelFriendRequest] {currentUserId} cancela solicitud a {targetUserId}");

                var request = await _friendshipDao.GetPendingRequestAsync(currentUserId, targetUserId)
                    ?? throw new FriendshipNotFoundException("No existe solicitud pendiente.");

                await _friendshipDao.RemoveFriendshipAsync(request);

                _logger.Info("[CancelFriendRequest] Solicitud cancelada.");

            }, "CancelFriendRequest");
        }

        public async Task RemoveFriend(int currentUserId, int friendUserId)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                if (currentUserId < 0)
                {
                    throw new GuestActionException("Los invitados no pueden eliminar amigos.");
                }

                _logger.Info($"[RemoveFriend] {currentUserId} elimina amistad con {friendUserId}");

                var friendship = await _friendshipDao.GetAcceptedFriendshipAsync(currentUserId, friendUserId)
                    ?? throw new FriendshipNotFoundException("No existe una amistad con ese usuario.");

                await _friendshipDao.RemoveFriendshipAsync(friendship);

                _logger.Info("[RemoveFriend] Amistad eliminada.");

            }, "RemoveFriend");
        }

        public async Task<List<FriendDto>> GetFriends(int currentUserId)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                if (currentUserId < 0)
                {
                    throw new GuestActionException("Los invitados no tienen lista de amigos.");
                }

                _logger.Info($"[GetFriends] Solicitando amigos de {currentUserId}");

                var users = await _friendshipDao.GetAcceptedFriendsAsync(currentUserId);

                return users.Select(u => new FriendDto
                {
                    FriendId = u.id_user,
                    Nickname = u.nickname,
                    Status = u.status
                }).ToList();

            }, "GetFriends");
        }

        public async Task<List<FriendDto>> GetPendingRequests(int currentUserId)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                if (currentUserId < 0)
                {
                    throw new GuestActionException("Los invitados no tienen solicitudes.");
                }

                _logger.Info($"[GetPendingRequests] {currentUserId}");

                var users = await _friendshipDao.GetPendingRequestsAsync(currentUserId);

                return users.Select(u => new FriendDto
                {
                    FriendId = u.id_user,
                    Nickname = u.nickname
                }).ToList();

            }, "GetPendingRequests");
        }

        public async Task<List<FriendDto>> GetSentRequests(int currentUserId)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                if (currentUserId < 0)
                {
                    throw new GuestActionException("Los invitados no tienen solicitudes enviadas.");
                }

                _logger.Info($"[GetSentRequests] {currentUserId}");

                var users = await _friendshipDao.GetSentRequestsAsync(currentUserId);

                return users.Select(u => new FriendDto
                {
                    FriendId = currentUserId,
                    UserId = u.id_user,
                    Nickname = u.nickname
                }).ToList();

            }, "GetSentRequests");
        }

        public async Task InviteFriendToLobby(string lobbyCode, int targetFriendId)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                _logger.Info($"[InviteFriendToLobby] Invitando a {targetFriendId} al lobby {lobbyCode}");

                int? currentUserId = _sessionManager.GetUserIdFromContext();

                if (currentUserId == null)
                {
                    throw new UserNotConnectedException("Usuario no autenticado.");
                }

                if (currentUserId.Value < 0)
                {
                    throw new GuestActionException("Los invitados no pueden invitar amigos.");
                }

                var inviter = _sessionManager.GetClient(currentUserId.Value);

                if (inviter?.CurrentLobby == null)
                {
                    throw new LobbyException("No tienes un lobby activo.");
                }

                if (inviter.CurrentLobby.LobbyCode != lobbyCode)
                {
                    throw new LobbyException("Intento de invitación desde un lobby distinto.");
                }

                var target = _sessionManager.GetClient(targetFriendId);

                if (target == null)
                {
                    throw new UserNotConnectedException("El amigo no está conectado.");
                }

                if (target.CurrentLobby != null)
                {
                    throw new UserAlreadyInLobbyException(
                        target.CurrentLobby == inviter.CurrentLobby
                        ? "El usuario ya está en este lobby."
                        : "El usuario está en otro lobby."
                    );
                }

                target.CallbackChannel.ReceiveLobbyInvite(inviter.Nickname, lobbyCode);

                _logger.Info("[InviteFriendToLobby] Invitación enviada.");

            }, "InviteFriendToLobby");
        }
    }
}