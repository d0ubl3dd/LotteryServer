using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ServiceModel;
using BusinessLogic.Logic;
using Contracts.Faults;
using DataAccess;
using DataAccess.DAOs;
using Tests.Builders;
using Contracts.Callbacks;
using BusinessLogic.Models;

namespace Tests.Logic
{
    public class FriendHandlerTests
    {
        private readonly Mock<IFriendshipDao> _mockFriendshipDao;
        private readonly Mock<ISessionManager> _mockSessionManager;
        private readonly Mock<IUserDao> _mockUserDao;
        private readonly Mock<ILotteryCallback> _mockCallback;
        private readonly FriendHandler _handler;

        public FriendHandlerTests()
        {
            _mockFriendshipDao = new Mock<IFriendshipDao>();
            _mockSessionManager = new Mock<ISessionManager>();
            _mockUserDao = new Mock<IUserDao>();
            _mockCallback = new Mock<ILotteryCallback>();

            _handler = new FriendHandler(_mockSessionManager.Object, _mockFriendshipDao.Object);
        }

        [Fact]
        public void Constructor_WhenSessionManagerIsNull_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new FriendHandler(null, _mockFriendshipDao.Object));
        }

        [Fact]
        public void Constructor_WhenFriendshipDaoIsNull_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new FriendHandler(_mockSessionManager.Object, null));
        }

        [Theory]
        [InlineData(10, 20)]
        [InlineData(999, 1)]
        public async Task SendRequestFriendship_WhenValid_ShouldCallDao(int userId, int targetId)
        {
            _mockFriendshipDao.Setup(d => d.FriendshipExistsAsync(userId, targetId)).ReturnsAsync(false);

            await _handler.SendRequestFriendship(userId, targetId);

            _mockFriendshipDao.Verify(d => d.RequestFriendshipAsync(userId, targetId), Times.Once);
        }

        [Fact]
        public async Task AcceptFriendRequest_WhenValid_ShouldAccept()
        {
            var friendship = new Friendship
            {
                id_user_sender = 20,
                id_user_receiver = 10,
                status = "Pending"
            };

            _mockFriendshipDao.Setup(d => d.GetPendingRequestAsync(20, 10)).ReturnsAsync(friendship);

            await _handler.AcceptFriendRequest(10, 20);

            _mockFriendshipDao.Verify(d => d.AcceptRequestAsync(friendship), Times.Once);
        }

        [Fact]
        public async Task RejectFriendRequest_WhenValid_ShouldRemove()
        {
            var friendship = new Friendship
            {
                id_user_sender = 20,
                id_user_receiver = 10,
                status = "Pending"
            };

            _mockFriendshipDao.Setup(d => d.GetPendingRequestAsync(20, 10)).ReturnsAsync(friendship);

            await _handler.RejectFriendRequest(10, 20);

            _mockFriendshipDao.Verify(d => d.RemoveFriendshipAsync(friendship), Times.Once);
        }

        [Fact]
        public async Task CancelFriendRequest_WhenValid_ShouldRemove()
        {
            var friendship = new Friendship
            {
                id_user_sender = 10,
                id_user_receiver = 20,
                status = "Pending"
            };

            _mockFriendshipDao.Setup(d => d.GetPendingRequestAsync(10, 20)).ReturnsAsync(friendship);

            await _handler.CancelFriendRequest(10, 20);

            _mockFriendshipDao.Verify(d => d.RemoveFriendshipAsync(friendship), Times.Once);
        }

        [Fact]
        public async Task RemoveFriend_WhenValid_ShouldRemove()
        {
            var friendship = new Friendship
            {
                id_user_sender = 10,
                id_user_receiver = 20,
                status = "Accepted"
            };

            _mockFriendshipDao.Setup(d => d.GetAcceptedFriendshipAsync(10, 20)).ReturnsAsync(friendship);

            await _handler.RemoveFriend(10, 20);

            _mockFriendshipDao.Verify(d => d.RemoveFriendshipAsync(friendship), Times.Once);
        }

        [Fact]
        public async Task GetFriends_WhenUserIsOnline_ShouldReturnOnlineStatus()
        {
            var friends = new List<User> { new UserBuilder().WithId(20).Build() };
            _mockFriendshipDao.Setup(d => d.GetAcceptedFriendsAsync(10)).ReturnsAsync(friends);
            _mockSessionManager.Setup(sm => sm.IsUserOnline(20)).Returns(true);

            var result = await _handler.GetFriends(10);

            Assert.Single(result);
            Assert.Equal("Online", result[0].Status);
        }

        [Fact]
        public async Task GetFriends_WhenUserIsOffline_ShouldReturnOfflineStatus()
        {
            var friends = new List<User> { new UserBuilder().WithId(20).Build() };
            _mockFriendshipDao.Setup(d => d.GetAcceptedFriendsAsync(10)).ReturnsAsync(friends);
            _mockSessionManager.Setup(sm => sm.IsUserOnline(20)).Returns(false);

            var result = await _handler.GetFriends(10);

            Assert.Single(result);
            Assert.Equal("Offline", result[0].Status);
        }

        [Fact]
        public async Task InviteFriendToLobby_WhenInviterNotInLobby_ShouldThrowLobbyException()
        {
            _mockSessionManager.Setup(sm => sm.GetUserIdFromContext()).Returns(10);

            var inviter = new PlayerClient(10, "Inviter", 1, _mockCallback.Object);
            inviter.CurrentLobby = null;
            _mockSessionManager.Setup(sm => sm.GetClient(10)).Returns(inviter);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.InviteFriendToLobby("CODE", 20));

            Assert.Equal("LOBBY_ERROR", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task InviteFriendToLobby_WhenTargetOffline_ShouldThrowUserOffline()
        {
            _mockSessionManager.Setup(sm => sm.GetUserIdFromContext()).Returns(10);

            var inviter = new PlayerClient(10, "Inviter", 1, _mockCallback.Object);
            var lobby = new Lobby("CODE", inviter, _mockUserDao.Object);
            inviter.CurrentLobby = lobby;

            _mockSessionManager.Setup(sm => sm.GetClient(10)).Returns(inviter);
            _mockSessionManager.Setup(sm => sm.GetClient(20)).Returns((PlayerClient)null);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.InviteFriendToLobby("CODE", 20));

            Assert.Equal("USER_OFFLINE", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task InviteFriendToLobby_WhenTargetInAnotherLobby_ShouldThrowUserBusy()
        {
            _mockSessionManager.Setup(sm => sm.GetUserIdFromContext()).Returns(10);

            var inviter = new PlayerClient(10, "Inviter", 1, _mockCallback.Object);
            var lobby1 = new Lobby("CODE1", inviter, _mockUserDao.Object);
            inviter.CurrentLobby = lobby1;

            var target = new PlayerClient(20, "Target", 1, _mockCallback.Object);
            var lobby2 = new Lobby("CODE2", target, _mockUserDao.Object);
            target.CurrentLobby = lobby2;

            _mockSessionManager.Setup(sm => sm.GetClient(10)).Returns(inviter);
            _mockSessionManager.Setup(sm => sm.GetClient(20)).Returns(target);

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.InviteFriendToLobby("CODE1", 20));

            Assert.Equal("LOBBY_USER_ALREADY_IN", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task InviteFriendToLobby_WhenValid_ShouldSendInvite()
        {
            _mockSessionManager.Setup(sm => sm.GetUserIdFromContext()).Returns(10);

            var inviter = new PlayerClient(10, "Inviter", 1, _mockCallback.Object);
            var lobby = new Lobby("CODE", inviter, _mockUserDao.Object);
            inviter.CurrentLobby = lobby;

            var target = new PlayerClient(20, "Target", 1, _mockCallback.Object);
            target.CurrentLobby = null;

            _mockSessionManager.Setup(sm => sm.GetClient(10)).Returns(inviter);
            _mockSessionManager.Setup(sm => sm.GetClient(20)).Returns(target);

            await _handler.InviteFriendToLobby("CODE", 20);

            _mockCallback.Verify(cb => cb.ReceiveLobbyInvite("Inviter", "CODE"), Times.Once);
        }
    }
}