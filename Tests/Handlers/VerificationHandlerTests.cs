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

        [Fact]
        public async Task SendCode_WhenEmailValid_ShouldStoreCodeAndSendEmail()
        {
            string email = "test@example.com";

            bool result = await _handler.SendVerificationCode(email);

            Assert.True(result);
            _mockEmailService.Verify(s => s.SendEmailAsync(email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task SendCode_WhenEmailServiceFails_ShouldThrowFault_EmailFailed()
        {
            string email = "fail@example.com";
            _mockEmailService.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                             .ThrowsAsync(new Exception("SMTP Error"));

            var ex = await Assert.ThrowsAsync<FaultException<ServiceFault>>(() =>
                _handler.SendVerificationCode(email));

            Assert.Equal("VERIFY_EMAIL_SEND_FAILED", ex.Detail.ErrorCode);
        }

        [Fact]
        public async Task VerifyCode_WhenCodeIsCorrect_ShouldReturnTrue()
        {
            string email = Guid.NewGuid().ToString() + "@test.com";
            string capturedCode = null;

            _mockEmailService.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string, string>((to, subject, body) =>
                {
                    var parts = body.Split(new[] { ": " }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1)
                    {
                        capturedCode = parts[1].Substring(0, 6);
                    }
                })
                .Returns(Task.CompletedTask);

            await _handler.SendVerificationCode(email);

            Assert.NotNull(capturedCode);

            bool isValid = await _handler.VerifyCode(email, capturedCode);

            Assert.True(isValid);
        }

        [Fact]
        public async Task VerifyCode_WhenCodeIsWrong_ShouldReturnFalse()
        {
            string email = Guid.NewGuid().ToString() + "@test.com";
            await _handler.SendVerificationCode(email);

            bool isValid = await _handler.VerifyCode(email, "000000");

            Assert.False(isValid);
        }

        [Fact]
        public async Task VerifyCode_WhenNoCodeRequested_ShouldReturnFalse()
        {
            bool isValid = await _handler.VerifyCode("nobody@test.com", "123456");

            Assert.False(isValid);
        }

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