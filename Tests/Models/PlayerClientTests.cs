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
            /* DOCUMENTACIÓN
             * ✔ Objetivo: Verificar que el constructor mapea datos Y que
             * las colecciones (WinningCards) se inicializan para evitar nulos.
             */

            // Arrange
            var user = new UserBuilder()
                .WithId(10)
                .WithNickname("MapTest")
                .Build();

            // Asignamos manualmente el avatar
            user.id_avatar = 5;

            var mockCallback = new Mock<ILotteryCallback>();

            // Act
            // Constructor de 4 parámetros (Correcto según tu clase)
            var client = new PlayerClient(user.id_user, user.nickname, user.id_avatar, mockCallback.Object);

            // Assert
            // 1. Propiedades Mapeadas
            Assert.Equal(10, client.UserId);
            Assert.Equal("MapTest", client.Nickname);
            Assert.Equal(5, client.AvatarId);
            Assert.Same(mockCallback.Object, client.CallbackChannel);

            // 2. Valores por Defecto (Importante validar la colección)
            Assert.Null(client.CurrentLobby);
            Assert.Equal(0, client.SelectedBoardId);

            Assert.NotNull(client.WinningCards); // Vital: Verificar que no sea nula
            Assert.Empty(client.WinningCards);   // Verificar que inicia vacía
        }

        [Fact]
        public void DefaultConstructor_ShouldInitializeCollections()
        {
            /* DOCUMENTACIÓN
             * ✔ Objetivo: Asegurar que incluso el constructor vacío 
             * inicializa la lista de cartas ganadoras.
             */

            // Act
            var client = new PlayerClient();

            // Assert
            Assert.NotNull(client.WinningCards);
            Assert.Empty(client.WinningCards);
        }
    }
}