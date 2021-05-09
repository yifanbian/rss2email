using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace RssToEmail
{
    public static class Triggers
    {
        [FunctionName(nameof(HttpTrigger))]
        public static async Task<IActionResult> HttpTrigger(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req,
            [DurableClient] IDurableClient durableClient,
            ILogger log)
        {
            string instanceId = await durableClient.StartNewAsync("RssToEmail", null);
            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
            return durableClient.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName(nameof(TimeTrigger))]
        public static async Task TimeTrigger(
            [TimerTrigger("0 */10 * * * *", RunOnStartup = true)] TimerInfo timer,
            [DurableClient] IDurableClient durableClient,
            ILogger log)
        {
            string instanceId = await durableClient.StartNewAsync("RssToEmail", null);
            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        }
    }
}
