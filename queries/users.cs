
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace bimsyncFunction
{
    public static class users
    {
        [FunctionName("create_user")]
        public static async Task<HttpResponseMessage> Create(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "users")]HttpRequestMessage req,
            [CosmosDB(databaseName: "bimsyncManagerdb", collectionName: "bimsyncManagerCollection", ConnectionStringSetting = "myDBConnectionString")]IAsyncCollector<User> usersOut,
            //[CosmosDB(databaseName: "bimsyncManagerdb", collectionName: "bimsyncManagerCollection", ConnectionStringSetting = "myDBConnectionString")]DocumentClient client,
            ILogger log)
        {
            log.LogInformation("Creating a new user");

            // Get request body
            AuthorisationCode authorisationCode = await req.Content.ReadAsAsync<AuthorisationCode>();

            if (authorisationCode == null)
            {
                new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Please pass an Authorisation Code in the request body", Encoding.UTF8, "application/json")
                };
            }

            bimsync.AccessToken accessToken = await bimsync.bimsyncServices.GetAccessToken(authorisationCode.AuthorizationCode);

            if (accessToken == null)
            {
                new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Could get an authorisation code from bimsync", Encoding.UTF8, "application/json")
                };
            }

            bimsync.User bimsyncUser = await bimsync.bimsyncServices.GetCurrentUser(accessToken);

            User user = new User
            {
                id = bimsyncUser.id,
                Name = bimsyncUser.name,
                PowerBISecret = System.Guid.NewGuid().ToString(),
                AccessToken = accessToken,
                RefreshDate = System.DateTime.Now + new System.TimeSpan(0, 0, accessToken.expires_in),
                BCFToken = ""
            };

            await usersOut.AddAsync(user);

            // Uri collectionUri = UriFactory.CreateDocumentCollectionUri("mydb", "mycollection");

            // IDocumentQuery<User> query = client.CreateDocumentQuery<User>(collectionUri)
            //     .Where(u => u.Age >= minAge)
            //     .AsDocumentQuery();

            //BCFToken bcfAccessToken = ObtainBCFToken(codeBCF).Result;

            //var user = _context.Users.FirstOrDefault(t => t.bimsync_id == bimsyncUser.id);
            // User user = null;

            // if (user == null)
            // {

            // }
            // else
            // {
            //     user.AccessToken = accessToken;
            //     user.RefreshDate = System.DateTime.Now + new System.TimeSpan(0, 0, accessToken.expires_in - 60);
            // }

            string jsonContent = JsonConvert.SerializeObject(user);

            return user == null
                ? new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("The user has not been created", Encoding.UTF8, "application/json") }
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(jsonContent, Encoding.UTF8, "application/json") };
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
                ? new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("Please pass a name on the query string or in the request body", Encoding.UTF8, "application/json") }
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(name, Encoding.UTF8, "application/json") };
        }
    }
}
