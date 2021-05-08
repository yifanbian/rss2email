using Microsoft.Extensions.Configuration;

namespace RssToEmail
{
    public class EmailConfig
    {
        public EmailSenderType Type { get; set; }
        public IConfigurationSection? Config { get; set; }

        public enum EmailSenderType
        {
            Smtp,
            MicrosoftGraph
        }
    }
}
