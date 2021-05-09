using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.ServiceModel.Syndication;
using System.Text;

namespace RssToEmail
{
    public class EmailGenerator
    {
        public IReadOnlyList<MailMessage> CreateEmail(RssFeed feed, List<SyndicationItem> feedItems)
        {
            if (feedItems.Count > 3)
            {
                return new List<MailMessage>() { CreateBatchEmail(feed, feedItems) };
            }
            else
            {
                return feedItems.Select(item => CreateSingleEmail(feed, item)).ToList();
            }
        }

        public MailMessage CreateSingleEmail(RssFeed feed, SyndicationItem feedItem)
        {
            return new MailMessage()
            {
                SubjectEncoding = Encoding.UTF8,
                BodyEncoding = Encoding.UTF8,
                Subject = $"[RssToEmail] Update in {feed.Name}: {feedItem.Title.Text}",
                IsBodyHtml = true,
                Body = RenderSingleItemEmail(feedItem)
            };
        }

        public MailMessage CreateBatchEmail(RssFeed feed, List<SyndicationItem> feedItems)
        {
            return new MailMessage()
            {
                SubjectEncoding = Encoding.UTF8,
                BodyEncoding = Encoding.UTF8,
                Subject = $"[RssToEmail] Digest of {feed.Name}: {feedItems.FirstOrDefault()?.Title?.Text}" +
                    (feedItems.Count > 1 ? $" and {feedItems.Count - 1} others" : ""),
                IsBodyHtml = true,
                Body = string.Join("\n<hr/>\n", feedItems.Select(RenderSingleItemEmail))
            };
        }

        internal string RenderSingleItemEmail(SyndicationItem item)
        {
            StringBuilder builder = new();
            builder.AppendLine($"<h1>{item.Title.Text}</h1>");
            builder.AppendLine($"<p>Publish Date: {item.PublishDate.UtcDateTime}</p>");
            builder.AppendLine("<p>URL:");
            foreach (var link in item.Links)
                builder.AppendLine($"<a href=\"{link.GetAbsoluteUri()}\">{link.GetAbsoluteUri()}</a>");
            builder.AppendLine("</p>");
            if (item.Summary is not null)
                builder.AppendLine($"<p>Summary: {item.Summary.Text}</p>");
            builder.AppendLine($"<div>{item.Content}</div>");
            return builder.ToString();
        }
    }
}
