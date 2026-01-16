using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using BusinessLogic.Logic;
using BusinessLogic.Models;
using BusinessLogic.Exceptions;
using Contracts.DTOs;
using Contracts.Callbacks;
using DataAccess;
using DataAccess.DAOs;
using Tests.Builders;

namespace Tests.Logic
{
    public class LobbyManagerTests
    {
        private readonly Mock<ISessionManager> _mockSessionManager;
        private readonly Mock<IUserDao> _mockUserDao;
        private readonly Mock<ILotteryCallback> _mockCallback;
        private readonly LobbyManager _manager;

        public LobbyManagerTests()
        {
            _mockSessionManager = new Mock<ISessionManager>();
            _mockUserDao = new Mock<IUserDao>();
            _mockCallback = new Mock<ILotteryCallback>();
            _manager = new LobbyManager(_mockSessionManager.Object, _mockUserDao.Object);
        }

        [Fact]
        public void Constructor_WhenSessionManagerIsNull_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new LobbyManager(null, _mockUserDao.Object));
        }

        [Fact]
        public void Constructor_WhenUserDaoIsNull_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new LobbyManager(_mockSessionManager.Object, null));
        }

        [Fact]
        public void CreateLobby_WhenHostIsNull_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _manager.CreateLobby(null));
        }

        [Theory]
        [InlineData(1, "Host1")]
        [InlineData(2, "Host2")]
        public void CreateLobby_WhenValid_ShouldCreateAndRegisterLobby(int hostId, string hostName)
        {
            var hostClient = new PlayerClient(hostId, hostName, 1, _mockCallback.Object);

            var result = _manager.CreateLobby(hostClient);

            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.LobbyCode));
            Assert.Single(result.Players);
            Assert.NotNull(hostClient.CurrentLobby);

            var lobby = _manager.FindLobbyByHostId(hostId);
            Assert.NotNull(lobby);
            Assert.Equal(result.LobbyCode, lobby.LobbyCode);
        }

        [Fact]
        public void JoinLobby_WhenCodeIsNull_ShouldThrowArgumentException()
        {
            var client = new PlayerClient(1, "User", 1, _mockCallback.Object);
            Assert.Throws<ArgumentException>(() => _manager.JoinLobby(client, null));
        }

        [Fact]
        public void JoinLobby_WhenLobbyNotFound_ShouldThrowLobbyNotFoundException()
        {
            var client = new PlayerClient(1, "User", 1, _mockCallback.Object);
            Assert.Throws<LobbyNotFoundException>(() => _manager.JoinLobby(client, "INVALID"));
        }

        [Fact]
        public void JoinLobby_WhenUserBanned_ShouldThrowPlayerBannedException()
        {
            var host = new PlayerClient(1, "Host", 1, _mockCallback.Object);
            var lobbyDto = _manager.CreateLobby(host);
            var lobby = _manager.FindLobbyByHostId(1);

            lobby.BanPlayer(99);

            var bannedClient = new PlayerClient(99, "Banned", 1, _mockCallback.Object);

            Assert.Throws<PlayerBannedException>(() => _manager.JoinLobby(bannedClient, lobbyDto.LobbyCode));
        }

        [Fact]
        public void JoinLobby_WhenUserAlreadyInLobby_ShouldThrowUserAlreadyInLobbyException()
        {
            var host = new PlayerClient(1, "Host", 1, _mockCallback.Object);
            var lobbyDto = _manager.CreateLobby(host);

            Assert.Throws<UserAlreadyInLobbyException>(() => _manager.JoinLobby(host, lobbyDto.LobbyCode));
        }

        [Fact]
        public void JoinLobby_WhenLobbyFull_ShouldThrowLobbyFullException()
        {
            var host = new PlayerClient(1, "Host", 1, _mockCallback.Object);
            var lobbyDto = _manager.CreateLobby(host);

            _manager.JoinLobby(new PlayerClient(2, "P2", 1, _mockCallback.Object), lobbyDto.LobbyCode);
            _manager.JoinLobby(new PlayerClient(3, "P3", 1, _mockCallback.Object), lobbyDto.LobbyCode);
            _manager.JoinLobby(new PlayerClient(4, "P4", 1, _mockCallback.Object), lobbyDto.LobbyCode);

            var extraClient = new PlayerClient(5, "P5", 1, _mockCallback.Object);

            Assert.Throws<LobbyFullException>(() => _manager.JoinLobby(extraClient, lobbyDto.LobbyCode));
        }

        [Fact]
        public void LeaveLobby_WhenPlayerNotInLobby_ShouldDoNothing()
        {
            var client = new PlayerClient(1, "User", 1, _mockCallback.Object);
            client.CurrentLobby = null;

            _manager.LeaveLobby(client);
        }

        [Fact]
        public void KickPlayer_WhenHostNotInLobby_ShouldThrowActionNotAllowed()
        {
            var host = new PlayerClient(1, "Host", 1, _mockCallback.Object);
            host.CurrentLobby = null;

            Assert.Throws<LobbyActionNotAllowedException>(() => _manager.KickPlayer(host, 2));
        }

        [Fact]
        public void KickPlayer_WhenNotHost_ShouldThrowActionNotAllowed()
        {
            var host = new PlayerClient(1, "Host", 1, _mockCallback.Object);
            var lobbyDto = _manager.CreateLobby(host);
            var client = new PlayerClient(2, "Player", 1, _mockCallback.Object);
            _manager.JoinLobby(client, lobbyDto.LobbyCode);

            Assert.Throws<LobbyActionNotAllowedException>(() => _manager.KickPlayer(client, 1));
        }

        [Fact]
        public void KickPlayer_WhenKickingSelf_ShouldThrowActionNotAllowed()
        {
            var host = new PlayerClient(1, "Host", 1, _mockCallback.Object);
            var lobbyDto = _manager.CreateLobby(host);

            Assert.Throws<LobbyActionNotAllowedException>(() => _manager.KickPlayer(host, 1));
        }

        [Fact]
        public void KickPlayer_WhenTargetNotInLobby_ShouldThrowClientNotFound()
        {
            var host = new PlayerClient(1, "Host", 1, _mockCallback.Object);
            _manager.CreateLobby(host);

            Assert.Throws<ClientNotFoundException>(() => _manager.KickPlayer(host, 99));
        }

        [Fact]
        public void FindLobbyByPlayerId_WhenExists_ShouldReturnLobby()
        {
            var host = new PlayerClient(1, "Host", 1, _mockCallback.Object);
            _manager.CreateLobby(host);

            var lobby = _manager.FindLobbyByPlayerId(1);
            Assert.NotNull(lobby);
            Assert.Equal(1, lobby.HostUserId);
        }

        [Fact]
        public void FindLobbyByPlayerId_WhenNotExists_ShouldReturnNull()
        {
            var lobby = _manager.FindLobbyByPlayerId(999);
            Assert.Null(lobby);
        }
    }
}