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
        private static readonly Dictionary<string, string> _codes = new Dictionary<string, string>();
        private readonly Random _random = new Random();
        
        private readonly string _senderEmail = "coilvicapplication@gmail.com";
        private readonly string _senderPassword = "aorv zezj pazz cdqj";

        public async Task<bool> SendVerificationCode(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            string code = _random.Next(100000, 999999).ToString();

            if (_codes.ContainsKey(email))
                _codes[email] = code;
            else
                _codes.Add(email, code);

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
                        Body = $"Tu código de verificación es: {code}",
                        IsBodyHtml = false
                    };

                    message.To.Add(email);

                    await smtp.SendMailAsync(message);
                }

                Console.WriteLine($"Código {code} enviado a {email}");
                return true;
            }
            catch (SmtpException smtpEx)
            {
                Console.WriteLine($"Error SMTP enviando código a {email}: {smtpEx.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inesperado enviando código a {email}: {ex.Message}");
                return false;
            }
        }
        public Task<bool> VerifyCode(string email, string code)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(code))
                return Task.FromResult(false);

            if (_codes.ContainsKey(email) && _codes[email] == code)
            {
                _codes.Remove(email);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
    }
}