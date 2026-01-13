using BusinessLogic.Exceptions;
using BusinessLogic.Logic;
using BusinessLogic.Models;
using Contracts.Callbacks;
using Contracts.DTOs;
using DataAccess;
using DataAccess.DAOs;
using Moq;
using System;
using System.Linq;
using Tests.Builders;
using Xunit;

namespace Tests.Logic
{
    public class LobbyManagerTests
    {
        private readonly Mock<ISessionManager> _mockSessionManager;
        private readonly Mock<ILotteryCallback> _mockCallback;
        private readonly Mock<IUserDao> _mockUserDao;
        private readonly LobbyManager _manager;

        public LobbyManagerTests()
        {
            _mockSessionManager = new Mock<ISessionManager>();
            _mockCallback = new Mock<ILotteryCallback>();
            _mockUserDao = new Mock<IUserDao>();

            _manager = new LobbyManager(_mockSessionManager.Object, _mockUserDao.Object);
        }

        [Fact]
        public void CreateLobby_WhenHostIsValid_ShouldCreateLobbyAndReturnState()
        {
            var hostUser = new UserBuilder().WithId(1).Build();

            var hostClient = new PlayerClient(hostUser.id_user, hostUser.nickname, hostUser.id_avatar, _mockCallback.Object);

            var result = _manager.CreateLobby(hostClient);

            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.LobbyCode));
            Assert.Single(result.Players);
            Assert.Equal(hostUser.id_user, result.Players[0].UserId);

            var lobby = _manager.FindLobbyByHostId(hostUser.id_user);
            Assert.NotNull(lobby);
        }

        [Fact]
        public void JoinLobby_WhenLobbyExistsAndOpen_ShouldAddPlayer()
        {
            var hUser = new UserBuilder().WithId(1).Build();
            var hostClient = new PlayerClient(hUser.id_user, hUser.nickname, hUser.id_avatar, _mockCallback.Object);

            var lobbyDto = _manager.CreateLobby(hostClient);

            var joinerUser = new UserBuilder().WithId(2).Build();
            var joinerClient = new PlayerClient(joinerUser.id_user, joinerUser.nickname, joinerUser.id_avatar, _mockCallback.Object);

            var result = _manager.JoinLobby(joinerClient, lobbyDto.LobbyCode);

            Assert.Equal(2, result.Players.Count);

            _mockCallback.Verify(cb => cb.PlayerJoined(It.Is<UserDto>(u => u.UserId == 2)), Times.AtLeastOnce);
        }

        [Fact]
        public void JoinLobby_WhenLobbyCodeInvalid_ShouldThrowException()
        {
            var u = new UserBuilder().Build();
            var client = new PlayerClient(u.id_user, u.nickname, u.id_avatar, _mockCallback.Object);

            Assert.Throws<LobbyNotFoundException>(() =>
                _manager.JoinLobby(client, "INVALID_CODE"));
        }

        [Fact]
        public void JoinLobby_WhenPlayerIsBanned_ShouldThrowException()
        {
            var hUser = new UserBuilder().WithId(1).Build();
            var hostClient = new PlayerClient(hUser.id_user, hUser.nickname, hUser.id_avatar, _mockCallback.Object);

            var lobbyDto = _manager.CreateLobby(hostClient);
            var lobby = _manager.FindLobbyByHostId(1);

            lobby.BanPlayer(99);

            var bUser = new UserBuilder().WithId(99).Build();
            var bannedClient = new PlayerClient(bUser.id_user, bUser.nickname, bUser.id_avatar, _mockCallback.Object);

            Assert.Throws<PlayerBannedException>(() =>
                _manager.JoinLobby(bannedClient, lobbyDto.LobbyCode));
        }

        [Fact]
        public void KickPlayer_WhenHostKicksPlayer_ShouldRemoveAndBan()
        {
            var hUser = new UserBuilder().WithId(1).Build();
            var hostClient = new PlayerClient(hUser.id_user, hUser.nickname, hUser.id_avatar, _mockCallback.Object);

            var lobbyDto = _manager.CreateLobby(hostClient);

            var vUser = new UserBuilder().WithId(2).Build();
            var victimClient = new PlayerClient(vUser.id_user, vUser.nickname, vUser.id_avatar, _mockCallback.Object);

            _manager.JoinLobby(victimClient, lobbyDto.LobbyCode);

            _mockSessionManager.Setup(sm => sm.GetClient(2)).Returns(victimClient);

            _manager.KickPlayer(hostClient, 2);

            var lobby = _manager.FindLobbyByHostId(1);
            Assert.Single(lobby.Players);
            Assert.True(lobby.IsBanned(2));

            _mockCallback.Verify(cb => cb.YouWereKicked(), Times.Once);
        }

        [Fact]
        public void LeaveLobby_WhenHostLeaves_ShouldCloseLobbyAndNotifyAll()
        {
            var hUser = new UserBuilder().WithId(1).Build();
            var hostClient = new PlayerClient(hUser.id_user, hUser.nickname, hUser.id_avatar, _mockCallback.Object);

            var pUser = new UserBuilder().WithId(2).Build();
            var playerClient = new PlayerClient(pUser.id_user, pUser.nickname, pUser.id_avatar, _mockCallback.Object);

            var lobbyDto = _manager.CreateLobby(hostClient);
            _manager.JoinLobby(playerClient, lobbyDto.LobbyCode);

            _manager.LeaveLobby(hostClient);

            var lobby = _manager.FindLobbyByHostId(1);
            Assert.Null(lobby);

            _mockCallback.Verify(cb => cb.LobbyClosed(), Times.AtLeastOnce);

            Assert.Null(playerClient.CurrentLobby);
        }
    }
}