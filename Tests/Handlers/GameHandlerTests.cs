using Xunit;
using Moq;
using System;
using System.Threading.Tasks;
using System.ServiceModel;
using BusinessLogic.Handlers;
using BusinessLogic.Logic; // Para ILobbyManager
using BusinessLogic.Models; // Para Lobby
using Contracts.DTOs;
using DataAccess;
using Contracts.Faults;
using Tests.Builders;
using Contracts.Callbacks;

namespace Tests.Handlers
{
    public class GameHandlerTests
    {
        private readonly Mock<ILobbyManager> _mockLobbyManager;
        private readonly GameHandler _handler;

        // Helpers
        private readonly Mock<ILotteryCallback> _mockCallback;

        public GameHandlerTests()
        {
            _mockLobbyManager = new Mock<ILobbyManager>();
            _mockCallback = new Mock<ILotteryCallback>();
            _handler = new GameHandler(_mockLobbyManager.Object);
        }

        // ==========================================
        // PRUEBAS: StartGame
        // ==========================================

        [Fact]
        public async Task StartGame_WhenConditionsMet_ShouldStartLobbyGame()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Host válido, Settings válidos, Lobby encontrado y libre.
             * ✔ Salida Esperada: Llamada a lobby.StartLobbyGame().
             */

            // Arrange
            var host = new UserBuilder().WithId(1).WithNickname("HostUser").Build();
            var settings = new GameSettingsDto();

            var hostClient = new PlayerClient(host, _mockCallback.Object);

            // Mock del Lobby con método virtual StartLobbyGame
            var mockLobby = new Mock<Lobby>("CODE1", hostClient);
            mockLobby.Setup(l => l.IsGameInProgress).Returns(false); // Juego NO iniciado
            mockLobby.Setup(l => l.StartLobbyGame(settings)); // Esperamos esta llamada

            _mockLobbyManager.Setup(m => m.FindLobbyByHostId(host.id_user))
                             .Returns(mockLobby.Object);

            // Act
            await _handler.StartGame(host, settings);

            // Assert
            mockLobby.Verify(l => l.StartLobbyGame(settings), Times.Once);
        }

        [Fact]
        public async Task StartGame_WhenLobbyNotFound_ShouldThrowFault_LobbyNotFound()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Host que no tiene lobby asignado.
             * ✔ Salida Esperada: Fault LOBBY_NOT_FOUND.
             */

            // Arrange
            var host = new UserBuilder().WithId(1).Build();
            _mockLobbyManager.Setup(m => m.FindLobbyByHostId(1)).Returns((Lobby)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.StartGame(host, new GameSettingsDto()));

            Assert.Equal("LOBBY_NOT_FOUND", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task StartGame_WhenGameAlreadyRunning_ShouldThrowFault_GameAlreadyActive()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Lobby donde IsGameInProgress es true.
             * ✔ Salida Esperada: Fault GAME_ALREADY_ACTIVE.
             */

            // Arrange
            var host = new UserBuilder().WithId(1).Build();
            var client = new PlayerClient(host, _mockCallback.Object);

            var mockLobby = new Mock<Lobby>("CODE", client);
            mockLobby.Object.StartLobbyGame(new GameSettingsDto()); // Forzamos estado activo si la lógica lo permite
                                                                    // O mejor, mockeamos la propiedad si fuera virtual, o usamos SetupGet si modificaste la clase para ser virtual.
                                                                    // Como IsGameInProgress tiene set privado, lo simulamos via reflexión o asumiendo estado inicial.
                                                                    // TRUCO: Como IsGameInProgress no es virtual, Mock no puede sobreescribirlo directamente a menos que 
                                                                    // usemos un método del builder o setter interno.
                                                                    // Para esta prueba, asumimos que llamar a StartLobbyGame pone IsGameInProgress = true en la lógica real de Lobby (que no está mockeada del todo aquí).

            // OPCIÓN MEJOR PARA TEST: Configurar el mock para que FindLobby devuelva un lobby "Real" en estado ocupado
            // O extraer interfaz ILobby (ideal).
            // Por ahora, usaremos reflexión para setear la propiedad privada si es necesario, 
            // pero dado tu código de Lobby, podemos llamar a StartLobbyGame real para cambiar el estado.

            mockLobby.Setup(l => l.IsGameInProgress).Returns(true); // Esto pone IsGameInProgress = true

            _mockLobbyManager.Setup(m => m.FindLobbyByHostId(1)).Returns(mockLobby.Object);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.StartGame(host, new GameSettingsDto()));

            Assert.Equal("GAME_ALREADY_ACTIVE", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task StartGame_WhenArgumentsNull_ShouldThrowFault_BadRequest()
        {
            /* DOCUMENTACIÓN
            * ✔ Entrada: host = null.
            * ✔ Salida Esperada: Fault GLOBAL_BAD_REQUEST.
            */

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.StartGame(null, new GameSettingsDto()));

            Assert.Equal("GLOBAL_BAD_REQUEST", ex.Detail.ErrorCode);
        }

        // ==========================================
        // PRUEBAS: UpdateGameSettings
        // ==========================================

        [Fact]
        public async Task UpdateSettings_WhenGameInProgress_ShouldThrowFault_GameAlreadyActive()
        {
            /* DOCUMENTACIÓN
            * ✔ Entrada: Intento de cambiar config mientras se juega.
            * ✔ Salida Esperada: Fault GAME_ALREADY_ACTIVE.
            */

            // Arrange
            var host = new UserBuilder().WithId(1).Build();
            var client = new PlayerClient(host, _mockCallback.Object);
            var mockLobby = new Mock<Lobby>("CODE", client);

            // Simulamos juego activo
            mockLobby.Setup(l => l.IsGameInProgress).Returns(true);

            _mockLobbyManager.Setup(m => m.FindLobbyByHostId(1)).Returns(mockLobby.Object);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.UpdateGameSettings(host, new GameSettingsDto()));

            Assert.Equal("GAME_ALREADY_ACTIVE", ex.Detail.ErrorCode);
        }
    }
}