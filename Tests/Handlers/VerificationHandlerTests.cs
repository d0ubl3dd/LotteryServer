using Xunit;
using Moq;
using System;
using System.Threading.Tasks;
using System.ServiceModel;
using BusinessLogic.Handlers;
using Contracts.Services.Email;
using Contracts.Faults;
using BusinessLogic.Exceptions;

namespace Tests.Handlers
{
    public class VerificationHandlerTests
    {
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly VerificationHandler _handler;

        public VerificationHandlerTests()
        {
            _mockEmailService = new Mock<IEmailService>();
            _handler = new VerificationHandler(_mockEmailService.Object);
        }

        // ==========================================
        // PRUEBAS: SendVerificationCode
        // ==========================================

        [Fact]
        public async Task SendCode_WhenEmailValid_ShouldStoreCodeAndSendEmail()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Email válido.
             * ✔ Salida Esperada: True, y llamada al servicio de email.
             */

            // Arrange
            string email = "test@example.com";

            // Act
            bool result = await _handler.SendVerificationCode(email);

            // Assert
            Assert.True(result);
            _mockEmailService.Verify(s => s.SendEmailAsync(email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task SendCode_WhenEmailServiceFails_ShouldThrowFault_EmailFailed()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Email válido, pero el servicio SMTP falla.
             * ✔ Salida Esperada: FaultException "VERIFY_EMAIL_SEND_FAILED".
             */

            // Arrange
            string email = "fail@example.com";
            _mockEmailService.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                             .ThrowsAsync(new Exception("SMTP Error"));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.SendVerificationCode(email));

            Assert.Equal("VERIFY_EMAIL_SEND_FAILED", ex.Detail.ErrorCode);
        }

        // ==========================================
        // PRUEBAS DE INTEGRACIÓN LÓGICA (Send -> Verify)
        // ==========================================

        [Fact]
        public async Task VerifyCode_WhenCodeIsCorrect_ShouldReturnTrue()
        {
            /* DOCUMENTACIÓN
             * ✔ Escenario: Flujo completo. Enviar código -> Capturarlo -> Verificarlo.
             * ✔ Truco: Usamos Callback de Moq para "robar" el código generado aleatoriamente.
             */

            // Arrange
            string email = Guid.NewGuid().ToString() + "@test.com"; // Email único para evitar colisiones estáticas
            string capturedCode = null;

            // Configurar el Mock para capturar el cuerpo del correo
            _mockEmailService.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string, string>((to, subject, body) =>
                {
                    // El body es: "Tu código de verificación es: 123456..."
                    // Extraemos los dígitos.
                    var parts = body.Split(new[] { ": " }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1)
                    {
                        capturedCode = parts[1].Substring(0, 6); // Asumiendo 6 dígitos
                    }
                })
                .Returns(Task.CompletedTask);

            // Paso 1: Enviar (Genera el código y lo guarda en el diccionario estático)
            await _handler.SendVerificationCode(email);

            Assert.NotNull(capturedCode); // Asegurar que capturamos algo

            // Act (Paso 2): Verificar con el código capturado
            bool isValid = await _handler.VerifyCode(email, capturedCode);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public async Task VerifyCode_WhenCodeIsWrong_ShouldReturnFalse()
        {
            /* DOCUMENTACIÓN
             * ✔ Escenario: Enviar código -> Intentar verificar con uno incorrecto.
             */

            // Arrange
            string email = Guid.NewGuid().ToString() + "@test.com";
            await _handler.SendVerificationCode(email); // Se genera un código X

            // Act
            bool isValid = await _handler.VerifyCode(email, "000000"); // Probamos con código incorrecto

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public async Task VerifyCode_WhenNoCodeRequested_ShouldReturnFalse()
        {
            /* DOCUMENTACIÓN
             * ✔ Escenario: Verificar un email que nunca pidió código.
             */

            // Act
            bool isValid = await _handler.VerifyCode("nobody@test.com", "123456");

            // Assert
            Assert.False(isValid);
        }

        // ==========================================
        // PRUEBAS DE VALIDACIÓN
        // ==========================================

        [Fact]
        public async Task SendCode_WhenEmailEmpty_ShouldThrowFault_BadRequest()
        {
            await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.SendVerificationCode(""));
        }

        [Fact]
        public async Task VerifyCode_WhenCodeEmpty_ShouldThrowFault_BadRequest()
        {
            await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.VerifyCode("a@a.com", ""));
        }
    }
}