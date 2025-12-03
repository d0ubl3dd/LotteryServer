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

        private sealed class VerificationEntry
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
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentNullException(nameof(email));
            }

            return await ExecuteFaultSafeAsync(async () =>
            {
                bool success;

                string code = _random.Next(100000, 999999).ToString();
                DateTime expiration = DateTime.UtcNow.AddMinutes(5);

                _codes[email] = new VerificationEntry { Code = code, Expiration = expiration };

                string subject = "Código de verificación - Lottery Game";
                string body = string.Format("Tu código de verificación es: {0}\n\nEste código expirará en 5 minutos.", code);

                try
                {
                    await _emailService.SendEmailAsync(email, subject, body);
                }
                catch (Exception exception)
                {
                    throw new EmailDeliveryException(string.Format("Falló el envío de correo a {0}", email), exception);
                }

                _logger.InfoFormat("[SendVerificationCode] Código enviado a {0}.", email);
                success = true;

                return success;

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

            return await ExecuteFaultSafeAsync(async () =>
            {
                bool isValid = false;

                if (_codes.TryGetValue(email, out var entry))
                {
                    if (DateTime.UtcNow <= entry.Expiration && entry.Code == code)
                    {
                        _codes.TryRemove(email, out _);
                        _logger.InfoFormat("[VerifyCode] Verificación exitosa para {0}.", email);
                        isValid = true;
                    }
                    else
                    {
                        _logger.WarnFormat("[VerifyCode] Código incorrecto o expirado para {0}.", email);
                        _codes.TryRemove(email, out _);
                    }
                }
                else
                {
                    _logger.WarnFormat("[VerifyCode] No se encontró solicitud de código para {0}.", email);
                }

                await Task.CompletedTask;

                return isValid;

            }, "VerifyCode");
        }
    }
}