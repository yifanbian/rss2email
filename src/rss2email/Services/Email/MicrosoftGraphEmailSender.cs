using Microsoft.Graph;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;

namespace RssToEmail
{
    public class MicrosoftGraphEmailSender : IEmailSender
    {
        private readonly IGraphServiceClient graphClient;
        private readonly Config config;

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
        public MicrosoftGraphEmailSender(Config config)
        {
            this.config = config;
            graphClient = new GraphServiceClient(new MicrosoftGraphAuthenticationProvider());
        }

        public Task SendAsync(MailMessage message)
        {
            var microsoftGraphMessage = new Message()
            {
                Subject = message.Subject,
                From = new() { EmailAddress = new() { Address = config.From } },
                ToRecipients = message.To
                    .Select(x => new Recipient() { EmailAddress = new() { Address = x.Address } })
                    .ToArray(),
                Body = new()
                {
                    Content = message.Body,
                    ContentType = message.IsBodyHtml ? BodyType.Html : BodyType.Text,
                }
            };
            var user = string.IsNullOrEmpty(config.From) ? graphClient.Me : graphClient.Users[config.From];
            var request = user.SendMail(microsoftGraphMessage).Request();
            return request.PostAsync();
        }

        public class Config
        {
            public string? From { get; set; }
        }
    }
}
