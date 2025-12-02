using BusinessLogic.Exceptions;
using BusinessLogic.Logic;
using BusinessLogic.Logic.Base;
using Contracts.Services;
using Contracts.Services.Email;
using Contracts.Services.Users;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace BusinessLogic.Handlers
{
    public class VerificationHandler : BaseHandler, IVerificationService
    {
        private readonly IEmailService _emailService;
        private readonly Random _random = new Random();

        private class VerificationEntry
        {
            public string Code { get; set; }
            public DateTime Expiration { get; set; }
        }

        private static readonly ConcurrentDictionary<string, VerificationEntry> _codes =
            new ConcurrentDictionary<string, VerificationEntry>();

        public VerificationHandler(IEmailService emailService) : base(typeof(VerificationHandler))
        {
            if (emailService == null)
            {
                throw new ArgumentNullException(nameof(emailService));
            }
            _emailService = emailService;
        }

        public async Task<bool> SendVerificationCode(string email)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                bool success;

                if (string.IsNullOrEmpty(email))
                {
                    throw new ArgumentNullException(nameof(email));
                }

                string code = _random.Next(100000, 999999).ToString();
                DateTime expiration = DateTime.UtcNow.AddMinutes(5);

                _codes[email] = new VerificationEntry { Code = code, Expiration = expiration };

                string subject = "Código de verificación - Lottery Game";
                string body = $"Tu código de verificación es: {code}\n\nEste código expirará en 5 minutos.";

                try
                {
                    await _emailService.SendEmailAsync(email, subject, body);
                }
                catch (Exception ex)
                {
                    throw new EmailDeliveryException($"Falló el envío de correo a {email}", ex);
                }

                _logger.Info($"[SendVerificationCode] Código enviado a {email}.");
                success = true;

                return success;

            }, "SendVerificationCode");
        }

        public async Task<bool> VerifyCode(string email, string code)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                bool isValid = false;

                if (string.IsNullOrEmpty(email))
                {
                    throw new ArgumentNullException(nameof(email));
                }

                if (string.IsNullOrEmpty(code))
                {
                    throw new ArgumentNullException(nameof(code));
                }

                if (_codes.TryGetValue(email, out var entry))
                {
                    if (DateTime.UtcNow <= entry.Expiration && entry.Code == code)
                    {
                        _codes.TryRemove(email, out _);
                        _logger.Info($"[VerifyCode] Verificación exitosa para {email}.");
                        isValid = true;
                    }
                    else
                    {
                        _logger.Warn($"[VerifyCode] Código incorrecto o expirado para {email}.");
                        _codes.TryRemove(email, out _);
                    }
                }
                else
                {
                    _logger.Warn($"[VerifyCode] No se encontró solicitud de código para {email}.");
                }

                return isValid;

            }, "VerifyCode");
        }
    }
}