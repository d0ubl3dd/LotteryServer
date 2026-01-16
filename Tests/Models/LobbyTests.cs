using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ServiceModel;
using BusinessLogic.Models;
using BusinessLogic.Exceptions;
using Contracts.Callbacks;
using Contracts.DTOs;
using DataAccess;
using DataAccess.DAOs;
using Tests.Builders;
using System.Reflection;

namespace Tests.Models
{
    public class LobbyTests
    {
        private readonly Mock<IUserDao> _mockUserDao;
        private readonly Mock<ILotteryCallback> _mockHostCallback;
        private readonly PlayerClient _hostClient;
        private readonly Lobby _lobby;

        public LobbyTests()
        {
            _mockUserDao = new Mock<IUserDao>();
            _mockHostCallback = new Mock<ILotteryCallback>();

            _hostClient = new PlayerClient(1, "Host", 1, _mockHostCallback.Object);
            _lobby = new Lobby("CODE123", _hostClient, _mockUserDao.Object);
        }

        [Fact]
        public void Constructor_ShouldAddHostToPlayers()
        {
            Assert.Single(_lobby.Players);
            Assert.Equal(_hostClient, _lobby.Players[0]);
            Assert.Equal(_lobby, _hostClient.CurrentLobby);
        }

        [Fact]
        public void AddPlayer_WhenLobbyIsNotFull_ShouldAddPlayer()
        {
            var newPlayer = new PlayerClient(2, "Player2", 1, Mock.Of<ILotteryCallback>());

            bool result = _lobby.AddPlayer(newPlayer);

            Assert.True(result);
            Assert.Equal(2, _lobby.Players.Count);
        }

        [Fact]
        public void AddPlayer_WhenLobbyIsFull_ShouldReturnFalse()
        {
            _lobby.AddPlayer(new PlayerClient(2, "P2", 1, Mock.Of<ILotteryCallback>()));
            _lobby.AddPlayer(new PlayerClient(3, "P3", 1, Mock.Of<ILotteryCallback>()));
            _lobby.AddPlayer(new PlayerClient(4, "P4", 1, Mock.Of<ILotteryCallback>()));

            var extraPlayer = new PlayerClient(5, "P5", 1, Mock.Of<ILotteryCallback>());
            bool result = _lobby.AddPlayer(extraPlayer);

            Assert.False(result);
            Assert.Equal(4, _lobby.Players.Count);
        }

        [Fact]
        public void AddPlayer_WhenPlayerBanned_ShouldReturnFalse()
        {
            _lobby.BanPlayer(2);
            var bannedPlayer = new PlayerClient(2, "Banned", 1, Mock.Of<ILotteryCallback>());

            bool result = _lobby.AddPlayer(bannedPlayer);

            Assert.False(result);
        }

        [Fact]
        public void RemovePlayer_WhenHostLeaves_ShouldTriggerHostLeftEvent()
        {
            bool eventTriggered = false;
            _lobby.HostLeft += () => eventTriggered = true;

            _lobby.RemovePlayer(_hostClient);

            Assert.True(eventTriggered);
            Assert.Empty(_lobby.Players);
        }

        [Fact]
        public void BroadcastChatMessage_WhenSpam_ShouldThrowChatException()
        {
            for (int i = 0; i < 10; i++)
            {
                _lobby.BroadcastChatMessage("Host", "Spam");
            }

            Assert.Throws<ChatException>(() => _lobby.BroadcastChatMessage("Host", "Spam"));
        }

        [Fact]
        public async Task NotifyGameWinAsync_ShouldUpdateScoreAndEndGame()
        {
            _lobby.StartLobbyGame(new GameSettingsDto());

            var user = new User { id_user = 1, score = 100 };
            _mockUserDao.Setup(d => d.GetUserByIdAsync(1)).ReturnsAsync(user);

            await _lobby.NotifyGameWinAsync(1);

            Assert.Equal(1100, user.score);
            _mockUserDao.Verify(d => d.SaveChangesAsync(), Times.Once);
            Assert.False(_lobby.IsGameInProgress);
        }

        [Fact]
        public void MarkPosition_ShouldAddToPlayerSet()
        {
            _lobby.MarkPosition(1, 5);
            Assert.Contains(5, _hostClient.MarkedPositions);
        }

        [Fact]
        public void BanPlayer_ShouldPreventRejoin()
        {
            _lobby.BanPlayer(99);
            Assert.True(_lobby.IsBanned(99));
        }

        private void SetDrawnCards(Lobby lobby, List<int> cards)
        {
            var field = typeof(Lobby).GetField("_drawnCards", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                var list = (List<int>)field.GetValue(lobby);
                list.Clear();
                list.AddRange(cards);
            }
        }
    }
}