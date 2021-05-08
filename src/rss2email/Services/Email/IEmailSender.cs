using System.Net.Mail;
using System.Threading.Tasks;

namespace RssToEmail
{
    public interface IEmailSender
    {
        public Task SendAsync(MailMessage message);
    }
}
