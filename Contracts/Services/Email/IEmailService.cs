using System.Threading.Tasks;

namespace Contracts.Services.Email
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
    }
}