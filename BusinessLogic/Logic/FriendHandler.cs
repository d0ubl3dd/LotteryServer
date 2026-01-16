using BusinessLogic.Exceptions;
using BusinessLogic.Logic.Base;
using BusinessLogic.Models;
using BusinessLogic.Utilities;
using Contracts.DTOs;
using DataAccess;
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
        private readonly ISessionManager _sessionManager;

        public FriendHandler(ISessionManager sessionManager, IFriendshipDao friendshipDao)
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
                    throw new GuestActionException("Guests cannot send friend requests.");
                }

                _logger.InfoFormat("[SendRequestFriendship] {0} -> {1}", currentUserId, targetUserId);

                if (currentUserId == targetUserId)
                {
                    throw new InvalidFriendshipRequestException("You cannot add yourself.");
                }

                if (await _friendshipDao.FriendshipExistsAsync(currentUserId, targetUserId))
                {
                    throw new FriendshipDuplicateException("A friendship or request already exists.");
                }

                await _friendshipDao.RequestFriendshipAsync(currentUserId, targetUserId);

                _logger.Info("[SendRequestFriendship] Request sent.");

            }, "SendRequestFriendship");
        }

        public async Task AcceptFriendRequest(int currentUserId, int requesterId)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                if (currentUserId < 0)
                {
                    throw new GuestActionException("Guests cannot accept requests.");
                }

                _logger.InfoFormat("[AcceptFriendRequest] {0} accepts from {1}", currentUserId, requesterId);

                Friendship request = await _friendshipDao.GetPendingRequestAsync(requesterId, currentUserId)
                    ?? throw new FriendshipNotFoundException("The request does not exist.");

                await _friendshipDao.AcceptRequestAsync(request);

                _logger.Info("[AcceptFriendRequest] Request accepted.");

            }, "AcceptFriendRequest");
        }

        public async Task RejectFriendRequest(int currentUserId, int requesterId)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                if (currentUserId < 0)
                {
                    throw new GuestActionException("Guests cannot reject requests.");
                }

                _logger.InfoFormat("[RejectFriendRequest] {0} rejects {1}", currentUserId, requesterId);

                Friendship request = await _friendshipDao.GetPendingRequestAsync(requesterId, currentUserId)
                    ?? throw new FriendshipNotFoundException("The request does not exist.");

                await _friendshipDao.RemoveFriendshipAsync(request);

                _logger.Info("[RejectFriendRequest] Request rejected.");

            }, "RejectFriendRequest");
        }

        public async Task CancelFriendRequest(int currentUserId, int targetUserId)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                if (currentUserId < 0)
                {
                    throw new GuestActionException("Guests cannot cancel requests.");
                }

                _logger.InfoFormat("[CancelFriendRequest] {0} cancels request to {1}", currentUserId, targetUserId);

                Friendship request = await _friendshipDao.GetPendingRequestAsync(currentUserId, targetUserId)
                    ?? throw new FriendshipNotFoundException("No pending request exists.");

                await _friendshipDao.RemoveFriendshipAsync(request);

                _logger.Info("[CancelFriendRequest] Request canceled.");

            }, "CancelFriendRequest");
        }

        public async Task RemoveFriend(int currentUserId, int friendUserId)
        {
            await ExecuteFaultSafeAsync(async () =>
            {
                if (currentUserId < 0)
                {
                    throw new GuestActionException("Guests cannot remove friends.");
                }

                _logger.InfoFormat("[RemoveFriend] {0} removes friendship with {1}", currentUserId, friendUserId);

                Friendship friendship = await _friendshipDao.GetAcceptedFriendshipAsync(currentUserId, friendUserId)
                    ?? throw new FriendshipNotFoundException("No friendship exists with that user.");

                await _friendshipDao.RemoveFriendshipAsync(friendship);

                _logger.Info("[RemoveFriend] Friendship removed.");

            }, "RemoveFriend");
        }

        public async Task<List<FriendDto>> GetFriends(int currentUserId)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                if (currentUserId < 0)
                {
                    throw new GuestActionException("Guests do not have a friend list.");
                }

                _logger.InfoFormat("[GetFriends] Requesting friends for {0}", currentUserId);

                List<User> users = await _friendshipDao.GetAcceptedFriendsAsync(currentUserId);

                List<FriendDto> friendList = users.Select(u => new FriendDto
                {
                    FriendId = u.id_user,
                    Nickname = u.nickname,
                    Status = _sessionManager.IsUserOnline(u.id_user) ? "Online" : "Offline"
                }).ToList();

                return friendList;

            }, "GetFriends");
        }

        public async Task<List<FriendDto>> GetPendingRequests(int currentUserId)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                if (currentUserId < 0)
                {
                    throw new GuestActionException("Guests do not have requests.");
                }

                _logger.InfoFormat("[GetPendingRequests] {0}", currentUserId);

                List<User> users = await _friendshipDao.GetPendingRequestsAsync(currentUserId);

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
                    throw new GuestActionException("Guests do not have sent requests.");
                }

                _logger.InfoFormat("[GetSentRequests] {0}", currentUserId);

                List<User> users = await _friendshipDao.GetSentRequestsAsync(currentUserId);

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
                _logger.InfoFormat("[InviteFriendToLobby] Inviting {0} to lobby {1}", targetFriendId, lobbyCode);

                int? currentUserId = _sessionManager.GetUserIdFromContext();

                if (currentUserId == null)
                {
                    throw new UserNotConnectedException("User not authenticated.");
                }

                if (currentUserId.Value < 0)
                {
                    throw new GuestActionException("Guests cannot invite friends.");
                }

                PlayerClient inviter = _sessionManager.GetClient(currentUserId.Value);

                if (inviter?.CurrentLobby == null)
                {
                    throw new LobbyException("You do not have an active lobby.");
                }

                if (inviter.CurrentLobby.LobbyCode != lobbyCode)
                {
                    throw new LobbyException("Invitation attempt from a different lobby.");
                }

                PlayerClient target = _sessionManager.GetClient(targetFriendId);

                if (target == null)
                {
                    throw new UserNotConnectedException("Friend is not online.");
                }

                if (target.CurrentLobby != null)
                {
                    throw new UserAlreadyInLobbyException(
                        target.CurrentLobby == inviter.CurrentLobby
                        ? "The user is already in this lobby."
                        : "The user is in another lobby."
                    );
                }

                target.CallbackChannel.ReceiveLobbyInvite(inviter.Nickname, lobbyCode);

                _logger.Info("[InviteFriendToLobby] Invitation sent.");

                await Task.CompletedTask;

            }, "InviteFriendToLobby");
        }
    }
}