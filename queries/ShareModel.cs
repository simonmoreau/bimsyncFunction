
using System.IO;
using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace bimsyncFunction
{
    public static class ShareModel
    {
        [FunctionName("ShareModel")]
        public static void Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req,
            [CosmosDB("bimsyncManagerdb", "bimsyncManagerCollection", ConnectionStringSetting = "myDBConnectionString")]out dynamic document,
            TraceWriter log)
        {
            string name = req.Query["name"];

            string requestBody = new StreamReader(req.Body).ReadToEnd();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            document = new { Description = name, id = Guid.NewGuid(), Constante = "test" };

            log.Info($"C# Queue trigger function inserted one row");
            log.Info($"Description={name}");
        }
    }
}
