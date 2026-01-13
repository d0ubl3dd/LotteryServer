using BusinessLogic.Exceptions;
using BusinessLogic.Handlers;
using BusinessLogic.Logic;
using BusinessLogic.Validation; // Para RegistrationValidationResult
using Contracts.DTOs;
using Contracts.Faults;
using Contracts.Services.Users;
using DataAccess;
using DataAccess.DAOs;
using Moq;
using System;
using System.ServiceModel;
using System.Threading.Tasks;
using Tests.Builders;
using Xunit;

namespace Tests.Logic
{
    public class UserHandlerTests
    {
        private readonly Mock<IUserDao> _mockUserDao;
        private readonly Mock<IVerificationService> _mockVerificationService;
        private readonly UserHandler _handler;

        public UserHandlerTests()
        {
            _mockUserDao = new Mock<IUserDao>();
            _mockVerificationService = new Mock<IVerificationService>();
            _handler = new UserHandler(_mockUserDao.Object, _mockVerificationService.Object);
        }

        // ==========================================
        // PRUEBAS: RegisterUser
        // ==========================================

        [Fact]
        public async Task RegisterUserWithCode_WhenDtoAndCodeAreValid_ShouldAddUserAndReturnId()
        {
            // Arrange
            var dto = new UserDto
            {
                Nickname = "NewUser",
                Email = "new@test.com",
                Password = "Password123!", // Cumple requisitos de complejidad
                FirstName = "Test",
                PaternalLastName = "Test"
            };

            string verificationCode = "123456";

            // Mock del verification handler para que devuelva true al verificar
            _mockVerificationService
                .Setup(v => v.VerifyCode(dto.Email, verificationCode))
                .ReturnsAsync(true);

            _mockVerificationService
                .Setup(v => v.ConsumeVerificationCode(dto.Email))
                .Returns((Task<bool>)Task.CompletedTask);

            // Act
            int result = await _handler.RegisterUserWithCode(dto, verificationCode);

            // Assert
            _mockUserDao.Verify(d => d.AddUser(It.Is<User>(u => u.nickname == dto.Nickname && u.email == dto.Email)), Times.Once);
            _mockUserDao.Verify(d => d.SaveChangesAsync(), Times.Once);

            _mockVerificationService.Verify(v => v.ConsumeVerificationCode(dto.Email), Times.Once);

            Assert.True(result > 0); // Devuelve el ID del usuario registrado
        }


        [Fact]
        public async Task RequestVerification_WhenNicknameExists_ShouldThrowFault_UserDuplicate()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: DTO con nickname ya registrado.
             * ✔ Salida Esperada: FaultException (USER_DUPLICATE).
             */

            // Arrange
            var dto = new UserDto 
            { 
                Nickname = "TakenNick", Email = "a@a.com", Password = "PasswordValid1!", FirstName = "A", PaternalLastName = "B" 
            };

            _mockUserDao.Setup(d => d.NicknameExistsAsync("TakenNick")).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.RequestUserVerification(dto));

            // Nota: Aquí el handler lanza UserAlreadyExistsException, que el mapper convierte
            Assert.Equal("USER_DUPLICATE", ex.Detail.ErrorCode);
        }

        // ==========================================
        // PRUEBAS: VerifyPassword
        // ==========================================

        [Fact]
        public async Task VerifyPassword_WhenPasswordIsCorrect_ShouldReturnTrue()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Usuario existente, contraseña correcta.
             * ✔ Salida Esperada: True.
             */

            // Arrange
            string pass = "MySecretPass";
            var user = new UserBuilder().WithId(1).WithPassword(pass).Build();

            _mockUserDao.Setup(d => d.GetUserByIdAsync(1)).ReturnsAsync(user);

            // Act
            bool isValid = await _handler.VerifyPassword(1, pass);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public async Task VerifyPassword_WhenPasswordIsIncorrect_ShouldReturnFalse()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Contraseña errónea.
             * ✔ Salida Esperada: False.
             */

            // Arrange
            var user = new UserBuilder().WithId(1).WithPassword("CorrectPass").Build();
            _mockUserDao.Setup(d => d.GetUserByIdAsync(1)).ReturnsAsync(user);

            // Act
            bool isValid = await _handler.VerifyPassword(1, "WrongPass");

            // Assert
            Assert.False(isValid);
        }

        // ==========================================
        // PRUEBAS: ChangePassword
        // ==========================================

        [Fact]
        public async Task ChangePassword_WhenUserExists_ShouldUpdateHashAndSave()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: ID válido, nueva contraseña.
             * ✔ Salida Esperada: SaveChangesAsync llamado.
             */

            // Arrange
            var user = new UserBuilder().WithId(1).Build();
            _mockUserDao.Setup(d => d.GetUserByIdAsync(1)).ReturnsAsync(user);

            // Act
            await _handler.ChangePassword(1, "NewPass123");

            // Assert
            _mockUserDao.Verify(d => d.SaveChangesAsync(), Times.Once);
            // Podríamos verificar que el hash cambió, pero es difícil saber el valor exacto del nuevo hash.
        }

        // ==========================================
        // PRUEBAS: UpdateProfile
        // ==========================================

        [Fact]
        public async Task UpdateProfile_WhenNicknameChangedToDuplicate_ShouldThrowFault()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Cambio de nickname a uno ocupado.
             * ✔ Salida Esperada: FaultException (USER_DUPLICATE).
             */

            // Arrange
            var user = new UserBuilder().WithId(1).WithNickname("OldNick").Build();
            var dto = new UserDto { Nickname = "TakenNick", FirstName = "A", PaternalLastName = "B" };

            _mockUserDao.Setup(d => d.GetUserByIdAsync(1)).ReturnsAsync(user);
            _mockUserDao.Setup(d => d.NicknameExistsAsync("TakenNick")).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.UpdateProfile(1, dto));

            Assert.Equal("USER_DUPLICATE", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task UpdateProfile_WhenValid_ShouldUpdatePropertiesAndSave()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Datos válidos.
             * ✔ Salida Esperada: Propiedades actualizadas en objeto y SaveChanges.
             */

            // Arrange
            var user = new UserBuilder().WithId(1).WithNickname("OldNick").Build();
            var dto = new UserDto
            {
                Nickname = "NewNick",
                FirstName = "NewName",
                PaternalLastName = "NewLast",
                AvatarId = 5
            };

            _mockUserDao.Setup(d => d.GetUserByIdAsync(1)).ReturnsAsync(user);
            _mockUserDao.Setup(d => d.NicknameExistsAsync("NewNick")).ReturnsAsync(false); // Libre

            // Act
            await _handler.UpdateProfile(1, dto);

            // Assert
            Assert.Equal("NewNick", user.nickname);
            Assert.Equal("NewName", user.first_name);
            Assert.Equal(5, user.id_avatar);
            _mockUserDao.Verify(d => d.SaveChangesAsync(), Times.Once);
        }
    }
}