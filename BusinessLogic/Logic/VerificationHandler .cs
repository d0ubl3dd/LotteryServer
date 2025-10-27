using Contracts.Services.Users;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Logic
{
    internal class VerificationHandler : IVerificationService
    {
        private static readonly ConcurrentDictionary<string, (string Code, DateTime Expiration)> _codes =
            new ConcurrentDictionary<string, (string, DateTime)>();

        public async Task<bool> SendVerificationCode(string email)
        {
            string code = new Random().Next(100000, 999999).ToString();
            _codes[email] = (code, DateTime.UtcNow.AddMinutes(10));

            try
            {
                using (var client = new SmtpClient("smtp.gmail.com", 587))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential("lottery.game@gmail.com", "token");

                    var mail = new MailMessage("lottery.game@gmail.com", email)
                    {
                        Subject = "Código de verificación",
                        Body = $"Tu código de verificación es: {code}",
                        IsBodyHtml = false
                    };
                    await client.SendMailAsync(mail);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error enviando correo: {ex.Message}");
                return false;
            }
        }

        public Task<bool> VerifyCode(string email, string code)
        {
            if (_codes.TryGetValue(email, out var stored))
            {
                if (stored.Code == code && stored.Expiration > DateTime.UtcNow)
                {
                    _codes.TryRemove(email, out _);
                    return Task.FromResult(true);
                }
            }
            return Task.FromResult(false);
        }        
    }
}
