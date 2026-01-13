using Xunit;
using Moq;
using BusinessLogic.Models;
using DataAccess;
using Contracts.Callbacks;
using Tests.Builders;

namespace Tests.Models
{
    public class PlayerClientTests
    {
        [Fact]
        public void Constructor_ShouldMapPropertiesAndInitializeDefaults()
        {
            var user = new UserBuilder()
                .WithId(10)
                .WithNickname("MapTest")
                .Build();

            user.id_avatar = 5;

            var mockCallback = new Mock<ILotteryCallback>();

            var client = new PlayerClient(user.id_user, user.nickname, user.id_avatar, mockCallback.Object);

            Assert.Equal(10, client.UserId);
            Assert.Equal("MapTest", client.Nickname);
            Assert.Equal(5, client.AvatarId);
            Assert.Same(mockCallback.Object, client.CallbackChannel);

            Assert.Null(client.CurrentLobby);
            Assert.Equal(0, client.SelectedBoardId);

            Assert.NotNull(client.WinningCards);
            Assert.Empty(client.WinningCards);

            Assert.NotNull(client.MarkedPositions);
            Assert.Empty(client.MarkedPositions);
        }

        [Fact]
        public void DefaultConstructor_ShouldInitializeCollections()
        {
            var client = new PlayerClient();

            Assert.NotNull(client.WinningCards);
            Assert.Empty(client.WinningCards);

            Assert.NotNull(client.MarkedPositions);
            Assert.Empty(client.MarkedPositions);
        }
    }
}