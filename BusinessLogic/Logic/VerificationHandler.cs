using Contracts.Faults;
using Contracts.Services.Users;
using log4net;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;
using System.ServiceModel;
using System.Threading.Tasks;

namespace BusinessLogic.Handlers
{
    public class VerificationHandler : IVerificationService
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(VerificationHandler));

        private class VerificationEntry
        {
            public string Code { get; set; }
            public DateTime Expiration { get; set; }
        }

        private static readonly ConcurrentDictionary<string, VerificationEntry> _codes =
            new ConcurrentDictionary<string, VerificationEntry>();

        private readonly Random _random = new Random();

        private readonly string _senderEmail = "coilvicapplication@gmail.com";
        private readonly string _senderPassword = "aorv zezj pazz cdqj";

        public async Task<bool> SendVerificationCode(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                var reason = "Intento de envío de código fallido: email vacío.";
                _logger.Warn(reason);
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = reason },
                    new FaultReason(reason)
                );
            }

            try
            {
                string code = _random.Next(100000, 999999).ToString();
                DateTime expiration = DateTime.UtcNow.AddMinutes(5);

                _codes[email] = new VerificationEntry { Code = code, Expiration = expiration };

                using (var smtp = new SmtpClient("smtp.gmail.com"))
                {
                    smtp.Port = 587;
                    smtp.Credentials = new NetworkCredential(_senderEmail, _senderPassword);
                    smtp.EnableSsl = true;

                    var message = new MailMessage
                    {
                        From = new MailAddress(_senderEmail, "Lottery App"),
                        Subject = "Código de verificación",
                        Body = $"Tu código de verificación es: {code}\n\nExpira en 5 minutos.",
                        IsBodyHtml = false
                    };

                    message.To.Add(email);

                    await smtp.SendMailAsync(message);
                }

                _logger.Info($"Código de verificación enviado correctamente a {email}.");
                return true;
            }
            catch (SmtpException ex)
            {
                var reason = $"No se pudo enviar el código de verificación al email {email}: {ex.Message}";
                _logger.Warn(reason, ex);
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = reason },
                    new FaultReason(reason)
                );
            }
            catch (Exception ex)
            {
                var fatalReason = "Error interno enviando código de verificación.";
                _logger.Error(fatalReason, ex);
                throw new FaultException<ServiceFault>(
                    new ServiceFault { Message = fatalReason },
                    new FaultReason(fatalReason)
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
                    new ServiceFault { Message = reason },
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