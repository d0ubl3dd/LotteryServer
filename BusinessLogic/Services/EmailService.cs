using Contracts.Services;
using Contracts.Services.Email;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace BusinessLogic.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _senderEmail = "coilvicapplication@gmail.com";
        private readonly string _senderPassword = "aorv zezj pazz cdqj";
        private readonly string _smtpHost = "smtp.gmail.com";
        private readonly int _smtpPort = 587;

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            using (var smtp = new SmtpClient(_smtpHost))
            {
                smtp.Port = _smtpPort;
                smtp.Credentials = new NetworkCredential(_senderEmail, _senderPassword);
                smtp.EnableSsl = true;

                var message = new MailMessage
                {
                    From = new MailAddress(_senderEmail, "Lottery App"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false
                };

                message.To.Add(to);

                await smtp.SendMailAsync(message);
            }
        }
    }
}