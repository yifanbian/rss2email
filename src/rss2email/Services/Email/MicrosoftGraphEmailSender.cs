using Microsoft.Graph;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;

namespace RssToEmail
{
    public class MicrosoftGraphEmailSender : IEmailSender
    {
        private readonly IAuthenticationProvider _authenticationProvider;
        private readonly IGraphServiceClient _graphClient;
        private readonly Config _config;

        public MicrosoftGraphEmailSender(Config config)
        {
            _config = config;
            _authenticationProvider = new MicrosoftGraphAuthenticationProvider(
                config.TenantId,
                config.ClientId,
                config.ClientSecret);
            _graphClient = new GraphServiceClient(_authenticationProvider);
        }

        public Task SendAsync(MailMessage message)
        {
            var microsoftGraphMessage = new Message()
            {
                Subject = message.Subject,
                From = new() { EmailAddress = new() { Address = _config.From } },
                Sender = new() { EmailAddress = new() { Address = _config.From } },
                ToRecipients = message.To
                    .Select(x => new Recipient() { EmailAddress = new() { Address = x.Address } })
                    .ToArray(),
                Body = new()
                {
                    Content = message.Body,
                    ContentType = message.IsBodyHtml ? BodyType.Html : BodyType.Text,
                }
            };
            var user = string.IsNullOrEmpty(_config.From) ? _graphClient.Me : _graphClient.Users[_config.From];
            var request = user.SendMail(microsoftGraphMessage).Request();
            return request.PostAsync();
        }

        public class Config
        {
            public string TenantId { get; set; } = string.Empty;
            public string ClientId { get; set; } = string.Empty;
            public string ClientSecret { get; set; } = string.Empty;
            public string? From { get; set; }
        }
    }
}
