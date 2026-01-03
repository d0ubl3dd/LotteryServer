using Xunit;
using Moq;
using System;
using System.Threading.Tasks;
using System.ServiceModel;
using BusinessLogic.Handlers;
using BusinessLogic.Logic;
using BusinessLogic.Models;
using BusinessLogic.Exceptions;
using Contracts.Faults;
using Contracts.Callbacks;
using DataAccess;
using Tests.Builders;

namespace Tests.Handlers
{
    public class ChatHandlerTests
    {
        private readonly Mock<ISessionManager> _mockSessionManager;
        private readonly Mock<ILobbyManager> _mockLobbyManager;
        private readonly ChatHandler _handler;

        // Objetos auxiliares para configurar el entorno de pruebas
        private readonly Mock<ILotteryCallback> _mockCallback;

        public ChatHandlerTests()
        {
            _mockSessionManager = new Mock<ISessionManager>();
            _mockLobbyManager = new Mock<ILobbyManager>();
            _mockCallback = new Mock<ILotteryCallback>();

            // SUT (System Under Test)
            _handler = new ChatHandler(_mockSessionManager.Object, _mockLobbyManager.Object);
        }

        // ==========================================
        // PRUEBAS DE ENVÍO DE MENSAJES (CASOS FELICES)
        // ==========================================

        [Fact]
        public async Task SendMessage_WhenUserAndLobbyAreValid_ShouldBroadcastMessage()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Usuario válido, en sesión activa, unido a un Lobby. Mensaje: "Hola Mundo".
             * ✔ Salida Esperada: Se llama al método BroadcastChatMessage del Lobby.
             * ✔ Validación: Verify del Mock de Lobby.
             * ✔ Descripción: Verifica el flujo principal de comunicación.
             */

            // Arrange
            string message = "Hola Mundo";
            var user = new UserBuilder().WithNickname("Chatter").Build();

            // 1. Preparamos el cliente (PlayerClient)
            var client = new PlayerClient(
                user.id_user,
                user.nickname,
                user.id_avatar,
                _mockCallback.Object
            );
            // 2. Preparamos un MOCK del Lobby
            // Nota: Al ser 'virtual' el método Broadcast, podemos mockearlo aunque Lobby sea una clase concreta.
            // Pasamos argumentos dummy al constructor base de Lobby para satisfacerlo.
            var mockLobby = new Mock<Lobby>("CODE1", client);
            mockLobby.Setup(l => l.BroadcastChatMessage(It.IsAny<string>(), It.IsAny<string>()))
                     .Returns(true); // Simulamos que el broadcast fue exitoso

            // 3. Conectamos todo: El cliente está en ese Lobby simulado
            client.CurrentLobby = mockLobby.Object;

            // 4. El SessionManager devuelve este cliente
            _mockSessionManager.Setup(sm => sm.GetClient(user.id_user)).Returns(client);

            // Act
            await _handler.SendMessage(user, message);

            // Assert
            // Verificamos que el Handler delegó la responsabilidad al Lobby correctamente
            mockLobby.Verify(l => l.BroadcastChatMessage(user.nickname, message), Times.Once);
        }

        [Fact]
        public async Task SendMessage_WhenMessageIsEmpty_ShouldDoNothing()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Mensaje vacío o solo espacios.
             * ✔ Salida Esperada: El método retorna sin error y SIN llamar al Lobby.
             * ✔ Descripción: Optimización para evitar tráfico de red innecesario.
             */

            // Arrange
            var user = new UserBuilder().Build();

            // Act
            await _handler.SendMessage(user, "   "); // Espacios en blanco

            // Assert
            // No necesitamos configurar SessionManager porque el código debería retornar antes de llamarlo.
            _mockSessionManager.Verify(sm => sm.GetClient(It.IsAny<int>()), Times.Never);
        }

        // ==========================================
        // PRUEBAS DE ERROR (Manejo de Faults WCF)
        // ==========================================

        [Fact]
        public async Task SendMessage_WhenUserIsNull_ShouldThrowFault_BadRequest()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: currentUser = null.
             * ✔ Salida Esperada: FaultException "GLOBAL_BAD_REQUEST" (Mapeado de ArgumentNullException).
             * ✔ Falla detectada: Validación defensiva dentro del ExecuteFaultSafeAsync.
             */

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.SendMessage(null, "Hola"));

            Assert.Equal("GLOBAL_BAD_REQUEST", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task SendMessage_WhenUserIsNotOnline_ShouldThrowFault_UserOffline()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Usuario válido pero que NO está en el diccionario de SessionManager.
             * ✔ Salida Esperada: FaultException "USER_OFFLINE" (Mapeado de UserNotOnlineException).
             * ✔ Camino: SessionManager.GetClient devuelve null -> Handler lanza Excepción -> BaseHandler atrapa.
             */

            // Arrange
            var user = new UserBuilder().WithId(99).Build();

            // Simulamos que no se encuentra sesión
            _mockSessionManager.Setup(sm => sm.GetClient(user.id_user)).Returns((PlayerClient)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.SendMessage(user, "Hola"));

            Assert.Equal("USER_OFFLINE", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task SendMessage_WhenUserIsNotInLobby_ShouldThrowFault_UserNotInLobby()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Usuario online (Client válido), pero propiedad CurrentLobby es null.
             * ✔ Salida Esperada: FaultException "CHAT_USER_NOT_IN_LOBBY".
             * ✔ Descripción: Un usuario en el menú principal no puede enviar mensajes de chat de juego.
             */

            // Arrange
            var user = new UserBuilder().Build();
            var client = new PlayerClient(
                user.id_user,
                user.nickname,
                user.id_avatar,
                _mockCallback.Object
            );
            client.CurrentLobby = null; // ESTADO CRÍTICO: No está en lobby

            _mockSessionManager.Setup(sm => sm.GetClient(user.id_user)).Returns(client);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.SendMessage(user, "Hola"));

            Assert.Equal("CHAT_USER_NOT_IN_LOBBY", ex.Detail.ErrorCode);
        }
    }
}