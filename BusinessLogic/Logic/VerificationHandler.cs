using BusinessLogic.Exceptions;
using BusinessLogic.Logic.Base;
using Contracts.Services.Email;
using Contracts.Services.Users;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace BusinessLogic.Handlers
{
    public class VerificationHandler : BaseHandler, IVerificationService
    {
        private const int ExpirationMinutes = 5;
        private const string EmailSubject = "Código de verificación - Lottery Game";

        private readonly IEmailService _emailService;
        private readonly Random _random = new Random();

        private sealed class VerificationEntry
        {
            public string Code { get; set; }
            public DateTime Expiration { get; set; }
            public bool IsUsed { get; set; }
        }

        private static readonly ConcurrentDictionary<string, VerificationEntry> _codes =
            new ConcurrentDictionary<string, VerificationEntry>();

        public VerificationHandler(IEmailService emailService) : base(typeof(VerificationHandler))
        {
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        }

        public async Task<bool> SendVerificationCode(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentNullException(nameof(email));
            }

            return await ExecuteFaultSafeAsync(async () =>
            {
                string code = _random.Next(100000, 999999).ToString();
                DateTime expiration = DateTime.UtcNow.AddMinutes(ExpirationMinutes);

                _codes[email] = new VerificationEntry
                {
                    Code = code,
                    Expiration = expiration,
                    IsUsed = false
                };

                string body = $"Tu código de verificación es: {code}\n\nEste código expirará en {ExpirationMinutes} minutos.";

                try
                {
                    await _emailService.SendEmailAsync(email, EmailSubject, body);
                }
                catch (Exception exception)
                {
                    throw new EmailDeliveryException($"Falló el envío de correo a {email}", exception);
                }

                _logger.InfoFormat("[SendVerificationCode] Código enviado a {0}.", email);

                return true;

            }, "SendVerificationCode");
        }

        public async Task<bool> VerifyCode(string email, string code)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentNullException(nameof(email));
            }

            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentNullException(nameof(code));
            }

            return await ExecuteFaultSafeAsync(() =>
            {
                if (!_codes.TryGetValue(email, out var entry))
                {
                    _logger.WarnFormat("[VerifyCode] No se encontró código para {0}.", email);
                    return Task.FromResult(false);
                }

                bool isValid = !entry.IsUsed && DateTime.UtcNow <= entry.Expiration && entry.Code == code;

                if (isValid)
                {
                    _logger.InfoFormat("[VerifyCode] Código válido para {0}.", email);
                    return Task.FromResult(true);
                }

                _logger.WarnFormat("[VerifyCode] Código inválido, usado o expirado para {0}.", email);

                if (DateTime.UtcNow > entry.Expiration)
                {
                    _codes.TryRemove(email, out _);
                }

                return Task.FromResult(false);

            }, "VerifyCode");
        }

        public async Task<bool> ConsumeVerificationCode(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentNullException(nameof(email));
            }

            return await ExecuteFaultSafeAsync(() =>
            {
                if (_codes.TryGetValue(email, out var entry) &&
                    !entry.IsUsed &&
                    DateTime.UtcNow <= entry.Expiration)
                {
                    entry.IsUsed = true;
                    _codes.TryRemove(email, out _);

                    _logger.InfoFormat("[ConsumeVerificationCode] Código consumido para {0}.", email);
                    return Task.FromResult(true);
                }

                return Task.FromResult(false);

            }, "ConsumeVerificationCode");
        }
    }
}