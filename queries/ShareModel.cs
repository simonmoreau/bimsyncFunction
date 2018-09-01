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
    public static class ShareModel
    {
        [FunctionName("ShareModel")]
        public static async Task<HttpResponseMessage> Create(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "api/share")]HttpRequestMessage req,
            [CosmosDB(
                databaseName: "bimsyncManagerdb",
                collectionName: "bimsyncManagerCollection",
                ConnectionStringSetting = "myDBConnectionString",
                SqlQuery = "select * from bimsyncManagerdb u where u.PowerBISecret = {secret}")]IEnumerable<User> users,
            [CosmosDB(
                databaseName: "bimsyncManagerdb",
                collectionName: "bimsyncManagerCollection",
                ConnectionStringSetting = "myDBConnectionString")]IAsyncCollector<SharingCode> sharingCodesOut,
           ILogger log)
        {
            log.LogInformation("Creating a new shared model");

            // Get request body
            AuthorisationCode authorisationCode = await req.Content.ReadAsAsync<AuthorisationCode>();

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
