using Contracts.Faults;
using Contracts.Services;
using Contracts.Services.Email;
using Contracts.Services.Users;
using log4net;
using System;
using System.Collections.Concurrent;
using System.ServiceModel;
using System.Threading.Tasks;

namespace BusinessLogic.Handlers
{
    public class VerificationHandler : IVerificationService
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(VerificationHandler));

        private readonly IEmailService _emailService;

        private class VerificationEntry
        {
            public string Code { get; set; }
            public DateTime Expiration { get; set; }
        }

        private static readonly ConcurrentDictionary<string, VerificationEntry> _codes =
            new ConcurrentDictionary<string, VerificationEntry>();

        private readonly Random _random = new Random();

        public VerificationHandler(IEmailService emailService)
        {
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        }

        public async Task<bool> SendVerificationCode(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                var reason = "Intento de envío de código fallido: email vacío.";
                _logger.Warn(reason);
                throw new FaultException<ServiceFault>(
                    new ServiceFault
                    {
                        Message = reason
                    },
                    new FaultReason(reason)
                );
            }

            try
            {
                string code = _random.Next(100000, 999999).ToString();
                DateTime expiration = DateTime.UtcNow.AddMinutes(5);

                _codes[email] = new VerificationEntry { Code = code, Expiration = expiration };

                string subject = "Código de verificación";
                string body = $"Tu código de verificación es: {code}\n\nExpira en 5 minutos.";

                await _emailService.SendEmailAsync(email, subject, body);

                _logger.Info($"Código de verificación enviado correctamente a {email}.");
                return true;
            }
            catch (Exception ex)
            {
                var reason = $"No se pudo enviar el código de verificación al email {email}: {ex.Message}";
                _logger.Error(reason, ex);

                throw new FaultException<ServiceFault>(
                    new ServiceFault
                    {
                        Message = "Ocurrió un error al enviar el correo de verificación."
                    },
                    new FaultReason(reason)
                );
            }
        }

        public Task<bool> VerifyCode(string email, string code)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(code))
            {
                var reason = "Intento de verificación inválido: email o código vacío.";
                _logger.Warn(reason);
                throw new FaultException<ServiceFault>(
                    new ServiceFault
                    {
                        Message = reason
                    },
                    new FaultReason(reason)
                );
            }

            if (_codes.TryGetValue(email, out var entry))
            {
                if (DateTime.UtcNow <= entry.Expiration && entry.Code == code)
                {
                    _codes.TryRemove(email, out _);
                    _logger.Info($"Código de verificación correcto para {email}.");
                    return Task.FromResult(true);
                }

                _logger.Info($"Código inválido o expirado para {email}.");
                _codes.TryRemove(email, out _);
            }

            return Task.FromResult(false);
        }
    }
}