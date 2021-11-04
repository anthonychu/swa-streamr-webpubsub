using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Azure.Messaging.WebPubSub;
using System.Collections.Generic;

namespace Api
{
    public static class Connection
    {
        [FunctionName("Connection")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            var userId = StaticWebAppsAuth.Parse(req)?.Identity?.Name;
            var canSend = userId != null;

            var connectionString = Environment.GetEnvironmentVariable("WebPubSubConnectionString");
            var client = new WebPubSubServiceClient(connectionString, "streamr");

            var roles = new List<string> { "webpubsub.joinLeaveGroup.streamr" };
            if (canSend)
            {
                roles.Add("webpubsub.sendToGroup.streamr");
            }

            var uri = await client.GenerateClientAccessUriAsync(TimeSpan.FromHours(1), userId, roles);

            return new OkObjectResult(new ConnectionResult
            {
                Uri = uri.AbsoluteUri,
                CanSend = canSend
            });
        }

        public class ConnectionResult
        {
            public string Uri { get; set; }
            public bool CanSend { get; set; }
        }
    }
}
