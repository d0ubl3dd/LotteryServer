using Xunit;
using System.Threading.Tasks;
using System.ServiceModel;
using BusinessLogic.Handlers;
using DataAccess;
using Contracts.Faults;
using BusinessLogic.Exceptions; // Para las excepciones específicas si quieres probarlas directo, o Faults

namespace Tests.Handlers
{
    public class GuestHandlerTests
    {
        private readonly GuestHandler _handler;

        public GuestHandlerTests()
        {
            // No hay dependencias que mockear :)
            _handler = new GuestHandler();
        }

        // ==========================================
        // PRUEBAS: LoginGuest (Casos Exitosos)
        // ==========================================

        [Fact]
        public async Task LoginGuest_WhenNicknameIsValid_ShouldReturnUserWithNegativeId()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Nickname válido "Visitante".
             * ✔ Salida Esperada: Objeto User con ID < 0 y status "Online".
             * ✔ Validación: Assert sobre propiedades del objeto retornado.
             */

            // Arrange
            string nickname = "Visitante";

            // Act
            User result = await _handler.LoginGuest(nickname);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(nickname, result.nickname);
            Assert.True(result.id_user < 0, "El ID del invitado debe ser negativo.");
            Assert.Equal("Online", result.status);
            Assert.Equal("guest@temp.com", result.email);
        }

        [Fact]
        public async Task LoginGuest_WhenCalledMultipleTimes_ShouldGenerateUniqueIds()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Dos llamadas consecutivas.
             * ✔ Salida Esperada: Dos usuarios con IDs diferentes (y decremetales).
             * ✔ Propósito: Verificar Interlocked.Decrement.
             */

            // Act
            User guest1 = await _handler.LoginGuest("GuestOne");
            User guest2 = await _handler.LoginGuest("GuestTwo");

            // Assert
            Assert.NotEqual(guest1.id_user, guest2.id_user);
            Assert.True(guest2.id_user < guest1.id_user, "Los IDs deben ser decrecientes (-1, -2, etc).");
        }

        // ==========================================
        // PRUEBAS: Validaciones (Faults)
        // ==========================================

        [Fact]
        public async Task LoginGuest_WhenNicknameIsEmpty_ShouldThrowFault_EmptyNickname()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Nickname vacío "".
             * ✔ Salida Esperada: FaultException "AUTH_EMPTY_NICKNAME".
             */

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.LoginGuest(""));

            Assert.Equal("AUTH_EMPTY_NICKNAME", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task LoginGuest_WhenNicknameIsTooShort_ShouldThrowFault_InvalidLength()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: "Ab" (Muy corto).
             * ✔ Salida Esperada: FaultException "AUTH_INVALID_LENGTH".
             */

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.LoginGuest("Ab"));

            Assert.Equal("AUTH_INVALID_LENGTH", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task LoginGuest_WhenNicknameHasSpecialChars_ShouldThrowFault_InvalidFormat()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: "Guest$#".
             * ✔ Salida Esperada: FaultException "AUTH_INVALID_FORMAT".
             */

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.LoginGuest("Guest$#"));

            Assert.Equal("AUTH_INVALID_FORMAT", ex.Detail.ErrorCode);
        }
    }
}