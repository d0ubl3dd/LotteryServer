using Xunit;
using Moq;
using System;
using System.Threading.Tasks;
using System.ServiceModel; // Necesario para FaultException
using BusinessLogic.Handlers;
using BusinessLogic.Validation; // Para los enums si los usas
using DataAccess.DAOs;
using DataAccess;
using Contracts.Faults; // Para ServiceFault
using Tests.Builders;

namespace Tests.Handlers
{
    public class AuthenticationHandlerTests
    {
        private readonly Mock<IUserDao> _mockUserDao;
        private readonly AuthenticationHandler _handler;

        public AuthenticationHandlerTests()
        {
            _mockUserDao = new Mock<IUserDao>();
            _handler = new AuthenticationHandler(_mockUserDao.Object);
        }

        // ==========================================
        // PRUEBAS DE LOGIN (CASOS FELICES)
        // ==========================================

        [Fact]
        public async Task LoginUser_WhenCredentialsAreValid_ShouldReturnUserAndSetOnline()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Usuario válido "JugadorPro", Pass "Pass123"
             * ✔ Salida Esperada: Objeto User, Status="Online" en BD.
             * ✔ Validación: Assert directo sobre el resultado.
             */

            // Arrange
            string password = "Pass123";
            var validUser = new UserBuilder()
                .WithNickname("JugadorPro")
                .WithPassword(password)
                .WithFailedAttempts(2) // Estaba sucio, debe limpiarse
                .Build();

            _mockUserDao.Setup(dao => dao.GetUserByNicknameAsync(validUser.nickname)).ReturnsAsync(validUser);
            _mockUserDao.Setup(dao => dao.GetUserByIdAsync(validUser.id_user)).ReturnsAsync(validUser);

            // Act
            User result = await _handler.LoginUser(validUser.nickname, password);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Online", validUser.status);
            Assert.Equal(0, validUser.failedLoginAttempts);
            _mockUserDao.Verify(dao => dao.SaveChangesAsync(), Times.Once);
        }

        // ==========================================
        // PRUEBAS DE LOGIN (CASOS DE ERROR WCF)
        // ==========================================

        [Fact]
        public async Task LoginUser_WhenUserDoesNotExist_ShouldThrowFault_UserNotFound()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Usuario inexistente.
             * ✔ Salida Esperada: FaultException<ServiceFault> con código "AUTH_USER_NOT_FOUND".
             * ✔ Falla que atrapa: Mapeo correcto de excepciones de dominio a WCF.
             */

            // Arrange
            string username = "Fantasma";
            _mockUserDao.Setup(dao => dao.GetUserByNicknameAsync(username)).ReturnsAsync((User)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.LoginUser(username, "pass"));

            // Validamos el Código de Error definido en ExceptionMapper
            Assert.Equal("AUTH_USER_NOT_FOUND", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task LoginUser_WhenPasswordIsIncorrect_ShouldThrowFault_InvalidCredentials()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Pass incorrecta.
             * ✔ Salida Esperada: FaultException con código "AUTH_INVALID_CREDENTIALS".
             * ✔ Validación Adicional: Incremento de intentos fallidos.
             */

            // Arrange
            var user = new UserBuilder().WithNickname("Test").WithPassword("RealPass").Build();

            _mockUserDao.Setup(dao => dao.GetUserByNicknameAsync(user.nickname)).ReturnsAsync(user);
            _mockUserDao.Setup(dao => dao.GetUserByIdAsync(user.id_user)).ReturnsAsync(user);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.LoginUser(user.nickname, "WrongPass"));

            Assert.Equal("AUTH_INVALID_CREDENTIALS", ex.Detail.ErrorCode);
            Assert.Equal(1, user.failedLoginAttempts); // Efecto secundario
            _mockUserDao.Verify(dao => dao.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task LoginUser_WhenAccountIsLocked_ShouldThrowFault_AccountLocked()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Usuario con isLocked = true.
             * ✔ Salida Esperada: FaultException con código "AUTH_ACCOUNT_LOCKED".
             */

            // Arrange
            var lockedUser = new UserBuilder().Locked().Build();
            _mockUserDao.Setup(dao => dao.GetUserByNicknameAsync(lockedUser.nickname)).ReturnsAsync(lockedUser);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.LoginUser(lockedUser.nickname, "anyPass"));

            Assert.Equal("AUTH_ACCOUNT_LOCKED", ex.Detail.ErrorCode);
            // Aseguramos que NO intentó hacer login ni guardar nada
            _mockUserDao.Verify(dao => dao.SaveChangesAsync(), Times.Never);
        }

        // ==========================================
        // PRUEBAS DE LOGOUT
        // ==========================================

        [Fact]
        public async Task LogoutUser_WhenUserIsMissingInDb_ShouldThrowFault_UserNotFound()
        {
            /* DOCUMENTACIÓN
             * ✔ Caso Borde: Usuario enviado existe en memoria pero fue borrado de BD.
             * ✔ Salida Esperada: FaultException "AUTH_USER_NOT_FOUND".
             */

            // Arrange
            var user = new UserBuilder().WithId(99).Build();
            _mockUserDao.Setup(dao => dao.GetUserByIdAsync(99)).ReturnsAsync((User)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.LogoutUser(user));

            Assert.Equal("AUTH_USER_NOT_FOUND", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task LogoutUser_WhenArgumentIsNull_ShouldThrowFault_BadRequest()
        {
            /* DOCUMENTACIÓN
             * ✔ Caso Negativo: Argumento nulo.
             * ✔ Salida Esperada: FaultException "GLOBAL_BAD_REQUEST" (Mapeado de ArgumentNullException).
             */

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.LogoutUser(null));

            Assert.Equal("GLOBAL_BAD_REQUEST", ex.Detail.ErrorCode);
        }
    }
}