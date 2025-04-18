using System.Threading.Tasks;

namespace SecondHandPlatform.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
    }
}