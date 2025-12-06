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
        public void Constructor_ShouldMapPropertiesFromUserCorrectly()
        {
            /* DOCUMENTACIÓN
             * ✔ Objetivo: Verificar que el constructor inicializa correctamente 
             * las propiedades de solo lectura basándose en el User.
             */

            // Arrange
            var user = new UserBuilder()
                .WithId(10)
                .WithNickname("MapTest")
                .Build();

            // Asignamos manualmente el avatar al objeto User, ya que el Builder usa un default
            user.id_avatar = 5;

            var mockCallback = new Mock<ILotteryCallback>();

            // Act
            var client = new PlayerClient(user, mockCallback.Object);

            // Assert
            Assert.Equal(10, client.UserId);
            Assert.Equal("MapTest", client.Nickname);
            Assert.Equal(5, client.AvatarId);
            Assert.Same(mockCallback.Object, client.CallbackChannel);
            Assert.Null(client.CurrentLobby); // Por defecto debe ser null
        }
    }
}