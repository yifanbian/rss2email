using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RssToEmail
{
    public class RssToEmailFunctions
    {
        private readonly EmailGenerator _emailGenerator;
        private readonly IEmailSender _emailSender;
        private readonly IOptions<RssToEmailConfig> _rssToEmailConfig;
        private readonly ILogger<RssToEmailFunctions> _logger;

        public RssToEmailFunctions(
            EmailGenerator emailGenerator,
            IEmailSender emailSender,
            IOptions<RssToEmailConfig> rssToEmailConfig,
            ILogger<RssToEmailFunctions> logger)
        {
            _emailGenerator = emailGenerator;
            _emailSender = emailSender;
            _rssToEmailConfig = rssToEmailConfig;
            _logger = logger;
        }

        [FunctionName("RssToEmail")]
        public async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            [DurableClient] IDurableEntityClient entityClient)
        {
            var subscriptions = _rssToEmailConfig.Value.Subscriptions;
            var tasks = subscriptions
                .Select(subscription => context.CallSubOrchestratorAsync(
                    nameof(CheckSubscriptionUpdate),
                    subscription))
                .ToArray();
            await Task.WhenAll(tasks);
        }

        [FunctionName(nameof(CheckSubscriptionUpdate))]
        public async Task CheckSubscriptionUpdate(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var rssSubscription = context.GetInput<RssSubscription>();
            _logger.LogInformation(
                "Starting to process RSS subscription '{SubscriptionName}' with {FeedCount} feeds",
                rssSubscription.Name,
                rssSubscription.Feeds.Count);
            var tasks = rssSubscription.Feeds
                .Select(feed => context.CallActivityAsync(
                    nameof(CheckFeedUpdate),
                    (rssSubscription, feed)))
                .ToArray();
            await Task.WhenAll(tasks);
            _logger.LogInformation(
                "Finished processing RSS subscription '{SubscriptionName}'",
                rssSubscription.Name);
        }

        [FunctionName(nameof(CheckFeedUpdate))]
        public async Task CheckFeedUpdate(
            [ActivityTrigger] IDurableActivityContext context,
            [DurableClient] IDurableEntityClient entityClient)
        {
            var (subscription, feed) = context.GetInput<(RssSubscription, RssFeed)>();
            _logger.LogInformation(
                "Starting to process RSS feed '{FeedName}' from subscription '{SubscriptionName}'",
                feed.Name,
                subscription.Name);
            using XmlReader reader = XmlReader.Create(feed.FeedUrl);
            SyndicationFeed rssFeed = SyndicationFeed.Load(reader);

            EntityId feedStateEntityId = new(nameof(RssFeedState), $"{subscription.Name}|{feed.Name}");
            var previousFeedStates = await entityClient.ReadEntityStateAsync<List<string>>(feedStateEntityId);
            var feedStates = rssFeed.Items.ToDictionary(item =>
            {
                return $"{item.Id}|{item.Title}|{item.PublishDate}|{item.Content}|{item.Links.FirstOrDefault()?.GetAbsoluteUri()}".GetBase64EncodedSha1();
            });

            var newArticles = feedStates.Keys
                .Except(previousFeedStates.EntityExists ? previousFeedStates.EntityState : new List<string>())
                .Select(hash => feedStates[hash])
                .OrderByDescending(item => item.PublishDate)
                .ToList();

            var emails = _emailGenerator.CreateEmail(feed, newArticles);
            foreach (var email in emails)
            {
                email.To.Add(subscription.Recipient);
                await _emailSender.SendAsync(email);
            }

            await entityClient.SignalEntityAsync(feedStateEntityId, "Set", feedStates.Keys);

            _logger.LogInformation(
                "Finished processing RSS feed '{FeedName}' from subscription '{SubscriptionName}'",
                feed.Name,
                subscription.Name);
        }

        [FunctionName(nameof(PurgeState))]
        public async Task<IActionResult> PurgeState(
            [HttpTrigger(AuthorizationLevel.Admin, "get")] HttpRequest req,
            [DurableClient] IDurableClient durableClient)
        {
            var subscriptions = req.Query.TryGetValue("subscription[]", out var limitedSubscription) ?
                _rssToEmailConfig.Value.Subscriptions.Where(x => limitedSubscription.Contains(x.Name)).ToList() :
                _rssToEmailConfig.Value.Subscriptions;
            foreach (var subscription in subscriptions)
            {
                var feeds = req.Query.TryGetValue($"feed[{subscription.Name}]", out var limitedFeeds) ?
                    subscription.Feeds.Where(x => limitedFeeds.Contains(x.Name)).ToList() :
                    subscription.Feeds;
                foreach (var feed in feeds)
                {
                    await durableClient.SignalEntityAsync(
                        new EntityId(nameof(RssFeedState), $"{subscription.Name}|{feed.Name}"),
                        "clean");
                    _logger.LogInformation(
                        "Purged cache for Subscription '{SubscriptionName}' > Feed '{FeedName}'",
                        subscription.Name,
                        feed.Name);
                }
            }
            return new NoContentResult();
        }

        [FunctionName(nameof(RssFeedState))]
        [SuppressMessage("Entity", "DF0305")]
        public void RssFeedState([EntityTrigger] IDurableEntityContext ctx)
        {
            switch (ctx.OperationName.ToLowerInvariant())
            {
                case "get":
                    ctx.Return(ctx.GetState<List<string>>(() => new()));
                    break;
                case "set":
                    ctx.SetState(ctx.GetInput<List<string>>());
                    break;
                case "clean":
                    ctx.DeleteState();
                    break;
            }
        }
    }
}
