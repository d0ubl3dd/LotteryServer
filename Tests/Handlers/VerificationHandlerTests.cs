using Xunit;
using Moq;
using System;
using System.Threading.Tasks;
using System.ServiceModel;
using BusinessLogic.Handlers;
using BusinessLogic.Exceptions;
using Contracts.Services.Email;
using Contracts.Faults;

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
        public void Constructor_WhenEmailServiceIsNull_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new VerificationHandler(null));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task SendVerificationCode_WhenEmailIsInvalid_ShouldThrowArgumentNullException(string email)
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.SendVerificationCode(email));
        }

        [Theory]
        [InlineData("test1@example.com")]
        [InlineData("user.name@domain.co")]
        [InlineData("admin@localhost")]
        public async Task SendVerificationCode_WhenValid_ShouldSendEmailAndReturnTrue(string email)
        {
            _mockEmailService.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                             .Returns(Task.CompletedTask);

            bool result = await _handler.SendVerificationCode(email);

            Assert.True(result);
            _mockEmailService.Verify(s => s.SendEmailAsync(email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Theory]
        [InlineData(null, "123456")]
        [InlineData("", "123456")]
        public async Task VerifyCode_WhenEmailInvalid_ShouldThrowArgumentNullException(string email, string code)
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.VerifyCode(email, code));
        }

        [Theory]
        [InlineData("test@test.com", null)]
        [InlineData("test@test.com", "")]
        public async Task VerifyCode_WhenCodeInvalid_ShouldThrowArgumentNullException(string email, string code)
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.VerifyCode(email, code));
        }

        [Fact]
        public async Task VerifyCode_WhenCodeNeverSent_ShouldReturnFalse()
        {
            bool result = await _handler.VerifyCode("unknown@test.com", "123456");
            Assert.False(result);
        }

        [Theory]
        [InlineData("111111")]
        [InlineData("000000")]
        [InlineData("abcdef")]
        public async Task VerifyCode_WhenCodeSentButDoesNotMatch_ShouldReturnFalse(string wrongCode)
        {
            string email = Guid.NewGuid().ToString() + "@test.com";

            await _handler.SendVerificationCode(email);

            bool result = await _handler.VerifyCode(email, wrongCode);

            Assert.False(result);
        }

        [Fact]
        public async Task VerifyCode_WhenCodeIsCorrect_ShouldReturnTrue()
        {
            string email = Guid.NewGuid().ToString() + "@test.com";
            string capturedCode = null;

            _mockEmailService.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string, string>((e, s, body) =>
                {
                    var split = body.Split(new[] { ": " }, StringSplitOptions.None);
                    if (split.Length > 1)
                    {
                        capturedCode = split[1].Substring(0, 6);
                    }
                })
                .Returns(Task.CompletedTask);

            await _handler.SendVerificationCode(email);

            Assert.NotNull(capturedCode);

            bool result = await _handler.VerifyCode(email, capturedCode);

            Assert.True(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task ConsumeVerificationCode_WhenEmailInvalid_ShouldThrowArgumentNullException(string email)
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.ConsumeVerificationCode(email));
        }

        [Fact]
        public async Task ConsumeVerificationCode_WhenNotSent_ShouldReturnFalse()
        {
            bool result = await _handler.ConsumeVerificationCode("never@sent.com");
            Assert.False(result);
        }

        [Fact]
        public async Task ConsumeVerificationCode_WhenSentButNotVerified_ShouldReturnTrueAndRemove()
        {
            string email = Guid.NewGuid().ToString() + "@consume.com";

            await _handler.SendVerificationCode(email);

            bool result = await _handler.ConsumeVerificationCode(email);

            Assert.True(result);

            bool secondTry = await _handler.ConsumeVerificationCode(email);
            Assert.False(secondTry);
        }

        [Fact]
        public async Task FullFlow_SendVerifyConsume_ShouldWorkCorrectly()
        {
            string email = Guid.NewGuid().ToString() + "@flow.com";
            string capturedCode = null;

            _mockEmailService.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string, string>((e, s, body) =>
                {
                    var split = body.Split(new[] { ": " }, StringSplitOptions.None);
                    capturedCode = split[1].Substring(0, 6);
                })
                .Returns(Task.CompletedTask);

            await _handler.SendVerificationCode(email);

            bool verifyResult = await _handler.VerifyCode(email, capturedCode);
            Assert.True(verifyResult);

            bool consumeResult = await _handler.ConsumeVerificationCode(email);
            Assert.True(consumeResult);

            bool verifyAgain = await _handler.VerifyCode(email, capturedCode);
            Assert.False(verifyAgain);
        }

        [Fact]
        public async Task ConsumeVerificationCode_WhenAlreadyConsumed_ShouldReturnFalse()
        {
            string email = Guid.NewGuid().ToString() + "@double.com";
            await _handler.SendVerificationCode(email);

            await _handler.ConsumeVerificationCode(email);
            bool result = await _handler.ConsumeVerificationCode(email);

            Assert.False(result);
        }
    }
}