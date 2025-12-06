using Xunit;
using Moq;
using System;
using System.Threading.Tasks;
using System.ServiceModel;
using BusinessLogic.Handlers;
using DataAccess.DAOs;
using DataAccess;
using Contracts.DTOs;
using Contracts.Faults;
using Tests.Builders;

namespace Tests.Handlers
{
    public class SocialMediaHandlerTests
    {
        private readonly Mock<ISocialMediaDao> _mockSocialDao;
        private readonly Mock<IUserDao> _mockUserDao;
        private readonly SocialMediaHandler _handler;

        public SocialMediaHandlerTests()
        {
            _mockSocialDao = new Mock<ISocialMediaDao>();
            _mockUserDao = new Mock<IUserDao>();
            _handler = new SocialMediaHandler(_mockSocialDao.Object, _mockUserDao.Object);
        }

        // ==========================================
        // PRUEBAS: GetSocialMedia
        // ==========================================

        [Fact]
        public async Task GetSocialMedia_WhenUserExistsAndHasData_ShouldReturnDto()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Usuario válido con redes sociales registradas.
             * ✔ Salida Esperada: DTO con los datos de redes.
             */

            // Arrange
            var user = new UserBuilder().WithId(1).Build();
            var socialData = new SocialMedia { id_user = 1, facebook = "fb_user" };

            _mockUserDao.Setup(d => d.GetUserByIdAsync(1)).ReturnsAsync(user);
            _mockSocialDao.Setup(d => d.GetSocialMediaByUserIdAsync(1)).ReturnsAsync(socialData);

            // Act
            var result = await _handler.GetSocialMedia(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("fb_user", result.Facebook);
        }

        [Fact]
        public async Task GetSocialMedia_WhenUserExistsButNoData_ShouldReturnEmptyDto()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Usuario válido sin redes sociales.
             * ✔ Salida Esperada: DTO con propiedades nulas (no error).
             */

            // Arrange
            var user = new UserBuilder().WithId(1).Build();
            _mockUserDao.Setup(d => d.GetUserByIdAsync(1)).ReturnsAsync(user);
            _mockSocialDao.Setup(d => d.GetSocialMediaByUserIdAsync(1)).ReturnsAsync((SocialMedia)null);

            // Act
            var result = await _handler.GetSocialMedia(1);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Facebook);
        }

        [Fact]
        public async Task GetSocialMedia_WhenUserNotFound_ShouldThrowFault_UserNotFound()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: ID de usuario inexistente.
             * ✔ Salida Esperada: FaultException "AUTH_USER_NOT_FOUND".
             */

            // Arrange
            _mockUserDao.Setup(d => d.GetUserByIdAsync(99)).ReturnsAsync((User)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.GetSocialMedia(99));

            Assert.Equal("AUTH_USER_NOT_FOUND", ex.Detail.ErrorCode);
        }

        // ==========================================
        // PRUEBAS: UpdateSocialMedia
        // ==========================================

        [Fact]
        public async Task UpdateSocialMedia_WhenNewRecord_ShouldAddAndSave()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Usuario válido, sin registro previo de redes.
             * ✔ Salida Esperada: Llamada a AddSocialMediaAsync y SaveChangesAsync.
             */

            // Arrange
            var user = new UserBuilder().WithId(1).Build();
            var dto = new SocialMediaDto { IdUser = 1, Twitter = "new_tw" };

            _mockUserDao.Setup(d => d.GetUserByIdAsync(1)).ReturnsAsync(user);
            _mockSocialDao.Setup(d => d.GetSocialMediaByUserIdAsync(1)).ReturnsAsync((SocialMedia)null); // No existe previo

            // Act
            var success = await _handler.UpdateSocialMedia(dto);

            // Assert
            Assert.True(success);
            _mockSocialDao.Verify(d => d.AddSocialMediaAsync(It.Is<SocialMedia>(s => s.twitter == "new_tw")), Times.Once);
            _mockSocialDao.Verify(d => d.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateSocialMedia_WhenExistingRecord_ShouldUpdateAndSave()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Usuario válido con registro previo.
             * ✔ Salida Esperada: Llamada a UpdateSocialMediaAsync y SaveChangesAsync.
             */

            // Arrange
            var user = new UserBuilder().WithId(1).Build();
            var existingSocial = new SocialMedia { id_user = 1, twitter = "old_tw" };
            var dto = new SocialMediaDto { IdUser = 1, Twitter = "updated_tw" };

            _mockUserDao.Setup(d => d.GetUserByIdAsync(1)).ReturnsAsync(user);
            _mockSocialDao.Setup(d => d.GetSocialMediaByUserIdAsync(1)).ReturnsAsync(existingSocial);

            // Act
            var success = await _handler.UpdateSocialMedia(dto);

            // Assert
            Assert.True(success);
            Assert.Equal("updated_tw", existingSocial.twitter); // Verificamos que se actualizó el objeto en memoria
            _mockSocialDao.Verify(d => d.UpdateSocialMediaAsync(existingSocial), Times.Once);
            _mockSocialDao.Verify(d => d.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateSocialMedia_WhenTwitterDuplicate_ShouldThrowFault_UserDuplicate()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Twitter handle ya usado por otro usuario.
             * ✔ Salida Esperada: FaultException "USER_DUPLICATE".
             */

            // Arrange
            var user = new UserBuilder().WithId(1).Build();
            var dto = new SocialMediaDto { IdUser = 1, Twitter = "taken_handle" };

            _mockUserDao.Setup(d => d.GetUserByIdAsync(1)).ReturnsAsync(user);
            // Simulamos que YA existe
            _mockSocialDao.Setup(d => d.ExistsTwitterUsernameExcludingUserAsync(1, "taken_handle"))
                          .ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.UpdateSocialMedia(dto));

            Assert.Equal("USER_DUPLICATE", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task UpdateSocialMedia_WhenDtoIsNull_ShouldThrowFault_BadRequest()
        {
            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.UpdateSocialMedia(null));

            Assert.Equal("GLOBAL_BAD_REQUEST", ex.Detail.ErrorCode);
        }
    }
}