using System.Collections.Generic;

namespace RssToEmail
{
    public class RssToEmailConfig
    {
        public List<RssSubscription> Subscriptions { get; set; } = new();
    }
}
