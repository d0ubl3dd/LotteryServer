using Xunit;
using Moq;
using System.Collections.Generic;
using BusinessLogic.Models;
using Contracts.Callbacks;
using DataAccess.DAOs;

namespace Tests.Models
{
    public class PlayerClientTests
    {
        [Fact]
        public void DefaultConstructor_ShouldInitializeCollections()
        {
            var player = new PlayerClient();

            Assert.NotNull(player.MarkedPositions);
            Assert.Empty(player.MarkedPositions);
            Assert.NotNull(player.WinningCards);
            Assert.Empty(player.WinningCards);
        }

        [Theory]
        [InlineData(1, "User1", 10)]
        [InlineData(2, "PlayerTwo", 5)]
        [InlineData(-1, "Guest", 1)]
        [InlineData(999, "Admin", 0)]
        public void ParameterizedConstructor_ShouldSetPropertiesCorrectly(int id, string name, int avatar)
        {
            var callback = new Mock<ILotteryCallback>().Object;
            var player = new PlayerClient(id, name, avatar, callback);

            Assert.Equal(id, player.UserId);
            Assert.Equal(name, player.Nickname);
            Assert.Equal(avatar, player.AvatarId);
            Assert.Same(callback, player.CallbackChannel);
            Assert.NotNull(player.MarkedPositions);
            Assert.NotNull(player.WinningCards);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(100)]
        public void SelectedBoardId_SetAndGet_ShouldWork(int boardId)
        {
            var player = new PlayerClient();
            player.SelectedBoardId = boardId;

            Assert.Equal(boardId, player.SelectedBoardId);
        }

        [Fact]
        public void CallbackChannel_SetAndGet_ShouldWork()
        {
            var player = new PlayerClient();
            var callback = new Mock<ILotteryCallback>().Object;

            player.CallbackChannel = callback;

            Assert.Same(callback, player.CallbackChannel);
        }

        [Fact]
        public void CurrentLobby_SetAndGet_ShouldWork()
        {
            var player = new PlayerClient();
            var lobbyHost = new PlayerClient(1, "H", 1, null);
            var lobby = new Lobby("ABC", lobbyHost, new Mock<IUserDao>().Object);

            player.CurrentLobby = lobby;

            Assert.Same(lobby, player.CurrentLobby);
        }

        [Fact]
        public void CurrentLobby_SetNull_ShouldWork()
        {
            var player = new PlayerClient();
            var lobbyHost = new PlayerClient(1, "H", 1, null);
            var lobby = new Lobby("ABC", lobbyHost, new Mock<IUserDao>().Object);
            player.CurrentLobby = lobby;

            player.CurrentLobby = null;

            Assert.Null(player.CurrentLobby);
        }

        [Fact]
        public void MarkedPositions_ShouldAllowAddingItems()
        {
            var player = new PlayerClient();

            player.MarkedPositions.Add(1);
            player.MarkedPositions.Add(5);

            Assert.Equal(2, player.MarkedPositions.Count);
            Assert.Contains(1, player.MarkedPositions);
            Assert.Contains(5, player.MarkedPositions);
        }

        [Fact]
        public void MarkedPositions_ShouldAllowClearing()
        {
            var player = new PlayerClient();
            player.MarkedPositions.Add(1);
            player.MarkedPositions.Add(2);

            player.MarkedPositions.Clear();

            Assert.Empty(player.MarkedPositions);
        }

        [Fact]
        public void WinningCards_SetAndGet_ShouldWork()
        {
            var player = new PlayerClient();
            var newSet = new HashSet<int> { 1, 2, 3 };

            player.WinningCards = newSet;

            Assert.Same(newSet, player.WinningCards);
            Assert.Equal(3, player.WinningCards.Count);
        }

        [Fact]
        public void WinningCards_ShouldAllowModifications()
        {
            var player = new PlayerClient();

            player.WinningCards.Add(10);
            player.WinningCards.Add(20);

            Assert.Contains(10, player.WinningCards);
            Assert.Contains(20, player.WinningCards);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ParameterizedConstructor_WithEmptyNickname_ShouldStoreAsIs(string emptyNick)
        {
            var player = new PlayerClient(1, emptyNick, 1, null);
            Assert.Equal(emptyNick, player.Nickname);
        }

        [Fact]
        public void DefaultConstructor_PropertiesShouldBeDefault()
        {
            var player = new PlayerClient();

            Assert.Equal(0, player.UserId);
            Assert.Null(player.Nickname);
            Assert.Equal(0, player.AvatarId);
            Assert.Null(player.CallbackChannel);
            Assert.Null(player.CurrentLobby);
            Assert.Equal(0, player.SelectedBoardId);
        }
    }
}