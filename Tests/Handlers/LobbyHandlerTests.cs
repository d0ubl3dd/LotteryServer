using Xunit;
using Moq;
using System;
using System.Threading.Tasks;
using System.ServiceModel;
using BusinessLogic.Logic;
using BusinessLogic.Models;
using DataAccess;
using Contracts.DTOs;
using Contracts.Faults;
using Contracts.Callbacks;
using Tests.Builders;

namespace Tests.Handlers
{
    public class LobbyHandlerTests
    {
        private readonly Mock<ILobbyManager> _mockLobbyManager;
        private readonly Mock<ISessionManager> _mockSessionManager;
        private readonly Mock<ILotteryCallback> _mockCallback;
        private readonly LobbyHandler _handler;

        public LobbyHandlerTests()
        {
            _mockLobbyManager = new Mock<ILobbyManager>();
            _mockSessionManager = new Mock<ISessionManager>();
            _mockCallback = new Mock<ILotteryCallback>();

            _handler = new LobbyHandler(_mockLobbyManager.Object, _mockSessionManager.Object);
        }

        // ==========================================
        // PRUEBAS: CreateLobby
        // ==========================================

        [Fact]
        public async Task CreateLobby_WhenUserIsValidAndNotInLobby_ShouldReturnLobbyState()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Usuario válido, sesión activa, sin lobby actual.
             * ✔ Salida Esperada: DTO del nuevo lobby.
             */

            // Arrange
            var user = new UserBuilder().WithId(1).Build();

            // FIX: Constructor actualizado con 4 parámetros
            var client = new PlayerClient(user.id_user, user.nickname, user.id_avatar, _mockCallback.Object);

            // Aseguramos que no esté en ningún lobby
            client.CurrentLobby = null;

            var expectedDto = new LobbyStateDto { LobbyCode = "ABC1234" };

            _mockSessionManager.Setup(sm => sm.GetClient(user.id_user)).Returns(client);
            _mockLobbyManager.Setup(lm => lm.CreateLobby(client)).Returns(expectedDto);

            // Act
            var result = await _handler.CreateLobby(user);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("ABC1234", result.LobbyCode);
            _mockLobbyManager.Verify(lm => lm.CreateLobby(client), Times.Once);
        }

        [Fact]
        public async Task CreateLobby_WhenUserAlreadyInLobby_ShouldThrowFault_AlreadyInLobby()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Usuario que ya tiene la propiedad CurrentLobby asignada.
             * ✔ Salida Esperada: FaultException "LOBBY_USER_ALREADY_IN".
             */

            // Arrange
            var user = new UserBuilder().Build();

            // FIX: Constructor actualizado con 4 parámetros
            var client = new PlayerClient(user.id_user, user.nickname, user.id_avatar, _mockCallback.Object);

            // Simulamos que ya está en un lobby (Mock parcial)
            var mockExistingLobby = new Mock<Lobby>("EXIST", client);
            client.CurrentLobby = mockExistingLobby.Object;

            _mockSessionManager.Setup(sm => sm.GetClient(user.id_user)).Returns(client);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.CreateLobby(user));

            Assert.Equal("LOBBY_USER_ALREADY_IN", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task CreateLobby_WhenSessionNotFound_ShouldThrowFault_UserOffline()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Usuario válido pero GetClient retorna null.
             * ✔ Salida Esperada: FaultException "USER_OFFLINE" (UserNotConnectedException).
             */

            // Arrange
            var user = new UserBuilder().Build();
            _mockSessionManager.Setup(sm => sm.GetClient(user.id_user)).Returns((PlayerClient)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.CreateLobby(user));

            Assert.Equal("USER_OFFLINE", ex.Detail.ErrorCode);
        }

        // ==========================================
        // PRUEBAS: JoinLobby
        // ==========================================

        [Fact]
        public async Task JoinLobby_WhenCodeIsValid_ShouldReturnLobbyState()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Código válido, usuario libre.
             * ✔ Salida Esperada: Llama a JoinLobby en el manager y retorna DTO.
             */

            // Arrange
            var user = new UserBuilder().Build();

            // FIX: Constructor actualizado con 4 parámetros
            var client = new PlayerClient(user.id_user, user.nickname, user.id_avatar, _mockCallback.Object);

            string code = "CODE12";
            var expectedDto = new LobbyStateDto { LobbyCode = code };

            _mockSessionManager.Setup(sm => sm.GetClient(user.id_user)).Returns(client);
            _mockLobbyManager.Setup(lm => lm.JoinLobby(client, code)).Returns(expectedDto);

            // Act
            var result = await _handler.JoinLobby(user, code);

            // Assert
            Assert.Equal(code, result.LobbyCode);
        }

        [Fact]
        public async Task JoinLobby_WhenCodeIsEmpty_ShouldThrowFault_BadRequest()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Código vacío o nulo.
             * ✔ Salida Esperada: FaultException "GLOBAL_BAD_REQUEST".
             */

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.JoinLobby(new UserBuilder().Build(), ""));

            Assert.Equal("GLOBAL_BAD_REQUEST", ex.Detail.ErrorCode);
        }

        // ==========================================
        // PRUEBAS: KickPlayer
        // ==========================================

        [Fact]
        public async Task KickPlayer_WhenHostIsInLobby_ShouldCallManagerKick()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Host en un lobby válido intentando echar a alguien.
             * ✔ Salida Esperada: Se invoca _lobbyManager.KickPlayer.
             */

            // Arrange
            var hostUser = new UserBuilder().WithId(1).Build();

            // FIX: Constructor actualizado con 4 parámetros
            var hostClient = new PlayerClient(hostUser.id_user, hostUser.nickname, hostUser.id_avatar, _mockCallback.Object);

            var mockLobby = new Mock<Lobby>("MYLOBBY", hostClient);
            hostClient.CurrentLobby = mockLobby.Object;

            _mockSessionManager.Setup(sm => sm.GetClient(hostUser.id_user)).Returns(hostClient);

            // Act
            await _handler.KickPlayer(hostUser, 50); // Echar al ID 50

            // Assert
            _mockLobbyManager.Verify(lm => lm.KickPlayer(hostClient, 50), Times.Once);
        }

        [Fact]
        public async Task KickPlayer_WhenHostNotInLobby_ShouldThrowFault_LobbyError()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Usuario intenta kickear pero CurrentLobby es null.
             * ✔ Salida Esperada: FaultException "LOBBY_ERROR".
             */

            // Arrange
            var user = new UserBuilder().Build();

            // FIX: Constructor actualizado con 4 parámetros
            var client = new PlayerClient(user.id_user, user.nickname, user.id_avatar, _mockCallback.Object);

            client.CurrentLobby = null; // No está en lobby

            _mockSessionManager.Setup(sm => sm.GetClient(user.id_user)).Returns(client);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.KickPlayer(user, 50));

            Assert.Equal("LOBBY_ERROR", ex.Detail.ErrorCode);
        }
    }
}