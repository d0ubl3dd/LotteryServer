using Contracts.Services.Users;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace BusinessLogic.Handlers
{
    public class VerificationHandler : IVerificationService
    {
        private class VerificationEntry
        {
            public string Code { get; set; }
            public DateTime Expiration { get; set; }
        }

        private static readonly Dictionary<string, VerificationEntry> _codes = new Dictionary<string, VerificationEntry>();
        private readonly Random _random = new Random();

        private readonly string _senderEmail = "coilvicapplication@gmail.com";
        private readonly string _senderPassword = "aorv zezj pazz cdqj";

        public async Task<bool> SendVerificationCode(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            string code = _random.Next(100000, 999999).ToString();
            DateTime expiration = DateTime.UtcNow.AddMinutes(5);

            if (_codes.ContainsKey(email))
                _codes[email] = new VerificationEntry { Code = code, Expiration = expiration };
            else
                _codes.Add(email, new VerificationEntry { Code = code, Expiration = expiration });

            try
            {
                using (var smtp = new SmtpClient("smtp.gmail.com"))
                {
                    smtp.Port = 587;
                    smtp.Credentials = new NetworkCredential(_senderEmail, _senderPassword);
                    smtp.EnableSsl = true;

                    var message = new MailMessage
                    {
                        From = new MailAddress(_senderEmail, "Lottery App"),
                        Subject = "Código de verificación",
                        Body = $"Tu código de verificación es: {code}\n\nEste código expira en 5 minutos.",
                        IsBodyHtml = false
                    };
                    message.To.Add(email);
                    await smtp.SendMailAsync(message);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        public Task<bool> VerifyCode(string email, string code)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(code))
                return Task.FromResult(false);

            if (_codes.TryGetValue(email, out var entry))
            {
                if (DateTime.UtcNow <= entry.Expiration && entry.Code == code)
                {
                    _codes.Remove(email);
                    return Task.FromResult(true);
                }                
                _codes.Remove(email);
            }
            return Task.FromResult(false);
        }
    }
}