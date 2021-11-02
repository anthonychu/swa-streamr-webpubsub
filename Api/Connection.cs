using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.WebPubSub;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Api
{
    public class Connection
    {
        [Function("Connection")]
        public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger<Connection>();
            logger.LogInformation("C# HTTP trigger function processed a request.");

            var userId = StaticWebAppsAuth.ParseUserInfo(req)?.Identity.Name;
            var canSend = userId != null;

            var connectionString = Environment.GetEnvironmentVariable("WebPubSubConnectionString");
            var client = new WebPubSubServiceClient(connectionString, "streamr");

            var roles = new List<string> { "webpubsub.joinLeaveGroup.streamr" };
            if (canSend)
            {
                roles.Add("webpubsub.sendToGroup.streamr");
            }

            var uri = await client.GenerateClientAccessUriAsync(TimeSpan.FromHours(1), userId, roles);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            response.WriteString(JsonSerializer.Serialize(new ConnectionResult
            {
                Uri = uri.AbsoluteUri,
                CanSend = canSend
            }));

            return response;
        }

        public class ConnectionResult
        {
            public string Uri { get; set; }
            public bool CanSend { get; set; }
        }
    }
}
