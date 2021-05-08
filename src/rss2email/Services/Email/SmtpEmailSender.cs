using MailKit.Net.Smtp;
using MimeKit;
using System.Linq;
using System.Threading.Tasks;

namespace RssToEmail
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly Config config;

        public SmtpEmailSender(Config config)
        {
            this.config = config;
        }

        public async Task SendAsync(System.Net.Mail.MailMessage message)
        {
            using var smtpClient = new SmtpClient();
            await smtpClient.ConnectAsync(config.Host, config.Port, config.EnableSsl);
            if (!string.IsNullOrEmpty(config.Username) || !string.IsNullOrEmpty(config.Password))
                await smtpClient.AuthenticateAsync(config.Username, config.Password);
            var mimeMessage = new MimeMessage()
            {
                Subject = message.Subject,
                Body = new TextPart(message.IsBodyHtml ? MimeKit.Text.TextFormat.Html : MimeKit.Text.TextFormat.Text)
                {
                    Text = message.Body
                },
            };
            mimeMessage.From.Add(MailboxAddress.Parse(config.From));
            mimeMessage.To.AddRange(message.To.Select(x => MailboxAddress.Parse(x.Address)));
            await smtpClient.SendAsync(mimeMessage);
        }

        public class Config
        {
            public string Host { get; set; } = string.Empty;
            public int Port { get; set; } = 465;
            public bool EnableSsl { get; set; } = true;
            public string From { get; set; } = string.Empty;
            public string? Username { get; set; }
            public string? Password { get; set; }
        }
    }
}
