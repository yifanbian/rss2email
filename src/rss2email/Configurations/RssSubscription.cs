using System.Collections.Generic;

namespace RssToEmail
{
    public class RssSubscription
    {
        public string Name { get; set; } = string.Empty;
        public string Recipient { get; set; } = string.Empty;
        public List<RssFeed> Feeds { get; set; } = new();
    }
}
