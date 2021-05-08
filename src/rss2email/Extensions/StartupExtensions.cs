using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace RssToEmail
{
    public static class StartupExtensions
    {
        public static void AddEmailServices(this IServiceCollection services)
        {
            services.AddOptions<EmailConfig>()
                .Configure<IConfiguration>((options, configuration) =>
                {
                    configuration.GetSection("Email").Bind(options);
                });
            services.AddSingleton<IEmailSender>(s =>
            {
                var config = s.GetRequiredService<IOptions<EmailConfig>>().Value;
                if (config.Config is null)
                {
                    throw new ArgumentNullException("Email:Config not set");
                }

                switch (config.Type)
                {
                    case EmailConfig.EmailSenderType.Smtp:
                        return new SmtpEmailSender(config.Config.Get<SmtpEmailSender.Config>());
                    case EmailConfig.EmailSenderType.MicrosoftGraph:
                        return new MicrosoftGraphEmailSender(config.Config.Get<MicrosoftGraphEmailSender.Config>());
                    default:
                        throw new ArgumentException($"Unknown Email Sender Type: {config.Type}");
                }
            });
        }
    }
}
