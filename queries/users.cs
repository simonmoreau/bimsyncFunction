
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Text;
using System.Net;
using System.Net.Http;

namespace bimsyncFunction
{
    public static class users
    {
        [FunctionName("create_user")]
        public static async Task<HttpResponseMessage> Create(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "users")]HttpRequestMessage req,
            ILogger log)
        {
            log.LogInformation("I am creating a new user");

            string name = "test";

            // Get request body
            dynamic data = await req.Content.ReadAsAsync<object>();
            name = name ?? data?.name;

            return name == null
                ? new HttpResponseMessage(HttpStatusCode.BadRequest) {Content = new StringContent("Please pass a name on the query string or in the request body", Encoding.UTF8, "application/json")}
                : new HttpResponseMessage(HttpStatusCode.OK) {Content = new StringContent(name, Encoding.UTF8, "application/json")};
        }

        [FunctionName("get_user")]
        public static async Task<HttpResponseMessage> Get(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "users/{id:int?}")]HttpRequestMessage req, int? id,
            ILogger log)
        {
            log.LogInformation("I am getting an existing user");

            if (id == null)
                log.LogInformation("The id is missing");
            else
                log.LogInformation("The id is" + id.ToString());

            string name = "test";

            // Get request body
            //dynamic data = await req.Content.ReadAsAsync<object>();
            //name = name ?? data?.name;

            return name == null
                ? new HttpResponseMessage(HttpStatusCode.BadRequest) {Content = new StringContent("Please pass a name on the query string or in the request body", Encoding.UTF8, "application/json")}
                : new HttpResponseMessage(HttpStatusCode.OK) {Content = new StringContent(name, Encoding.UTF8, "application/json")};
        }
    }
}
