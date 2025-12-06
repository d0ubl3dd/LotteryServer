using Xunit;
using Moq;
using System;
using System.Reflection;
using System.ServiceModel;
using BusinessLogic.Logic;
using BusinessLogic.Models;
using BusinessLogic.Exceptions;
using Contracts.Callbacks;
using DataAccess;
using Contracts.Faults;
using Tests.Builders;

namespace Tests.Logic
{
    public class GlobalSessionManagerTests
    {
        private GlobalSessionManager _sut; // System Under Test
        private readonly Mock<ILotteryCallback> _mockCallback;

        public GlobalSessionManagerTests()
        {
            _mockCallback = new Mock<ILotteryCallback>();
            // TRUCO AVANZADO: Instanciamos el Singleton usando reflexión para tener aislamiento total.
            _sut = CreateIsolatedInstance();
        }

        private GlobalSessionManager CreateIsolatedInstance()
        {
            // Buscamos el constructor privado
            var constructor = typeof(GlobalSessionManager).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null, new Type[0], null);

            // Invocamos el constructor para obtener una nueva instancia limpia
            return (GlobalSessionManager)constructor.Invoke(null);
        }

        // ==========================================
        // PRUEBAS: RegisterClient
        // ==========================================

        [Fact]
        public void RegisterClient_WhenUserIsValid_ShouldAddUserToDictionary()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Usuario válido y Callback válido.
             * ✔ Salida Esperada: El usuario se agrega a la memoria. GetClient lo recupera.
             */

            // Arrange
            var user = new UserBuilder().WithId(10).WithNickname("Gamer").Build();

            // Act
            _sut.RegisterClient(user, _mockCallback.Object);

            // Assert
            var client = _sut.GetClient(user.id_user);
            Assert.NotNull(client);
            Assert.Equal("Gamer", client.Nickname);
        }

        [Fact]
        public void RegisterClient_WhenArgumentsNull_ShouldThrowFault_BadRequest()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: user = null.
             * ✔ Salida Esperada: FaultException (GLOBAL_BAD_REQUEST).
             */

            // Act & Assert
            Assert.Throws<FaultException<ServiceFault>>(() =>
                _sut.RegisterClient(null, _mockCallback.Object));
        }

        // ==========================================
        // PRUEBAS: GetClient
        // ==========================================

        [Fact]
        public void GetClient_WhenUserNotRegistered_ShouldThrowFault_ClientNotFound()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: ID de usuario que no está en la lista.
             * ✔ Salida Esperada: FaultException (SESSION_CLIENT_NOT_FOUND).
             * Nota: Asegúrate que ClientNotFoundException esté mapeada en ExceptionMapper.
             */

            // Act & Assert
            var ex = Assert.Throws<FaultException<ServiceFault>>(() =>
                _sut.GetClient(999));

            // Ajusta este string al código que hayas definido en ExceptionMapper para ClientNotFoundException
            // Si no lo has mapeado, el default podría ser otro. Asumiré "SESSION_CLIENT_NOT_FOUND".
            Assert.Equal("SESSION_CLIENT_NOT_FOUND", ex.Detail.ErrorCode);
        }

        // ==========================================
        // PRUEBAS: UnregisterClient
        // ==========================================

        [Fact]
        public void UnregisterClient_WhenUserExists_ShouldRemoveAndReturnClient()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Usuario previamente registrado.
             * ✔ Salida Esperada: El usuario se elimina y el método retorna el cliente eliminado.
             */

            // Arrange
            var user = new UserBuilder().WithId(5).Build();
            _sut.RegisterClient(user, _mockCallback.Object);

            // Act
            var removedClient = _sut.UnregisterClient(user.id_user);

            // Assert
            Assert.NotNull(removedClient);
            Assert.Equal(5, removedClient.UserId);

            // Verificamos que ya no está
            Assert.Throws<FaultException<ServiceFault>>(() => _sut.GetClient(5));
        }

        [Fact]
        public void UnregisterClient_WhenUserDoesNotExist_ShouldReturnNull()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: ID no registrado.
             * ✔ Salida Esperada: Retorna null (según lógica del método) y no lanza excepción.
             */

            // Act
            var result = _sut.UnregisterClient(999);

            // Assert
            Assert.Null(result);
        }

        // ==========================================
        // PRUEBA ESPECIAL: AutoDisconnect (Simulación)
        // ==========================================

        [Fact]
        public void AutoDisconnect_WhenChannelCloses_ShouldRemoveUser()
        {
            /* DOCUMENTACIÓN
             * ✔ Descripción: Simulamos que el canal WCF se cierra (evento Closed).
             * ✔ Mocking Avanzado: Usamos .As<ICommunicationObject>() para disparar eventos.
             */

            // Arrange
            var user = new UserBuilder().WithId(20).Build();

            // Creamos un Mock que implemente ILotteryCallback Y ADEMÁS ICommunicationObject
            var mockChannel = new Mock<ILotteryCallback>();
            var mockComm = mockChannel.As<ICommunicationObject>(); // Interfaz oculta de WCF

            // Act - 1: Registramos
            _sut.RegisterClient(user, mockChannel.Object);

            // Act - 2: Disparamos el evento 'Closed' manualmente
            mockComm.Raise(c => c.Closed += null, EventArgs.Empty);

            // Assert
            // El usuario debió ser removido automáticamente
            Assert.Throws<FaultException<ServiceFault>>(() => _sut.GetClient(20));
        }
    }
}