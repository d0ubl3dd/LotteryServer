using BusinessLogic.Exceptions;
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
        private readonly Random _random = new Random();

        private class VerificationEntry
        {
            public string Code { get; set; }
            public DateTime Expiration { get; set; }
        }

        private static readonly ConcurrentDictionary<string, VerificationEntry> _codes =
            new ConcurrentDictionary<string, VerificationEntry>();

        public VerificationHandler(IEmailService emailService)
        {
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        }

        public async Task<bool> SendVerificationCode(string email)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                if (string.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));

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
                return true;

            }, "SendVerificationCode");
        }

        public async Task<bool> VerifyCode(string email, string code)
        {
            return await ExecuteFaultSafeAsync(async () =>
            {
                if (string.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));
                if (string.IsNullOrEmpty(code)) throw new ArgumentNullException(nameof(code));

                if (_codes.TryGetValue(email, out var entry))
                {
                    if (DateTime.UtcNow <= entry.Expiration && entry.Code == code)
                    {
                        _codes.TryRemove(email, out _);
                        _logger.Info($"[VerifyCode] Verificación exitosa para {email}.");
                        return true;
                    }

                    _logger.Warn($"[VerifyCode] Código incorrecto o expirado para {email}.");

                    _codes.TryRemove(email, out _);
                }
                else
                {
                    _logger.Warn($"[VerifyCode] No se encontró solicitud de código para {email}.");
                }

                return false;

            }, "VerifyCode");
        }

        private async Task<T> ExecuteFaultSafeAsync<T>(Func<Task<T>> action, string operationName)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                HandleException(ex, operationName);
                return default;
            }
        }

        private void HandleException(Exception ex, string operationName)
        {
            if (ex is FaultException<ServiceFault>)
                throw ex;

            string errorCode;
            string clientMessage;

            switch (ex)
            {
                case EmailDeliveryException _:
                    errorCode = "VERIFY_EMAIL_SEND_FAILED";
                    clientMessage = "No pudimos enviar el correo de verificación. Por favor verifica tu dirección o intenta más tarde.";
                    _logger.Error($"[{operationName}] Error SMTP: {ex.InnerException?.Message ?? ex.Message}");
                    break;

                case VerificationException _:
                    errorCode = "VERIFY_ERROR";
                    clientMessage = "Error en el proceso de verificación.";
                    _logger.Warn($"[{operationName}] {ex.Message}");
                    break;

                case ArgumentNullException _:
                    errorCode = "VERIFY_BAD_REQUEST";
                    clientMessage = "Datos de verificación incompletos.";
                    _logger.Error($"[{operationName}] Argumento inválido: {ex.Message}");
                    break;

                default:
                    errorCode = "VERIFY_INTERNAL_ERROR";
                    clientMessage = "Error interno en el servicio de verificación.";
                    _logger.Fatal($"[{operationName}] Error inesperado: {ex}", ex);
                    break;
            }

            throw new FaultException<ServiceFault>(
                new ServiceFault
                {
                    ErrorCode = errorCode,
                    Message = clientMessage
                },
                new FaultReason(clientMessage)
            );
        }
    }
}