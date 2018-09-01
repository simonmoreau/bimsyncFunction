
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
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
            [CosmosDB(
                databaseName: "bimsyncManagerdb",
                collectionName: "bimsyncManagerCollection",
                ConnectionStringSetting = "myDBConnectionString")]IAsyncCollector<User> usersOut,
            [CosmosDB(
                databaseName: "bimsyncManagerdb",
                collectionName: "bimsyncManagerCollection",
                ConnectionStringSetting = "myDBConnectionString")]DocumentClient client,
            ILogger log)
        {
            log.LogInformation("Creating a new user");

            // Get request body
            AuthorisationCode authorisationCode = await req.Content.ReadAsAsync<AuthorisationCode>();

            if (authorisationCode == null)
            {
                new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(JsonConvert.SerializeObject("Please pass an Authorisation Code and a redirect URI in the request body"), Encoding.UTF8, "application/json")
                };
            }

            bimsync.AccessToken accessToken = await bimsync.bimsyncServices.GetAccessToken(authorisationCode);

            if (accessToken == null)
            {
                new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(JsonConvert.SerializeObject("Could not get an authorisation code from bimsync"), Encoding.UTF8, "application/json")
                };
            }

            bimsync.User bimsyncUser = await bimsync.bimsyncServices.GetCurrentUser(accessToken);

            Uri collectionUri = UriFactory.CreateDocumentCollectionUri("bimsyncManagerdb", "bimsyncManagerCollection");
            IEnumerable<User> existingUsers = client.CreateDocumentQuery<User>(collectionUri).Where(u => u.id == bimsyncUser.id).AsEnumerable();

            string secret = System.Guid.NewGuid().ToString();
            string bcfToken = "";
            if (existingUsers.Count() != 0)
            {
                secret = existingUsers.FirstOrDefault().PowerBISecret;
                bcfToken = existingUsers.FirstOrDefault().BCFToken;
            }


            User user = new User
            {
                id = bimsyncUser.id,
                Name = bimsyncUser.name,
                PowerBISecret = secret,
                AccessToken = accessToken,
                RefreshDate = System.DateTime.Now + new System.TimeSpan(0, 0, accessToken.expires_in),
                BCFToken = bcfToken
            };

            await usersOut.AddAsync(user);

            string jsonContent = JsonConvert.SerializeObject(user);

            return user == null
                ? new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("The user has not been created", Encoding.UTF8, "application/json") }
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(jsonContent, Encoding.UTF8, "application/json") };
        }

        [FunctionName("get_user")]
        public static async Task<HttpResponseMessage> Get(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "users/{secret}")]HttpRequestMessage req,
            [CosmosDB(
                databaseName: "bimsyncManagerdb",
                collectionName: "bimsyncManagerCollection",
                ConnectionStringSetting = "myDBConnectionString",
                SqlQuery = "select * from bimsyncManagerdb u where u.PowerBISecret = {secret}")]IEnumerable<User> users,
            [CosmosDB(
                databaseName: "bimsyncManagerdb",
                collectionName: "bimsyncManagerCollection",
                ConnectionStringSetting = "myDBConnectionString")]IAsyncCollector<User> usersOut,
            ILogger log)
        {
            log.LogInformation("Getting an existing user");

            if (users.Count() != 0)
            {
                User user = users.FirstOrDefault();
                //Refrech the token if necessary
                if (user.RefreshDate < DateTime.Now)
                {
                    bimsync.AccessToken accessToken = await bimsync.bimsyncServices.RefreshAccessToken(user.AccessToken.refresh_token);

                    if (accessToken == null)
                    {
                        new HttpResponseMessage(HttpStatusCode.BadRequest)
                        {
                            Content = new StringContent(JsonConvert.SerializeObject("Could not get an authorisation code from bimsync"), Encoding.UTF8, "application/json")
                        };
                    }

                    user.AccessToken = accessToken;
                    user.RefreshDate = System.DateTime.Now + new System.TimeSpan(0, 0, accessToken.expires_in);

                    await usersOut.AddAsync(user);
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                            JsonConvert.SerializeObject(user),
                            Encoding.UTF8,
                            "application/json"
                            )
                };
            }
            else
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(JsonConvert.SerializeObject("The user does not exist"), Encoding.UTF8, "application/json")
                };
            }
        }
    }
}
